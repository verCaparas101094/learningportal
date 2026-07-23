using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetPublishedCourseDetails;

/// <summary>Builds employee-safe published course details.</summary>
public sealed class GetPublishedCourseDetailsQueryHandler(
    ICourseRepository courses, ILessonRepository lessons, IEnrollmentRepository enrollments,
    IUserManagementService users, ICurrentUserService currentUser)
    : IQueryHandler<GetPublishedCourseDetailsQuery, Result<CourseDetailsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseDetailsResponse>> HandleAsync(
        GetPublishedCourseDetailsQuery query, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid studentId)
            return Result<CourseDetailsResponse>.Failure(Errors.Authentication.Unauthorized());
        if (string.IsNullOrWhiteSpace(query.Slug))
            return Result<CourseDetailsResponse>.Failure(Errors.Validation.Required("slug"));

        var course = await courses.GetPublishedBySlugAsync(query.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (course is null)
            return Result<CourseDetailsResponse>.Failure(Errors.Common.NotFound("Course", query.Slug));
        var publishedLessons = await lessons.GetPublishedByCourseAsync(course.Id, cancellationToken);
        var enrollment = await enrollments.GetByCourseAndStudentAsync(course.Id, studentId, cancellationToken);
        var instructor = await users.GetUserByIdAsync(course.InstructorId, cancellationToken);
        var lessonItems = publishedLessons.Select(x => new PublishedLessonSummaryResponse(
            x.Id, x.Title, x.Order, x.EstimatedMinutes, x.LessonType.ToString())).ToArray();

        return Result<CourseDetailsResponse>.Success(new(
            course.Id, course.Title, course.Slug, course.Description, course.Category, course.ThumbnailUrl,
            instructor.IsSuccess ? instructor.Value.DisplayName : "Instructor",
            lessonItems.Sum(x => x.EstimatedMinutes), lessonItems.Length, lessonItems,
            enrollment?.Status.ToString(), enrollment?.Id, enrollment is null,
            enrollment?.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress));
    }
}
