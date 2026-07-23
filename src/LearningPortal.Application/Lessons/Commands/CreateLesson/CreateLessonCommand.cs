#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Commands.CreateLesson;
public sealed record CreateLessonCommand(Guid CourseId, string Title, string Description, string Content,
    int Order, int EstimatedMinutes, string LessonType) : ICommand<Result<LessonResponse>>;
