using LearningPortal.Domain.Common;
using LearningPortal.Domain.Courses;

namespace LearningPortal.Domain.Skills;

/// <summary>Represents a stable learning and instructor-qualification skill.</summary>
public sealed class Skill : AuditableEntity
{
    private Skill() { }
    private Skill(string name, string slug, string? description)
    {
        Name = name;
        Slug = slug;
        Description = description;
    }

    /// <summary>Gets the display name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the normalized stable code.</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }
    /// <summary>Gets whether the skill may be used for new qualifications.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Creates an active skill.</summary>
    public static Skill Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var slug = SlugNormalizer.Normalize(name);
        if (name.Trim().Length > 100 || slug.Length is 0 or > 100 || description?.Length > 1000)
            throw new ArgumentException("Skill values are invalid.");
        return new Skill(name.Trim(), slug, string.IsNullOrWhiteSpace(description) ? null : description.Trim());
    }
}
