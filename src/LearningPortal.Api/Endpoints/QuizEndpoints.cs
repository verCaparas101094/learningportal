using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Quizzes;
using LearningPortal.Shared.Quizzes;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps quiz authoring and learner-attempt routes.</summary>
public static class QuizEndpoints
{
    /// <summary>Maps quiz routes.</summary>
    public static IEndpointRouteBuilder MapQuizEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var administration = endpoints.MapGroup("/api").RequireAdminOrInstructor();
        administration.MapGet("/courses/{courseId:guid}/quizzes", GetCourseAdministrationAsync);
        administration.MapPost("/courses/{courseId:guid}/quizzes", CreateAsync);
        administration.MapGet("/quizzes/{quizId:guid}/administration", GetAdministrationAsync);
        administration.MapPut("/quizzes/{quizId:guid}", UpdateAsync);
        administration.MapPost("/quizzes/{quizId:guid}/questions", AddQuestionAsync);
        administration.MapPut("/quizzes/{quizId:guid}/questions/{questionId:guid}", UpdateQuestionAsync);
        administration.MapPut("/quizzes/{quizId:guid}/publish", PublishAsync);
        administration.MapPut("/quizzes/{quizId:guid}/archive", ArchiveAsync);

        var learner = endpoints.MapGroup("/api/learning").RequireAuthorization();
        learner.MapGet("/quizzes/{quizId:guid}", GetQuizAsync);
        learner.MapPost("/quizzes/{quizId:guid}/attempts", StartAsync);
        learner.MapGet("/quizzes/{quizId:guid}/attempts/me", GetMyAttemptsAsync);
        learner.MapGet("/quiz-attempts/{attemptId:guid}", GetAttemptAsync);
        learner.MapGet("/quiz-attempts/{attemptId:guid}/resume", ResumeAsync);
        learner.MapPost("/quiz-attempts/{attemptId:guid}/submit", SubmitAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid courseId, SaveQuizRequest request, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<CreateQuizCommand, QuizAdministrationResponse>(
            new(courseId, request), ct);
        return result.IsSuccess
            ? Results.Created($"/api/quizzes/{result.Value.Id:D}/administration", result.Value)
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(
        Guid quizId, SaveQuizRequest request, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<UpdateQuizCommand, QuizAdministrationResponse>(
            new(quizId, request), ct)).ToHttpResult();

    private static async Task<IResult> AddQuestionAsync(
        Guid quizId, SaveQuizQuestionRequest request, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<AddQuizQuestionCommand, QuizAdministrationResponse>(
            new(quizId, request), ct)).ToHttpResult();

    private static async Task<IResult> UpdateQuestionAsync(
        Guid quizId, Guid questionId, SaveQuizQuestionRequest request,
        ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<UpdateQuizQuestionCommand, QuizAdministrationResponse>(
            new(quizId, questionId, request), ct)).ToHttpResult();

    private static async Task<IResult> PublishAsync(Guid quizId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<PublishQuizCommand, QuizAdministrationResponse>(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> ArchiveAsync(Guid quizId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<ArchiveQuizCommand, QuizAdministrationResponse>(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> GetAdministrationAsync(
        Guid quizId, IQueryHandler<GetQuizAdministration, Result<QuizAdministrationResponse>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> GetCourseAdministrationAsync(
        Guid courseId,
        IQueryHandler<GetCourseQuizAdministration, Result<IReadOnlyList<QuizAdministrationResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(courseId), ct)).ToHttpResult();

    private static async Task<IResult> GetQuizAsync(
        Guid quizId, IQueryHandler<GetQuiz, Result<QuizResponse>> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> StartAsync(
        Guid quizId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<StartQuizAttempt, StartQuizAttemptResponse>(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> GetAttemptAsync(
        Guid attemptId, IQueryHandler<GetQuizAttempt, Result<QuizAttemptResponse>> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new(attemptId), ct)).ToHttpResult();

    private static async Task<IResult> ResumeAsync(
        Guid attemptId, IQueryHandler<ResumeQuizAttempt, Result<QuizAttemptResponse>> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new(attemptId), ct)).ToHttpResult();

    private static async Task<IResult> GetMyAttemptsAsync(
        Guid quizId,
        IQueryHandler<GetMyAttempts, Result<IReadOnlyList<QuizAttemptResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(quizId), ct)).ToHttpResult();

    private static async Task<IResult> SubmitAsync(
        Guid attemptId, SubmitQuizAttemptRequest request, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<SubmitQuizAttempt, QuizAttemptResponse>(
            new(attemptId, request), ct)).ToHttpResult();
}
