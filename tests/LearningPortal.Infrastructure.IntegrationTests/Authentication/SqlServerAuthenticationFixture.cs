using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Networking;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Xunit;

namespace LearningPortal.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Provides an isolated, migrated SQL Server container and the production Infrastructure registrations.
/// </summary>
public sealed class SqlServerAuthenticationFixture : IAsyncLifetime
{
    private const string Password = "StrongPassword!123";
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private MsSqlContainer? _container;
    private ServiceProvider? _serviceProvider;

    /// <summary>Gets the barrier used to coordinate concurrent refresh requests.</summary>
    public RefreshRotationCoordinator Coordinator =>
        Services.GetRequiredService<RefreshRotationCoordinator>();

    /// <summary>Creates an independent dependency-injection scope.</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    /// <summary>Starts SQL Server, builds the service provider, and applies migrations to the isolated database.</summary>
    public async Task EnsureInitializedAsync()
    {
        if (!SqlServerFactAttribute.IsEnabled)
        {
            throw new InvalidOperationException(
                $"Set {SqlServerFactAttribute.RunIntegrationTestsVariable}=true before initializing SQL Server tests.");
        }

        if (_serviceProvider is not null)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_serviceProvider is not null)
            {
                return;
            }

            _container = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                .Build();
            await _container.StartAsync();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                    ["Jwt:Issuer"] = "LearningPortal.IntegrationTests",
                    ["Jwt:Audience"] = "LearningPortal.IntegrationTests.Client",
                    ["Jwt:SigningKey"] = "LearningPortal-Integration-Tests-Signing-Key-At-Least-32-Bytes!",
                    ["Jwt:ExpirationMinutes"] = "15",
                    ["Jwt:RefreshTokenExpirationDays"] = "30"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddInfrastructure(configuration);
            services.AddSingleton<RefreshRotationCoordinator>();
            services.AddScoped<JwtAccessTokenGenerator>();
            services.Replace(ServiceDescriptor.Singleton<ISystemClock>(
                new IntegrationSystemClock(
                    new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero))));
            services.Replace(ServiceDescriptor.Singleton<IClientIpAddressProvider>(
                new IntegrationClientIpAddressProvider("203.0.113.20")));
            services.Replace(ServiceDescriptor.Scoped<IAccessTokenGenerator, CoordinatedAccessTokenGenerator>());

            _serviceProvider = services.BuildServiceProvider(validateScopes: true);
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>Creates a confirmed Identity user and returns its initial refresh token.</summary>
    public async Task<AuthenticationSeed> CreateAuthenticationAsync()
    {
        await EnsureInitializedAsync();
        await using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var email = $"learner-{Guid.CreateVersion7():N}@example.com";
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Relational Test Learner",
            IsEnabled = true,
            LockoutEnabled = true
        };

        EnsureSucceeded(await userManager.CreateAsync(user, Password));
        var login = await identityService.LoginAsync(email, Password);
        if (login.IsFailure)
        {
            throw new InvalidOperationException($"Test login failed with code '{login.Error?.Code}'.");
        }

        return new AuthenticationSeed(user.Id, login.Value);
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        _initializationLock.Dispose();
    }

    private ServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException(
            "The SQL Server fixture must be initialized before creating a scope.");

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

/// <summary>Contains a seeded user identifier and its initial authentication response.</summary>
public sealed record AuthenticationSeed(Guid UserId, AuthenticationResponse Authentication);

internal sealed class IntegrationSystemClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

internal sealed class IntegrationClientIpAddressProvider(string ipAddress) : IClientIpAddressProvider
{
    public string IpAddress { get; } = ipAddress;
}
