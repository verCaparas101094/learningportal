using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Infrastructure.IntegrationTests.Authentication;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.IntegrationTests.Lessons;

/// <summary>Verifies SQL Server lesson constraints and concurrency.</summary>
public sealed class LessonPersistenceRelationalTests(SqlServerAuthenticationFixture fixture)
    : IClassFixture<SqlServerAuthenticationFixture>
{
    /// <summary>Verifies course-order uniqueness and rowversion generation.</summary>
    [SqlServerFact]
    public async Task Save_EnforcesUniqueCourseOrderAndCreatesRowVersion()
    {
        await fixture.EnsureInitializedAsync();
        var course = Course.Create("Lessons", $"lessons-{Guid.NewGuid():N}", "", "Testing", null, Guid.NewGuid());
        await using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Courses.Add(course);
            db.Lessons.Add(Lesson.Create(course.Id, "One", "", 1, 10, LessonType.Article,
                "Content", null, VideoProvider.None));
            await db.SaveChangesAsync();
            Assert.NotEmpty(await db.Lessons.Where(x => x.CourseId == course.Id).Select(x => x.RowVersion).SingleAsync());
        }
        await using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Lessons.Add(Lesson.Create(course.Id, "Duplicate", "", 1, 10, LessonType.Pdf,
                null, "https://example.com/file.pdf", VideoProvider.None));
            await Assert.ThrowsAsync<DuplicateLessonOrderException>(() => db.SaveChangesAsync());
        }
    }

    /// <summary>Verifies the enhanced lesson schema is present after migrations.</summary>
    [SqlServerFact]
    public async Task Migration_CreatesContentColumns()
    {
        await fixture.EnsureInitializedAsync();
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Lessons') AND name IN (N'MarkdownContent', N'ExternalUrl', N'VideoProvider');";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(3, count);
    }
}
