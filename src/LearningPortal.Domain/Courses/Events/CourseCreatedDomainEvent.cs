using LearningPortal.Domain.Common.Events;

namespace LearningPortal.Domain.Courses.Events;

/// <summary>
/// Represents the domain fact that a course was created.
/// </summary>
public sealed record CourseCreatedDomainEvent : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CourseCreatedDomainEvent"/> class.
    /// </summary>
    /// <param name="courseId">The identifier of the created course.</param>
    /// <param name="title">The normalized title of the created course.</param>
    public CourseCreatedDomainEvent(Guid courseId, string title)
    {
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("A course identifier is required.", nameof(courseId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        CourseId = courseId;
        Title = title;
    }

    /// <summary>
    /// Gets the identifier of the created course.
    /// </summary>
    public Guid CourseId { get; }

    /// <summary>
    /// Gets the normalized title of the created course.
    /// </summary>
    public string Title { get; }
}
