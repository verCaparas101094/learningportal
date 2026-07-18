using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LearningPortal.Api.Health;

/// <summary>Writes a compact JSON health report suitable for operators.</summary>
public static class HealthResponseWriter
{
    /// <summary>Serializes the health report to the response body.</summary>
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description
            })
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            cancellationToken: context.RequestAborted);
    }
}
