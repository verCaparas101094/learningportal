using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.CreateCourse;

/// <summary>Creates and persists a valid course aggregate.</summary>
public sealed class CreateCourseCommandHandler(
    IRepository<Course> repository,
    IUnitOfWork unitOfWork,
    ILogger<CreateCourseCommandHandler> logger)
    : ICommandHandler<CreateCourseCommand, Result<CourseDto>>
{
    /// <inheritdoc />
    public async Task<Result<CourseDto>> HandleAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var course = Course.Create(command.Title, command.Description);
        await repository.AddAsync(course, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created course {CourseId} with title {CourseTitle}.", course.Id, course.Title);

        return Result<CourseDto>.Success(new CourseDto(
            course.Id,
            course.Title,
            course.Description,
            course.CreatedAtUtc));
    }
}
