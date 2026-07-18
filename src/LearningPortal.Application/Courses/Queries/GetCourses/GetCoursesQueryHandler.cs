using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses.Queries.GetCourses;

/// <summary>Projects course aggregates into transport-safe course models.</summary>
public sealed class GetCoursesQueryHandler(IRepository<Course> repository)
    : IQueryHandler<GetCoursesQuery, Result<IReadOnlyList<CourseDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CourseDto>>> HandleAsync(
        GetCoursesQuery query,
        CancellationToken cancellationToken = default)
    {
        var courses = await repository.ListAsync(cancellationToken);
        var models = courses
            .Select(course => new CourseDto(course.Id, course.Title, course.Description, course.CreatedAtUtc))
            .ToArray();

        return Result<IReadOnlyList<CourseDto>>.Success(models);
    }
}
