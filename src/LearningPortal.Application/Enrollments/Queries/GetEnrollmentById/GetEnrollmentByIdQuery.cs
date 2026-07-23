using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetEnrollmentById;

/// <summary>Requests one authorized enrollment.</summary>
public sealed record GetEnrollmentByIdQuery(Guid EnrollmentId) : IQuery<Result<EnrollmentResponse>>;
