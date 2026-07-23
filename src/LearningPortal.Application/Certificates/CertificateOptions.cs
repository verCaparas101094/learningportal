namespace LearningPortal.Application.Certificates;

/// <summary>Configures certificate verification links.</summary>
public sealed class CertificateOptions
{
    /// <summary>Gets the configuration section.</summary>
    public const string SectionName = "Certificates";
    /// <summary>Gets or sets the public verification page base URL.</summary>
    public string VerificationBaseUrl { get; set; } = "https://localhost:7080/verify-certificate";
}
