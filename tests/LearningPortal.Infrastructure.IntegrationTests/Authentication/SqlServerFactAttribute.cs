using Xunit;

namespace LearningPortal.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Marks an opt-in Docker-backed SQL Server integration test.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    internal const string RunIntegrationTestsVariable = "LEARNINGPORTAL_RUN_SQL_INTEGRATION_TESTS";

    /// <summary>Initializes the test attribute and skips discovery unless SQL integration tests are enabled.</summary>
    public SqlServerFactAttribute()
    {
        if (!IsEnabled)
        {
            Skip = $"Set {RunIntegrationTestsVariable}=true to run Docker-backed SQL Server integration tests.";
        }
    }

    internal static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(RunIntegrationTestsVariable),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);
}
