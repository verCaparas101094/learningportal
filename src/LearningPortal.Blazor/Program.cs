using LearningPortal.Blazor;
using LearningPortal.Blazor.Components;
using LearningPortal.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddBlazorPresentation(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapPortalAuthenticationEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();

/// <summary>Provides a public entry point for integration-test hosting.</summary>
public partial class Program;
