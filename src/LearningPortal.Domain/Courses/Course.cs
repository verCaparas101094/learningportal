using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Courses;

/// <summary>Represents a learning course and protects its core invariants.</summary>
public sealed class Course : Entity
{
    private Course()
    {
    }

    private Course(string title, string description)
    {
        Title = title;
        Description = description;
    }

    /// <summary>Gets the course title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the course description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Creates a course after enforcing domain-level invariants.</summary>
    public static Course Create(string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);

        return new Course(title.Trim(), description.Trim());
    }
}
