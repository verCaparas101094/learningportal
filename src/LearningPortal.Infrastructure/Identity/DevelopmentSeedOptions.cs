namespace LearningPortal.Infrastructure.Identity;

/// <summary>Controls local-development-only users and learning sample data.</summary>
public sealed class DevelopmentSeedOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "DevelopmentSeed";

    /// <summary>Gets or sets whether development seeding is enabled.</summary>
    public bool Enabled { get; set; }
}
