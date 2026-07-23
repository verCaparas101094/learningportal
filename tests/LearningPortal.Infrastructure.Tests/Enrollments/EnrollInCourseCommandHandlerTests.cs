using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Enrollments.Commands.EnrollInCourse;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Enrollments;

/// <summary>Verifies employee enrollment application rules.</summary>
public sealed class EnrollInCourseCommandHandlerTests
{
    /// <summary>Verifies successful enrollment.</summary>
    [Fact]
    public async Task Handle_PublishedCourse_UsesCurrentUserAndPersists()
    {
        var studentId = Guid.NewGuid();
        var course = CreateCourse(published: true);
        var enrollmentRepository = new FakeEnrollmentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(course, studentId, enrollmentRepository, unitOfWork);

        var result = await handler.HandleAsync(new(course.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(studentId, result.Value.StudentId);
        Assert.Same(enrollmentRepository.Added, enrollmentRepository.Active);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    /// <summary>Verifies draft rejection.</summary>
    [Fact]
    public async Task Handle_DraftCourse_ReturnsCourseNotPublished()
    {
        var course = CreateCourse(published: false);
        var handler = CreateHandler(course, Guid.NewGuid(), new(), new());

        var result = await handler.HandleAsync(new(course.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Enrollment.CourseNotPublished", result.Error!.Code);
    }

    /// <summary>Verifies archived courses cannot receive new enrollments.</summary>
    [Fact]
    public async Task Handle_ArchivedCourse_ReturnsCourseNotPublished()
    {
        var course = CreateCourse(published: true);
        Assert.True(course.TryArchive());
        var handler = CreateHandler(course, Guid.NewGuid(), new(), new());

        var result = await handler.HandleAsync(new(course.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Enrollment.CourseNotPublished", result.Error!.Code);
    }

    /// <summary>Verifies duplicate rejection.</summary>
    [Fact]
    public async Task Handle_DuplicateActiveEnrollment_ReturnsConflict()
    {
        var studentId = Guid.NewGuid();
        var course = CreateCourse(published: true);
        var repository = new FakeEnrollmentRepository
        {
            Active = Enrollment.Create(course.Id, studentId, new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero))
        };
        var handler = CreateHandler(course, studentId, repository, new());

        var result = await handler.HandleAsync(new(course.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Enrollment.Duplicate", result.Error!.Code);
    }

    private static EnrollInCourseCommandHandler CreateHandler(
        Course course, Guid userId, FakeEnrollmentRepository enrollments, FakeUnitOfWork unitOfWork) =>
        new(new FakeCourseRepository(course), enrollments, unitOfWork,
            new FakeCurrentUser(userId), new FakeClock(),
            NullLogger<EnrollInCourseCommandHandler>.Instance);

    private static Course CreateCourse(bool published)
    {
        var course = Course.Create("Course", $"course-{Guid.NewGuid():N}", "Description", "Technology", null, Guid.NewGuid());
        if (published) Assert.True(course.TryPublish());
        return course;
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 23, 8, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? DisplayName => "Student";
        public string? Email => "student@example.com";
        public IReadOnlyCollection<string> Roles => ["Student"];
        public bool IsAuthenticated => true;
        public bool HasRole(string role) => Roles.Contains(role);
        public bool HasClaim(string claimType, string? claimValue = null) => false;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCourseRepository(Course course) : ICourseRepository
    {
        public Task<Course?> GetByIdAsync(Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult<Course?>(course);
        public Task<Course?> GetByIdReadOnlyAsync(Guid courseId, CancellationToken cancellationToken = default) => Task.FromResult<Course?>(course);
        public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Course?>(course);
        public Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Course>>([course]);
        public Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(string? search, CourseStatus? status, Guid? instructorId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Course>, int)>(([course], 1));
        public Task<bool> SlugExistsAsync(string slug, Guid? excludedCourseId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(Course value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Course value) { }
        public void SetOriginalRowVersion(Course value, byte[] rowVersion) { }
    }

    private sealed class FakeEnrollmentRepository : IEnrollmentRepository
    {
        public Enrollment? Active { get; set; }
        public Enrollment? Added { get; private set; }
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<Enrollment?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<Enrollment?> GetByCourseAndStudentAsync(Guid courseId, Guid studentId, CancellationToken cancellationToken = default) => Task.FromResult(Active);
        public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetStudentPageAsync(Guid studentId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Enrollment>, int)>(([], 0));
        public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetCoursePageAsync(Guid courseId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Enrollment>, int)>(([], 0));
        public Task<IReadOnlySet<Guid>> GetActiveCourseIdsAsync(Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task<IReadOnlyList<Enrollment>> GetActiveByStudentAndCoursesAsync(Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Enrollment>>([]);
        public Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default) { Added = enrollment; Active = enrollment; return Task.CompletedTask; }
        public void SetOriginalRowVersion(Enrollment enrollment, byte[] rowVersion) { }
    }
}
