using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.PublishCourse;

/// <summary>Publishes an authorized Draft course.</summary>
public sealed class PublishCourseCommandHandler(
    ICourseRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<PublishCourseCommandHandler> logger)
    : ICommandHandler<PublishCourseCommand, Result<CourseResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseResponse>> HandleAsync(
        PublishCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var courseResult = await GetAuthorizedCourseAsync(command.CourseId, cancellationToken);
        if (courseResult.IsFailure)
        {
            return Result<CourseResponse>.Failure(courseResult.Error!);
        }

        var course = courseResult.Value;
        if (course.Status == CourseStatus.Published)
        {
            return Result<CourseResponse>.Success(course.ToResponse());
        }

        if (!course.TryPublish())
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidState("published"));
        }

        var persistenceError = await CoursePersistence.SaveAsync(unitOfWork, cancellationToken);
        if (persistenceError is not null)
        {
            return Result<CourseResponse>.Failure(persistenceError);
        }

        logger.LogInformation("Published course {CourseId}.", course.Id);
        return Result<CourseResponse>.Success(course.ToResponse());
    }

    private async Task<Result<Course>> GetAuthorizedCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<Course>.Failure(authorizationError);
        }

        var course = await repository.GetByIdAsync(courseId, cancellationToken);
        if (course is null)
        {
            return Result<Course>.Failure(Errors.CourseManagement.NotFound(courseId));
        }

        return CourseAuthorization.CanAccess(currentUser, course)
            ? Result<Course>.Success(course)
            : Result<Course>.Failure(Errors.Authorization.Forbidden());
    }
}
