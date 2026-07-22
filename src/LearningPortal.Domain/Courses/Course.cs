using LearningPortal.Domain.Common;
using LearningPortal.Domain.Courses.Events;

namespace LearningPortal.Domain.Courses;

/// <summary>Represents a learning course and protects its core invariants.</summary>
public sealed class Course : AuditableEntity, ISoftDelete
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

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; private set; }

    /// <summary>Creates a course after enforcing domain-level invariants.</summary>
    public static Course Create(string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);

        var course = new Course(title.Trim(), description.Trim());
        course.AddDomainEvent(new CourseCreatedDomainEvent(course.Id, course.Title));

        return course;
    }
}
