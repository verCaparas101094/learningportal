using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetMyEnrollments;

/// <summary>Builds the current employee's enrollment page.</summary>
public sealed class GetMyEnrollmentsQueryHandler(
    IEnrollmentRepository enrollments, ICourseRepository courses, ILessonRepository lessons,
    ICurrentUserService currentUser, IValidator<GetMyEnrollmentsQuery> validator)
    : IQueryHandler<GetMyEnrollmentsQuery, Result<PagedMyLearningResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedMyLearningResponse>> HandleAsync(
        GetMyEnrollmentsQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<PagedMyLearningResponse>.Failure(Errors.Validation.Failed(
                string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct())));
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid studentId)
            return Result<PagedMyLearningResponse>.Failure(Errors.Authentication.Unauthorized());

        EnrollmentStatus? status = string.IsNullOrWhiteSpace(query.Status) ? null
            : Enum.Parse<EnrollmentStatus>(query.Status, true);
        var (items, totalCount) = await enrollments.GetStudentPageAsync(
            studentId, status, query.Search, query.PageNumber, query.PageSize, cancellationToken);
        var courseMap = (await courses.GetByIdsReadOnlyAsync(items.Select(x => x.CourseId).Distinct().ToArray(), cancellationToken))
            .Where(x => x.Status == CourseStatus.Published)
            .ToDictionary(x => x.Id);
        var lessonMap = (await lessons.GetPublishedByCoursesAsync(courseMap.Keys.ToArray(), cancellationToken))
            .GroupBy(x => x.CourseId).ToDictionary(x => x.Key, x => x.ToArray());
        var responseItems = items.Where(x => courseMap.ContainsKey(x.CourseId)).Select(enrollment =>
        {
            var course = courseMap[enrollment.CourseId];
            var courseLessons = lessonMap.GetValueOrDefault(course.Id) ?? [];
            return new MyLearningItemResponse(
                enrollment.Id, course.Id, course.Title, course.Slug,
                EnrollmentMappings.Summary(course.Description), enrollment.Status.ToString(),
                enrollment.EnrolledAtUtc, courseLessons.Sum(x => x.EstimatedMinutes), courseLessons.Length,
                enrollment.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress);
        }).ToArray();
        return Result<PagedMyLearningResponse>.Success(new(
            responseItems, query.PageNumber, query.PageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)));
    }
}
