using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetCourseEnrollments;

/// <summary>Returns students for an administrator or owning instructor.</summary>
public sealed class GetCourseEnrollmentsQueryHandler(
    ICourseRepository courses, IEnrollmentRepository enrollments, IUserManagementService users,
    ICurrentUserService currentUser, IValidator<GetCourseEnrollmentsQuery> validator)
    : IQueryHandler<GetCourseEnrollmentsQuery, Result<PagedEnrollmentsResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedEnrollmentsResponse>> HandleAsync(
        GetCourseEnrollmentsQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<PagedEnrollmentsResponse>.Failure(Errors.Validation.Failed(
                string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct())));
        var course = await courses.GetByIdReadOnlyAsync(query.CourseId, cancellationToken);
        if (course is null) return Result<PagedEnrollmentsResponse>.Failure(Errors.CourseManagement.NotFound(query.CourseId));
        if (!currentUser.HasRole(ApplicationRoles.Administrator)
            && (!currentUser.HasRole(ApplicationRoles.Instructor) || course.InstructorId != currentUser.UserId))
            return Result<PagedEnrollmentsResponse>.Failure(Errors.Authorization.Forbidden());

        EnrollmentStatus? status = string.IsNullOrWhiteSpace(query.Status) ? null
            : Enum.Parse<EnrollmentStatus>(query.Status, true);
        var (items, totalCount) = await enrollments.GetCoursePageAsync(
            query.CourseId, status, query.Search, query.PageNumber, query.PageSize, cancellationToken);
        var userMap = await users.GetUsersByIdsAsync(items.Select(x => x.StudentId).Distinct().ToArray(), cancellationToken);
        var responseItems = items.Select(x =>
        {
            userMap.TryGetValue(x.StudentId, out var user);
            return new EnrollmentListItemResponse(
                x.Id, x.CourseId, course.Title, course.Slug, x.StudentId,
                user?.DisplayName ?? "Employee", user?.Email ?? string.Empty,
                x.Status.ToString(), x.EnrolledAtUtc);
        }).ToArray();
        return Result<PagedEnrollmentsResponse>.Success(new(
            responseItems, query.PageNumber, query.PageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)));
    }
}
