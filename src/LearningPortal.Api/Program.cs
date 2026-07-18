using LearningPortal.Api;
using LearningPortal.Api.Endpoints;
using LearningPortal.Application;
using LearningPortal.Infrastructure;

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

app.UseApiPipeline();
app.MapIdentityEndpoints();
app.MapCourseEndpoints();
app.MapPortalHealthChecks();

await app.RunAsync();

/// <summary>Provides a public entry point for integration-test hosting.</summary>
public partial class Program;
