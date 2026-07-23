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
- `Courses/Course.cs`, `CourseStatus.cs`, and `SlugNormalizer.cs` — own the Draft/Published/Archived lifecycle, editable data, ownership, and normalized slugs.
- `Courses/Events/*` — capture course creation, publication, archival, and deletion facts without dispatching them.
- `Courses/Exceptions/*` — translate provider-detected slug and rowversion conflicts without leaking EF Core into Application.
- `Repositories/ICourseRepository.cs` — defines tracked lookup, filtered paging, slug uniqueness, deletion, and rowversion operations required by course management.
- `Repositories/IRepository.cs` — defines persistence behavior required by use cases without coupling Domain to EF Core.
- `Repositories/IUnitOfWork.cs` — defines the transaction boundary used to commit aggregate changes atomically.

## LearningPortal.Shared

This project contains stable contracts that both HTTP hosts may reference.

- `LearningPortal.Shared.csproj` — declares the contract-only assembly.
- `Courses/*` — contains compact course detail/list/page responses and list/create/update request contracts, including Base64 rowversion values.
- `Identity/LoginRequest.cs` — models credentials accepted by the authentication API.
- `Identity/RefreshTokenRequest.cs` — models a raw token submitted for secure rotation.
- `Identity/RevokeTokenRequest.cs` — models a raw token submitted for idempotent revocation.
- `Identity/AuthenticationResponse.cs` — returns access and refresh tokens with their independent UTC expirations.
- `UserManagement/UserResponse.cs` — exposes only the administrator-safe user fields required by the management API.
- `UserManagement/PagedUsersResponse.cs` — carries a user page and its pagination metadata.
- `UserManagement/GetUsersRequest.cs` — binds the supported search and pagination query-string values.
- `UserManagement/AssignUserRoleRequest.cs` — models one additive role assignment.
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
- `Abstractions/Identity/ICurrentUserService.cs` — exposes authenticated identity, profile claims, roles, and reusable claim/role checks without coupling Application to HTTP.
- `Abstractions/Identity/IUserManagementService.cs` — keeps user-management use cases independent from ASP.NET Identity and EF Core.
- `Abstractions/Networking/IClientIpAddressProvider.cs` — supplies request-origin metadata without coupling authentication use cases to `HttpContext`.
- `Abstractions/Time/ISystemClock.cs` — abstracts UTC time for deterministic auditing and tests.
- `Authorization/ApplicationRoles.cs` — defines and validates the Administrator, Instructor, and Student role allowlist.
- `Authorization/ApplicationClaimTypes.cs` — centralizes JWT/current-user claim names and reserves the future permission claim.
- `Authorization/Policies.cs` — defines stable names for every registered role policy.
- `Abstractions/Messaging/ICommand.cs` — marks state-changing CQRS messages.
- `Abstractions/Messaging/ICommandDispatcher.cs` — dispatches Result-based commands through custom pipeline components without MediatR.
- `Abstractions/Messaging/ICommandHandler.cs` — defines asynchronous command execution for dependency injection and testing.
- `Abstractions/Messaging/ICommandPipelineBehavior.cs` — defines composable pre/post-handler behavior and its asynchronous continuation delegate.
- `Abstractions/Messaging/IQuery.cs` — marks read-only CQRS messages.
- `Abstractions/Messaging/IQueryHandler.cs` — defines asynchronous query execution for dependency injection and testing.
- `Courses/Commands/*` — validates and handles authorized create, Draft update, publish, archive, and soft-delete operations.
- `Courses/Queries/*` — validates and handles ownership-filtered course detail and paginated list reads.
- `Courses/CourseAuthorization.cs`, `CourseMappings.cs`, and `CoursePersistence.cs` — centralize ownership checks, transport projection, and standard conflict mapping.
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
- `UserManagement/Queries/GetUsers/*` — validates and handles filtered, paginated user-list requests.
- `UserManagement/Queries/GetUserById/*` — handles administrator user lookup by identifier.
- `UserManagement/Commands/SetUserEnabled/*` — validates and handles user enable/disable operations.
- `UserManagement/Commands/AssignUserRole/*` — validates and handles additive allowlisted role assignment.

## LearningPortal.Infrastructure

This project contains replaceable external-system implementations.

- `LearningPortal.Infrastructure.csproj` — references Application/Domain and owns EF Core, SQL Server, Identity, JWT, and health-check packages.
- `DependencyInjection.cs` — composes SQL Server, Identity role validation/seeding, JWT validation, authorization policies, repositories, unit of work, and database readiness checks.
- `Authorization/AuthorizationServiceCollectionExtensions.cs` — registers the authenticated fallback and named role policies in one focused location.
- `Authorization/ApplicationRoleValidator.cs` — prevents creation of Identity roles outside the application allowlist.
- `Identity/ApplicationUser.cs` — extends the Identity persistence model with portal-specific profile data and an explicit enabled-account state.
- `Identity/JwtOptions.cs` — provides strongly typed, startup-validated token settings.
- `Identity/IdentityService.cs` — executes the complete refresh transaction through SQL Server's retry strategy and uses a factory-created context for concurrency replay recovery.
- `Identity/RefreshToken.cs` — encapsulates hash-only refresh-token persistence, security-stamp binding, IP audit metadata, expiration, rotation, revocation, and rowversion state.
- `Identity/IRefreshTokenProtector.cs` — abstracts secure opaque-token generation and one-way hashing for unit testing.
- `Identity/RefreshTokenProtector.cs` — creates 512-bit tokens and SHA-256 hashes while ensuring raw values are never persisted.
- `Identity/IAccessTokenGenerator.cs` — abstracts signed access-token generation from refresh-token lifecycle logic.
- `Identity/JwtAccessTokenGenerator.cs` — emits signed JWTs containing sub, name, email, role, jti, and numeric iat claims.
- `Identity/CurrentUserService.cs` — projects user, profile, role, and future permission claims from the authenticated HTTP principal.
- `Identity/IIdentityRoleSeeder.cs` — abstracts the asynchronous, idempotent role-seeding operation.
- `Identity/IdentityRoleSeeder.cs` — creates only missing application roles with UUIDv7 identifiers and never seeds users.
- `Identity/IdentityRoleSeedingExtensions.cs` — invokes the scoped role seeder from the API composition root during startup.
- `Identity/UserManagementService.cs` — implements filtered reads, enabled-state changes, and additive role assignment through ASP.NET Identity.
- `Networking/ClientIpAddressProvider.cs` — captures the remote request IP for refresh-token creation and revocation audit fields.
- `Time/SystemClock.cs` — implements application time through the platform TimeProvider.
- `Persistence/ApplicationDbContext.cs` — is the single EF Core unit of work for business aggregates and Identity tables.
- `Persistence/Configurations/CourseConfiguration.cs` — maps course lengths, string status, rowversion, filtered unique slug, and management indexes.
- `Persistence/Repositories/CourseRepository.cs` — implements tracked mutations and ownership-filtered, soft-delete-aware course reads.
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
- `Migrations/*_CourseManagementFoundation.cs` and `.Designer.cs` — evolve Courses with management fields, safe legacy backfill, constraints, and indexes.

## LearningPortal.Api

This project is the Minimal API host and composition root.

- `LearningPortal.Api.csproj` — references Application, Infrastructure, and Shared while hosting ASP.NET Core and Swagger.
- `Program.cs` — builds logging/DI, seeds missing Identity roles, orders middleware, maps endpoints, and exposes an integration-test entry point.
- `DependencyInjection.cs` — centralizes API CORS, Problem Details factory registration, Swagger, and ordered pipeline registration.
- `Endpoints/IdentityEndpoints.cs` — maps anonymous login, refresh, and idempotent revoke endpoints to the custom CQRS dispatcher and Result-to-HTTP conventions.
- `Endpoints/CourseEndpoints.cs` — maps Administrator/Instructor course list, detail, create, update, publish, archive, and delete routes.
- `Endpoints/HealthEndpoints.cs` — exposes distinct liveness and SQL-backed readiness probes.
- `Endpoints/UserManagementEndpoints.cs` — maps the compact administrator-only user-management API.
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
- `Extensions/AuthorizationEndpointExtensions.cs` — applies named role policies to Minimal API endpoints without magic strings.
- `Health/HealthResponseWriter.cs` — emits compact JSON health output for operators and orchestrators.
- `appsettings.json` — contains safe defaults for SQL Server, JWT metadata, CORS, logging, and host filtering; its JWT secret is intentionally blank.
- `appsettings.Development.json` — supplies local-only developer logging and a replaceable development signing key.
- `Properties/launchSettings.json` — defines repeatable HTTP/HTTPS development ports and Swagger launch behavior.
- `LearningPortal.Api.http` — provides executable sample requests for the liveness and token endpoints.

## LearningPortal.Blazor

This project is the interactive Blazor Web App host.

- `LearningPortal.Blazor.csproj` — hosts Razor components and references Application authorization constants plus Shared contracts.
- `Program.cs` — configures logging, middleware, static assets, server interactivity, and the host health endpoint.
- `DependencyInjection.cs` — validates the API URL and registers Razor components, component authorization state, a typed HTTP client, and health checks.
- `Models/ApiHealthResponse.cs` — models only the health payload consumed by the UI.
- `Models/CourseFormModel.cs` — provides DataAnnotations-backed create/edit form values.
- `Services/LearningPortalApiClient.cs` and `ApiProblemException.cs` — provide authenticated typed user/course API calls and safe RFC 7807 error details.
- `Components/App.razor` — defines the HTML document shell, asset links, router output, and Blazor reconnect script.
- `Components/Routes.razor` — owns authorization-aware route discovery, focus behavior, and the default layout.
- `Components/RedirectToAccessDenied.razor` — redirects unauthenticated or unauthorized component navigation safely.
- `Components/_Imports.razor` — centralizes authorization, layout, design-component, and framework namespaces.
- `Components/Pages/Dashboard.razor` and `.css` — demonstrate the authenticated design system with static learning and course samples.
- `Components/Pages/MyCourses.razor` — provides the scoped learner placeholder.
- `Components/Pages/Users.razor` and `.css` — provide the administrator-only responsive user table, debounced search, pagination, account-state actions, and additive role panel.
- `Components/Pages/Courses.razor` and `.css` — provide Administrator/Instructor course filtering, paging, forms, lifecycle actions, feedback, and responsive presentation.
- `Components/Pages/AccessDenied.razor` and `.css` — explain unauthenticated and role-denied navigation.
- `Components/Pages/Error.razor` — provides a safe host-level error page with a request identifier.
- `Components/Pages/NotFound.razor` — provides the route-not-found experience.
- `Components/Layout/MainLayout.razor` and `.css` — provide the responsive authenticated shell, role-aware navigation, top bar, profile summary, and global error UI.
- `Components/Layout/NavigationSection.razor` and `.css` — group role-specific links consistently within the sidebar.
- `Components/Shared/AppCard.razor` and `.css` — provide the base rounded content surface.
- `Components/Shared/StatCard.razor` and `.css` — display concise dashboard metrics.
- `Components/Shared/StatusBadge.razor` and `.css` — display allowlisted semantic status tones.
- `Components/Shared/PageHeader.razor` and `.css` — standardize page titles, descriptions, and actions.
- `Components/Shared/EmptyState.razor` and `.css` — provide a consistent no-content placeholder.
- `Components/Shared/LoadingState.razor` and `.css` — provide an accessible lightweight loading indicator.
- `Components/Shared/AuthenticatedContent.razor` — protects component content with the current principal and optional application roles without changing JWT behavior.
- `Components/Courses/CourseForm.razor` and `CourseStatusBadge.razor` — provide validated course editing and lifecycle presentation.
- `Components/Shared/ConfirmDialog.razor` and `PaginationControls.razor` — provide reusable confirmation and paging interactions.
- `Components/Layout/ReconnectModal.razor` — displays interactive-server connection state and retry actions.
- `Components/Layout/ReconnectModal.razor.css` — scopes reconnect dialog styling.
- `Components/Layout/ReconnectModal.razor.js` — integrates browser events with Blazor reconnect/reload behavior.
- `wwwroot/app.css` — defines accessible theme tokens, focus states, buttons, and shared shell/card variants.
- `appsettings.json` — configures the API base URL, host filtering, and normal logging levels.
- `appsettings.Development.json` — increases local diagnostic logging without changing production defaults.
- `Properties/launchSettings.json` — defines stable Blazor ports and an HTTP-only API URL override for that profile.

## LearningPortal.Infrastructure.Tests

This project verifies authentication behavior against real Identity services and an isolated EF Core store.

- `LearningPortal.Infrastructure.Tests.csproj` — declares the fast .NET 10 xUnit assembly for authentication and authorization tests.
- `Authentication/AuthenticationTestContext.cs` — builds a deterministic Identity/EF test host with fake UTC time and client IP abstractions.
- `Authentication/IdentityServiceTests.cs` — verifies login, lockout, hash-only storage, rotation, replay containment, expiry, idempotent revocation, duplicate refresh rejection, and JWT claims.
- `Authorization/ApplicationRolesTests.cs` — verifies the fixed role allowlist and case-insensitive validation.
- `Authorization/CurrentUserServiceTests.cs` — verifies current-user claim projection, distinct roles, and claim/role helpers.
- `Authorization/AuthorizationPolicyTests.cs` — verifies the authenticated fallback and role composition of every named policy.
- `Authorization/AuthorizationEndpointExtensionsTests.cs` — verifies that each Minimal API helper applies the expected policy metadata.
- `Authorization/IdentityRoleSeederTests.cs` — verifies idempotent role creation and rejection of unsupported role creation/assignment.
- `UserManagement/UserManagementServiceTests.cs` — verifies pagination, lookup failures, enabled state, and valid/invalid/idempotent role assignment.
- `Courses/CourseDomainTests.cs` and `CourseApplicationTests.cs` — verify lifecycle, slug, ownership, pagination, role, state, duplicate, and concurrency behavior.
- `Courses/CourseApiClientTests.cs` — verifies course query construction, form validation, mutations, lifecycle routes, and Problem Details conflict parsing.

## LearningPortal.Infrastructure.IntegrationTests

This slower test project validates behavior that EF InMemory cannot represent by running the real SQL Server provider against an isolated container.

- `LearningPortal.Infrastructure.IntegrationTests.csproj` — declares the categorized SQL Server Testcontainers test assembly separately from fast service tests.
- `Authentication/SqlServerFactAttribute.cs` — marks Docker-backed tests as opt-in and reports them as skipped during ordinary test runs.
- `Authentication/SqlServerAuthenticationFixture.cs` — starts an opt-in SQL Server container, applies migrations only to that isolated database, and builds production Infrastructure registrations.
- `Authentication/RefreshRotationCoordinator.cs` — synchronizes two independent refresh requests after both have loaded the original token.
- `Authentication/CoordinatedAccessTokenGenerator.cs` — inserts the test coordination barrier while delegating JWT creation to the production generator.
- `Authentication/RefreshTokenRelationalTests.cs` — verifies relational transactions, independent-context concurrency, replay revocation, unique hashes, rowversion, and migration indexes.
- `Courses/CoursePersistenceRelationalTests.cs` — verifies SQL Server filtered slug uniqueness and soft-delete query filtering.
