namespace LearningPortal.Application.AiTutor;
/// <summary>Configures the local Ollama tutor connection and safety bounds.</summary>
public sealed class OllamaOptions
{
    /// <summary>Gets configuration section name.</summary>
    public const string SectionName="Ollama";
    /// <summary>Gets or sets local endpoint.</summary>
    public string BaseUrl{get;set;}="http://localhost:11434";
    /// <summary>Gets or sets model.</summary>
    public string Model{get;set;}="llama3.2:3b";
    /// <summary>Gets or sets timeout.</summary>
    public int RequestTimeoutSeconds{get;set;}=120;
    /// <summary>Gets or sets context bound.</summary>
    public int MaxContextCharacters{get;set;}=30000;
    /// <summary>Gets or sets question bound.</summary>
    public int MaxQuestionCharacters{get;set;}=2000;
    /// <summary>Gets or sets history bound.</summary>
    public int MaxConversationMessages{get;set;}=20;
    /// <summary>Gets or sets sampling temperature.</summary>
    public double Temperature{get;set;}=.2;
    /// <summary>Gets or sets feature state.</summary>
    public bool Enabled{get;set;}=true;
}
