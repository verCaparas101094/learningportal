using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.AiTutor;
using LearningPortal.Application.Authorization;
using LearningPortal.Shared.AiTutor;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps learner-owned local AI Tutor routes.</summary>
public static class AiTutorEndpoints
{
    /// <summary>Maps AI Tutor conversation and administrator health endpoints.</summary>
    public static IEndpointRouteBuilder MapAiTutorEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/api/ai-tutor/conversations")
            .RequireAuthorization();
        conversations.MapPost("/", StartAsync);
        conversations.MapGet("/", GetMineAsync);
        conversations.MapGet("/{conversationId:guid}", GetAsync);
        conversations.MapPost("/{conversationId:guid}/messages", SendAsync);
        conversations.MapPost("/{conversationId:guid}/archive", ArchiveAsync);

        endpoints.MapGet("/api/admin/ai-tutor/health", HealthAsync)
            .RequireAuthorization(Policies.AdminOnly);
        return endpoints;
    }

    private static async Task<IResult> StartAsync(
        StartAiTutorConversationRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync<StartAiTutorConversation, AiTutorConversationResponse>(
            new StartAiTutorConversation(request.CourseId, request.LessonId),
            cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetMineAsync(
        IQueryHandler<GetMyAiTutorConversations,
            Result<IReadOnlyList<AiTutorConversationListItemResponse>>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new GetMyAiTutorConversations(), cancellationToken))
            .ToHttpResult();

    private static async Task<IResult> GetAsync(
        Guid conversationId,
        IQueryHandler<GetAiTutorConversation, Result<AiTutorConversationResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new GetAiTutorConversation(conversationId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> SendAsync(
        Guid conversationId,
        SendAiTutorMessageRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync<SendAiTutorMessage, AiTutorReplyResponse>(
            new SendAiTutorMessage(conversationId, request.Question),
            cancellationToken)).ToHttpResult();

    private static async Task<IResult> ArchiveAsync(
        Guid conversationId,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync<ArchiveAiTutorConversation, AiTutorConversationResponse>(
            new ArchiveAiTutorConversation(conversationId),
            cancellationToken)).ToHttpResult();

    private static async Task<IResult> HealthAsync(
        IQueryHandler<CheckOllamaHealth, Result<OllamaHealthResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new CheckOllamaHealth(), cancellationToken)).ToHttpResult();
}
