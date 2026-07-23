using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.ArchiveCourse;

/// <summary>Archives an authorized Published course.</summary>
public sealed class ArchiveCourseCommandHandler(
    ICourseRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<ArchiveCourseCommandHandler> logger)
    : ICommandHandler<ArchiveCourseCommand, Result<CourseResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseResponse>> HandleAsync(
        ArchiveCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<CourseResponse>.Failure(authorizationError);
        }

        var course = await repository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.NotFound(command.CourseId));
        }

        if (!CourseAuthorization.CanAccess(currentUser, course))
        {
            return Result<CourseResponse>.Failure(Errors.Authorization.Forbidden());
        }

        if (course.Status == CourseStatus.Archived)
        {
            return Result<CourseResponse>.Success(course.ToResponse());
        }

        if (!course.TryArchive())
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidState("archived"));
        }

        var persistenceError = await CoursePersistence.SaveAsync(unitOfWork, cancellationToken);
        if (persistenceError is not null)
        {
            return Result<CourseResponse>.Failure(persistenceError);
        }

        logger.LogInformation("Archived course {CourseId}.", course.Id);
        return Result<CourseResponse>.Success(course.ToResponse());
    }
}
