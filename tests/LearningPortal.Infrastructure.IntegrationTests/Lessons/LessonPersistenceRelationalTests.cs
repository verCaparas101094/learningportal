using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Infrastructure.IntegrationTests.Authentication;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
            db.Lessons.Add(Lesson.Create(course.Id, "One", "", "Content", 1, 10, LessonType.Article));
            await db.SaveChangesAsync();
            Assert.NotEmpty(await db.Lessons.Where(x => x.CourseId == course.Id).Select(x => x.RowVersion).SingleAsync());
        }
        await using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Lessons.Add(Lesson.Create(course.Id, "Duplicate", "", "Content", 1, 10, LessonType.Video));
            await Assert.ThrowsAsync<DuplicateLessonOrderException>(() => db.SaveChangesAsync());
        }
    }
}
