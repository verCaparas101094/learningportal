using LearningPortal.Domain.Common;
using LearningPortal.Domain.Courses.Events;

namespace LearningPortal.Domain.Courses;

/// <summary>Represents the course aggregate.</summary>
public sealed class Course : AuditableEntity, ISoftDelete
{
    private Course()
    {
    }

    private Course(
        string title,
        string slug,
        string description,
        string category,
        string? thumbnailUrl,
        Guid instructorId)
    {
        Title = title;
        Slug = slug;
        Description = description;
        Category = category;
        ThumbnailUrl = thumbnailUrl;
        InstructorId = instructorId;
        Status = CourseStatus.Draft;
    }

    /// <summary>Gets the title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the normalized unique slug.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the category.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Gets the optional thumbnail URL.</summary>
    public string? ThumbnailUrl { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public CourseStatus Status { get; private set; }

    /// <summary>Gets the assigned instructor identifier.</summary>
    public Guid InstructorId { get; private set; }
    /// <summary>Gets the stable skill identifier used for instructor qualification.</summary>
    public Guid? SkillId { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; private set; }

    /// <summary>Creates a Draft course.</summary>
    public static Course Create(
        string title,
        string slug,
        string description,
        string category,
        string? thumbnailUrl,
        Guid instructorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentOutOfRangeException.ThrowIfEqual(instructorId, Guid.Empty);

        var normalizedSlug = SlugNormalizer.Normalize(slug);
        if (normalizedSlug.Length is 0 or > 200
            || title.Trim().Length > 200
            || description.Length > 5_000
            || category.Trim().Length > 100
            || NormalizeOptional(thumbnailUrl)?.Length > 2_048)
        {
            throw new ArgumentException("Course values exceed the supported domain limits.");
        }

        var course = new Course(
            title.Trim(),
            normalizedSlug,
            description.Trim(),
            category.Trim(),
            NormalizeOptional(thumbnailUrl),
            instructorId);
        course.AddDomainEvent(new CourseCreatedDomainEvent(course.Id, course.Title));

        return course;
    }

    /// <summary>Updates editable Draft course details.</summary>
    public bool TryUpdate(
        string title,
        string slug,
        string description,
        string category,
        string? thumbnailUrl)
    {
        if (Status != CourseStatus.Draft
            || string.IsNullOrWhiteSpace(title)
            || title.Trim().Length > 200
            || description is null
            || description.Length > 5_000
            || string.IsNullOrWhiteSpace(category)
            || category.Trim().Length > 100
            || NormalizeOptional(thumbnailUrl)?.Length > 2_048)
        {
            return false;
        }

        var normalizedSlug = SlugNormalizer.Normalize(slug);
        if (normalizedSlug.Length is 0 or > 200)
        {
            return false;
        }

        Title = title.Trim();
        Slug = normalizedSlug;
        Description = description.Trim();
        Category = category.Trim();
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        return true;
    }

    /// <summary>Publishes a Draft course and treats an already Published course as success.</summary>
    public bool TryPublish()
    {
        if (Status == CourseStatus.Published)
        {
            return true;
        }

        if (Status != CourseStatus.Draft)
        {
            return false;
        }

        Status = CourseStatus.Published;
        AddDomainEvent(new CoursePublishedDomainEvent(Id));
        return true;
    }

    /// <summary>Archives a Published course and treats an already Archived course as success.</summary>
    public bool TryArchive()
    {
        if (Status == CourseStatus.Archived)
        {
            return true;
        }

        if (Status != CourseStatus.Published)
        {
            return false;
        }

        Status = CourseStatus.Archived;
        AddDomainEvent(new CourseArchivedDomainEvent(Id));
        return true;
    }

    /// <summary>Associates the course with a stable skill.</summary>
    public bool TrySetSkill(Guid skillId)
    {
        if (skillId == Guid.Empty || Status != CourseStatus.Draft) return false;
        SkillId = skillId;
        return true;
    }

    /// <summary>Assigns an eligible instructor.</summary>
    public bool TryAssignInstructor(Guid instructorId)
    {
        if (instructorId == Guid.Empty || Status == CourseStatus.Archived) return false;
        InstructorId = instructorId;
        return true;
    }

    /// <summary>Prepares a Draft course for repository deletion.</summary>
    public bool TryDelete()
    {
        if (Status != CourseStatus.Draft)
        {
            return false;
        }

        AddDomainEvent(new CourseDeletedDomainEvent(Id));
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
