namespace LearningPortal.EndToEndTests;

internal sealed record E2eSettings(
    string BlazorBaseUrl,
    string ApiBaseUrl,
    string AdminEmail,
    string AdminPassword,
    string InstructorEmail,
    string InstructorPassword,
    string StudentEmail,
    string StudentPassword,
    bool Headless,
    float TimeoutMilliseconds)
{
    public static E2eSettings Load() => new(
        Get("E2E_BLAZOR_BASE_URL", "https://localhost:7080"),
        Get("E2E_API_BASE_URL", "https://localhost:7081"),
        Get("E2E_ADMIN_EMAIL", "admin@learningportal.local"),
        Get("E2E_ADMIN_PASSWORD", "Admin123!"),
        Get("E2E_INSTRUCTOR_EMAIL", "instructor@learningportal.local"),
        Get("E2E_INSTRUCTOR_PASSWORD", "Instructor123!"),
        Get("E2E_STUDENT_EMAIL", "student@learningportal.local"),
        Get("E2E_STUDENT_PASSWORD", "Student123!"),
        bool.TryParse(Get("E2E_HEADLESS", "true"), out var headless) && headless,
        float.TryParse(Get("E2E_TIMEOUT_MS", "30000"), out var timeout)
            ? timeout
            : 30_000);

    private static string Get(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : fallback;
}
