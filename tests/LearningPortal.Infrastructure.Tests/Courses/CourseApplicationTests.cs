using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.Courses.Commands.ArchiveCourse;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Commands.PublishCourse;
using LearningPortal.Application.Courses.Commands.UpdateCourse;
using LearningPortal.Application.Courses.Queries.GetCourseById;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Courses.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Courses;

/// <summary>Verifies course ownership, conflicts, state rules, and pagination.</summary>
public sealed class CourseApplicationTests
{
    /// <summary>Verifies administrator instructor assignment.</summary>
    [Fact]
    public async Task Administrator_CanCreateForInstructor()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);

        var result = await context.CreateHandler.HandleAsync(CreateCommand(instructorId));

        Assert.True(result.IsSuccess);
        Assert.Equal(instructorId, result.Value.InstructorId);
    }

    /// <summary>Verifies instructor self-assignment.</summary>
    [Fact]
    public async Task InstructorCreate_IgnoresRequestedInstructor()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(
            ApplicationRoles.Instructor,
            instructorId,
            Guid.CreateVersion7());

        var result = await context.CreateHandler.HandleAsync(CreateCommand(Guid.CreateVersion7()));

        Assert.True(result.IsSuccess);
        Assert.Equal(instructorId, result.Value.InstructorId);
    }

    /// <summary>Verifies instructor ownership.</summary>
    [Fact]
    public async Task Instructor_CannotViewOrEditAnotherInstructorsCourse()
    {
        var ownerId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Instructor, Guid.CreateVersion7(), ownerId);
        var course = CreateCourse(ownerId);
        context.Repository.Courses.Add(course);

        var view = await new GetCourseByIdQueryHandler(context.Repository, context.CurrentUser)
            .HandleAsync(new GetCourseByIdQuery(course.Id));
        var update = await context.UpdateHandler.HandleAsync(new UpdateCourseCommand(
            course.Id,
            "Updated",
            "updated",
            string.Empty,
            "Category",
            null,
            "AQ=="));

        Assert.Equal(ErrorType.Forbidden, view.Error?.ErrorType);
        Assert.Equal(ErrorType.Forbidden, update.Error?.ErrorType);
    }

    /// <summary>Verifies server-side ownership filtering.</summary>
    [Fact]
    public async Task ListQuery_FiltersInstructorOwnedCourses()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Instructor, instructorId, instructorId);
        context.Repository.Courses.Add(CreateCourse(instructorId, "owned"));
        context.Repository.Courses.Add(CreateCourse(Guid.CreateVersion7(), "other"));

        var result = await context.ListHandler.HandleAsync(new GetCoursesQuery(null, null, 1, 10));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(instructorId, result.Value.Items.Single().InstructorId);
    }

    /// <summary>Verifies duplicate slug rejection.</summary>
    [Fact]
    public async Task DuplicateSlug_IsRejected()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);
        context.Repository.Courses.Add(CreateCourse(instructorId, "existing"));

        var result = await context.CreateHandler.HandleAsync(
            CreateCommand(instructorId) with { Slug = "Existing" });

        Assert.Equal("Course.DuplicateSlug", result.Error?.Code);
    }

    /// <summary>Verifies course pagination.</summary>
    [Fact]
    public async Task ListQuery_Paginates()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);
        for (var index = 0; index < 15; index++)
        {
            context.Repository.Courses.Add(CreateCourse(instructorId, $"course-{index}"));
        }

        var result = await context.ListHandler.HandleAsync(new GetCoursesQuery(null, null, 2, 10));

        Assert.Equal(5, result.Value.Items.Count);
        Assert.Equal(15, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
    }

    /// <summary>Verifies publication state requirements.</summary>
    [Fact]
    public async Task Publish_RequiresDraft()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);
        var course = CreateCourse(instructorId);
        course.TryPublish();
        course.TryArchive();
        context.Repository.Courses.Add(course);

        var result = await context.PublishHandler.HandleAsync(new PublishCourseCommand(course.Id));

        Assert.Equal("Course.InvalidState", result.Error?.Code);
    }

    /// <summary>Verifies archival state requirements.</summary>
    [Fact]
    public async Task Archive_RequiresPublished()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);
        var course = CreateCourse(instructorId);
        context.Repository.Courses.Add(course);

        var result = await context.ArchiveHandler.HandleAsync(new ArchiveCourseCommand(course.Id));

        Assert.Equal("Course.InvalidState", result.Error?.Code);
    }

    /// <summary>Verifies concurrency result mapping.</summary>
    [Fact]
    public async Task Update_MapsRowVersionConflict()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Administrator, Guid.CreateVersion7(), instructorId);
        var course = CreateCourse(instructorId);
        context.Repository.Courses.Add(course);
        context.UnitOfWork.Exception = new CourseConcurrencyException(new Exception("Concurrent update."));

        var result = await context.UpdateHandler.HandleAsync(new UpdateCourseCommand(
            course.Id,
            "Updated",
            "updated",
            string.Empty,
            "Category",
            null,
            "AQ=="));

        Assert.Equal("Course.ConcurrencyConflict", result.Error?.Code);
    }

    /// <summary>Verifies Student denial.</summary>
    [Fact]
    public async Task Student_IsForbidden()
    {
        var instructorId = Guid.CreateVersion7();
        var context = CreateContext(ApplicationRoles.Student, Guid.CreateVersion7(), instructorId);

        var result = await context.ListHandler.HandleAsync(new GetCoursesQuery(null, null, 1, 10));

        Assert.Equal(ErrorType.Forbidden, result.Error?.ErrorType);
    }

    private static CourseTestContext CreateContext(
        string role,
        Guid userId,
        Guid validInstructorId)
    {
        var repository = new FakeCourseRepository();
        var unitOfWork = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUser(userId, role);
        var userManagement = new FakeUserManagementService(validInstructorId);

        return new CourseTestContext(
            repository,
            unitOfWork,
            currentUser,
            new CreateCourseCommandHandler(
                repository,
                unitOfWork,
                currentUser,
                userManagement,
                NullLogger<CreateCourseCommandHandler>.Instance),
            new UpdateCourseCommandHandler(
                repository,
                unitOfWork,
                currentUser,
                NullLogger<UpdateCourseCommandHandler>.Instance),
            new PublishCourseCommandHandler(
                repository,
                unitOfWork,
                currentUser,
                NullLogger<PublishCourseCommandHandler>.Instance),
            new ArchiveCourseCommandHandler(
                repository,
                unitOfWork,
                currentUser,
                NullLogger<ArchiveCourseCommandHandler>.Instance),
            new GetCoursesQueryHandler(
                repository,
                currentUser,
                new GetCoursesQueryValidator()));
    }

    private static CreateCourseCommand CreateCommand(Guid? instructorId) => new(
        "Course",
        "course",
        "Description",
        "Category",
        null,
        instructorId);

    private static Course CreateCourse(
        Guid instructorId,
        string slug = "course") =>
        Course.Create("Course", slug, "Description", "Category", null, instructorId);

    private sealed record CourseTestContext(
        FakeCourseRepository Repository,
        FakeUnitOfWork UnitOfWork,
        FakeCurrentUser CurrentUser,
        CreateCourseCommandHandler CreateHandler,
        UpdateCourseCommandHandler UpdateHandler,
        PublishCourseCommandHandler PublishHandler,
        ArchiveCourseCommandHandler ArchiveHandler,
        GetCoursesQueryHandler ListHandler);

    private sealed class FakeCourseRepository : ICourseRepository
    {
        public List<Course> Courses { get; } = [];

        public Task<Course?> GetByIdAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Courses.SingleOrDefault(course => course.Id == courseId));

        public Task<Course?> GetByIdReadOnlyAsync(
            Guid courseId,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(courseId, cancellationToken);

        public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(Courses.SingleOrDefault(course => course.Slug == slug && course.Status == CourseStatus.Published));

        public Task<IReadOnlyList<Course>> GetByIdsReadOnlyAsync(
            IReadOnlyCollection<Guid> courseIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Course>>(Courses.Where(course => courseIds.Contains(course.Id)).ToArray());

        public Task<(IReadOnlyList<Course> Items, int TotalCount)> GetPageAsync(
            string? search,
            CourseStatus? status,
            Guid? instructorId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = Courses.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(course =>
                    course.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || course.Slug.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || course.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (status is not null)
            {
                query = query.Where(course => course.Status == status);
            }

            if (instructorId is not null)
            {
                query = query.Where(course => course.InstructorId == instructorId);
            }

            var all = query.ToArray();
            IReadOnlyList<Course> page = all
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult((page, all.Length));
        }

        public Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludedCourseId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Courses.Any(course =>
                course.Slug == slug
                && (excludedCourseId is null || course.Id != excludedCourseId)));

        public Task AddAsync(Course course, CancellationToken cancellationToken = default)
        {
            Courses.Add(course);
            return Task.CompletedTask;
        }

        public void Remove(Course course) => Courses.Remove(course);

        public void SetOriginalRowVersion(Course course, byte[] rowVersion)
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Exception? Exception { get; set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(1);
        }
    }

    private sealed class FakeCurrentUser(Guid userId, string role) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
        public string? DisplayName => "Course Manager";
        public string? Email => "manager@example.com";
        public IReadOnlyCollection<string> Roles { get; } = [role];
        public bool IsAuthenticated => true;

        public bool HasRole(string value) =>
            Roles.Contains(value, StringComparer.OrdinalIgnoreCase);

        public bool HasClaim(string claimType, string? claimValue = null) => false;
    }

    private sealed class FakeUserManagementService(Guid validInstructorId) : IUserManagementService
    {
        public Task<IReadOnlyDictionary<Guid, UserResponse>> GetUsersByIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, UserResponse>>(new Dictionary<Guid, UserResponse>());

        public Task<Result<UserResponse>> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == validInstructorId
                ? Result<UserResponse>.Success(new UserResponse(
                    userId,
                    "instructor@example.com",
                    "Instructor",
                    true,
                    [ApplicationRoles.Instructor]))
                : Result<UserResponse>.Failure(Errors.UserManagement.UserNotFound(userId)));

        public Task<Result<PagedUsersResponse>> GetUsersAsync(
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<UserResponse>> SetEnabledAsync(
            Guid userId,
            bool isEnabled,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<UserResponse>> AssignRoleAsync(
            Guid userId,
            string role,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
