#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessons;
public sealed record GetLessonsQuery(string? Search, int PageNumber, int PageSize) : IQuery<Result<PagedLessonsResponse>>;
