using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Courses;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Lessons;

internal static class LessonSupport
{
    public static Error? Authorize(ICurrentUserService user, Course course) =>
        CourseAuthorization.ValidateManager(user) ?? (CourseAuthorization.CanAccess(user, course) ? null : Errors.Authorization.Forbidden());
    public static LessonResponse ToResponse(this Lesson x) => new(x.Id, x.CourseId, x.Title, x.Description, x.Content,
        x.Order, x.EstimatedMinutes, x.LessonType.ToString(), x.Status.ToString(), x.CreatedAtUtc, x.UpdatedAtUtc,
        Convert.ToBase64String(x.RowVersion));
    public static LessonListItemResponse ToListItem(this Lesson x) => new(x.Id, x.CourseId, x.Title, x.Order,
        x.EstimatedMinutes, x.LessonType.ToString(), x.Status.ToString(), Convert.ToBase64String(x.RowVersion));
    public static async Task<Error?> SaveAsync(IUnitOfWork unit, CancellationToken ct)
    {
        try { await unit.SaveChangesAsync(ct); return null; }
        catch (LessonConcurrencyException) { return Errors.LessonManagement.ConcurrencyConflict(); }
        catch (DuplicateLessonOrderException) { return Errors.LessonManagement.DuplicateOrder(); }
    }
    public static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length > 0; }
        catch (FormatException) { bytes = []; return false; }
    }
}
