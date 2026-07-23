using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetCourseEnrollments;

/// <summary>Requests an authorized course enrollment page.</summary>
public sealed record GetCourseEnrollmentsQuery(
    Guid CourseId, string? Search, string? Status, int PageNumber, int PageSize)
    : IQuery<Result<PagedEnrollmentsResponse>>;
