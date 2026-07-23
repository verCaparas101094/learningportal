using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Enrollments.Commands.WithdrawEnrollment;
using LearningPortal.Application.Enrollments.Queries.GetEnrollmentById;
using LearningPortal.Application.Enrollments.Queries.GetCourseEnrollments;
using LearningPortal.Application.Enrollments.Queries.GetMyEnrollments;
using LearningPortal.Application.Enrollments.Queries.GetPublishedCourseCatalog;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Enrollments;

/// <summary>Verifies enrollment ownership, withdrawal, and student-safe projections.</summary>
public sealed class EnrollmentApplicationQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies an owner can withdraw an active enrollment.</summary>
    [Fact]
    public async Task Withdraw_OwnedActiveEnrollment_Succeeds()
    {
        var studentId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), studentId, Now);
        var repository = new FakeEnrollmentRepository(enrollment);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new WithdrawEnrollmentCommandHandler(
            repository, unitOfWork, new FakeCurrentUser(studentId), new FakeClock(),
            NullLogger<WithdrawEnrollmentCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new(enrollment.Id, Convert.ToBase64String([1])), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    /// <summary>Verifies completed enrollment withdrawal is rejected.</summary>
    [Fact]
    public async Task Withdraw_CompletedEnrollment_ReturnsInvalidState()
    {
        var studentId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), studentId, Now);
        Assert.True(enrollment.TryComplete(Now.AddMinutes(1)));
        var handler = new WithdrawEnrollmentCommandHandler(
            new FakeEnrollmentRepository(enrollment), new FakeUnitOfWork(),
            new FakeCurrentUser(studentId), new FakeClock(),
            NullLogger<WithdrawEnrollmentCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new(enrollment.Id, Convert.ToBase64String([1])), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Enrollment.InvalidState", result.Error!.Code);
    }

    /// <summary>Verifies a different student cannot withdraw an enrollment.</summary>
    [Fact]
    public async Task Withdraw_ForeignEnrollment_ReturnsForbidden()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var handler = new WithdrawEnrollmentCommandHandler(
            new FakeEnrollmentRepository(enrollment), new FakeUnitOfWork(),
            new FakeCurrentUser(Guid.NewGuid()), new FakeClock(),
            NullLogger<WithdrawEnrollmentCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new(enrollment.Id, Convert.ToBase64String([1])), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error!.Code);
    }

    /// <summary>Verifies administrators can withdraw another student's active enrollment.</summary>
    [Fact]
    public async Task Withdraw_Administrator_CanWithdrawAnotherStudentsEnrollment()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var handler = new WithdrawEnrollmentCommandHandler(
            new FakeEnrollmentRepository(enrollment), new FakeUnitOfWork(),
            new FakeCurrentUser(Guid.NewGuid(), "Administrator"), new FakeClock(),
            NullLogger<WithdrawEnrollmentCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new(enrollment.Id, Convert.ToBase64String([1])), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
    }

    /// <summary>Verifies enrollment details enforce ownership.</summary>
    [Fact]
    public async Task EnrollmentDetails_ForeignStudent_ReturnsForbidden()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var handler = new GetEnrollmentByIdQueryHandler(
            new FakeEnrollmentRepository(enrollment), new FakeCourseRepository([]),
            new FakeCurrentUser(Guid.NewGuid()));

        var result = await handler.HandleAsync(new(enrollment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error!.Code);
    }

    /// <summary>Verifies the catalog requests and returns published courses only.</summary>
    [Fact]
    public async Task Catalog_ReturnsPublishedCoursesOnly()
    {
        var published = CreateCourse(published: true);
        var draft = CreateCourse(published: false);
        var courses = new FakeCourseRepository([published, draft]);
        var handler = new GetPublishedCourseCatalogQueryHandler(
            courses, new FakeLessonRepository(), new FakeEnrollmentRepository(),
            new FakeUserManagementService(), new FakeCurrentUser(Guid.NewGuid()),
            new GetPublishedCourseCatalogQueryValidator());

        var result = await handler.HandleAsync(new(null, 1, 12), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CourseStatus.Published, courses.RequestedStatus);
        Assert.Single(result.Value.Items);
        Assert.Equal(published.Id, result.Value.Items.Single().CourseId);
    }

    /// <summary>Verifies catalog cards contain the current student's enrollment state.</summary>
    [Fact]
    public async Task Catalog_IncludesCurrentStudentsEnrollmentState()
    {
        var studentId = Guid.NewGuid();
        var course = CreateCourse(published: true);
        var enrollment = Enrollment.Create(course.Id, studentId, Now);
        var handler = new GetPublishedCourseCatalogQueryHandler(
            new FakeCourseRepository([course]), new FakeLessonRepository(),
            new FakeEnrollmentRepository(enrollment), new FakeUserManagementService(),
            new FakeCurrentUser(studentId), new GetPublishedCourseCatalogQueryValidator());

        var result = await handler.HandleAsync(new(null, 1, 12), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Enrolled", item.EnrollmentStatus);
        Assert.Equal(enrollment.Id, item.EnrollmentId);
        Assert.True(item.IsEnrolled);
        Assert.False(item.CanEnroll);
    }

    /// <summary>Verifies My Learning is scoped to the current user and published content.</summary>
    [Fact]
    public async Task MyLearning_UsesCurrentStudentAndHidesUnpublishedCourses()
    {
        var studentId = Guid.NewGuid();
        var published = CreateCourse(published: true);
        var draft = CreateCourse(published: false);
        var repository = new FakeEnrollmentRepository(
            Enrollment.Create(published.Id, studentId, Now),
            Enrollment.Create(draft.Id, studentId, Now));
        var handler = new GetMyEnrollmentsQueryHandler(
            repository, new FakeCourseRepository([published, draft]), new FakeLessonRepository(),
            new FakeCurrentUser(studentId), new GetMyEnrollmentsQueryValidator());

        var result = await handler.HandleAsync(new(null, null, 1, 12), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(studentId, repository.RequestedStudentId);
        Assert.Single(result.Value.Items);
        Assert.Equal(published.Id, result.Value.Items.Single().CourseId);
    }

    /// <summary>Verifies a non-owning instructor cannot list course enrollments.</summary>
    [Fact]
    public async Task CourseEnrollments_NonOwningInstructor_ReturnsForbidden()
    {
        var course = CreateCourse(published: true);
        var handler = new GetCourseEnrollmentsQueryHandler(
            new FakeCourseRepository([course]), new FakeEnrollmentRepository(),
            new FakeUserManagementService(), new FakeCurrentUser(Guid.NewGuid(), "Instructor"),
            new GetCourseEnrollmentsQueryValidator());

        var result = await handler.HandleAsync(new(course.Id, null, null, 1, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error!.Code);
    }

    private static Course CreateCourse(bool published)
    {
        var course = Course.Create(
            "Course", $"course-{Guid.NewGuid():N}", "Description", "Technology", null, Guid.NewGuid());
        if (published) Assert.True(course.TryPublish());
        return course;
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(2);
    }

    private sealed class FakeCurrentUser(Guid? userId, params string[] roles) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public string? DisplayName => "Student";
        public string? Email => "student@example.com";
        public IReadOnlyCollection<string> Roles => roles.Length == 0 ? ["Student"] : roles;
        public bool IsAuthenticated => userId is not null;
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

    private sealed class FakeEnrollmentRepository(params Enrollment[] items) : IEnrollmentRepository
    {
        public Guid? RequestedStudentId { get; private set; }
        public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public Task<Enrollment?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public Task<Enrollment?> GetByCourseAndStudentAsync(
            Guid courseId, Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x =>
                x.CourseId == courseId && x.StudentId == studentId && x.Status != EnrollmentStatus.Withdrawn));
        public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetStudentPageAsync(
            Guid studentId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            RequestedStudentId = studentId;
            var matches = items.Where(x => x.StudentId == studentId && (status is null || x.Status == status)).ToArray();
            return Task.FromResult<(IReadOnlyList<Enrollment>, int)>((matches, matches.Length));
        }
        public Task<(IReadOnlyList<Enrollment> Items, int TotalCount)> GetCoursePageAsync(
            Guid courseId, EnrollmentStatus? status, string? search, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Enrollment>, int)>(([], 0));
        public Task<IReadOnlySet<Guid>> GetActiveCourseIdsAsync(
            Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task<IReadOnlyList<Enrollment>> GetActiveByStudentAndCoursesAsync(
            Guid studentId, IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Enrollment>>(items.Where(x =>
                x.StudentId == studentId && courseIds.Contains(x.CourseId)
                && x.Status != EnrollmentStatus.Withdrawn).ToArray());
        public Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public void SetOriginalRowVersion(Enrollment enrollment, byte[] rowVersion) { }
    }

    private sealed class FakeCourseRepository(IReadOnlyList<Course> items) : ICourseRepository
    {
        public CourseStatus? RequestedStatus { get; private set; }
        public Task<Course?> GetByIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == courseId));
        public Task<Course?> GetByIdReadOnlyAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x => x.Id == courseId));
        public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.SingleOrDefault(x => x.Slug == slug && x.Status == CourseStatus.Published));
        public Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Course>>(items.Where(x => ids.Contains(x.Id)).ToArray());
        public Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(
            string? search, CourseStatus? status, Guid? instructorId, int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            RequestedStatus = status;
            var matches = items.Where(x => status is null || x.Status == status).ToArray();
            return Task.FromResult<(IReadOnlyList<Course>, int)>((matches, matches.Length));
        }
        public Task<bool> SlugExistsAsync(
            string slug, Guid? excludedCourseId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task AddAsync(Course course, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Course course) { }
        public void SetOriginalRowVersion(Course course, byte[] rowVersion) { }
    }

    private sealed class FakeLessonRepository : ILessonRepository
    {
        public Task<Lesson?> GetByIdAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Lesson?>(null);
        public Task<Lesson?> GetByIdReadOnlyAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Lesson?>(null);
        public Task<IReadOnlyList<Lesson>> GetPublishedByCourseAsync(
            Guid courseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Lesson>>([]);
        public Task<IReadOnlyList<Lesson>> GetPublishedByCoursesAsync(
            IReadOnlyCollection<Guid> courseIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Lesson>>([]);
        public Task<(IReadOnlyList<Lesson> Items, int TotalCount)> GetPageAsync(
            Guid? courseId, string? search, int pageNumber, int pageSize, Guid? instructorId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Lesson>, int)>(([], 0));
        public Task<bool> OrderExistsAsync(
            Guid courseId, int order, Guid? excludedLessonId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Lesson lesson) { }
        public void SetOriginalRowVersion(Lesson lesson, byte[] rowVersion) { }
        public Task<LessonMoveResult> MoveAsync(
            Guid lessonId, int newOrder, byte[] rowVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(LessonMoveResult.NotFound);
    }

    private sealed class FakeUserManagementService : IUserManagementService
    {
        public Task<Result<LearningPortal.Shared.UserManagement.PagedUsersResponse>> GetUsersAsync(
            string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<UserResponse>> GetUserByIdAsync(
            Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<UserResponse>.Success(
                new(userId, "instructor@example.com", "Instructor", true, ["Instructor"])));
        public Task<IReadOnlyDictionary<Guid, UserResponse>> GetUsersByIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, UserResponse>>(userIds.ToDictionary(
                id => id, id => new UserResponse(
                    id, "instructor@example.com", "Instructor", true, ["Instructor"])));
        public Task<Result<UserResponse>> SetEnabledAsync(
            Guid userId, bool isEnabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<UserResponse>> AssignRoleAsync(
            Guid userId, string role, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
