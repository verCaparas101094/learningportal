namespace LearningPortal.Blazor.Models;

/// <summary>Represents the API health response displayed by the portal shell.</summary>
/// <param name="Status">The aggregate API health status.</param>
public sealed record ApiHealthResponse(string Status);
