#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Learning;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Learning;
public sealed record AccessLessonCommand(Guid EnrollmentId, Guid LessonId) : ICommand<Result<CourseProgressResponse>>;
public sealed record CompleteLessonCommand(Guid EnrollmentId, Guid LessonId) : ICommand<Result<CompleteLessonResponse>>;
public sealed class AccessLessonCommandHandler(IEnrollmentRepository enrollments, ICourseRepository courses, ILessonRepository lessons, ILessonProgressRepository progress, IUnitOfWork unit, ICurrentUserService user, ISystemClock clock) : ICommandHandler<AccessLessonCommand, Result<CourseProgressResponse>>
{
    public async Task<Result<CourseProgressResponse>> HandleAsync(AccessLessonCommand command, CancellationToken ct = default)
    {
        var loaded = await LearningCommandSupport.LoadAsync(command.EnrollmentId, command.LessonId, enrollments, courses, lessons, user, ct);
        if (loaded.Error is not null) return Result<CourseProgressResponse>.Failure(loaded.Error);
        var (enrollment, published) = loaded.Value!.Value;
        var record = await progress.GetByEnrollmentAndLessonAsync(enrollment.Id, command.LessonId, ct);
        if (record is null) { record = LessonProgress.Start(enrollment.Id, command.LessonId, enrollment.StudentId, clock.UtcNow); await progress.AddAsync(record, ct); } else record.Access(clock.UtcNow);
        enrollment.TryStart(clock.UtcNow);
        await unit.SaveChangesAsync(ct);
        var all = (await progress.GetByEnrollmentAsync(enrollment.Id, ct)).Append(record).GroupBy(x => x.LessonId).Select(x => x.Last()).ToArray();
        return Result<CourseProgressResponse>.Success(LearningMappings.ToCourseProgress(enrollment, published, all));
    }
}
public sealed class CompleteLessonCommandHandler(IEnrollmentRepository enrollments, ICourseRepository courses, ILessonRepository lessons, ILessonProgressRepository progress, IUnitOfWork unit, ICurrentUserService user, ISystemClock clock) : ICommandHandler<CompleteLessonCommand, Result<CompleteLessonResponse>>
{
    public async Task<Result<CompleteLessonResponse>> HandleAsync(CompleteLessonCommand command, CancellationToken ct = default)
    {
        var loaded = await LearningCommandSupport.LoadAsync(command.EnrollmentId, command.LessonId, enrollments, courses, lessons, user, ct);
        if (loaded.Error is not null) return Result<CompleteLessonResponse>.Failure(loaded.Error);
        var (enrollment, published) = loaded.Value!.Value;
        var record = await progress.GetByEnrollmentAndLessonAsync(enrollment.Id, command.LessonId, ct);
        if (record is null) { record = LessonProgress.Start(enrollment.Id, command.LessonId, enrollment.StudentId, clock.UtcNow); await progress.AddAsync(record, ct); }
        record.Complete(clock.UtcNow);
        var all = (await progress.GetByEnrollmentAsync(enrollment.Id, ct)).Append(record).GroupBy(x => x.LessonId).Select(x => x.Last()).ToArray();
        if (published.Count > 0 && published.All(x => all.Any(p => p.LessonId == x.Id && p.Status == LessonProgressStatus.Completed))) enrollment.TryComplete(clock.UtcNow);
        await unit.SaveChangesAsync(ct);
        var summary = LearningMappings.ToCourseProgress(enrollment, published, all);
        var idx = published.ToList().FindIndex(x => x.Id == command.LessonId);
        return Result<CompleteLessonResponse>.Success(new(record.ToResponse(), summary, idx >= 0 && idx < published.Count - 1 ? published[idx + 1].Id : null));
    }
}
internal static class LearningCommandSupport
{
    internal static async Task<(Error? Error, (Enrollment Enrollment, IReadOnlyList<Domain.Lessons.Lesson> Lessons)? Value)> LoadAsync(Guid enrollmentId, Guid lessonId, IEnrollmentRepository enrollments, ICourseRepository courses, ILessonRepository lessons, ICurrentUserService user, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is not Guid student) return (Errors.Authentication.Unauthorized(), null);
        var enrollment = await enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null) return (Errors.Enrollment.NotFound(enrollmentId), null);
        if (enrollment.StudentId != student) return (Errors.Authorization.Forbidden(), null);
        if (enrollment.Status == EnrollmentStatus.Withdrawn) return (Errors.Enrollment.InvalidState(), null);
        var course = await courses.GetByIdReadOnlyAsync(enrollment.CourseId, ct);
        if (course is null || course.Status != CourseStatus.Published) return (Errors.Authorization.Forbidden(), null);
        var published = await lessons.GetPublishedByCourseAsync(course.Id, ct);
        if (!published.Any(x => x.Id == lessonId)) return (Errors.LessonManagement.NotFound(lessonId), null);
        return (null, (enrollment, published));
    }
}
