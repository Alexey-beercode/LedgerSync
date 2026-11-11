using LedgerSync.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLedgerSyncServices();

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.UseLedgerSyncPipeline();

await app.RunAsync();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
