using LearningPortal.Api;
using LearningPortal.Api.Endpoints;
using LearningPortal.Application;
using LearningPortal.Infrastructure;
using LearningPortal.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

await app.SeedIdentityRolesAsync();

app.UseApiPipeline();
app.MapIdentityEndpoints();
app.MapCourseEndpoints();
app.MapLessonEndpoints();
app.MapEnrollmentEndpoints();
app.MapLearningEndpoints();
app.MapQuizEndpoints();
app.MapInstructorEligibilityEndpoints();
app.MapUserManagementEndpoints();
app.MapPortalHealthChecks();

await app.RunAsync();

/// <summary>Provides a public entry point for integration-test hosting.</summary>
public partial class Program;
