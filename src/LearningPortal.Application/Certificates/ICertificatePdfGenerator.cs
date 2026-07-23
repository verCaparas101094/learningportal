using LearningPortal.Domain.Certificates;

namespace LearningPortal.Application.Certificates;

/// <summary>Generates an on-demand PDF from certificate snapshots.</summary>
public interface ICertificatePdfGenerator
{
    /// <summary>Generates PDF bytes.</summary>
    byte[] Generate(Certificate certificate, string verificationUrl);
}
