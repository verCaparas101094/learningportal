using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Courses.Events;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Courses;

/// <summary>Verifies course aggregate lifecycle rules.</summary>
public sealed class CourseDomainTests
{
    /// <summary>Verifies normalized Draft creation.</summary>
    [Fact]
    public void Create_NormalizesValuesAndStartsDraft()
    {
        var course = CreateCourse("  Enterprise Learning  ", " Enterprise__Learning! ");

        Assert.Equal("Enterprise Learning", course.Title);
        Assert.Equal("enterprise-learning", course.Slug);
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.Contains(course.DomainEvents, domainEvent => domainEvent is CourseCreatedDomainEvent);
    }

    /// <summary>Verifies Draft editing.</summary>
    [Fact]
    public void DraftCourse_CanBeUpdated()
    {
        var course = CreateCourse();

        var updated = course.TryUpdate(
            "Updated Course",
            "Updated__Course",
            "Updated description",
            "Leadership",
            null);

        Assert.True(updated);
        Assert.Equal("updated-course", course.Slug);
        Assert.Equal("Updated Course", course.Title);
    }

    /// <summary>Verifies Published edit protection.</summary>
    [Fact]
    public void PublishedCourse_CannotBeEdited()
    {
        var course = CreateCourse();
        Assert.True(course.TryPublish());

        Assert.False(course.TryUpdate("Changed", "changed", string.Empty, "Category", null));
        Assert.Equal("Course", course.Title);
    }

    /// <summary>Verifies Draft publication.</summary>
    [Fact]
    public void DraftCourse_CanBePublished()
    {
        var course = CreateCourse();

        Assert.True(course.TryPublish());
        Assert.Equal(CourseStatus.Published, course.Status);
        Assert.Contains(course.DomainEvents, domainEvent => domainEvent is CoursePublishedDomainEvent);
    }

    /// <summary>Verifies Published archival.</summary>
    [Fact]
    public void PublishedCourse_CanBeArchived()
    {
        var course = CreateCourse();
        course.TryPublish();

        Assert.True(course.TryArchive());
        Assert.Equal(CourseStatus.Archived, course.Status);
        Assert.Contains(course.DomainEvents, domainEvent => domainEvent is CourseArchivedDomainEvent);
    }

    /// <summary>Verifies Draft deletion preparation.</summary>
    [Fact]
    public void DraftCourse_CanBePreparedForSoftDeletion()
    {
        var course = CreateCourse();

        Assert.True(course.TryDelete());
        Assert.Contains(course.DomainEvents, domainEvent => domainEvent is CourseDeletedDomainEvent);
    }

    /// <summary>Verifies Draft archive rejection.</summary>
    [Fact]
    public void DraftCourse_CannotBeArchived()
    {
        var course = CreateCourse();

        Assert.False(course.TryArchive());
        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    /// <summary>Verifies Archived publication rejection.</summary>
    [Fact]
    public void ArchivedCourse_CannotReturnToPublished()
    {
        var course = CreateCourse();
        course.TryPublish();
        course.TryArchive();

        Assert.False(course.TryPublish());
        Assert.Equal(CourseStatus.Archived, course.Status);
    }

    private static Course CreateCourse(
        string title = "Course",
        string slug = "course") =>
        Course.Create(
            title,
            slug,
            "Description",
            "Category",
            null,
            Guid.CreateVersion7());
}
