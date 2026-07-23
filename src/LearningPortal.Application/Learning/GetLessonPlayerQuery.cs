#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Learning;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Learning;
public sealed record GetLessonPlayerQuery(string CourseSlug, Guid LessonId) : IQuery<Result<LessonPlayerResponse>>;
public sealed class GetLessonPlayerQueryHandler(ICourseRepository courses, ILessonRepository lessons, IEnrollmentRepository enrollments, ILessonProgressRepository progress, ICurrentUserService user, IMarkdownRenderer markdown, IVideoEmbedResolver videos) : IQueryHandler<GetLessonPlayerQuery, Result<LessonPlayerResponse>>
{
    public async Task<Result<LessonPlayerResponse>> HandleAsync(GetLessonPlayerQuery query, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is not Guid student) return Result<LessonPlayerResponse>.Failure(Errors.Authentication.Unauthorized());
        var course = await courses.GetPublishedBySlugAsync(query.CourseSlug.Trim().ToLowerInvariant(), ct);
        if (course is null) return Result<LessonPlayerResponse>.Failure(Errors.Common.NotFound("Course", query.CourseSlug));
        var enrollment = await enrollments.GetByCourseAndStudentAsync(course.Id, student, ct);
        if (enrollment is null || enrollment.Status == EnrollmentStatus.Withdrawn) return Result<LessonPlayerResponse>.Failure(Errors.Authorization.Forbidden());
        var published = await lessons.GetPublishedByCourseAsync(course.Id, ct);
        var current = published.SingleOrDefault(x => x.Id == query.LessonId);
        if (current is null) return Result<LessonPlayerResponse>.Failure(Errors.LessonManagement.NotFound(query.LessonId));
        var records = await progress.GetByEnrollmentAsync(enrollment.Id, ct);
        var map = records.ToDictionary(x => x.LessonId);
        var index = published.ToList().FindIndex(x => x.Id == current.Id);
        var content = LearningMappings.Content(current, markdown, videos);
        var summary = LearningMappings.ToCourseProgress(enrollment, published, records);
        return Result<LessonPlayerResponse>.Success(new(enrollment.Id, course.Id, course.Title, course.Slug, current.Id, current.Title, current.LessonType.ToString(), content.Html, content.Source, content.Embed, content.Direct,
            published.Select(x => new LearningLessonOutlineItemResponse(x.Id, x.Title, x.Order, x.EstimatedMinutes, x.LessonType.ToString(), map.GetValueOrDefault(x.Id)?.Status.ToString() ?? LessonProgressStatus.NotStarted.ToString(), x.Id == current.Id)).ToArray(),
            index > 0 ? published[index - 1].Id : null, index < published.Count - 1 ? published[index + 1].Id : null, summary));
    }
}
