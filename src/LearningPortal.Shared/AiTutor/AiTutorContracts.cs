#pragma warning disable CS1591
namespace LearningPortal.Shared.AiTutor;
public sealed record StartAiTutorConversationRequest(Guid CourseId,Guid? LessonId);
public sealed record SendAiTutorMessageRequest(string Question);
public sealed record AiTutorMessageResponse(Guid Id,string Role,string Content,DateTimeOffset CreatedAtUtc,int Sequence);
public sealed record AiTutorConversationResponse(Guid Id,Guid CourseId,Guid? LessonId,string Title,string Status,DateTimeOffset LastMessageAtUtc,IReadOnlyList<AiTutorMessageResponse> Messages);
public sealed record AiTutorConversationListItemResponse(Guid Id,Guid CourseId,Guid? LessonId,string Title,string Status,DateTimeOffset LastMessageAtUtc);
public sealed record AiTutorReplyResponse(AiTutorConversationResponse Conversation,string Reply);
public sealed record OllamaHealthResponse(bool Enabled,bool Reachable,bool ModelAvailable,string Status);
