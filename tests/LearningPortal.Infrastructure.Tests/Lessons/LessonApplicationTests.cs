using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.Lessons.Queries.GetLessonsByCourse;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson query ownership, search, and pagination flow.</summary>
public sealed class LessonApplicationTests
{
    /// <summary>Verifies an owning instructor receives the repository page.</summary>
    [Fact]
    public async Task GetByCourse_Owner_ReturnsPage()
    {
        var instructor = Guid.NewGuid();
        var course = Course.Create("Course", "course", "", "Category", null, instructor);
        var lesson = Lesson.Create(course.Id, "Search result", "", "Content", 1, 10, LessonType.Article);
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
        public Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(string? s, CourseStatus? status, Guid? i, int p, int z, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> SlugExistsAsync(string s, Guid? e = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Course c, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(Course c) => throw new NotSupportedException();
        public void SetOriginalRowVersion(Course c, byte[] v) => throw new NotSupportedException();
    }
    private sealed class LessonRepositoryFake(IReadOnlyList<Lesson> items, int count) : ILessonRepository
    {
        public string? Search { get; private set; }
        public Task<(IReadOnlyList<Lesson> Items, int TotalCount)> GetPageAsync(Guid? c, string? s, int p, int z, Guid? i = null, CancellationToken ct = default)
        { Search = s; return Task.FromResult((items, count)); }
        public Task<Lesson?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Lesson?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> OrderExistsAsync(Guid c, int o, Guid? e = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Lesson l, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(Lesson l) => throw new NotSupportedException();
        public void SetOriginalRowVersion(Lesson l, byte[] v) => throw new NotSupportedException();
        public Task<LessonMoveResult> MoveAsync(Guid id, int o, byte[] v, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
