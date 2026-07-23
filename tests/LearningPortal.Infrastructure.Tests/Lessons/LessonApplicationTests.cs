using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
using LearningPortal.Application.Lessons.Commands.CreateLesson;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using LearningPortal.Infrastructure.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson query ownership, search, and pagination flow.</summary>
public sealed class LessonApplicationTests
{
    /// <summary>Verifies each supported content type can be created through the handler.</summary>
    [Theory]
    [InlineData("Article", "# Article", null, "None")]
    [InlineData("Video", null, "https://youtu.be/abc12345", "YouTube")]
    [InlineData("Pdf", null, "https://example.com/file.pdf", "None")]
    [InlineData("ExternalLink", null, "https://example.com/resource", "None")]
    public async Task Create_SupportedType_Succeeds(
        string type, string? markdown, string? url, string expectedProvider)
    {
        var instructor = Guid.NewGuid();
        var course = Course.Create("Course", "course", "", "Category", null, instructor);
        var lessons = new LessonRepositoryFake([], 0);
        var handler = new CreateLessonCommandHandler(lessons, new CourseRepositoryFake(course), new UnitOfWorkFake(),
            new UserFake(instructor), new VideoEmbedResolver(), new MarkdownRenderer());
        var result = await handler.HandleAsync(new(course.Id, "Lesson", "", 1, 10, type, markdown, url));
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedProvider, result.Value.VideoProvider);
        Assert.NotNull(lessons.Added);
    }

    /// <summary>Verifies an owning instructor receives the repository page.</summary>
    [Fact]
    public async Task GetByCourse_Owner_ReturnsPage()
    {
        var instructor = Guid.NewGuid();
        var course = Course.Create("Course", "course", "", "Category", null, instructor);
        var lesson = Lesson.Create(course.Id, "Search result", "", 1, 10, LessonType.Article,
            "Content", null, VideoProvider.None);
        var lessons = new LessonRepositoryFake([lesson], 1);
        var handler = new GetLessonsByCourseQueryHandler(lessons, new CourseRepositoryFake(course), new UserFake(instructor));

        var result = await handler.HandleAsync(new(course.Id, "Search", 1, 10));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Search", lessons.Search);
        Assert.Equal(1, result.Value.TotalCount);
    }

    /// <summary>Verifies a different instructor is forbidden.</summary>
    [Fact]
    public async Task GetByCourse_NonOwner_IsForbidden()
    {
        var course = Course.Create("Course", "course", "", "Category", null, Guid.NewGuid());
        var handler = new GetLessonsByCourseQueryHandler(new LessonRepositoryFake([], 0),
            new CourseRepositoryFake(course), new UserFake(Guid.NewGuid()));
        var result = await handler.HandleAsync(new(course.Id, null, 1, 10));
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error!.Code);
    }

    private sealed class UserFake(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id; public string? DisplayName => null; public string? Email => null;
        public IReadOnlyCollection<string> Roles => [ApplicationRoles.Instructor]; public bool IsAuthenticated => true;
        public bool HasRole(string role) => Roles.Contains(role); public bool HasClaim(string type, string? value = null) => false;
    }
    private sealed class CourseRepositoryFake(Course course) : ICourseRepository
    {
        public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Course?>(course);
        public Task<Course?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Course?>(course);
        public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default) => Task.FromResult<Course?>(course);
        public Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Course>>([course]);
        public Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(string? s, CourseStatus? status, Guid? i, int p, int z, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> SlugExistsAsync(string s, Guid? e = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Course c, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(Course c) => throw new NotSupportedException();
        public void SetOriginalRowVersion(Course c, byte[] v) => throw new NotSupportedException();
    }
    private sealed class LessonRepositoryFake(IReadOnlyList<Lesson> items, int count) : ILessonRepository
    {
        public string? Search { get; private set; }
        public Lesson? Added { get; private set; }
        public Task<(IReadOnlyList<Lesson> Items, int TotalCount)> GetPageAsync(Guid? c, string? s, int p, int z, Guid? i = null, CancellationToken ct = default)
        { Search = s; return Task.FromResult((items, count)); }
        public Task<Lesson?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Lesson?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Lesson>> GetPublishedByCourseAsync(Guid id, CancellationToken ct = default) => Task.FromResult(items);
        public Task<IReadOnlyList<Lesson>> GetPublishedByCoursesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) => Task.FromResult(items);
        public Task<bool> OrderExistsAsync(Guid c, int o, Guid? e = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task AddAsync(Lesson l, CancellationToken ct = default) { Added = l; return Task.CompletedTask; }
        public void Remove(Lesson l) => throw new NotSupportedException();
        public void SetOriginalRowVersion(Lesson l, byte[] v) => throw new NotSupportedException();
        public Task<LessonMoveResult> MoveAsync(Guid id, int o, byte[] v, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
