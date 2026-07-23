using System.ComponentModel.DataAnnotations;

namespace LearningPortal.Application.InstructorEligibility;

/// <summary>Configures automatic instructor qualification.</summary>
public sealed class InstructorEligibilityOptions
{
    /// <summary>Gets the configuration section name.</summary>
    public const string SectionName = "InstructorEligibility";
    /// <summary>Gets or sets the minimum qualifying percentage.</summary>
    [Range(1, 100)]
    public decimal QualificationThreshold { get; set; } = 80m;
}
