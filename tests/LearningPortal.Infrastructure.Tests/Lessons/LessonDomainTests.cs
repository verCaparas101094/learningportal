using LearningPortal.Domain.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson aggregate rules.</summary>
public sealed class LessonDomainTests
{
    /// <summary>Verifies valid Draft creation.</summary>
    [Fact]
    public void Create_ValidValues_CreatesDraft()
    {
        var lesson = CreateLesson();
        Assert.Equal(LessonStatus.Draft, lesson.Status);
        Assert.Equal(1, lesson.Order);
    }

    /// <summary>Verifies invalid ordering and duration.</summary>
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void Create_InvalidNumbers_Throws(int order, int minutes) =>
        Assert.Throws<ArgumentException>(() => Lesson.Create(Guid.NewGuid(), "Title", "", "Content", order, minutes, LessonType.Article));

    /// <summary>Verifies Draft editing.</summary>
    [Fact]
    public void Draft_CanBeEdited()
    {
        var lesson = CreateLesson();
        Assert.True(lesson.TryUpdate("Updated", "", "New content", 2, 20, LessonType.Video));
        Assert.Equal(2, lesson.Order);
    }

    /// <summary>Verifies Published lessons are read-only.</summary>
    [Fact]
    public void Published_IsReadOnly()
    {
        var lesson = CreateLesson();
        Assert.True(lesson.TryPublish());
        Assert.False(lesson.TryUpdate("Updated", "", "New", 2, 20, LessonType.Video));
        Assert.False(lesson.TryDelete());
    }

    private static Lesson CreateLesson() =>
        Lesson.Create(Guid.NewGuid(), "Lesson", "", "Content", 1, 15, LessonType.Article);
}
