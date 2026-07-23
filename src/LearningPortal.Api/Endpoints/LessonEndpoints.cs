using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Lessons.Commands.CreateLesson;
using LearningPortal.Application.Lessons.Commands.DeleteLesson;
using LearningPortal.Application.Lessons.Commands.MoveLesson;
using LearningPortal.Application.Lessons.Commands.PublishLesson;
using LearningPortal.Application.Lessons.Commands.UpdateLesson;
using LearningPortal.Application.Lessons.Queries.GetLessonById;
using LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
using LearningPortal.Application.Lessons;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps lesson-management endpoints.</summary>
public static class LessonEndpoints
{
    /// <summary>Maps authorized lesson routes.</summary>
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/courses/{courseId:guid}/lessons", GetByCourseAsync).RequireAdminOrInstructor();
        endpoints.MapPost("/api/courses/{courseId:guid}/lessons", CreateAsync).RequireAdminOrInstructor();
        endpoints.MapGet("/api/lessons/{lessonId:guid}", GetAsync).RequireAdminOrInstructor();
        endpoints.MapPut("/api/lessons/{lessonId:guid}", UpdateAsync).RequireAdminOrInstructor();
        endpoints.MapPut("/api/lessons/{lessonId:guid}/publish", PublishAsync).RequireAdminOrInstructor();
        endpoints.MapPut("/api/lessons/{lessonId:guid}/move", MoveAsync).RequireAdminOrInstructor();
        endpoints.MapDelete("/api/lessons/{lessonId:guid}", DeleteAsync).RequireAdminOrInstructor();
        endpoints.MapPost("/api/lessons/preview", Preview).RequireAdminOrInstructor();
        return endpoints;
    }
    private static async Task<IResult> GetByCourseAsync(Guid courseId, [AsParameters] GetLessonsRequest r,
        IQueryHandler<GetLessonsByCourseQuery, Result<PagedLessonsResponse>> h, CancellationToken ct) =>
        (await h.HandleAsync(new(courseId, r.Search, r.PageNumber, r.PageSize), ct)).ToHttpResult();
    private static async Task<IResult> GetAsync(Guid lessonId,
        IQueryHandler<GetLessonByIdQuery, Result<LessonResponse>> h, CancellationToken ct) =>
        (await h.HandleAsync(new(lessonId), ct)).ToHttpResult();
    private static async Task<IResult> CreateAsync(Guid courseId, CreateLessonRequest r, ICommandDispatcher d, CancellationToken ct)
    {
        var result = await d.SendAsync<CreateLessonCommand, LessonResponse>(
            new(courseId, r.Title, r.Description, r.Order, r.EstimatedMinutes, r.LessonType,
                r.MarkdownContent, r.ExternalUrl), ct);
        return result.IsSuccess ? Results.Created($"/api/lessons/{result.Value.Id}", result.Value) : result.Error!.ToProblem();
    }
    private static async Task<IResult> UpdateAsync(Guid lessonId, UpdateLessonRequest r, ICommandDispatcher d, CancellationToken ct) =>
        (await d.SendAsync<UpdateLessonCommand, LessonResponse>(
            new(lessonId, r.Title, r.Description, r.Order, r.EstimatedMinutes, r.LessonType,
                r.MarkdownContent, r.ExternalUrl, r.RowVersion), ct)).ToHttpResult();
    private static async Task<IResult> PublishAsync(Guid lessonId, ICommandDispatcher d, CancellationToken ct) =>
        (await d.SendAsync<PublishLessonCommand, LessonResponse>(new(lessonId), ct)).ToHttpResult();
    private static async Task<IResult> MoveAsync(Guid lessonId, MoveLessonRequest r, ICommandDispatcher d, CancellationToken ct) =>
        (await d.SendAsync<MoveLessonCommand, LessonResponse>(new(lessonId, r.NewOrder, r.RowVersion), ct)).ToHttpResult();
    private static async Task<IResult> DeleteAsync(Guid lessonId, ICommandDispatcher d, CancellationToken ct)
    {
        var result = await d.SendAsync<DeleteLessonCommand, bool>(new(lessonId), ct);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
    private static IResult Preview(LessonContentPreviewRequest request, ILessonContentPreviewService service) =>
        service.Preview(request).ToHttpResult();
}
