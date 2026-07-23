using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Enrollments.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Enrollments.Commands.EnrollInCourse;

/// <summary>Creates employee enrollments.</summary>
public sealed class EnrollInCourseCommandHandler(
    ICourseRepository courses, IEnrollmentRepository enrollments, IUnitOfWork unitOfWork,
    ICurrentUserService currentUser, ISystemClock clock, ILogger<EnrollInCourseCommandHandler> logger)
    : ICommandHandler<EnrollInCourseCommand, Result<EnrollmentResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EnrollmentResponse>> HandleAsync(
        EnrollInCourseCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid studentId)
            return Result<EnrollmentResponse>.Failure(Errors.Authentication.Unauthorized());

        var course = await courses.GetByIdReadOnlyAsync(command.CourseId, cancellationToken);
        if (course is null) return Result<EnrollmentResponse>.Failure(Errors.CourseManagement.NotFound(command.CourseId));
        if (course.Status != CourseStatus.Published)
            return Result<EnrollmentResponse>.Failure(Errors.Enrollment.CourseNotPublished());
        if (await enrollments.GetByCourseAndStudentAsync(command.CourseId, studentId, cancellationToken) is not null)
            return Result<EnrollmentResponse>.Failure(Errors.Enrollment.Duplicate());

        var enrollment = Enrollment.Create(command.CourseId, studentId, clock.UtcNow);
        await enrollments.AddAsync(enrollment, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateActiveEnrollmentException)
        {
            return Result<EnrollmentResponse>.Failure(Errors.Enrollment.Duplicate());
        }

        logger.LogInformation("Student {StudentId} enrolled in course {CourseId}.", studentId, command.CourseId);
        return Result<EnrollmentResponse>.Success(enrollment.ToResponse());
    }
}
