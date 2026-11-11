# Надёжность LedgerSync

Документ показывает, **от чего защищена синхронизация, каким механизмом, где это в коде и каким тестом доказано**.
Цель: ни один сбой (источника, базы, Google, процесса, параллельного запуска) не приводит к потере данных,
дублям, частично применённой синхронизации или «молчаливой» ошибке без следа в журнале.

## Гарантии

1. **Нет дублей** — документ с одним `ExternalId` существует в базе ровно один раз.
2. **Атомарность** — запуск применяется целиком или не применяется вовсе.
3. **Каждый запуск оставляет след** — в `sync_logs` есть запись с итоговым статусом и причиной ошибки.
4. **Сбой Google не влияет на данные** — документы уже закоммичены, доставку можно повторить.
5. **Один запуск в момент времени** — на всех экземплярах приложения.
6. **Плохие данные не роняют сервис** — плохой документ отклоняется с причиной, остальные сохраняются.

## Карта рисков

| # | Риск | Механизм защиты | Где в коде | Чем проверено |
|---|---|---|---|---|
| 1 | Повторная синхронизация создаёт дубли | Поиск известных документов по `ExternalId` перед записью: update/skip вместо insert | `SyncDocumentsCommandHandler.ApplyDocumentsAsync` | `Handle_SameDataTwice_DoesNotCreateDuplicates`, `Sync_RunTwice_StoresEachDocumentOnce` |
| 2 | Дубль из-за ошибки в коде или гонки | **Unique index** `ix_external_documents_external_id` в PostgreSQL; нарушение → `PersistenceConflictException` → откат | `ExternalDocumentConfiguration`, `UnitOfWork` | `UniqueIndex_RejectsSecondDocumentWithSameExternalId` (реальный PostgreSQL) |
| 3 | Один документ несколько раз в одном ответе | Дедупликация внутри пачки, берётся самая свежая версия | `SyncDocumentsCommandHandler.DeduplicateBatch` | `Handle_SameDocumentTwiceInBatch_StoresLatestVersionOnce` |
| 4 | Два запуска одновременно (двойной клик, несколько инстансов) | **PostgreSQL advisory lock** на выделенном соединении; второй запуск → `409`. При падении процесса PostgreSQL снимает lock сам | `PostgresAdvisorySyncRunLock`, `SyncAlreadyRunningException` | `AdvisoryLock_IsExclusiveAcrossConnectionsAndReleasedOnDispose`, `Sync_WhileAnotherRunHoldsLock_Returns409`, `Handle_AnotherRunInProgress_...` |
| 5 | Частично применённая синхронизация при ошибке БД | Все изменения документов + счётчики + ошибки → **один `SaveChanges` = одна транзакция**. При ошибке изменения отбрасываются, запуск перечитывается и помечается `Failed` | `SyncDocumentsCommandHandler.ExecuteRunAsync`, `FailAfterRollbackAsync` | `Handle_DatabaseSaveFails_RollsBackAllDocumentsAndRecordsFailedRun` |
| 6 | Запуск без записи в журнале | Запись `Running` создаётся **до** обращения к источнику; любой исход завершает её как `Success` / `PartialSuccess` / `Failed` | `SyncDocumentsCommandHandler.HandleAsync`, `SyncLog` | `Handle_ExternalSourceFails_RecordsFailedRun`, `Sync_SourceBroken_ReturnsFailedRunAndPersistsIt` |
| 7 | Процесс упал посреди запуска — запись навсегда `Running` | При следующем запуске, под lock, все `Running` помечаются `Failed` («interrupted») | `SyncDocumentsCommandHandler.RecoverInterruptedRunsAsync` | `Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed` |
| 8 | Клиент оборвал HTTP-запрос посреди запуска | После захвата lock запуск не привязан к `CancellationToken` запроса и всегда доводится до конца | `SyncDocumentsCommandHandler.HandleAsync` (`token = CancellationToken.None`) | ревью кода |
| 9 | Внешний источник недоступен, медленный, флапает | Resilience pipeline: таймаут попытки, retry с exponential backoff + jitter, общий таймаут, circuit breaker. После исчерпания → `Failed` с причиной | `DependencyInjection.ConfigureResilience`, `HttpExternalDocumentsClient` | `Sync_SourceBroken_...(Unavailable)`; в логах видны повторы Polly |
| 10 | Источник вернул не JSON / не массив | `JsonException` → `ExternalSourceInvalidResponseException` → `Failed`, документы не трогаются | `HttpExternalDocumentsClient.FetchDocumentsAsync` | `Sync_SourceBroken_...(Malformed)` |
| 11 | Один битый документ ломает всю пачку | Каждый элемент парсится отдельно; нечитаемый → отклонён с причиной (`field 'amount' has an invalid value`), остальные идут дальше | `HttpExternalDocumentsClient.ParseDocuments` | `Sync_MixedBatch_SavesValidDocumentsAndRecordsRejections` |
| 12 | Невалидные данные попадают в базу | FluentValidation до домена: обязательные поля, известные тип/статус, сумма ≥ 0 и 2 знака, ISO-валюта. Плюс инварианты в домене и `CHECK amount >= 0` в БД | `ExternalDocumentPayloadValidator`, `Guard`, `ExternalDocumentConfiguration` | `ExternalDocumentPayloadValidatorTests`, `Handle_InvalidItems_AreRejectedWhileValidOnesAreSaved` |
| 13 | Старая версия документа перезаписывает новую (доставка не по порядку) | Обновление только если входящий `ExternalUpdatedAt` не старше сохранённого | `ExternalDocument.IsNewerThan` | `Handle_OlderVersionOfKnownDocument_IsIgnored`, сценарий mock `Stale` |
| 14 | Дата из будущего навсегда блокирует обновления документа | Валидатор отклоняет `externalUpdatedAt` позже «сейчас + 5 минут» | `ExternalDocumentPayloadValidator.MaxFutureSkew` | `Validate_InvalidField_IsReportedForThatField` |
| 15 | Разные часовые пояса → ложные обновления или ошибка `timestamptz` | Все даты нормализуются в UTC в домене | `Guard.Utc` | `Create_NormalizesCurrencyAndTimestampToUtc`, `HasChanges_SameValuesInOtherOffset_IsFalse` |
| 16 | Документ по несуществующему заказу | Отклоняется с причиной в `sync_log_errors`, статус `PartialSuccess`; FK `external_documents → orders` | `SyncDocumentsCommandHandler`, `ExternalDocumentConfiguration` | `Handle_UnknownOrder_RejectsDocumentAndKeepsOthers` |
| 17 | Google недоступен → откат документов или 500 | Отправка только **после** коммита; любое исключение логгера перехватывается, результат пишется в `google_sheet_status` | `GoogleSheetReporter` | `Handle_GoogleSheetsUnavailable_...`, `GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent` |
| 18 | Повтор отправки в Google создаёт дубль строки | Upsert по `RunId` в CSV-mock и в `Code.gs` (+ `LockService`) | `FileGoogleSheetLogger`, `google-apps-script/Code.gs` | `Sync_SuccessfulRun_IsWrittenToMockSheetOnce` |
| 19 | Потеря строки журнала Google | Статус `Failed` + текст ошибки хранятся в БД; ручной resend идемпотентен | `SyncLog.MarkGoogleSheetFailed`, `ResendLatestSyncLogCommandHandler` | `ResendLatest_AfterGoogleOutage_DeliversSameRunOnce` |
| 20 | Счётчики запуска не сходятся | Доменный инвариант `Created + Updated + Skipped == Received`, иначе исключение | `SyncLog.Complete` | `Complete_CountersDoNotAddUp_Throws`, `Handle_AnyOutcome_CountersAddUpToReceived` |
| 21 | Изменение уже завершённого запуска | `SyncRunAlreadyFinishedException` | `SyncLog.EnsureRunning` | `FinishedRun_CannotBeChanged` |
| 22 | Неверная конфигурация обнаруживается только в рантайме | Options + DataAnnotations + `ValidateOnStart`: приложение не стартует | `DependencyInjection`, `*Options` | ревью кода |
| 23 | База ещё не поднялась при старте контейнера | Миграции с повторами (10 попыток), healthcheck PostgreSQL в compose, `/health/ready` | `DatabaseMigrator`, `docker-compose.yml` | запуск `docker compose up` |
| 24 | Внутренние детали утекают клиенту | Глобальный `IExceptionHandler`: известные ошибки → 400/404/409, остальные → 500 без деталей, но с `traceId` | `GlobalExceptionHandler` | `Orders_UnknownOrder_Returns404ProblemDetails`, `Logs_InvalidTake_Returns400` |
| 25 | Огромный ответ источника съедает память | `MaxResponseContentBufferSize` (10 МБ по умолчанию) | `DependencyInjection.AddExternalSource` | ревью кода |
| 26 | Слишком длинный текст ошибки ломает сохранение запуска | Обрезка до размера колонки в домене | `SyncLog.Truncate`, `SyncLogError.Create` | `Fail_LongMessage_IsTruncatedToColumnSize` |
| 27 | Сбой невозможно расследовать | Структурированные события с EventId, scope `RunId` во всех записях запуска, JSON-логи в Docker, `traceId` в ответах об ошибках | `SyncLogMessages`, `SyncDocumentsCommandHandler` | `docker compose logs api` |

## Как увидеть это вживую

Сценарии mock-источника переключаются без перезапуска (адрес для Docker; локально — порт `5080`):

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

Второй запуск: `documentsCreated: 0`, `documentsSkipped: 4` — дублей нет.

```bash
curl -X PUT http://localhost:8080/mock/external/scenario/Mixed
```

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

`PartialSuccess`: битые документы и `ORD-404` отклонены, причины видны в `GET /api/sync/logs`.

```bash
curl -X PUT http://localhost:8080/mock/external/scenario/Unavailable
```

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

`Failed` с причиной `HTTP 503` после повторов. Документы не тронуты, запуск записан в журнал и в Google Sheets.

Отказ Google: запустить с недоступным адресом Apps Script (`GOOGLE_SHEETS_MODE=AppsScript`,
`GOOGLE_SHEETS_APPS_SCRIPT_URL=http://localhost:9/`). Синхронизация успешна, документы сохранены,
`googleSheetStatus: Failed`. После возврата в `File` вызов `POST /api/sync/logs/latest/send-to-google` даёт `Sent`,
и строка в таблице одна.

## Структурированные события

| EventId | Уровень | Событие |
|---|---|---|
| 1000 | Information | Старт запуска |
| 1001 | Information | Завершение: статус, received / created / updated / skipped, длительность |
| 1002 | Warning | Запуск отклонён: уже идёт другой |
| 1003 | Warning | Зависший запуск помечен `Failed` |
| 1100 | Error | Ошибка внешнего источника |
| 1101 | Warning | Документ отклонён (причина) |
| 1102 | Warning | Устаревшая версия документа проигнорирована |
| 1103 | Warning | Дубль внутри одного ответа |
| 1200 | Error | Ошибка обработки/сохранения, откат |
| 1300 | Information | Строка отправлена в Google Sheets |
| 1301 | Error | Ошибка отправки в Google Sheets |
| 1302 | Error | Не удалось сохранить статус доставки |
