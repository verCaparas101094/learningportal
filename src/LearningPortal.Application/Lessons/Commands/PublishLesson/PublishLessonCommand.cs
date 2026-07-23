#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.PublishLesson;
public sealed record PublishLessonCommand(Guid LessonId) : ICommand<Result<LessonResponse>>;
