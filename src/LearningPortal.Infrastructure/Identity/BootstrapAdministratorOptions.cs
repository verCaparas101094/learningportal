namespace LearningPortal.Infrastructure.Identity;

/// <summary>Configures an optional idempotent bootstrap administrator account.</summary>
public sealed class BootstrapAdministratorOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "BootstrapAdministrator";

    /// <summary>Gets or sets whether bootstrap account creation is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the bootstrap administrator email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the bootstrap administrator password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the bootstrap administrator display name.</summary>
    public string DisplayName { get; set; } = "Portal Administrator";
}
