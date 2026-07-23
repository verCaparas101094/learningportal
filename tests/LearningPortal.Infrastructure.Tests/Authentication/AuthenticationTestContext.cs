using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Networking;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearningPortal.Infrastructure.Tests.Authentication;

internal sealed class AuthenticationTestContext : IAsyncDisposable
{
    internal const string Email = "learner@example.com";
    internal const string Password = "StrongPassword!123";
    internal const string Role = "Learner";

    private readonly ServiceProvider _serviceProvider;
    private readonly AsyncServiceScope _scope;

    private AuthenticationTestContext(
        ServiceProvider serviceProvider,
        AsyncServiceScope scope,
        TestSystemClock clock,
        ApplicationUser user)
    {
        _serviceProvider = serviceProvider;
        _scope = scope;
        Clock = clock;
        User = user;
    }

    internal TestSystemClock Clock { get; }

    internal ApplicationUser User { get; }

    internal IIdentityService IdentityService =>
        _scope.ServiceProvider.GetRequiredService<IIdentityService>();

    internal IRefreshTokenProtector RefreshTokenProtector =>
        _scope.ServiceProvider.GetRequiredService<IRefreshTokenProtector>();

    internal ApplicationDbContext DbContext =>
        _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    internal UserManager<ApplicationUser> UserManager =>
        _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    internal static async Task<AuthenticationTestContext> CreateAsync(
        bool isLockedOut = false,
        bool isEnabled = true)
    {
        var services = new ServiceCollection();
        var clock = new TestSystemClock(new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero));
        var databaseName = $"AuthenticationTests-{Guid.CreateVersion7()}";

        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication();
        services.AddHttpContextAccessor();
        services.AddDbContextFactory<ApplicationDbContext>(
            options => options.UseInMemoryDatabase(databaseName),
            ServiceLifetime.Scoped);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = "LearningPortal.Tests";
            options.Audience = "LearningPortal.Tests.Client";
            options.SigningKey = "LearningPortal-Tests-Signing-Key-With-At-Least-32-Bytes!";
            options.ExpirationMinutes = 15;
            options.RefreshTokenExpirationDays = 30;
        });
        services.AddSingleton<ISystemClock>(clock);
        services.AddSingleton<IClientIpAddressProvider>(new TestClientIpAddressProvider("203.0.113.10"));
        services.AddSingleton<IRefreshTokenProtector, RefreshTokenProtector>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var scope = serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = Email,
            Email = Email,
            EmailConfirmed = true,
            DisplayName = "Enterprise Learner",
            IsEnabled = isEnabled,
            LockoutEnabled = true
        };

        var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(Role));
        EnsureSucceeded(roleResult);
        EnsureSucceeded(await roleManager.CreateAsync(
            new IdentityRole<Guid>(ApplicationRoles.Student)));
        var createResult = await userManager.CreateAsync(user, Password);
        EnsureSucceeded(createResult);
        var addRoleResult = await userManager.AddToRoleAsync(user, Role);
        EnsureSucceeded(addRoleResult);

        if (isLockedOut)
        {
            var lockoutResult = await userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow.AddHours(1));
            EnsureSucceeded(lockoutResult);
        }

        return new AuthenticationTestContext(serviceProvider, scope, clock, user);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

internal sealed class TestSystemClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    internal void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}

internal sealed class TestClientIpAddressProvider(string ipAddress) : IClientIpAddressProvider
{
    public string IpAddress { get; } = ipAddress;
}
