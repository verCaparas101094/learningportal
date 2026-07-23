using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Learning;
using LearningPortal.Shared.Learning;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps learner player and progress routes.</summary>
public static class LearningEndpoints
{
    /// <summary>Maps authenticated learner endpoints.</summary>
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/learning/courses/{courseSlug}/lessons/{lessonId:guid}", GetPlayerAsync).RequireAuthorization();
        endpoints.MapPost("/api/learning/enrollments/{enrollmentId:guid}/lessons/{lessonId:guid}/access", AccessAsync).RequireAuthorization();
        endpoints.MapPut("/api/learning/enrollments/{enrollmentId:guid}/lessons/{lessonId:guid}/complete", CompleteAsync).RequireAuthorization();
        endpoints.MapGet("/api/learning/enrollments/{enrollmentId:guid}/progress", GetProgressAsync).RequireAuthorization();
        endpoints.MapGet("/api/learning/enrollments/{enrollmentId:guid}/continue", GetContinueAsync).RequireAuthorization();
        return endpoints;
    }
    private static async Task<IResult> GetPlayerAsync(string courseSlug, Guid lessonId, IQueryHandler<GetLessonPlayerQuery, Result<LessonPlayerResponse>> handler, CancellationToken ct) => (await handler.HandleAsync(new(courseSlug, lessonId), ct)).ToHttpResult();
    private static async Task<IResult> AccessAsync(Guid enrollmentId, Guid lessonId, ICommandDispatcher dispatcher, CancellationToken ct) => (await dispatcher.SendAsync<AccessLessonCommand, CourseProgressResponse>(new(enrollmentId, lessonId), ct)).ToHttpResult();
    private static async Task<IResult> CompleteAsync(Guid enrollmentId, Guid lessonId, ICommandDispatcher dispatcher, CancellationToken ct) => (await dispatcher.SendAsync<CompleteLessonCommand, CompleteLessonResponse>(new(enrollmentId, lessonId), ct)).ToHttpResult();
    private static async Task<IResult> GetProgressAsync(Guid enrollmentId, IQueryHandler<GetCourseProgressQuery, Result<CourseProgressResponse>> handler, CancellationToken ct) => (await handler.HandleAsync(new(enrollmentId), ct)).ToHttpResult();
    private static async Task<IResult> GetContinueAsync(Guid enrollmentId, IQueryHandler<GetContinueLearningDestinationQuery, Result<ContinueLearningDestinationResponse?>> handler, CancellationToken ct) => (await handler.HandleAsync(new(enrollmentId), ct)).ToHttpResult();
}
