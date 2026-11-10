# LedgerSync

Order Management service that synchronizes financial documents (invoices, acts of work) from an external system
into the internal Order Management database, protects against duplicates, keeps a sync history
and reports every sync run to Google Sheets.

> Work in progress. Full documentation will be added once the implementation is complete.

## Stack

- .NET 10, ASP.NET Core Web API
- PostgreSQL + Entity Framework Core
- xUnit

## Solution structure (Clean Architecture)

| Project | Responsibility |
|---|---|
| `Domain` | Entities, enums and business rules. No external dependencies. |
| `Application` | Use cases, DTOs and abstractions (ports) for external services and persistence. |
| `Infrastructure` | EF Core / PostgreSQL, external API client, Google Sheets logger. |
| `Api` | HTTP endpoints, DI composition root, configuration. |
| `tests` | Automated tests. |

## Branching

- `main` — stable releases.
- `develop` — integration branch.
- `feature/*` — feature branches merged into `develop`.
