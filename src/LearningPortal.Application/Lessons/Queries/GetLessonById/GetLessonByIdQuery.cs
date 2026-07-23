#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessonById;
public sealed record GetLessonByIdQuery(Guid LessonId) : IQuery<Result<LessonResponse>>;
