using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetMyEnrollments;

/// <summary>Requests the current employee's learning page.</summary>
public sealed record GetMyEnrollmentsQuery(
    string? Search, string? Status, int PageNumber, int PageSize) : IQuery<Result<PagedMyLearningResponse>>;
