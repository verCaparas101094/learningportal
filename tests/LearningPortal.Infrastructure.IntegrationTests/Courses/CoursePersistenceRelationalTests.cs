using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Courses.Exceptions;
using LearningPortal.Infrastructure.IntegrationTests.Authentication;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.IntegrationTests.Courses;

/// <summary>Verifies SQL Server course constraints and soft-delete filtering.</summary>
public sealed class CoursePersistenceRelationalTests(
    SqlServerAuthenticationFixture fixture)
    : IClassFixture<SqlServerAuthenticationFixture>
{
    /// <summary>Verifies filtered slug uniqueness and exclusion of deleted courses.</summary>
    [SqlServerFact]
    public async Task SlugUniqueIndexViolation_IsTranslatedAndAllowsReuseAfterSoftDelete()
    {
        await fixture.EnsureInitializedAsync();
        var instructorId = Guid.CreateVersion7();
        var slug = $"course-{Guid.CreateVersion7():N}";
        var courseId = Guid.Empty;

        await using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = CreateCourse(instructorId, slug);
            courseId = course.Id;
            await context.Courses.AddAsync(course);
            await context.SaveChangesAsync();
        }

        await using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Courses.AddAsync(CreateCourse(instructorId, slug));
            var exception = await Assert.ThrowsAsync<DuplicateCourseSlugException>(
                () => context.SaveChangesAsync());
            Assert.IsType<DbUpdateException>(exception.InnerException);
        }

        await using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = await context.Courses.SingleAsync(item => item.Id == courseId);
            context.Courses.Remove(course);
            await context.SaveChangesAsync();
        }

        await using (var scope = fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await context.Courses.AnyAsync(item => item.Id == courseId));
            await context.Courses.AddAsync(CreateCourse(instructorId, slug));
            await context.SaveChangesAsync();
        }
    }

    private static Course CreateCourse(Guid instructorId, string slug) =>
        Course.Create(
            "Relational Course",
            slug,
            "Relational course description.",
            "Testing",
            null,
            instructorId);
}
