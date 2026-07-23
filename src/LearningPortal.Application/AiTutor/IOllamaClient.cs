using LearningPortal.Domain.AiTutor;
namespace LearningPortal.Application.AiTutor;
/// <summary>Calls only the configured local Ollama service.</summary>
public interface IOllamaClient
{
    /// <summary>Generates a non-streaming tutor reply.</summary>
    Task<OllamaGenerationResult> GenerateAsync(string context,IReadOnlyList<AiTutorMessage> history,string question,CancellationToken cancellationToken);
    /// <summary>Checks endpoint and configured model availability.</summary>
    Task<OllamaHealthResult> CheckHealthAsync(CancellationToken cancellationToken);
}
/// <summary>Represents a safe generation outcome.</summary>
public sealed record OllamaGenerationResult(bool Success,string? Content,string? ErrorCode);
/// <summary>Represents safe local service health.</summary>
public sealed record OllamaHealthResult(bool Enabled,bool Reachable,bool ModelAvailable,string Status);
