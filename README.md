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

## Authorization architecture

Authorization defaults to authenticated access. Login, refresh, revoke, liveness, and readiness explicitly remain anonymous.

| Role | Intended capabilities |
| --- | --- |
| `Administrator` | Full portal access and access to every role policy |
| `Instructor` | Create courses, manage lessons, and view students |
| `Student` | Enroll, learn, and take quizzes |

Application code uses `ApplicationRoles`, `ApplicationClaimTypes`, and `Policies` instead of magic strings. The registered policies are `AdminOnly`, `InstructorOnly`, `StudentOnly`, and `AdminOrInstructor`; administrators satisfy all four policies.

Minimal API endpoints can use `RequireAdministrator()`, `RequireInstructor()`, `RequireStudent()`, or `RequireAdminOrInstructor()`. The current-user abstraction exposes the authenticated identifier, display name, email, roles, and claim/role helpers without exposing `HttpContext` to Application.

The `permission` claim name is reserved so permission requirements can be added later without changing token contracts. This foundation intentionally does not include permission tables, dynamic policy providers, role-management UI, or database-backed permission evaluation.

At startup, only missing `Administrator`, `Instructor`, and `Student` Identity roles are created. Role seeding is idempotent and never creates users.

## Tests

Run the fast authentication and authorization tests without Docker:

```powershell
dotnet test tests/LearningPortal.Infrastructure.Tests/LearningPortal.Infrastructure.Tests.csproj --configuration Release
```

The relational authentication suite uses an isolated SQL Server Testcontainer and is opt-in:

```powershell
$env:LEARNINGPORTAL_RUN_SQL_INTEGRATION_TESTS = "true"
dotnet test tests/LearningPortal.Infrastructure.IntegrationTests/LearningPortal.Infrastructure.IntegrationTests.csproj --configuration Release
```

The integration fixture applies migrations only to its disposable container database. It never uses or migrates the configured application database.

## Production configuration

Set `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, and `Cors__AllowedOrigins__0` through the deployment platform's secret/configuration provider. The base configuration deliberately contains no production JWT secret. Apply reviewed EF Core migrations during deployment rather than automatically mutating the database at application startup.
