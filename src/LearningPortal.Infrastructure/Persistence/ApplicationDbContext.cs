using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Courses.Exceptions;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Enrollments.Exceptions;
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Skills;
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
    /// <summary>Gets course enrollments.</summary>
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    /// <summary>Gets learner lesson progress records.</summary>
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    /// <summary>Gets quizzes.</summary>
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    /// <summary>Gets quiz questions.</summary>
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    /// <summary>Gets quiz answer choices.</summary>
    public DbSet<QuizAnswerChoice> QuizAnswerChoices => Set<QuizAnswerChoice>();
    /// <summary>Gets owned quiz attempts.</summary>
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    /// <summary>Gets submitted answer snapshots.</summary>
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    /// <summary>Gets stable skills.</summary>
    public DbSet<Skill> Skills => Set<Skill>();
    /// <summary>Gets instructor eligibility records.</summary>
    public DbSet<InstructorEligibility> InstructorEligibility => Set<InstructorEligibility>();

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
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries.Any(entry => entry.Entity is Enrollment))
        {
            throw new EnrollmentConcurrencyException(exception);
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
        catch (DbUpdateException exception)
            when (IsEnrollmentUniqueIndexViolation(exception))
        {
            throw new DuplicateActiveEnrollmentException(exception);
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

    private static bool IsEnrollmentUniqueIndexViolation(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is Enrollment)
        && exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(
            EnrollmentConfiguration.ActiveEnrollmentIndexName,
            StringComparison.OrdinalIgnoreCase);
}
