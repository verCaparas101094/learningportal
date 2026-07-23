namespace LearningPortal.Application.Authorization;

/// <summary>
/// Defines the application roles recognized by LearningPortal authorization.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>Identifies administrators with full portal access.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Identifies instructors who create courses, manage lessons, and view students.</summary>
    public const string Instructor = "Instructor";

    /// <summary>Identifies students who enroll, learn, and take quizzes.</summary>
    public const string Student = "Student";

    /// <summary>Gets every role that may be created or assigned by the application.</summary>
    public static IReadOnlyCollection<string> All =>
        [Administrator, Instructor, Student];

    /// <summary>Determines whether a role name belongs to the application role allowlist.</summary>
    /// <param name="roleName">The role name to validate.</param>
    /// <returns><see langword="true"/> when the role name is recognized.</returns>
    public static bool IsValid(string? roleName) =>
        string.Equals(roleName, Administrator, StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, Instructor, StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, Student, StringComparison.OrdinalIgnoreCase);
}
