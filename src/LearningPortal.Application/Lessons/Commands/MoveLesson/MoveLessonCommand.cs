#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.MoveLesson;
public sealed record MoveLessonCommand(Guid LessonId, int NewOrder, string RowVersion) : ICommand<Result<LessonResponse>>;
