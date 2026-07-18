using System.Reflection;
using LearningPortal.Api.Extensions;
using LearningPortal.Api.ProblemDetails;
using Microsoft.OpenApi;

namespace LearningPortal.Api;

/// <summary>Configures services owned by the HTTP API host.</summary>
public static class DependencyInjection
{
    private const string CorsPolicyName = "BlazorClient";

    /// <summary>Adds API presentation services.</summary>
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("At least one CORS allowed origin must be configured.");
        }

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        services.AddSingleton<IApiProblemDetailsFactory, ApiProblemDetailsFactory>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Learning Portal API",
                Version = "v1",
                Description = "Enterprise Learning Portal HTTP API."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter the access token returned by POST /api/auth/token."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
        });

        return services;
    }

    /// <summary>Configures the ordered API middleware pipeline.</summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseCorrelationId();
        app.UseExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Learning Portal API v1");
                options.DisplayRequestDuration();
            });
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
