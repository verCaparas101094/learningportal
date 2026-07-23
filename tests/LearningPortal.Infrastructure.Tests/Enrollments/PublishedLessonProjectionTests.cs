using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Enrollments;

/// <summary>Verifies catalog-facing lesson projections exclude unpublished content.</summary>
public sealed class PublishedLessonProjectionTests
{
    /// <summary>Verifies published lesson retrieval does not include draft lessons.</summary>
    [Fact]
    public async Task GetPublishedByCourseAsync_ExcludesDraftLessons()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"published-lessons-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var course = Course.Create("Course", $"course-{Guid.NewGuid():N}", "Description", "Testing", null, Guid.NewGuid());
        Assert.True(course.TryPublish());
        var published = Lesson.Create(course.Id, "Published", "", 1, 10, LessonType.Article,
            "Content", null, VideoProvider.None);
        Assert.True(published.TryPublish());
        var draft = Lesson.Create(course.Id, "Draft", "", 2, 10, LessonType.Article,
            "Content", null, VideoProvider.None);
        await context.Courses.AddAsync(course);
        await context.Lessons.AddRangeAsync(published, draft);
        await context.SaveChangesAsync();
        var repository = new LessonRepository(context);

        var result = await repository.GetPublishedByCourseAsync(course.Id);

        var lesson = Assert.Single(result);
        Assert.Equal(published.Id, lesson.Id);
    }
}
