# File placement and purpose

This catalog explains why every source/configuration file in the generated solution exists and why it is placed where it is.

## Solution root

- `.gitignore` — excludes compiled output, IDE state, test artifacts, and local secret files from source control.
- `Directory.Build.props` — enforces .NET 10 compiler quality settings for every project: nullable analysis, XML docs, deterministic output, implicit usings, and warnings as errors.
- `Directory.Packages.props` — centrally pins package versions so all projects consume compatible EF Core/ASP.NET Core dependencies.
- `global.json` — selects the supported .NET 10 SDK feature band for repeatable local and CI builds.
- `LearningPortal.sln` — groups all six projects for restore, build, and IDE navigation.
- `README.md` — documents architecture, secure configuration, migration, and startup procedures.
- `docs/FILE_CATALOG.md` — provides the requested file-by-file placement rationale.

## LearningPortal.Domain

This project is the dependency-free business core.

- `LearningPortal.Domain.csproj` — declares the Domain assembly without framework or infrastructure dependencies.
- `Common/Entity.cs` — provides GUID identity and an encapsulated in-memory domain-event collection for persisted entities.
- `Common/Events/IDomainEvent.cs` — defines immutable identity and UTC occurrence metadata required by every domain event.
- `Common/Events/DomainEvent.cs` — generates UUIDv7 event identifiers and UTC timestamps for derived domain facts.
- `Common/AuditableEntity.cs` — provides interceptor-managed creation/update metadata and SQL Server rowversion state.
- `Common/ISoftDelete.cs` — marks entities whose deletes must be retained with deletion audit metadata.
- `Courses/Course.cs` — owns course state and construction invariants; it belongs under the course aggregate feature.
- `Courses/Events/CourseCreatedDomainEvent.cs` — captures the course identifier and normalized title when a course is created.
- `Repositories/IRepository.cs` — defines persistence behavior required by use cases without coupling Domain to EF Core.
- `Repositories/IUnitOfWork.cs` — defines the transaction boundary used to commit aggregate changes atomically.

## LearningPortal.Shared

This project contains stable contracts that both HTTP hosts may reference.

- `LearningPortal.Shared.csproj` — declares the contract-only assembly.
- `Courses/CourseDto.cs` — is the immutable course representation sent across process boundaries.
- `Courses/CreateCourseRequest.cs` — models course creation input independently of the Application command.
- `Identity/LoginRequest.cs` — models credentials accepted by the authentication API.
- `Identity/RefreshTokenRequest.cs` — models a raw token submitted for secure rotation.
- `Identity/RevokeTokenRequest.cs` — models a raw token submitted for idempotent revocation.
- `Identity/AuthenticationResponse.cs` — returns access and refresh tokens with their independent UTC expirations.
- `Results/ErrorType.cs` — classifies failures without introducing HTTP concerns into lower layers.
- `Results/Error.cs` — carries stable machine and human-readable failure information.
- `Results/Errors.cs` — centralizes consistently coded validation, common, authentication, and authorization errors.
- `Results/Result.cs` — represents value-free success/failure without exception-driven business control flow.
- `Results/ResultT.cs` — represents generic success values or explicit failures in a separate immutable type.

## LearningPortal.Application

This project coordinates domain behavior through CQRS and exposes ports implemented by Infrastructure.

- `LearningPortal.Application.csproj` — references only Domain/Shared and application-level validation/logging abstractions.
- `DependencyInjection.cs` — is the Application composition extension for handlers and FluentValidation validators.
- `Abstractions/Identity/IIdentityService.cs` — keeps authentication use cases independent from ASP.NET Identity implementation types.
- `Abstractions/Identity/ICurrentUserService.cs` — exposes authenticated user identity without coupling Application or Domain to HTTP.
- `Abstractions/Networking/IClientIpAddressProvider.cs` — supplies request-origin metadata without coupling authentication use cases to `HttpContext`.
- `Abstractions/Time/ISystemClock.cs` — abstracts UTC time for deterministic auditing and tests.
- `Abstractions/Messaging/ICommand.cs` — marks state-changing CQRS messages.
- `Abstractions/Messaging/ICommandDispatcher.cs` — dispatches Result-based commands through custom pipeline components without MediatR.
- `Abstractions/Messaging/ICommandHandler.cs` — defines asynchronous command execution for dependency injection and testing.
- `Abstractions/Messaging/ICommandPipelineBehavior.cs` — defines composable pre/post-handler behavior and its asynchronous continuation delegate.
- `Abstractions/Messaging/IQuery.cs` — marks read-only CQRS messages.
- `Abstractions/Messaging/IQueryHandler.cs` — defines asynchronous query execution for dependency injection and testing.
- `Courses/Commands/CreateCourse/CreateCourseCommand.cs` — is the Application-layer request to create a course.
- `Courses/Commands/CreateCourse/CreateCourseCommandValidator.cs` — contains input rules that do not belong to core domain invariants.
- `Courses/Commands/CreateCourse/CreateCourseCommandHandler.cs` — coordinates aggregate creation, repository persistence, the unit of work, Result output, and structured logging.
- `Courses/Queries/GetCourses/GetCoursesQuery.cs` — represents the read-side request for the course catalog.
- `Courses/Queries/GetCourses/GetCoursesQueryHandler.cs` — loads aggregates and projects them to shared DTOs without leaking EF entities.
- `Authentication/Commands/Login/LoginCommand.cs` — represents credential authentication in the existing CQRS pipeline.
- `Authentication/Commands/Login/LoginCommandValidator.cs` — validates email and password shape before Identity access.
- `Authentication/Commands/Login/LoginCommandHandler.cs` — delegates validated login requests to the identity abstraction.
- `Authentication/Commands/Refresh/RefreshTokenCommand.cs` — represents refresh-token rotation in the CQRS pipeline.
- `Authentication/Commands/Refresh/RefreshTokenCommandValidator.cs` — rejects missing or oversized refresh tokens before hashing.
- `Authentication/Commands/Refresh/RefreshTokenCommandHandler.cs` — delegates validated rotation requests to the identity abstraction.
- `Authentication/Commands/Revoke/RevokeRefreshTokenCommand.cs` — represents refresh-token revocation in the CQRS pipeline.
- `Authentication/Commands/Revoke/RevokeRefreshTokenCommandValidator.cs` — validates revocation input before persistence access.
- `Authentication/Commands/Revoke/RevokeRefreshTokenCommandHandler.cs` — delegates idempotent revocation to the identity abstraction.
- `Behaviors/ValidationBehavior.cs` — runs all command validators before handlers and returns failed Results instead of throwing.
- `Messaging/CommandDispatcher.cs` — resolves handlers and composes the registered custom command pipeline in deterministic order.

## LearningPortal.Infrastructure

This project contains replaceable external-system implementations.

- `LearningPortal.Infrastructure.csproj` — references Application/Domain and owns EF Core, SQL Server, Identity, JWT, and health-check packages.
- `DependencyInjection.cs` — composes scoped and factory-created SQL Server contexts through one retry/interceptor configuration, plus Identity, JWT validation, repositories, unit of work, and database readiness checks.
- `Identity/ApplicationUser.cs` — extends the Identity persistence model with portal-specific profile data and an explicit enabled-account state.
- `Identity/JwtOptions.cs` — provides strongly typed, startup-validated token settings.
- `Identity/IdentityService.cs` — executes the complete refresh transaction through SQL Server's retry strategy and uses a factory-created context for concurrency replay recovery.
- `Identity/RefreshToken.cs` — encapsulates hash-only refresh-token persistence, security-stamp binding, IP audit metadata, expiration, rotation, revocation, and rowversion state.
- `Identity/IRefreshTokenProtector.cs` — abstracts secure opaque-token generation and one-way hashing for unit testing.
- `Identity/RefreshTokenProtector.cs` — creates 512-bit tokens and SHA-256 hashes while ensuring raw values are never persisted.
- `Identity/IAccessTokenGenerator.cs` — abstracts signed access-token generation from refresh-token lifecycle logic.
- `Identity/JwtAccessTokenGenerator.cs` — emits signed JWTs containing sub, name, email, role, jti, and numeric iat claims.
- `Identity/CurrentUserService.cs` — resolves the GUID user identifier from the current authenticated HTTP principal.
- `Networking/ClientIpAddressProvider.cs` — captures the remote request IP for refresh-token creation and revocation audit fields.
- `Time/SystemClock.cs` — implements application time through the platform TimeProvider.
- `Persistence/ApplicationDbContext.cs` — is the single EF Core unit of work for business aggregates and Identity tables.
- `Persistence/Configurations/CourseConfiguration.cs` — keeps course SQL mapping, lengths, precision, and indexes outside the domain entity.
- `Persistence/Configurations/ApplicationUserConfiguration.cs` — preserves existing users as enabled when the account-state column is deployed.
- `Persistence/Configurations/RefreshTokenConfiguration.cs` — maps hash-only token storage, security-stamp and IP fields, lookup indexes, Identity ownership, and rowversion concurrency.
- `Persistence/Extensions/ModelBuilderExtensions.cs` — ignores in-memory domain events and applies audit, rowversion, soft-delete, index, and global-filter conventions.
- `Persistence/Interceptors/AuditSaveChangesInterceptor.cs` — owns audit-field population and converts tracked deletes into soft deletes.
- `Persistence/Repositories/Repository.cs` — implements the Domain repository contract with async EF Core operations and no-tracking reads.
- `Migrations/20260722082720_DomainFoundation.cs` — deploys audit-user, soft-delete, rowversion, and soft-delete index columns.
- `Migrations/20260722082720_DomainFoundation.Designer.cs` — records EF Core metadata for the Domain Foundation migration.
- `Migrations/ApplicationDbContextModelSnapshot.cs` — tracks the current database model used to calculate future migrations.
- `Migrations/20260723032505_EnterpriseAuthentication.cs` — adds enabled-account state and creates secure refresh-token persistence with unique-hash and expiration indexes.
- `Migrations/20260723032505_EnterpriseAuthentication.Designer.cs` — records EF metadata for the finalized authentication migration.

## LearningPortal.Api

This project is the Minimal API host and composition root.

- `LearningPortal.Api.csproj` — references Application, Infrastructure, and Shared while hosting ASP.NET Core and Swagger.
- `Program.cs` — builds logging/DI, orders middleware, maps endpoints, and exposes an integration-test entry point.
- `DependencyInjection.cs` — centralizes API CORS, Problem Details factory registration, Swagger, and ordered pipeline registration.
- `Endpoints/IdentityEndpoints.cs` — maps anonymous login, refresh, and idempotent revoke endpoints to the custom CQRS dispatcher and Result-to-HTTP conventions.
- `Endpoints/CourseEndpoints.cs` — maps protected course HTTP routes to CQRS handlers and Result responses.
- `Endpoints/HealthEndpoints.cs` — exposes distinct liveness and SQL-backed readiness probes.
- `Constants/CorrelationIdConstants.cs` — defines the shared request header and context-item keys used for correlation.
- `Constants/ExceptionErrorCodes.cs` — defines stable machine-readable codes for exception mappings.
- `Exceptions/NotFoundException.cs` — represents expected missing-resource failures at the API boundary.
- `Exceptions/ConflictException.cs` — represents expected state-conflict failures at the API boundary.
- `Exceptions/ForbiddenAccessException.cs` — represents expected authorization failures at the API boundary.
- `Middleware/CorrelationIdMiddleware.cs` — accepts or creates a UUIDv7 correlation identifier and returns it on every response.
- `Middleware/ExceptionHandlingMiddleware.cs` — maps unhandled exceptions to safe Result errors and RFC 7807 responses.
- `ProblemDetails/IApiProblemDetailsFactory.cs` — abstracts consistent problem document creation for testability.
- `ProblemDetails/ApiProblemDetailsFactory.cs` — centralizes status, title, type, trace, correlation, and error-code output.
- `Extensions/MiddlewareExtensions.cs` — exposes explicit correlation and exception pipeline registration methods.
- `Extensions/ResultExtensions.cs` — translates transport-neutral Result errors into appropriate HTTP status codes.
- `Health/HealthResponseWriter.cs` — emits compact JSON health output for operators and orchestrators.
- `appsettings.json` — contains safe defaults for SQL Server, JWT metadata, CORS, logging, and host filtering; its JWT secret is intentionally blank.
- `appsettings.Development.json` — supplies local-only developer logging and a replaceable development signing key.
- `Properties/launchSettings.json` — defines repeatable HTTP/HTTPS development ports and Swagger launch behavior.
- `LearningPortal.Api.http` — provides executable sample requests for the liveness and token endpoints.

## LearningPortal.Blazor

This project is the interactive Blazor Web App host.

- `LearningPortal.Blazor.csproj` — hosts Razor components and references only Shared contracts.
- `Program.cs` — configures logging, middleware, static assets, server interactivity, and the host health endpoint.
- `DependencyInjection.cs` — validates the API URL and registers Razor components, a typed HTTP client, and health checks.
- `Models/ApiHealthResponse.cs` — models only the health payload consumed by the UI.
- `Services/LearningPortalApiClient.cs` — encapsulates asynchronous HTTP access so components remain testable and presentation-focused.
- `Components/App.razor` — defines the HTML document shell, asset links, router output, and Blazor reconnect script.
- `Components/Routes.razor` — owns route discovery, focus behavior, and the default layout.
- `Components/_Imports.razor` — centralizes namespaces and Razor imports shared by all components.
- `Components/Pages/Home.razor` — provides the portal landing page and displays API connectivity through the typed client.
- `Components/Pages/Error.razor` — provides a safe host-level error page with a request identifier.
- `Components/Pages/NotFound.razor` — provides the route-not-found experience.
- `Components/Layout/MainLayout.razor` — supplies the shared page layout and global Blazor error UI.
- `Components/Layout/MainLayout.razor.css` — scopes layout and error-banner styling to `MainLayout`.
- `Components/Layout/ReconnectModal.razor` — displays interactive-server connection state and retry actions.
- `Components/Layout/ReconnectModal.razor.css` — scopes reconnect dialog styling.
- `Components/Layout/ReconnectModal.razor.js` — integrates browser events with Blazor reconnect/reload behavior.
- `wwwroot/app.css` — defines application-wide visual styles and the portal landing-page presentation.
- `appsettings.json` — configures the API base URL, host filtering, and normal logging levels.
- `appsettings.Development.json` — increases local diagnostic logging without changing production defaults.
- `Properties/launchSettings.json` — defines stable Blazor ports and an HTTP-only API URL override for that profile.

## LearningPortal.Infrastructure.Tests

This project verifies authentication behavior against real Identity services and an isolated EF Core store.

- `LearningPortal.Infrastructure.Tests.csproj` — declares the .NET 10 xUnit test assembly and references the layers exercised by authentication tests.
- `Authentication/AuthenticationTestContext.cs` — builds a deterministic Identity/EF test host with fake UTC time and client IP abstractions.
- `Authentication/IdentityServiceTests.cs` — verifies login, lockout, hash-only storage, rotation, replay containment, expiry, idempotent revocation, duplicate refresh rejection, and JWT claims.

## LearningPortal.Infrastructure.IntegrationTests

This slower test project validates behavior that EF InMemory cannot represent by running the real SQL Server provider against an isolated container.

- `LearningPortal.Infrastructure.IntegrationTests.csproj` — declares the categorized SQL Server Testcontainers test assembly separately from fast service tests.
- `Authentication/SqlServerFactAttribute.cs` — marks Docker-backed tests as opt-in and reports them as skipped during ordinary test runs.
- `Authentication/SqlServerAuthenticationFixture.cs` — starts an opt-in SQL Server container, applies migrations only to that isolated database, and builds production Infrastructure registrations.
- `Authentication/RefreshRotationCoordinator.cs` — synchronizes two independent refresh requests after both have loaded the original token.
- `Authentication/CoordinatedAccessTokenGenerator.cs` — inserts the test coordination barrier while delegating JWT creation to the production generator.
- `Authentication/RefreshTokenRelationalTests.cs` — verifies relational transactions, independent-context concurrency, replay revocation, unique hashes, rowversion, and migration indexes.
