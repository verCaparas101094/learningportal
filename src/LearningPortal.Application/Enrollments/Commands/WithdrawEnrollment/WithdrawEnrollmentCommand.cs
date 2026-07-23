using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Commands.WithdrawEnrollment;

/// <summary>Withdraws an active enrollment.</summary>
public sealed record WithdrawEnrollmentCommand(Guid EnrollmentId, string RowVersion) : ICommand<Result<EnrollmentResponse>>;
