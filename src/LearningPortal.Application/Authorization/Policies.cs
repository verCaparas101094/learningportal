namespace LearningPortal.Application.Authorization;

/// <summary>
/// Defines stable names for application authorization policies.
/// </summary>
public static class Policies
{
    /// <summary>Requires the Administrator role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Requires the Instructor role or the full-access Administrator role.</summary>
    public const string InstructorOnly = "InstructorOnly";

    /// <summary>Requires the Student role or the full-access Administrator role.</summary>
    public const string StudentOnly = "StudentOnly";

    /// <summary>Requires either the Administrator or Instructor role.</summary>
    public const string AdminOrInstructor = "AdminOrInstructor";
}
