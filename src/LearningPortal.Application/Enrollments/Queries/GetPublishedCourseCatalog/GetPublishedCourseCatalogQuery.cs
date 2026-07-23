using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetPublishedCourseCatalog;

/// <summary>Requests the published employee catalog.</summary>
public sealed record GetPublishedCourseCatalogQuery(
    string? Search, int PageNumber, int PageSize) : IQuery<Result<PagedCourseCatalogResponse>>;
