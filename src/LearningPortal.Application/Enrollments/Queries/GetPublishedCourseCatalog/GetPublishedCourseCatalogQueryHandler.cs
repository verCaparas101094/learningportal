using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetPublishedCourseCatalog;

/// <summary>Builds the published catalog without exposing lesson content.</summary>
public sealed class GetPublishedCourseCatalogQueryHandler(
    ICourseRepository courses, ILessonRepository lessons, IEnrollmentRepository enrollments,
    IUserManagementService users, ICurrentUserService currentUser,
    IValidator<GetPublishedCourseCatalogQuery> validator)
    : IQueryHandler<GetPublishedCourseCatalogQuery, Result<PagedCourseCatalogResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedCourseCatalogResponse>> HandleAsync(
        GetPublishedCourseCatalogQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<PagedCourseCatalogResponse>.Failure(Errors.Validation.Failed(
                string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct())));
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid studentId)
            return Result<PagedCourseCatalogResponse>.Failure(Errors.Authentication.Unauthorized());

        var (courseItems, totalCount) = await courses.GetPageAsync(
            query.Search, CourseStatus.Published, null, query.PageNumber, query.PageSize, cancellationToken);
        var courseIds = courseItems.Select(x => x.Id).ToArray();
        var lessonItems = await lessons.GetPublishedByCoursesAsync(courseIds, cancellationToken);
        var activeEnrollments = await enrollments.GetActiveByStudentAndCoursesAsync(
            studentId, courseIds, cancellationToken);
        var instructorMap = await users.GetUsersByIdsAsync(
            courseItems.Select(x => x.InstructorId).Distinct().ToArray(), cancellationToken);
        var enrollmentMap = activeEnrollments.ToDictionary(x => x.CourseId);
        var lessonMap = lessonItems.GroupBy(x => x.CourseId).ToDictionary(x => x.Key, x => x.ToArray());

        var items = courseItems.Select(course =>
        {
            enrollmentMap.TryGetValue(course.Id, out var enrollment);
            lessonMap.TryGetValue(course.Id, out var courseLessons);
            courseLessons ??= [];
            return new CourseCatalogItemResponse(
                course.Id, course.Title, course.Slug, EnrollmentMappings.Summary(course.Description),
                instructorMap.GetValueOrDefault(course.InstructorId)?.DisplayName ?? "Instructor",
                courseLessons.Sum(x => x.EstimatedMinutes), courseLessons.Length,
                enrollment?.Status.ToString(), enrollment?.Id, enrollment is not null,
                enrollment is null, enrollment?.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress);
        }).ToArray();

        return Result<PagedCourseCatalogResponse>.Success(new(
            items, query.PageNumber, query.PageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)));
    }
}
