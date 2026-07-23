namespace LearningPortal.Domain.Courses;

/// <summary>Defines the supported course lifecycle states.</summary>
public enum CourseStatus
{
    /// <summary>The course is editable and not publicly available.</summary>
    Draft = 0,

    /// <summary>The course is published and no longer editable.</summary>
    Published = 1,

    /// <summary>The previously published course is archived.</summary>
    Archived = 2
}
