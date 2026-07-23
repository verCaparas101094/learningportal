using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.DeleteCourse;

/// <summary>Soft deletes an authorized Draft course.</summary>
public sealed class DeleteCourseCommandHandler(
    ICourseRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<DeleteCourseCommandHandler> logger)
    : ICommandHandler<DeleteCourseCommand, Result<bool>>
{
    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(
        DeleteCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<bool>.Failure(authorizationError);
        }

        var course = await repository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result<bool>.Failure(Errors.CourseManagement.NotFound(command.CourseId));
        }

        if (!CourseAuthorization.CanAccess(currentUser, course))
        {
            return Result<bool>.Failure(Errors.Authorization.Forbidden());
        }

        if (!course.TryDelete())
        {
            return Result<bool>.Failure(Errors.CourseManagement.InvalidState("deleted"));
        }

        repository.Remove(course);
        var persistenceError = await CoursePersistence.SaveAsync(unitOfWork, cancellationToken);
        if (persistenceError is not null)
        {
            return Result<bool>.Failure(persistenceError);
        }

        logger.LogInformation("Deleted course {CourseId}.", course.Id);
        return Result<bool>.Success(true);
    }
}
