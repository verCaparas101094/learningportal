using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.UpdateCourse;

/// <summary>Updates an owned or administrator-managed Draft course.</summary>
public sealed class UpdateCourseCommandHandler(
    ICourseRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<UpdateCourseCommandHandler> logger)
    : ICommandHandler<UpdateCourseCommand, Result<CourseResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseResponse>> HandleAsync(
        UpdateCourseCommand command,
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

        if (course.Status != CourseStatus.Draft)
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidState("edited"));
        }

        var slug = SlugNormalizer.Normalize(command.Slug);
        if (await repository.SlugExistsAsync(slug, course.Id, cancellationToken))
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.DuplicateSlug());
        }

        repository.SetOriginalRowVersion(course, Convert.FromBase64String(command.RowVersion));
        if (!course.TryUpdate(
                command.Title,
                slug,
                command.Description,
                command.Category,
                command.ThumbnailUrl))
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidState("edited"));
        }

        var persistenceError = await CoursePersistence.SaveAsync(unitOfWork, cancellationToken);
        if (persistenceError is not null)
        {
            return Result<CourseResponse>.Failure(persistenceError);
        }

        logger.LogInformation("Updated course {CourseId}.", course.Id);
        return Result<CourseResponse>.Success(course.ToResponse());
    }
}
