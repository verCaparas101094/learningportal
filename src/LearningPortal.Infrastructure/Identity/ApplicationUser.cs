using Microsoft.AspNetCore.Identity;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>Represents an authenticated portal user persisted by ASP.NET Identity.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the user's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the user may authenticate.</summary>
    public bool IsEnabled { get; set; } = true;
}
