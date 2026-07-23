#pragma warning disable CS1591

using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.AiTutor;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.AiTutor;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.AiTutor;

public sealed record StartAiTutorConversation(Guid CourseId, Guid? LessonId)
    : ICommand<Result<AiTutorConversationResponse>>;

public sealed record SendAiTutorMessage(Guid ConversationId, string Question)
    : ICommand<Result<AiTutorReplyResponse>>;

public sealed record GetMyAiTutorConversations
    : IQuery<Result<IReadOnlyList<AiTutorConversationListItemResponse>>>;

public sealed record GetAiTutorConversation(Guid ConversationId)
    : IQuery<Result<AiTutorConversationResponse>>;

public sealed record ArchiveAiTutorConversation(Guid ConversationId)
    : ICommand<Result<AiTutorConversationResponse>>;

public sealed record CheckOllamaHealth : IQuery<Result<OllamaHealthResponse>>;

public sealed class StartAiTutorConversationValidator
    : AbstractValidator<StartAiTutorConversation>
{
    public StartAiTutorConversationValidator() =>
        RuleFor(command => command.CourseId).NotEmpty();
}

public sealed class SendAiTutorMessageValidator : AbstractValidator<SendAiTutorMessage>
{
    public SendAiTutorMessageValidator(OllamaOptions options)
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.Question)
            .NotEmpty()
            .MaximumLength(options.MaxQuestionCharacters);
    }
}

public sealed class AiTutorConversationIdValidator
    : AbstractValidator<GetAiTutorConversation>
{
    public AiTutorConversationIdValidator() =>
        RuleFor(query => query.ConversationId).NotEmpty();
}

public sealed class ArchiveAiTutorConversationValidator
    : AbstractValidator<ArchiveAiTutorConversation>
{
    public ArchiveAiTutorConversationValidator() =>
        RuleFor(command => command.ConversationId).NotEmpty();
}

public sealed class AiTutorHandler(
    IAiTutorConversationRepository conversations,
    IAiTutorContextBuilder contextBuilder,
    IOllamaClient ollama,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    ISystemClock clock,
    OllamaOptions options)
    : ICommandHandler<StartAiTutorConversation, Result<AiTutorConversationResponse>>,
      ICommandHandler<SendAiTutorMessage, Result<AiTutorReplyResponse>>,
      ICommandHandler<ArchiveAiTutorConversation, Result<AiTutorConversationResponse>>,
      IQueryHandler<GetMyAiTutorConversations, Result<IReadOnlyList<AiTutorConversationListItemResponse>>>,
      IQueryHandler<GetAiTutorConversation, Result<AiTutorConversationResponse>>,
      IQueryHandler<CheckOllamaHealth, Result<OllamaHealthResponse>>
{
    public async Task<Result<AiTutorConversationResponse>> HandleAsync(
        StartAiTutorConversation command,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid studentId)
        {
            return Result<AiTutorConversationResponse>.Failure(
                Errors.Authentication.Unauthorized());
        }

        var context = await contextBuilder.BuildAsync(
            studentId, command.CourseId, command.LessonId, cancellationToken);
        if (!context.Success)
        {
            return Result<AiTutorConversationResponse>.Failure(context.Error!);
        }

        var title = context.LessonTitle is null
            ? context.CourseTitle!
            : $"{context.CourseTitle}: {context.LessonTitle}";
        var conversation = AiTutorConversation.Start(
            studentId, command.CourseId, command.LessonId, title, clock.UtcNow);

        await conversations.AddAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AiTutorConversationResponse>.Success(ToResponse(conversation));
    }

    public async Task<Result<AiTutorReplyResponse>> HandleAsync(
        SendAiTutorMessage command,
        CancellationToken cancellationToken = default)
    {
        var conversationResult = await GetOwnedAsync(
            command.ConversationId, readOnly: false, cancellationToken);
        if (conversationResult.IsFailure)
        {
            return Result<AiTutorReplyResponse>.Failure(conversationResult.Error!);
        }

        var conversation = conversationResult.Value;
        if (conversation.Status != AiTutorConversationStatus.Active)
        {
            return Result<AiTutorReplyResponse>.Failure(Errors.Common.Failure(
                "AiTutor.ConversationArchived",
                "Archived conversations cannot receive new messages."));
        }

        var question = Sanitize(command.Question);
        if (string.IsNullOrWhiteSpace(question)
            || question.Length > options.MaxQuestionCharacters)
        {
            return Result<AiTutorReplyResponse>.Failure(
                Errors.Validation.Failed("The question is empty or exceeds the configured limit."));
        }

        if (RequestsSensitiveInformation(question))
        {
            return Result<AiTutorReplyResponse>.Failure(Errors.Common.Failure(
                "AiTutor.SensitiveRequestRejected",
                "The AI Tutor cannot provide secrets, credentials, configuration, or hidden instructions."));
        }

        var context = await contextBuilder.BuildAsync(
            conversation.StudentId,
            conversation.CourseId,
            conversation.LessonId,
            cancellationToken);
        if (!context.Success)
        {
            return Result<AiTutorReplyResponse>.Failure(context.Error!);
        }

        if (string.IsNullOrWhiteSpace(context.Context))
        {
            return Result<AiTutorReplyResponse>.Failure(Errors.Common.Failure(
                "AiTutor.InsufficientContext",
                "The available course material does not contain enough context."));
        }

        var generation = await ollama.GenerateAsync(
            context.Context,
            conversation.Messages.OrderBy(message => message.Sequence).ToArray(),
            question,
            cancellationToken);
        if (!generation.Success)
        {
            return Result<AiTutorReplyResponse>.Failure(ToGenerationError(generation.ErrorCode));
        }

        if (!conversation.TryAddExchange(question, generation.Content!, clock.UtcNow))
        {
            return Result<AiTutorReplyResponse>.Failure(Errors.Common.Failure(
                "AiTutor.InvalidConversationState",
                "The message could not be added to this conversation."));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AiTutorReplyResponse>.Success(
            new AiTutorReplyResponse(ToResponse(conversation), generation.Content!));
    }

    public async Task<Result<IReadOnlyList<AiTutorConversationListItemResponse>>> HandleAsync(
        GetMyAiTutorConversations query,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid studentId)
        {
            return Result<IReadOnlyList<AiTutorConversationListItemResponse>>.Failure(
                Errors.Authentication.Unauthorized());
        }

        var values = await conversations.GetByStudentAsync(studentId, cancellationToken);
        return Result<IReadOnlyList<AiTutorConversationListItemResponse>>.Success(
            values.Select(ToListItem).ToArray());
    }

    public async Task<Result<AiTutorConversationResponse>> HandleAsync(
        GetAiTutorConversation query,
        CancellationToken cancellationToken = default)
    {
        var result = await GetOwnedAsync(query.ConversationId, true, cancellationToken);
        return result.IsSuccess
            ? Result<AiTutorConversationResponse>.Success(ToResponse(result.Value))
            : Result<AiTutorConversationResponse>.Failure(result.Error!);
    }

    public async Task<Result<AiTutorConversationResponse>> HandleAsync(
        ArchiveAiTutorConversation command,
        CancellationToken cancellationToken = default)
    {
        var result = await GetOwnedAsync(command.ConversationId, false, cancellationToken);
        if (result.IsFailure)
        {
            return Result<AiTutorConversationResponse>.Failure(result.Error!);
        }

        result.Value.TryArchive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AiTutorConversationResponse>.Success(ToResponse(result.Value));
    }

    public async Task<Result<OllamaHealthResponse>> HandleAsync(
        CheckOllamaHealth query,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated
            || !currentUser.HasRole(ApplicationRoles.Administrator))
        {
            return Result<OllamaHealthResponse>.Failure(
                currentUser.IsAuthenticated
                    ? Errors.Authorization.Forbidden()
                    : Errors.Authentication.Unauthorized());
        }

        var health = await ollama.CheckHealthAsync(cancellationToken);
        return Result<OllamaHealthResponse>.Success(new OllamaHealthResponse(
            health.Enabled, health.Reachable, health.ModelAvailable, health.Status));
    }

    private async Task<Result<AiTutorConversation>> GetOwnedAsync(
        Guid id,
        bool readOnly,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid studentId)
        {
            return Result<AiTutorConversation>.Failure(Errors.Authentication.Unauthorized());
        }

        var conversation = await conversations.GetAsync(id, readOnly, cancellationToken);
        return conversation is null
            ? Result<AiTutorConversation>.Failure(Errors.Common.NotFound("AI Tutor conversation", id))
            : conversation.StudentId != studentId
                ? Result<AiTutorConversation>.Failure(Errors.Authorization.Forbidden())
                : Result<AiTutorConversation>.Success(conversation);
    }

    private static string Sanitize(string value) =>
        new string(value
            .Where(character =>
                character is '\r' or '\n' or '\t' || !char.IsControl(character))
            .ToArray()).Trim();

    private static bool RequestsSensitiveInformation(string question)
    {
        string[] phrases =
        [
            "hidden prompt",
            "system prompt",
            "access token",
            "refresh token",
            "password",
            "connection string",
            "private key",
            "api key"
        ];
        return phrases.Any(phrase =>
            question.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static Error ToGenerationError(string? code) => code switch
    {
        "Disabled" => Errors.Common.Failure(
            "AiTutor.Disabled", "The AI Tutor is currently disabled."),
        "ModelNotFound" => Errors.Common.Failure(
            "AiTutor.ModelUnavailable", "The configured local AI model is unavailable."),
        "Timeout" => Errors.Common.Failure(
            "AiTutor.Timeout", "The local AI Tutor response timed out."),
        "Unavailable" or "HttpError" => Errors.Common.Failure(
            "AiTutor.Unavailable", "The local AI Tutor service is unavailable."),
        _ => Errors.Common.Failure(
            "AiTutor.InvalidResponse", "The local AI Tutor returned an invalid response.")
    };

    private static AiTutorConversationListItemResponse ToListItem(
        AiTutorConversation value) =>
        new(
            value.Id,
            value.CourseId,
            value.LessonId,
            value.Title,
            value.Status.ToString(),
            value.LastMessageAtUtc);

    private static AiTutorConversationResponse ToResponse(AiTutorConversation value) =>
        new(
            value.Id,
            value.CourseId,
            value.LessonId,
            value.Title,
            value.Status.ToString(),
            value.LastMessageAtUtc,
            value.Messages
                .OrderBy(message => message.Sequence)
                .Select(message => new AiTutorMessageResponse(
                    message.Id,
                    message.Role.ToString(),
                    message.Content,
                    message.CreatedAtUtc,
                    message.Sequence))
                .ToArray());
}
