using LearningPortal.Domain.Courses;
using LearningPortal.Shared.Courses;

namespace LearningPortal.Application.Courses;

internal static class CourseMappings
{
    public static CourseResponse ToResponse(this Course course) => new(
        course.Id,
        course.Title,
        course.Slug,
        course.Description,
        course.Category,
        course.ThumbnailUrl,
        course.Status.ToString(),
        course.InstructorId,
        course.CreatedAtUtc,
        course.CreatedBy,
        course.UpdatedAtUtc,
        course.UpdatedBy,
        Convert.ToBase64String(course.RowVersion),
        course.SkillId);

    public static CourseListItemResponse ToListItem(this Course course) => new(
        course.Id,
        course.Title,
        course.Slug,
        course.Category,
        course.ThumbnailUrl,
        course.Status.ToString(),
        course.InstructorId,
        course.CreatedAtUtc,
        course.SkillId);
}
