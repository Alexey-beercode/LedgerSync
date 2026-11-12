# LedgerSync reliability

This document shows **what the synchronization is protected against, by which mechanism, where it is in the code
and which test proves it**. The goal: no failure (of the source, the database, Google, the process or a parallel
run) leads to data loss, duplicates, a partially applied sync or a "silent" error that leaves no trace in the journal.

## Guarantees

1. **No duplicates** — a document with a given `ExternalId` exists in the database exactly once.
2. **Atomicity** — a run is applied entirely or not at all.
3. **Every run leaves a trace** — `sync_logs` has a record with the final status and the error reason.
4. **A Google failure does not affect data** — the documents are already committed and delivery can be repeated.
5. **One run at a time** — across all application instances.
6. **Bad data does not bring the service down** — a bad document is rejected with a reason, the rest are saved.

## Risk map

| # | Risk | Protection | Where in the code | Proven by |
|---|---|---|---|---|
| 1 | A repeated sync creates duplicates | Known documents are looked up by `ExternalId` before writing: update/skip instead of insert | `SyncDocumentsCommandHandler.ApplyDocumentsAsync` | `Handle_SameDataTwice_DoesNotCreateDuplicates`, `Sync_RunTwice_StoresEachDocumentOnce` |
| 2 | A duplicate caused by a code bug or a race | **Unique index** `ix_external_documents_external_id` in PostgreSQL; a violation → `PersistenceConflictException` → rollback | `ExternalDocumentConfiguration`, `UnitOfWork` | `UniqueIndex_RejectsSecondDocumentWithSameExternalId` (real PostgreSQL) |
| 3 | The same document several times in one response | Deduplication within the batch, the latest version is used | `SyncDocumentsCommandHandler.DeduplicateBatch` | `Handle_SameDocumentTwiceInBatch_StoresLatestVersionOnce` |
| 4 | Two runs at once (double click, several instances) | **PostgreSQL advisory lock** on a dedicated connection; the second run → `409`. If the process crashes, PostgreSQL releases the lock itself | `PostgresAdvisorySyncRunLock`, `SyncAlreadyRunningException` | `AdvisoryLock_IsExclusiveAcrossConnectionsAndReleasedOnDispose`, `Sync_WhileAnotherRunHoldsLock_Returns409`, `Handle_AnotherRunInProgress_...` |
| 5 | A partially applied sync on a database error | All document changes + counters + errors → **one `SaveChanges` = one transaction**. On an error the changes are discarded, the run is re-read and marked `Failed` | `SyncDocumentsCommandHandler.ExecuteRunAsync`, `FailAfterRollbackAsync` | `Handle_DatabaseSaveFails_RollsBackAllDocumentsAndRecordsFailedRun` |
| 6 | A run without a journal record or stuck in `Running` | The `Running` record is created **before** the source is called; any outcome, including an unexpected client error, finishes it as `Success` / `PartialSuccess` / `Failed` | `SyncDocumentsCommandHandler.HandleAsync`, `SyncLog` | `Handle_ExternalSourceFails_RecordsFailedRun`, `Handle_UnexpectedClientError_FinishesRunAsFailedInsteadOfLeavingItRunning`, `Sync_SourceBroken_ReturnsFailedRunAndPersistsIt` |
| 7 | The process crashed mid-run — the record stays `Running` forever | On the next run, under the lock, all `Running` records are marked `Failed` ("interrupted") | `SyncDocumentsCommandHandler.RecoverInterruptedRunsAsync` | `Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed` |
| 8 | The client dropped the HTTP request mid-run | Once the lock is acquired, the run is not tied to the request's `CancellationToken` and always completes | `SyncDocumentsCommandHandler.HandleAsync` (`token = CancellationToken.None`) | code review |
| 9 | The external source is unavailable, slow or flapping | Standard resilience pipeline (`AddStandardResilienceHandler`): per-attempt timeout, retry with exponential backoff + jitter, total timeout, circuit breaker. Once exhausted → `Failed` with a reason | `InfrastructureServiceCollectionExtensions.AddExternalSource`, `HttpExternalDocumentsClient` | `Fetch_ServerError_ThrowsUnavailable`, `Fetch_NetworkFailure_ThrowsUnavailable`, `Sync_SourceBroken_...(Unavailable)`; Polly retries are visible in the logs |
| 10 | The source returned something that is not JSON / not an array | `JsonException` → `ExternalSourceInvalidResponseException` → `Failed`, documents are not touched | `HttpExternalDocumentsClient.FetchDocumentsAsync` | `Fetch_NotAJsonArray_ThrowsInvalidResponse`, `Sync_SourceBroken_...(Malformed)` |
| 11 | One broken document breaks the whole batch | Each item is parsed separately; an unreadable one → rejected with a reason (`field 'amount' has an invalid value`), the rest go on | `HttpExternalDocumentsClient.ParseDocuments` | `Fetch_OneUnreadableItem_ReturnsItAsUnreadableAndKeepsOthers`, `Sync_MixedBatch_SavesValidDocumentsAndRecordsRejections` |
| 12 | Invalid data reaches the database | FluentValidation before the domain: required fields, known type/status, amount ≥ 0 with 2 decimal places, ISO currency. Plus domain invariants and `CHECK amount >= 0` in the database | `ExternalDocumentPayloadValidator`, `Guard`, `ExternalDocumentConfiguration` | `ExternalDocumentPayloadValidatorTests`, `Handle_InvalidItems_AreRejectedWhileValidOnesAreSaved` |
| 13 | An old version of a document overwrites a newer one (out-of-order delivery) | Update only if the incoming `ExternalUpdatedAt` is not older than the stored one | `ExternalDocument.IsNewerThan` | `Handle_OlderVersionOfKnownDocument_IsIgnored`, mock scenario `Stale` |
| 14 | A date in the future blocks updates of the document forever | The validator rejects an `externalUpdatedAt` later than "now + 5 minutes" | `ExternalDocumentPayloadValidator.MaxFutureSkew` | `Validate_InvalidField_IsReportedForThatField` |
| 15 | Different time zones → false updates or a `timestamptz` error | All dates are normalized to UTC in the domain | `Guard.Utc` | `Create_NormalizesCurrencyAndTimestampToUtc`, `HasChanges_SameValuesInOtherOffset_IsFalse` |
| 16 | A document for a non-existent order | Rejected with a reason in `sync_log_errors`, status `PartialSuccess`; FK `external_documents → orders` | `SyncDocumentsCommandHandler`, `ExternalDocumentConfiguration` | `Handle_UnknownOrder_RejectsDocumentAndKeepsOthers` |
| 17 | Google is unavailable → documents rolled back or a 500 | Sending happens only **after** the commit; any logger exception is caught and the result is written to `google_sheet_status` | `GoogleSheetReporter` | `Handle_GoogleSheetsUnavailable_...`, `GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent` |
| 18 | Resending to Google creates a duplicate row | Upsert by `RunId`: the run's row is updated, not appended | `FileGoogleSheetLogger` | `Send_SameRunTwice_KeepsOneUpdatedRow`, `Sync_SuccessfulRun_IsWrittenToMockSheetOnce` |
| 19 | A Google journal row is lost | The `Failed` status and the error text are stored in the database; idempotent resending of the latest **or any** run by `runId` | `SyncLog.MarkGoogleSheetFailed`, `ResendSyncLogCommandHandler` | `ResendLatest_AfterGoogleOutage_DeliversSameRunOnce`, `ResendByRunId_OlderUndeliveredRun_IsDelivered...` |
| 20 | Run counters do not add up | Domain invariant `Created + Updated + Skipped == Received`, otherwise an exception | `SyncLog.Complete` | `Complete_CountersDoNotAddUp_Throws`, `Handle_AnyOutcome_CountersAddUpToReceived` |
| 21 | A finished run is modified | `SyncRunAlreadyFinishedException` | `SyncLog.EnsureRunning` | `FinishedRun_CannotBeChanged` |
| 22 | A wrong configuration is discovered only at runtime | Options + DataAnnotations + `ValidateOnStart`: the application refuses to start | `InfrastructureServiceCollectionExtensions`, `*Options` | `Production_WithoutExplicitConfiguration_RefusesToStart`, `ExternalApiOptionsTests` |
| 23 | The database is not up yet when the container starts | PostgreSQL healthcheck in compose: the API starts only after the database is ready | `DatabaseInitializer`, `docker-compose.yml` | running `docker compose up` |
| 24 | Internal details leak to the client | Global `IExceptionHandler`: known errors → 400/404/409, everything else → 500 without details but with a `traceId` | `GlobalExceptionHandler` | `Orders_UnknownOrder_Returns404ProblemDetails`, `Logs_InvalidTake_Returns400` |
| 25 | A very long error text breaks saving the run | Truncated to the column size in the domain | `SyncLog.Truncate`, `SyncLogError.Create` | `Fail_LongMessage_IsTruncatedToColumnSize` |
| 26 | A failure cannot be investigated | Structured events with EventIds, a `RunId` scope on all records of a run, JSON logs in Docker, a `traceId` in error responses | `SyncLogMessages`, `SyncDocumentsCommandHandler` | `docker compose logs api` |
| 27 | The amount was updated but the currency stayed old | Amount and currency are compared and updated together | `ExternalDocument.HasChanges`, `ApplyExternalChanges` | `Handle_CurrencyChanged_UpdatesAmountTogetherWithCurrency`, `ApplyExternalChanges_UpdatesAmountAndCurrencyTogether` |
| 28 | A known document "moved" to another order or changed its type unnoticed | Such a version is rejected with a reason in `sync_log_errors`, stored data is not changed | `ExternalDocument.HasSameIdentity`, `SyncDocumentsCommandHandler` | `Handle_KnownDocumentMovedToAnotherOrder_IsRejectedAndNotChanged` |
| 29 | The external system mock is reachable in production and can break the sync | Mock routes are registered only with `MockExternalApi:Enabled=true` | `MockExternalApiEndpoints`, `WebApplicationExtensions` | `MockExternalApi_Disabled_IsNotReachable` |
| 30 | Demo data reaches the production database | Demo orders are not part of the migrations; idempotent seeding in `DatabaseInitializer` only with `Persistence:SeedDemoData=true` | `DatabaseInitializer` | `DatabaseInitializer_RunTwice_InsertsDemoOrdersOnce` |
| 31 | Production silently talks to the external system mock because of default values | The external system address is required and has no default | `appsettings.json`, `ExternalApiOptions` | `Production_WithoutExplicitConfiguration_RefusesToStart`, `ExternalApiOptionsTests` |

## How to see it live

A full manual check on a clean database. The addresses are for Docker (`docker compose up -d --build`);
for a local run the port is `5080`. In Windows PowerShell use `curl.exe` instead of `curl`. The same requests are
available in Scalar UI (`/scalar`) and in `src/LedgerSync.Api/LedgerSync.Api.http`.

A sync is always started with the same command:

```bash
curl -X POST http://localhost:8080/api/sync/documents
```

The external system scenario is switched like this (replace `Updated` with the scenario you need):

```bash
curl -X PUT http://localhost:8080/mock/external/scenario/Updated
```

| Step | Action | Expected result |
|---|---|---|
| 1 | Sync (the `Baseline` scenario is active from the start) | `Success`, `documentsCreated: 4` |
| 2 | Sync again | `documentsCreated: 0`, `documentsSkipped: 4` — no duplicates |
| 3 | Scenario `Updated`, sync | `documentsCreated: 1`, `documentsUpdated: 2`, `documentsSkipped: 2` |
| 4 | Scenario `Mixed`, sync | `PartialSuccess`, `documentsSkipped: 6`; stored documents did not change |
| 5 | Scenario `Stale`, sync | `documentsSkipped: 1`: the old version did not overwrite the new one |
| 6 | Scenario `Malformed`, sync | `Failed`, `errorMessage: External source returned malformed JSON.` |
| 7 | Scenario `Unavailable`, sync | `Failed` with the reason `HTTP 503` after retries (about 2 seconds) |

The rejection reasons from step 4 (`EXT-BAD-001`, `EXT-BAD-002`, `EXT-INV-404`) are visible in the history,
in the `errors` field:

```bash
curl "http://localhost:8080/api/sync/logs?take=10"
```

The order's documents and the response for a non-existent order (`404` as RFC 7807):

```bash
curl http://localhost:8080/api/orders/ORD-001/documents
```

```bash
curl http://localhost:8080/api/orders/ORD-404/documents
```

The Google Sheets journal: one row per run, including unsuccessful ones.

```bash
docker compose exec api cat google-sheets-mock/sync-log.csv
```

Resending the latest run: status `Sent`, the number of rows in the journal does not change.

```bash
curl -X POST http://localhost:8080/api/sync/logs/latest/send-to-google
```

Database state: no duplicates, no stuck runs.

```bash
docker compose exec postgres psql -U postgres -d ledgersync -c "select external_id, order_number, amount, currency, status from external_documents order by 1"
```

```bash
docker compose exec postgres psql -U postgres -d ledgersync -c "select status, documents_received, documents_created, documents_updated, documents_skipped, google_sheet_status, error_message from sync_logs order by started_at"
```

Structured run logs (JSON, every run record has a `RunId`):

```bash
docker compose logs api
```

A Google failure, parallel runs, the rollback on a database error and recovery after a process crash are
inconvenient to reproduce by hand; they are covered by automated tests (`Handle_GoogleSheetsUnavailable_...`,
`GoogleSheetsDown_DocumentsStaySavedAndRunCanBeResent`, `Sync_WhileAnotherRunHoldsLock_Returns409`,
`Handle_DatabaseSaveFails_...`, `Handle_RunLeftRunningAfterCrash_IsMarkedAsFailed`).

## Structured events

| EventId | Level | Event |
|---|---|---|
| 1000 | Information | Run started |
| 1001 | Information | Run finished: status, received / created / updated / skipped, duration |
| 1002 | Warning | Run rejected: another one is in progress |
| 1003 | Warning | A stuck run was marked `Failed` |
| 1100 | Error | External source error |
| 1101 | Warning | Document rejected (reason) |
| 1102 | Warning | Outdated document version ignored |
| 1103 | Warning | Duplicate within one response |
| 1200 | Error | Processing/saving error, rollback |
| 1300 | Information | Row sent to Google Sheets |
| 1301 | Error | Error sending to Google Sheets |
| 1302 | Error | Failed to save the delivery status |
