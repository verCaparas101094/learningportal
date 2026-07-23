#pragma warning disable CS1591
using LearningPortal.Domain.Quizzes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LearningPortal.Infrastructure.Persistence.Configurations;
public sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{ public void Configure(EntityTypeBuilder<Quiz> b){b.ToTable("Quizzes");b.HasKey(x=>x.Id);b.Property(x=>x.Title).HasMaxLength(200).IsRequired();b.Property(x=>x.Description).HasMaxLength(2000);b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20);b.Property(x=>x.PassingPercentage).HasPrecision(5,2);b.Property(x=>x.RowVersion).IsRowVersion();b.HasOne<LearningPortal.Domain.Courses.Course>().WithMany().HasForeignKey(x=>x.CourseId).OnDelete(DeleteBehavior.Restrict);b.HasOne<LearningPortal.Domain.Lessons.Lesson>().WithMany().HasForeignKey(x=>x.LessonId).OnDelete(DeleteBehavior.Restrict);b.HasOne<LearningPortal.Domain.Skills.Skill>().WithMany().HasForeignKey(x=>x.SkillId).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>x.CourseId);b.HasIndex(x=>x.LessonId);b.HasIndex(x=>new{x.SkillId,x.IsInstructorAssessment,x.Status});} }
public sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{ public void Configure(EntityTypeBuilder<QuizQuestion> b){b.ToTable("QuizQuestions");b.HasKey(x=>x.Id);b.Property(x=>x.Text).HasMaxLength(4000).IsRequired();b.Property(x=>x.QuestionType).HasConversion<string>().HasMaxLength(20);b.Property(x=>x.Points).HasPrecision(10,2);b.Property(x=>x.Explanation).HasMaxLength(4000);b.Property(x=>x.RowVersion).IsRowVersion();b.HasOne<Quiz>().WithMany(x=>x.Questions).HasForeignKey(x=>x.QuizId).OnDelete(DeleteBehavior.Cascade);b.HasIndex(x=>new{x.QuizId,x.Order}).IsUnique();} }
public sealed class QuizAnswerChoiceConfiguration : IEntityTypeConfiguration<QuizAnswerChoice>
{ public void Configure(EntityTypeBuilder<QuizAnswerChoice> b){b.ToTable("QuizAnswerChoices");b.HasKey(x=>x.Id);b.Property(x=>x.Text).HasMaxLength(2000).IsRequired();b.HasOne<QuizQuestion>().WithMany(x=>x.AnswerChoices).HasForeignKey(x=>x.QuestionId).OnDelete(DeleteBehavior.Cascade);b.HasIndex(x=>new{x.QuestionId,x.Order}).IsUnique();} }
