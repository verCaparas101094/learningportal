using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Enrollments.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Enrollments.Commands.WithdrawEnrollment;

/// <summary>Withdraws owned enrollments or administrator-selected enrollments.</summary>
public sealed class WithdrawEnrollmentCommandHandler(
    IEnrollmentRepository enrollments, IUnitOfWork unitOfWork, ICurrentUserService currentUser,
    ISystemClock clock, ILogger<WithdrawEnrollmentCommandHandler> logger)
    : ICommandHandler<WithdrawEnrollmentCommand, Result<EnrollmentResponse>>
{
    /// <inheritdoc />
    public async Task<Result<EnrollmentResponse>> HandleAsync(
        WithdrawEnrollmentCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
            return Result<EnrollmentResponse>.Failure(Errors.Authentication.Unauthorized());

        var enrollment = await enrollments.GetByIdAsync(command.EnrollmentId, cancellationToken);
        if (enrollment is null) return Result<EnrollmentResponse>.Failure(Errors.Enrollment.NotFound(command.EnrollmentId));
        var isAdministrator = currentUser.HasRole(ApplicationRoles.Administrator);
        if (userId != enrollment.StudentId && !isAdministrator)
            return Result<EnrollmentResponse>.Failure(Errors.Authorization.Forbidden());
        if (!enrollment.TryWithdraw(clock.UtcNow))
            return Result<EnrollmentResponse>.Failure(Errors.Enrollment.InvalidState());

        enrollments.SetOriginalRowVersion(enrollment, Convert.FromBase64String(command.RowVersion));
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (EnrollmentConcurrencyException)
        {
            return Result<EnrollmentResponse>.Failure(Errors.Enrollment.ConcurrencyConflict());
        }

        logger.LogInformation("Enrollment {EnrollmentId} was withdrawn by {UserId}.", enrollment.Id, userId);
        return Result<EnrollmentResponse>.Success(enrollment.ToResponse());
    }
}
