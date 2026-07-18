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
- `Common/Entity.cs` — centralizes entity identity and audit timestamps shared by domain aggregates.
- `Courses/Course.cs` — owns course state and construction invariants; it belongs under the course aggregate feature.
- `Repositories/IRepository.cs` — defines persistence behavior required by use cases without coupling Domain to EF Core.
- `Repositories/IUnitOfWork.cs` — defines the transaction boundary used to commit aggregate changes atomically.

## LearningPortal.Shared

This project contains stable contracts that both HTTP hosts may reference.

- `LearningPortal.Shared.csproj` — declares the contract-only assembly.
- `Courses/CourseDto.cs` — is the immutable course representation sent across process boundaries.
- `Courses/CreateCourseRequest.cs` — models course creation input independently of the Application command.
- `Identity/LoginRequest.cs` — models credentials accepted by the authentication API.
- `Identity/TokenResponse.cs` — models the access token and its explicit UTC expiry.
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
- `Abstractions/Messaging/ICommand.cs` — marks state-changing CQRS messages.
- `Abstractions/Messaging/ICommandHandler.cs` — defines asynchronous command execution for dependency injection and testing.
- `Abstractions/Messaging/IQuery.cs` — marks read-only CQRS messages.
- `Abstractions/Messaging/IQueryHandler.cs` — defines asynchronous query execution for dependency injection and testing.
- `Courses/Commands/CreateCourse/CreateCourseCommand.cs` — is the Application-layer request to create a course.
- `Courses/Commands/CreateCourse/CreateCourseCommandValidator.cs` — contains input rules that do not belong to core domain invariants.
- `Courses/Commands/CreateCourse/CreateCourseCommandHandler.cs` — coordinates aggregate creation, repository persistence, the unit of work, Result output, and structured logging.
- `Courses/Queries/GetCourses/GetCoursesQuery.cs` — represents the read-side request for the course catalog.
- `Courses/Queries/GetCourses/GetCoursesQueryHandler.cs` — loads aggregates and projects them to shared DTOs without leaking EF entities.
- `Identity/LoginRequestValidator.cs` — validates login transport input before accessing the Identity store.

## LearningPortal.Infrastructure

This project contains replaceable external-system implementations.

- `LearningPortal.Infrastructure.csproj` — references Application/Domain and owns EF Core, SQL Server, Identity, JWT, and health-check packages.
- `DependencyInjection.cs` — composes SQL Server, Identity, JWT validation, repositories, unit of work, and database readiness checks.
- `Identity/ApplicationUser.cs` — extends the Identity persistence model with portal-specific user profile data.
- `Identity/JwtOptions.cs` — provides strongly typed, startup-validated token settings.
- `Identity/IdentityService.cs` — implements credential verification, lockout accounting, claims/role creation, and signed JWT issuance.
- `Persistence/ApplicationDbContext.cs` — is the single EF Core unit of work for business aggregates and Identity tables.
- `Persistence/Configurations/CourseConfiguration.cs` — keeps course SQL mapping, lengths, precision, and indexes outside the domain entity.
- `Persistence/Repositories/Repository.cs` — implements the Domain repository contract with async EF Core operations and no-tracking reads.

## LearningPortal.Api

This project is the Minimal API host and composition root.

- `LearningPortal.Api.csproj` — references Application, Infrastructure, and Shared while hosting ASP.NET Core and Swagger.
- `Program.cs` — builds logging/DI, orders middleware, maps endpoints, and exposes an integration-test entry point.
- `DependencyInjection.cs` — centralizes API CORS, Problem Details factory registration, Swagger, and ordered pipeline registration.
- `Endpoints/IdentityEndpoints.cs` — maps the anonymous token endpoint to validation and the identity port.
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
