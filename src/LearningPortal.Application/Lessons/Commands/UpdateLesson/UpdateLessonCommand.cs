#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.UpdateLesson;
public sealed record UpdateLessonCommand(Guid LessonId, string Title, string Description, string Content, int Order,
    int EstimatedMinutes, string LessonType, string RowVersion) : ICommand<Result<LessonResponse>>;
