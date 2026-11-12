# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first to cache dependencies between builds.
COPY LedgerSync.slnx ./
COPY src/LedgerSync.Domain/LedgerSync.Domain.csproj src/LedgerSync.Domain/
COPY src/LedgerSync.Application/LedgerSync.Application.csproj src/LedgerSync.Application/
COPY src/LedgerSync.Infrastructure/LedgerSync.Infrastructure.csproj src/LedgerSync.Infrastructure/
COPY src/LedgerSync.Api/LedgerSync.Api.csproj src/LedgerSync.Api/
RUN dotnet restore src/LedgerSync.Api/LedgerSync.Api.csproj

COPY src/ src/
RUN dotnet publish src/LedgerSync.Api/LedgerSync.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Folder for the mock Google Sheet, writable by the non-root app user.
RUN mkdir -p /app/google-sheets-mock && chown "$APP_UID" /app/google-sheets-mock

COPY --from=build /app/publish .

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LedgerSync.Api.dll"]
