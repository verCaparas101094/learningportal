using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetEnrollmentById;

/// <summary>Enforces enrollment ownership and course ownership.</summary>
public sealed class GetEnrollmentByIdQueryHandler(
    IEnrollmentRepository enrollments, ICourseRepository courses, ICurrentUserService currentUser)
    : IQueryHandler<GetEnrollmentByIdQuery, Result<EnrollmentResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EnrollmentResponse>> HandleAsync(
        GetEnrollmentByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
            return Result<EnrollmentResponse>.Failure(Errors.Authentication.Unauthorized());

        var enrollment = await enrollments.GetByIdReadOnlyAsync(query.EnrollmentId, cancellationToken);
        if (enrollment is null) return Result<EnrollmentResponse>.Failure(Errors.Enrollment.NotFound(query.EnrollmentId));
        var allowed = userId == enrollment.StudentId || currentUser.HasRole(ApplicationRoles.Administrator);
        if (!allowed && currentUser.HasRole(ApplicationRoles.Instructor))
        {
            var course = await courses.GetByIdReadOnlyAsync(enrollment.CourseId, cancellationToken);
            allowed = course?.InstructorId == userId;
        }
        return allowed
            ? Result<EnrollmentResponse>.Success(enrollment.ToResponse())
            : Result<EnrollmentResponse>.Failure(Errors.Authorization.Forbidden());
    }
}
