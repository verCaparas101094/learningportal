using System.Text;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Abstractions.Networking;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.InstructorEligibility;
using LearningPortal.Application.AiTutor;
using LearningPortal.Infrastructure.AiTutor;
using LearningPortal.Domain.Repositories;
using LearningPortal.Infrastructure.Authorization;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Lessons;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Interceptors;
using LearningPortal.Infrastructure.Persistence.Repositories;
using LearningPortal.Infrastructure.Networking;
using LearningPortal.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LearningPortal.Infrastructure;

/// <summary>Registers database, Identity, authentication, and infrastructure implementations.</summary>
public static class DependencyInjection
{
    /// <summary>Adds infrastructure services using the supplied application configuration.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IVideoEmbedResolver, VideoEmbedResolver>();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
        services.AddScoped<IClientIpAddressProvider, ClientIpAddressProvider>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContextFactory<ApplicationDbContext>(
            (serviceProvider, options) =>
                ConfigureApplicationDbContext(serviceProvider, options, connectionString),
            ServiceLifetime.Scoped);
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            ConfigureApplicationDbContext(serviceProvider, options, connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddRoleValidator<ApplicationRoleValidator>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "JWT signing key must be at least 32 bytes.")
            .Validate(options => options.ExpirationMinutes is > 0 and <= 1_440, "JWT expiration must be between 1 and 1440 minutes.")
            .Validate(options => options.RefreshTokenExpirationDays is > 0 and <= 90, "Refresh token expiration must be between 1 and 90 days.")
            .ValidateOnStart();
        var eligibilityOptions = configuration
            .GetSection(InstructorEligibilityOptions.SectionName)
            .Get<InstructorEligibilityOptions>() ?? new InstructorEligibilityOptions();
        if (eligibilityOptions.QualificationThreshold is < 1 or > 100)
            throw new InvalidOperationException("Instructor eligibility threshold must be between 1 and 100.");
        services.AddSingleton(eligibilityOptions);
        var ollamaOptions = configuration.GetSection(OllamaOptions.SectionName)
            .Get<OllamaOptions>() ?? new OllamaOptions();
        ValidateOllamaOptions(ollamaOptions);
        services.AddSingleton(ollamaOptions);
        services.AddHttpClient<IOllamaClient, OllamaClient>(client =>
        {
            client.BaseAddress = new Uri(
                ollamaOptions.BaseUrl.TrimEnd('/') + "/",
                UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(ollamaOptions.RequestTimeoutSeconds);
        });

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ApplicationClaimTypes.DisplayName,
                    RoleClaimType = ApplicationClaimTypes.Role
                };
            });

        services.AddApplicationAuthorization();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IQuizRepository, QuizRepository>();
        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IInstructorEligibilityRepository, InstructorEligibilityRepository>();
        services.AddScoped<IAiTutorConversationRepository, AiTutorConversationRepository>();
        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IIdentityRoleSeeder, IdentityRoleSeeder>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IRefreshTokenProtector, RefreshTokenProtector>();
        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("sql-server", tags: ["ready"]);

        return services;
    }

    private static void ValidateOllamaOptions(OllamaOptions options)
    {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "Ollama BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(options.Model)
            || options.RequestTimeoutSeconds is < 1 or > 600
            || options.MaxContextCharacters is < 1_000 or > 200_000
            || options.MaxQuestionCharacters is < 1 or > 10_000
            || options.MaxConversationMessages is < 1 or > 100
            || options.Temperature is < 0 or > 2)
        {
            throw new InvalidOperationException("The Ollama configuration is invalid.");
        }
    }

    private static void ConfigureApplicationDbContext(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer =>
        {
            sqlServer.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            sqlServer.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });
        options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
    }
}
