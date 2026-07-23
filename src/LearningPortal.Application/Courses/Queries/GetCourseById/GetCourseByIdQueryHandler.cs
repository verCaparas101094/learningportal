using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourseById;

/// <summary>Returns one course when the caller may manage it.</summary>
public sealed class GetCourseByIdQueryHandler(
    ICourseRepository repository,
    ICurrentUserService currentUser)
    : IQueryHandler<GetCourseByIdQuery, Result<CourseResponse>>
{
    /// <inheritdoc />
    public async Task<Result<CourseResponse>> HandleAsync(
        GetCourseByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<CourseResponse>.Failure(authorizationError);
        }

        var course = await repository.GetByIdReadOnlyAsync(query.CourseId, cancellationToken);
        if (course is null)
        {
            return Result<CourseResponse>.Failure(Errors.CourseManagement.NotFound(query.CourseId));
        }

        return CourseAuthorization.CanAccess(currentUser, course)
            ? Result<CourseResponse>.Success(course.ToResponse())
            : Result<CourseResponse>.Failure(Errors.Authorization.Forbidden());
    }
}
