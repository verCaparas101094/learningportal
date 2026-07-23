using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Courses.Commands.CreateCourse;

/// <summary>Creates an authorized Draft course.</summary>
public sealed class CreateCourseCommandHandler(
    ICourseRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IUserManagementService userManagementService,
    ILogger<CreateCourseCommandHandler> logger)
    : ICommandHandler<CreateCourseCommand, Result<CourseResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseResponse>> HandleAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<CourseResponse>.Failure(authorizationError);
        }

        var instructorId = await ResolveInstructorIdAsync(command.InstructorId, cancellationToken);
        if (instructorId is null)
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidInstructor());
        }

        var slug = SlugNormalizer.Normalize(command.Slug);
        if (await repository.SlugExistsAsync(slug, cancellationToken: cancellationToken))
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.DuplicateSlug());
        }

        var course = Course.Create(
            command.Title,
            slug,
            command.Description,
            command.Category,
            command.ThumbnailUrl,
            instructorId.Value);

        await repository.AddAsync(course, cancellationToken);
        var persistenceError = await CoursePersistence.SaveAsync(unitOfWork, cancellationToken);
        if (persistenceError is not null)
        {
            return Result<CourseResponse>.Failure(persistenceError);
        }

        logger.LogInformation(
            "Created course {CourseId} for instructor {InstructorId}.",
            course.Id,
            course.InstructorId);
        return Result<CourseResponse>.Success(course.ToResponse());
    }

    private async Task<Guid?> ResolveInstructorIdAsync(
        Guid? requestedInstructorId,
        CancellationToken cancellationToken)
    {
        if (!CourseAuthorization.IsAdministrator(currentUser))
        {
            return currentUser.UserId;
        }

        if (requestedInstructorId is null || requestedInstructorId == Guid.Empty)
        {
            return null;
        }

        var userResult = await userManagementService.GetUserByIdAsync(
            requestedInstructorId.Value,
            cancellationToken);

        return userResult.IsSuccess
               && userResult.Value.IsEnabled
               && userResult.Value.Roles.Contains(
                   Authorization.ApplicationRoles.Instructor,
                   StringComparer.OrdinalIgnoreCase)
            ? requestedInstructorId
            : null;
    }
}
