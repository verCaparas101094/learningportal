#pragma warning disable CS1591
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

public sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public const string ActiveAttemptIndexName = "UX_QuizAttempts_Quiz_Student_Active";

    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("QuizAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(attempt => attempt.Score).HasPrecision(10, 2);
        builder.Property(attempt => attempt.MaximumScore).HasPrecision(10, 2);
        builder.Property(attempt => attempt.Percentage).HasPrecision(5, 2);
        builder.Property(attempt => attempt.RowVersion).IsRowVersion();
        builder.HasOne<Quiz>().WithMany().HasForeignKey(attempt => attempt.QuizId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Enrollment>().WithMany().HasForeignKey(attempt => attempt.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(attempt => attempt.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attempt => new { attempt.QuizId, attempt.StudentId, attempt.AttemptNumber }).IsUnique();
        builder.HasIndex(attempt => new { attempt.QuizId, attempt.StudentId })
            .HasDatabaseName(ActiveAttemptIndexName)
            .HasFilter("[Status] = N'InProgress'")
            .IsUnique();
        builder.HasIndex(attempt => attempt.EnrollmentId);
    }
}

public sealed class QuizAttemptAnswerConfiguration : IEntityTypeConfiguration<QuizAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAttemptAnswer> builder)
    {
        builder.ToTable("QuizAttemptAnswers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.QuestionText).HasMaxLength(4000).IsRequired();
        builder.Property(answer => answer.QuestionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(answer => answer.SelectedChoiceIds).IsRequired();
        builder.Property(answer => answer.ChoiceSnapshot).IsRequired();
        builder.Property(answer => answer.PointsAwarded).HasPrecision(10, 2);
        builder.Property(answer => answer.MaximumPoints).HasPrecision(10, 2);
        builder.Property(answer => answer.Explanation).HasMaxLength(4000);
        builder.HasOne<QuizAttempt>().WithMany(attempt => attempt.Answers)
            .HasForeignKey(answer => answer.AttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(answer => new { answer.AttemptId, answer.QuestionId }).IsUnique();
    }
}
