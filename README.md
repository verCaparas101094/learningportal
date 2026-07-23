# LearningPortal

Enterprise Learning Portal foundation built on .NET 10, ASP.NET Core Minimal APIs, Blazor Web App, EF Core, SQL Server, and ASP.NET Identity.

## Architecture

Dependencies point inward:

```text
LearningPortal.Api --------> Application --------> Domain
       |                         |
       +----> Infrastructure ----+----> Shared

LearningPortal.Blazor ----------------> Shared
```

- `Domain` owns entities, invariants, and persistence contracts.
- `Application` owns CQRS use cases, validation, and infrastructure-neutral interfaces.
- `Infrastructure` implements persistence, Identity, JWT issuance, SQL Server, and health checks.
- `Api` is the Minimal API composition root and HTTP presentation layer.
- `Blazor` is the interactive server-rendered web host and typed API consumer.
- `Shared` contains wire contracts and the transport-neutral Result pattern.

The full file-by-file placement rationale is in [docs/FILE_CATALOG.md](docs/FILE_CATALOG.md).

## Local setup

1. Configure a SQL Server connection string if LocalDB is unavailable:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LearningPortal;User Id=sa;Password=<password>;Encrypt=True;TrustServerCertificate=True" --project src/LearningPortal.Api
   ```

2. Replace the development JWT key with a private value:

   ```powershell
   dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-bytes>" --project src/LearningPortal.Api
   ```

3. Apply the checked-in database migrations:

   ```powershell
   dotnet ef database update --project src/LearningPortal.Infrastructure --startup-project src/LearningPortal.Api
   ```

4. Run the API and Blazor hosts in separate terminals:

   ```powershell
   dotnet run --project src/LearningPortal.Api --launch-profile https
   dotnet run --project src/LearningPortal.Blazor --launch-profile https
   ```

Swagger is available at `https://localhost:7081/swagger`, the Blazor host at `https://localhost:7080`, and API health probes at `/health/live` and `/health/ready`.

## Production configuration

Set `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, and `Cors__AllowedOrigins__0` through the deployment platform's secret/configuration provider. The base configuration deliberately contains no production JWT secret. Apply reviewed EF Core migrations during deployment rather than automatically mutating the database at application startup.
