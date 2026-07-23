using LearningPortal.Domain.AiTutor;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Configures learner-owned AI Tutor conversations.</summary>
public sealed class AiTutorConversationConfiguration
    : IEntityTypeConfiguration<AiTutorConversation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiTutorConversation> builder)
    {
        builder.ToTable("AiTutorConversations");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Title).HasMaxLength(200).IsRequired();
        builder.Property(value => value.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(value => value.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(value => value.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(value => value.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.StudentId);
        builder.HasIndex(value => value.CourseId);
        builder.HasIndex(value => value.LessonId);
        builder.HasIndex(value => value.LastMessageAtUtc);
        builder.HasIndex(value => value.Status);
    }
}

/// <summary>Configures ordered, user-visible AI Tutor messages.</summary>
public sealed class AiTutorMessageConfiguration
    : IEntityTypeConfiguration<AiTutorMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiTutorMessage> builder)
    {
        builder.ToTable("AiTutorMessages");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Role)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(value => value.Content).HasMaxLength(10_000).IsRequired();
        builder.HasOne<AiTutorConversation>()
            .WithMany(value => value.Messages)
            .HasForeignKey(value => value.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(value => new { value.ConversationId, value.Sequence })
            .IsUnique();
    }
}
