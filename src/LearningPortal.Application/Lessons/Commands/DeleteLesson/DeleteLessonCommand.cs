#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.DeleteLesson;
public sealed record DeleteLessonCommand(Guid LessonId) : ICommand<Result<bool>>;
