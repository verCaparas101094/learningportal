using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Courses.Exceptions;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence.Configurations;
using LearningPortal.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence;

/// <summary>Provides the EF Core unit of work for domain data and ASP.NET Identity.</summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    /// <summary>Gets the course data set.</summary>
    public DbSet<Course> Courses => Set<Course>();
    /// <summary>Gets course lessons.</summary>
    public DbSet<Lesson> Lessons => Set<Lesson>();

    /// <summary>Gets the persisted hashed refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        builder.ConfigureDomainFoundation();
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries.Any(entry => entry.Entity is Course))
        {
            throw new CourseConcurrencyException(exception);
        }
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries.Any(entry => entry.Entity is Lesson))
        {
            throw new LessonConcurrencyException(exception);
        }
        catch (DbUpdateException exception)
            when (IsCourseSlugUniqueIndexViolation(exception))
        {
            throw new DuplicateCourseSlugException(exception);
        }
        catch (DbUpdateException exception)
            when (IsLessonOrderUniqueIndexViolation(exception))
        {
            throw new DuplicateLessonOrderException(exception);
        }
    }

    private static bool IsCourseSlugUniqueIndexViolation(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is Course)
        && exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(
            CourseConfiguration.SlugUniqueIndexName,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsLessonOrderUniqueIndexViolation(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is Lesson)
        && exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(LessonConfiguration.CourseOrderIndexName, StringComparison.OrdinalIgnoreCase);
}
