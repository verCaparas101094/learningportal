#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Learning;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Learning;
public sealed record GetCourseProgressQuery(Guid EnrollmentId) : IQuery<Result<CourseProgressResponse>>;
public sealed record GetContinueLearningDestinationQuery(Guid EnrollmentId) : IQuery<Result<ContinueLearningDestinationResponse?>>;
public sealed class GetCourseProgressQueryHandler(IEnrollmentRepository enrollments, ILessonRepository lessons, ILessonProgressRepository progress, ICurrentUserService user) : IQueryHandler<GetCourseProgressQuery, Result<CourseProgressResponse>>
{
    public async Task<Result<CourseProgressResponse>> HandleAsync(GetCourseProgressQuery query, CancellationToken ct = default)
    {
        var loaded = await LearningProgressSupport.LoadAsync(query.EnrollmentId, enrollments, lessons, progress, user, ct);
        return loaded.Error is not null ? Result<CourseProgressResponse>.Failure(loaded.Error) : Result<CourseProgressResponse>.Success(loaded.Value!.Summary);
    }
}
public sealed class GetContinueLearningDestinationQueryHandler(IEnrollmentRepository enrollments, ICourseRepository courses, ILessonRepository lessons, ILessonProgressRepository progress, ICurrentUserService user) : IQueryHandler<GetContinueLearningDestinationQuery, Result<ContinueLearningDestinationResponse?>>
{
    public async Task<Result<ContinueLearningDestinationResponse?>> HandleAsync(GetContinueLearningDestinationQuery query, CancellationToken ct = default)
    {
        var loaded = await LearningProgressSupport.LoadAsync(query.EnrollmentId, enrollments, lessons, progress, user, ct);
        if (loaded.Error is not null) return Result<ContinueLearningDestinationResponse?>.Failure(loaded.Error);
        var course = await courses.GetByIdReadOnlyAsync(loaded.Value!.Enrollment.CourseId, ct);
        if (course is null) return Result<ContinueLearningDestinationResponse?>.Failure(Errors.Common.NotFound("Course", loaded.Value.Enrollment.CourseId.ToString()));
        var records = loaded.Value.Progress.ToDictionary(x => x.LessonId);
        var lesson = loaded.Value.Lessons.FirstOrDefault(x => records.GetValueOrDefault(x.Id)?.Status == LessonProgressStatus.InProgress)
            ?? loaded.Value.Lessons.FirstOrDefault(x => !records.ContainsKey(x.Id) || records[x.Id].Status == LessonProgressStatus.NotStarted)
            ?? (loaded.Value.Enrollment.Status == EnrollmentStatus.Completed ? loaded.Value.Lessons.LastOrDefault() : null);
        return Result<ContinueLearningDestinationResponse?>.Success(lesson is null ? null : new(query.EnrollmentId, course.Slug, lesson.Id, lesson.Title));
    }
}
internal static class LearningProgressSupport
{
    internal sealed record Loaded(Enrollment Enrollment, IReadOnlyList<Domain.Lessons.Lesson> Lessons, IReadOnlyList<LessonProgress> Progress, CourseProgressResponse Summary);
    internal static async Task<(Error? Error, Loaded? Value)> LoadAsync(Guid enrollmentId, IEnrollmentRepository enrollments, ILessonRepository lessons, ILessonProgressRepository progress, ICurrentUserService user, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is not Guid student) return (Errors.Authentication.Unauthorized(), null);
        var enrollment = await enrollments.GetByIdReadOnlyAsync(enrollmentId, ct);
        if (enrollment is null) return (Errors.Enrollment.NotFound(enrollmentId), null);
        if (enrollment.StudentId != student) return (Errors.Authorization.Forbidden(), null);
        var published = await lessons.GetPublishedByCourseAsync(enrollment.CourseId, ct);
        var records = await progress.GetByEnrollmentAsync(enrollmentId, ct);
        return (null, new(enrollment, published, records, LearningMappings.ToCourseProgress(enrollment, published, records)));
    }
}
