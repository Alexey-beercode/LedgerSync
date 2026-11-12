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
| 6 | Запуск без записи в журнале или зависший в `Running` | Запись `Running` создаётся **до** обращения к источнику; любой исход, включая неожиданную ошибку клиента, завершает её как `Success` / `PartialSuccess` / `Failed` | `SyncDocumentsCommandHandler.HandleAsync`, `SyncLog` | `Handle_ExternalSourceFails_RecordsFailedRun`, `Handle_UnexpectedClientError_FinishesRunAsFailedInsteadOfLeavingItRunning`, `Sync_SourceBroken_ReturnsFailedRunAndPersistsIt` |
| 7 | Процесс упал посреди запуска — запись навсегда `Running` | При следующем запуске, под lock, все `Running` помечаются `Failed` («interrupted») | `SyncDocumentsCommandHandler.RecoverInterruptedRunsAsync` | `Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed` |
| 8 | Клиент оборвал HTTP-запрос посреди запуска | После захвата lock запуск не привязан к `CancellationToken` запроса и всегда доводится до конца | `SyncDocumentsCommandHandler.HandleAsync` (`token = CancellationToken.None`) | ревью кода |
| 9 | Внешний источник недоступен, медленный, флапает | Стандартный resilience pipeline (`AddStandardResilienceHandler`): таймаут попытки, retry с exponential backoff + jitter, общий таймаут, circuit breaker. После исчерпания → `Failed` с причиной | `InfrastructureServiceCollectionExtensions.AddExternalSource`, `HttpExternalDocumentsClient` | `Fetch_ServerError_ThrowsUnavailable`, `Fetch_NetworkFailure_ThrowsUnavailable`, `Sync_SourceBroken_...(Unavailable)`; в логах видны повторы Polly |
| 10 | Источник вернул не JSON / не массив | `JsonException` → `ExternalSourceInvalidResponseException` → `Failed`, документы не трогаются | `HttpExternalDocumentsClient.FetchDocumentsAsync` | `Fetch_NotAJsonArray_ThrowsInvalidResponse`, `Sync_SourceBroken_...(Malformed)` |
| 11 | Один битый документ ломает всю пачку | Каждый элемент парсится отдельно; нечитаемый → отклонён с причиной (`field 'amount' has an invalid value`), остальные идут дальше | `HttpExternalDocumentsClient.ParseDocuments` | `Fetch_OneUnreadableItem_ReturnsItAsUnreadableAndKeepsOthers`, `Sync_MixedBatch_SavesValidDocumentsAndRecordsRejections` |
| 12 | Невалидные данные попадают в базу | FluentValidation до домена: обязательные поля, известные тип/статус, сумма ≥ 0 и 2 знака, ISO-валюта. Плюс инварианты в домене и `CHECK amount >= 0` в БД | `ExternalDocumentPayloadValidator`, `Guard`, `ExternalDocumentConfiguration` | `ExternalDocumentPayloadValidatorTests`, `Handle_InvalidItems_AreRejectedWhileValidOnesAreSaved` |
| 13 | Старая версия документа перезаписывает новую (доставка не по порядку) | Обновление только если входящий `ExternalUpdatedAt` не старше сохранённого | `ExternalDocument.IsNewerThan` | `Handle_OlderVersionOfKnownDocument_IsIgnored`, сценарий mock `Stale` |
| 14 | Дата из будущего навсегда блокирует обновления документа | Валидатор отклоняет `externalUpdatedAt` позже «сейчас + 5 минут» | `ExternalDocumentPayloadValidator.MaxFutureSkew` | `Validate_InvalidField_IsReportedForThatField` |
| 15 | Разные часовые пояса → ложные обновления или ошибка `timestamptz` | Все даты нормализуются в UTC в домене | `Guard.Utc` | `Create_NormalizesCurrencyAndTimestampToUtc`, `HasChanges_SameValuesInOtherOffset_IsFalse` |
| 16 | Документ по несуществующему заказу | Отклоняется с причиной в `sync_log_errors`, статус `PartialSuccess`; FK `external_documents → orders` | `SyncDocumentsCommandHandler`, `ExternalDocumentConfiguration` | `Handle_UnknownOrder_RejectsDocumentAndKeepsOthers` |
| 17 | Google недоступен → откат документов или 500 | Отправка только **после** коммита; любое исключение логгера перехватывается, результат пишется в `google_sheet_status` | `GoogleSheetReporter` | `Handle_GoogleSheetsUnavailable_...`, `GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent` |
| 18 | Повтор отправки в Google создаёт дубль строки | Upsert по `RunId`: строка запуска обновляется, а не добавляется | `FileGoogleSheetLogger` | `Send_SameRunTwice_KeepsOneUpdatedRow`, `Sync_SuccessfulRun_IsWrittenToMockSheetOnce` |
| 19 | Потеря строки журнала Google | Статус `Failed` + текст ошибки хранятся в БД; идемпотентная повторная отправка последнего **или любого** запуска по `runId` | `SyncLog.MarkGoogleSheetFailed`, `ResendSyncLogCommandHandler` | `ResendLatest_AfterGoogleOutage_DeliversSameRunOnce`, `ResendByRunId_OlderUndeliveredRun_IsDelivered...` |
| 20 | Счётчики запуска не сходятся | Доменный инвариант `Created + Updated + Skipped == Received`, иначе исключение | `SyncLog.Complete` | `Complete_CountersDoNotAddUp_Throws`, `Handle_AnyOutcome_CountersAddUpToReceived` |
| 21 | Изменение уже завершённого запуска | `SyncRunAlreadyFinishedException` | `SyncLog.EnsureRunning` | `FinishedRun_CannotBeChanged` |
| 22 | Неверная конфигурация обнаруживается только в рантайме | Options + DataAnnotations + `ValidateOnStart`: приложение не стартует | `InfrastructureServiceCollectionExtensions`, `*Options` | `Production_WithoutExplicitConfiguration_RefusesToStart`, `ExternalApiOptionsTests` |
| 23 | База ещё не поднялась при старте контейнера | Healthcheck PostgreSQL в compose: API стартует только после готовности базы | `DatabaseInitializer`, `docker-compose.yml` | запуск `docker compose up` |
| 24 | Внутренние детали утекают клиенту | Глобальный `IExceptionHandler`: известные ошибки → 400/404/409, остальные → 500 без деталей, но с `traceId` | `GlobalExceptionHandler` | `Orders_UnknownOrder_Returns404ProblemDetails`, `Logs_InvalidTake_Returns400` |
| 25 | Слишком длинный текст ошибки ломает сохранение запуска | Обрезка до размера колонки в домене | `SyncLog.Truncate`, `SyncLogError.Create` | `Fail_LongMessage_IsTruncatedToColumnSize` |
| 26 | Сбой невозможно расследовать | Структурированные события с EventId, scope `RunId` во всех записях запуска, JSON-логи в Docker, `traceId` в ответах об ошибках | `SyncLogMessages`, `SyncDocumentsCommandHandler` | `docker compose logs api` |
| 27 | Сумма обновилась, а валюта осталась старой | Сумма и валюта сравниваются и обновляются вместе | `ExternalDocument.HasChanges`, `ApplyExternalChanges` | `Handle_CurrencyChanged_UpdatesAmountTogetherWithCurrency`, `ApplyExternalChanges_UpdatesAmountAndCurrencyTogether` |
| 28 | Известный документ «переехал» в другой заказ или сменил тип, и это прошло незаметно | Такая версия отклоняется с причиной в `sync_log_errors`, сохранённые данные не меняются | `ExternalDocument.HasSameIdentity`, `SyncDocumentsCommandHandler` | `Handle_KnownDocumentMovedToAnotherOrder_IsRejectedAndNotChanged` |
| 29 | Mock внешней системы доступен в production и позволяет сломать синхронизацию | Mock-маршруты регистрируются только при `MockExternalApi:Enabled=true` | `MockExternalApiEndpoints`, `WebApplicationExtensions` | `MockExternalApi_Disabled_IsNotReachable` |
| 30 | Демо-данные попадают в боевую базу | Демо-заказы не входят в миграции; идемпотентное добавление в `DatabaseInitializer` только при `Persistence:SeedDemoData=true` | `DatabaseInitializer` | `DatabaseInitializer_RunTwice_InsertsDemoOrdersOnce` |
| 31 | Production тихо ходит в mock внешней системы из-за значений по умолчанию | Адрес внешней системы обязателен, значения по умолчанию нет | `appsettings.json`, `ExternalApiOptions` | `Production_WithoutExplicitConfiguration_RefusesToStart`, `ExternalApiOptionsTests` |

## Как увидеть это вживую

Полный сценарий ручной проверки на чистой базе. Адреса даны для Docker (`docker compose up -d --build`);
при локальном запуске порт `5080`. В Windows PowerShell вместо `curl` нужно писать `curl.exe`. Те же запросы
есть в Scalar UI (`/scalar`) и в файле `src/LedgerSync.Api/LedgerSync.Api.http`.

Синхронизация запускается одной и той же командой:

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

Сценарий внешней системы переключается так (вместо `Updated` — нужный сценарий):

```bash
curl -X PUT http://localhost:8080/mock/external/scenario/Updated
```

| Шаг | Действие | Ожидаемый результат |
|---|---|---|
| 1 | Синхронизация (сценарий `Baseline` активен с запуска) | `Success`, `documentsCreated: 4` |
| 2 | Синхронизация ещё раз | `documentsCreated: 0`, `documentsSkipped: 4` — дублей нет |
| 3 | Сценарий `Updated`, синхронизация | `documentsCreated: 1`, `documentsUpdated: 2`, `documentsSkipped: 2` |
| 4 | Сценарий `Mixed`, синхронизация | `PartialSuccess`, `documentsSkipped: 6`; сохранённые документы не изменились |
| 5 | Сценарий `Stale`, синхронизация | `documentsSkipped: 1`: старая версия не затёрла новую |
| 6 | Сценарий `Malformed`, синхронизация | `Failed`, `errorMessage: External source returned malformed JSON.` |
| 7 | Сценарий `Unavailable`, синхронизация | `Failed` с причиной `HTTP 503` после повторов (около 2 секунд) |

Причины отклонений из шага 4 (`EXT-BAD-001`, `EXT-BAD-002`, `EXT-INV-404`) видны в истории, поле `errors`:

```bash
curl "http://localhost:8080/api/sync/logs?take=10"
```

Документы заказа и ответ на несуществующий заказ (`404` в формате RFC 7807):

```bash
curl http://localhost:8080/api/orders/ORD-001/documents
```

```bash
curl http://localhost:8080/api/orders/ORD-404/documents
```

Журнал Google Sheets: по одной строке на каждый запуск, включая неуспешные.

```bash
docker compose exec api cat google-sheets-mock/sync-log.csv
```

Повторная отправка последнего запуска: статус `Sent`, число строк в журнале не меняется.

```bash
curl -X POST http://localhost:8080/api/sync/logs/latest/send-to-google
```

Состояние базы: дублей нет, зависших запусков нет.

```bash
docker exec ledgersync-postgres psql -U postgres -d ledgersync -c "select external_id, order_number, amount, currency, status from external_documents order by 1"
```

```bash
docker exec ledgersync-postgres psql -U postgres -d ledgersync -c "select status, documents_received, documents_created, documents_updated, documents_skipped, google_sheet_status, error_message from sync_logs order by started_at"
```

Структурированные логи запусков (JSON, в каждой записи запуска есть `RunId`):

```bash
docker compose logs api
```

Отказ Google, параллельные запуски, откат при ошибке базы и восстановление после падения процесса вручную
воспроизводить неудобно; они проверяются автотестами (`Handle_GoogleSheetsUnavailable_...`,
`GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent`, `Sync_WhileAnotherRunHoldsLock_Returns409`,
`Handle_DatabaseSaveFails_...`, `Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed`).

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
