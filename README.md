# LearningPortal

Browser workflow setup and the full manual acceptance checklist are documented
in [docs/END_TO_END_TESTING.md](docs/END_TO_END_TESTING.md).

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

## Local authentication and development data

Interactive browser authentication is available at `/login` and `/register`.
The Blazor host exchanges credentials with the existing JWT API, stores tokens
inside an encrypted, secure, HttpOnly authentication cookie, rotates expiring
access tokens through the existing refresh endpoint, and revokes the refresh
token during logout. Registration always creates a `Student`; public callers
cannot select or obtain privileged roles.

Development data is opt-in and is never seeded outside the Development
environment. Configure:

```json
"DevelopmentSeed": {
  "Enabled": true
},
"DatabaseInitialization": {
  "ApplyMigrations": false
}
```

Apply migrations explicitly before first use:

```powershell
dotnet ef database update --project src/LearningPortal.Infrastructure --startup-project src/LearningPortal.Api
```

`DatabaseInitialization:ApplyMigrations` may be enabled locally to apply
migrations at Development startup. It is ignored outside Development.

> **Warning:** The following credentials are demonstration credentials for a
> local development database only. Never enable this seed or reuse these
> passwords in a deployed environment.

| Account | Email | Password |
| --- | --- | --- |
| Administrator | `admin@learningportal.local` | `Admin123!` |
| Instructor | `instructor@learningportal.local` | `Instructor123!` |
| Student | `student@learningportal.local` | `Student123!` |

The seed is idempotent and does not reset passwords for existing users. It also
creates one published “ASP.NET Core Fundamentals” course, four published
lessons, a required three-question quiz, an instructor skill qualification, and
an active student enrollment.

Suggested manual flow:

1. Apply migrations and start the API and Blazor hosts.
2. Sign in as Administrator and verify users, courses, lessons, quizzes,
   instructor eligibility, and local AI health.
3. Sign out and sign in as Student; browse the catalog, open My Learning,
   complete lessons, take the required quiz, and use `/ai-tutor`.
4. Refresh the browser to verify session restoration, then sign out and confirm
   protected routes return to sign-in.
5. Register a new learner at `/register`, confirm it cannot access administrator
   routes, and verify its Profile page.

## Local Ollama AI Tutor

The AI Tutor uses only a locally configured Ollama HTTP service. It has no
OpenAI, Azure OpenAI, Gemini, Anthropic, or other external AI fallback.
Install Ollama separately, then prepare and start the default model:

```powershell
ollama pull llama3.2:3b
ollama list
ollama serve
```

The default API configuration is:

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2:3b",
  "RequestTimeoutSeconds": 120,
  "MaxContextCharacters": 30000,
  "MaxQuestionCharacters": 2000,
  "MaxConversationMessages": 20,
  "Temperature": 0.2,
  "Enabled": true
}
```

Verify connectivity with `http://localhost:11434/api/tags` or the
administrator-only AI Tutor health page. If the tutor reports that the model is
unavailable, compare `ollama list` with the configured model name. If the
service is unreachable, start `ollama serve` and confirm the configured URL.
Set `Ollama__Enabled=false` to disable the optional feature without affecting
the application health probes.

Only learner-visible published course material and bounded recent conversation
history are sent to the configured Ollama service. User emails, tokens,
connection strings, certificate codes, hidden prompts, other learners' data,
and unpublished content are excluded. Prompts and responses are not logged.
Prompt-injection defenses reduce risk but cannot eliminate it; the model has no
portal tools, URL retrieval, filesystem, shell, or code-execution access.

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
