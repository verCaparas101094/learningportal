using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Courses.Commands.ArchiveCourse;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Commands.DeleteCourse;
using LearningPortal.Application.Courses.Commands.PublishCourse;
using LearningPortal.Application.Courses.Commands.UpdateCourse;
using LearningPortal.Application.Courses.Queries.GetCourseById;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps administrator and instructor course-management endpoints.</summary>
public static class CourseEndpoints
{
    /// <summary>Maps course-management routes.</summary>
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/courses")
            .WithTags("Course Management")
            .RequireAdminOrInstructor();

        group.MapGet("/", GetCoursesAsync)
            .WithName("GetCourses")
            .Produces<PagedCoursesResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{courseId:guid}", GetCourseByIdAsync)
            .WithName("GetCourseById")
            .Produces<CourseResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateCourseAsync)
            .WithName("CreateCourse")
            .Produces<CourseResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{courseId:guid}", UpdateCourseAsync)
            .WithName("UpdateCourse")
            .Produces<CourseResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{courseId:guid}/publish", PublishCourseAsync)
            .WithName("PublishCourse")
            .Produces<CourseResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{courseId:guid}/archive", ArchiveCourseAsync)
            .WithName("ArchiveCourse")
            .Produces<CourseResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{courseId:guid}", DeleteCourseAsync)
            .WithName("DeleteCourse")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetCoursesAsync(
        [AsParameters] GetCoursesRequest request,
        IQueryHandler<GetCoursesQuery, Result<PagedCoursesResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCoursesQuery(
                request.Search,
                request.Status,
                request.PageNumber,
                request.PageSize),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetCourseByIdAsync(
        Guid courseId,
        IQueryHandler<GetCourseByIdQuery, Result<CourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCourseByIdQuery(courseId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateCourseAsync(
        CreateCourseRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<CreateCourseCommand, CourseResponse>(
            new CreateCourseCommand(
                request.Title,
                request.Slug,
                request.Description,
                request.Category,
                request.ThumbnailUrl,
                request.InstructorId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/courses/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> UpdateCourseAsync(
        Guid courseId,
        UpdateCourseRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<UpdateCourseCommand, CourseResponse>(
            new UpdateCourseCommand(
                courseId,
                request.Title,
                request.Slug,
                request.Description,
                request.Category,
                request.ThumbnailUrl,
                request.RowVersion),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> PublishCourseAsync(
        Guid courseId,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<PublishCourseCommand, CourseResponse>(
            new PublishCourseCommand(courseId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ArchiveCourseAsync(
        Guid courseId,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<ArchiveCourseCommand, CourseResponse>(
            new ArchiveCourseCommand(courseId),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteCourseAsync(
        Guid courseId,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<DeleteCourseCommand, bool>(
            new DeleteCourseCommand(courseId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
}
