#pragma warning disable CS1591
namespace LearningPortal.Shared.Quizzes;

public sealed record QuizChoiceResponse(Guid Id, string Text, int Order);
public sealed record QuizQuestionResponse(
    Guid Id,
    string Text,
    string QuestionType,
    decimal Points,
    int Order,
    IReadOnlyList<QuizChoiceResponse> Choices);
public sealed record QuizResponse(
    Guid Id,
    Guid CourseId,
    Guid? LessonId,
    string Title,
    string Description,
    decimal PassingPercentage,
    int? MaximumAttempts,
    bool IsRequired,
    string Status,
    IReadOnlyList<QuizQuestionResponse> Questions);
public sealed record QuizListItemResponse(
    Guid Id,
    Guid CourseId,
    Guid? LessonId,
    string Title,
    string Description,
    decimal PassingPercentage,
    int? MaximumAttempts,
    bool IsRequired);
public sealed record QuizAdminChoiceResponse(Guid Id, string Text, bool IsCorrect, int Order);
public sealed record QuizAdminQuestionResponse(
    Guid Id,
    string Text,
    string QuestionType,
    decimal Points,
    int Order,
    string? Explanation,
    bool IsActive,
    IReadOnlyList<QuizAdminChoiceResponse> Choices);
public sealed record QuizAdministrationResponse(
    Guid Id,
    Guid CourseId,
    Guid? LessonId,
    string Title,
    string Description,
    decimal PassingPercentage,
    int? MaximumAttempts,
    bool IsRequired,
    string Status,
    string RowVersion,
    IReadOnlyList<QuizAdminQuestionResponse> Questions,
    bool IsInstructorAssessment = false,
    Guid? SkillId = null);
public sealed record SaveQuizRequest(
    Guid? LessonId,
    string Title,
    string Description,
    decimal PassingPercentage,
    int? MaximumAttempts,
    bool IsRequired,
    bool IsInstructorAssessment = false,
    Guid? SkillId = null);
public sealed record SaveQuizQuestionRequest(
    string Text,
    string QuestionType,
    decimal Points,
    int Order,
    string? Explanation,
    bool IsActive,
    IReadOnlyList<SaveQuizChoiceRequest> Choices);
public sealed record SaveQuizChoiceRequest(string Text, bool IsCorrect, int Order);
public sealed record StartQuizAttemptResponse(Guid AttemptId, int AttemptNumber, bool Resumed);
public sealed record SubmitQuizAnswerRequest(Guid QuestionId, IReadOnlyList<Guid> SelectedChoiceIds);
public sealed record SubmitQuizAttemptRequest(IReadOnlyList<SubmitQuizAnswerRequest> Answers);
public sealed record QuizAttemptAnswerResponse(
    Guid QuestionId,
    string QuestionText,
    IReadOnlyList<Guid> SelectedChoiceIds,
    bool IsCorrect,
    decimal PointsAwarded,
    decimal MaximumPoints,
    string? Explanation);
public sealed record QuizAttemptResponse(
    Guid Id,
    Guid QuizId,
    int AttemptNumber,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    decimal? Score,
    decimal? MaximumScore,
    decimal? Percentage,
    bool? Passed,
    IReadOnlyList<QuizAttemptAnswerResponse> Answers);
