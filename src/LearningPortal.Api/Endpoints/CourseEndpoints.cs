using FluentValidation;
using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps course catalog endpoints to CQRS handlers.</summary>
public static class CourseEndpoints
{
    /// <summary>Maps authenticated course endpoints.</summary>
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/courses")
            .WithTags("Courses")
            .RequireAuthorization();

        group.MapGet("/", GetCoursesAsync)
            .WithName("GetCourses")
            .WithSummary("Returns the course catalog.")
            .Produces<IReadOnlyList<CourseDto>>();

        group.MapPost("/", CreateCourseAsync)
            .WithName("CreateCourse")
            .WithSummary("Creates a course.")
            .Produces<CourseDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> GetCoursesAsync(
        IQueryHandler<GetCoursesQuery, Result<IReadOnlyList<CourseDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCoursesQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateCourseAsync(
        CreateCourseRequest request,
        IValidator<CreateCourseCommand> validator,
        ICommandHandler<CreateCourseCommand, Result<CourseDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourseCommand(request.Title, request.Description);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/courses/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }
}
