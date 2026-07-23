namespace LearningPortal.Infrastructure.Identity;

/// <summary>Controls explicit development database initialization.</summary>
public sealed class DatabaseInitializationOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "DatabaseInitialization";

    /// <summary>Gets or sets whether migrations are applied automatically in Development.</summary>
    public bool ApplyMigrations { get; set; }
}
