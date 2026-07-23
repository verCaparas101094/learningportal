using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourses;

/// <summary>Returns an ownership-filtered course page.</summary>
public sealed class GetCoursesQueryHandler(
    ICourseRepository repository,
    ICurrentUserService currentUser,
    IValidator<GetCoursesQuery> validator)
    : IQueryHandler<GetCoursesQuery, Result<PagedCoursesResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedCoursesResponse>> HandleAsync(
        GetCoursesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<PagedCoursesResponse>.Failure(Errors.Validation.Failed(
                string.Join(
                    " ",
                    validation.Errors.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal))));
        }

        var authorizationError = CourseAuthorization.ValidateManager(currentUser);
        if (authorizationError is not null)
        {
            return Result<PagedCoursesResponse>.Failure(authorizationError);
        }

        CourseStatus? status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : Enum.Parse<CourseStatus>(query.Status, true);
        var instructorId = CourseAuthorization.IsAdministrator(currentUser)
            ? null
            : currentUser.UserId;

        var (items, totalCount) = await repository.GetPageAsync(
            query.Search,
            status,
            instructorId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Result<PagedCoursesResponse>.Success(new PagedCoursesResponse(
            items.Select(course => course.ToListItem()).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)));
    }
}
