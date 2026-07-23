using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Skills;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>Seeds a small, repeatable end-to-end learning scenario for local development.</summary>
public sealed class DevelopmentDataSeeder(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    ILogger<DevelopmentDataSeeder> logger) : IDevelopmentDataSeeder
{
    private const string CourseSlug = "aspnet-core-fundamentals-demo";

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var administrator = await EnsureUserAsync(
            "admin@learningportal.local", "Admin123!", "System Administrator",
            [ApplicationRoles.Administrator], cancellationToken);
        var instructor = await EnsureUserAsync(
            "instructor@learningportal.local", "Instructor123!", "Demo Instructor",
            [ApplicationRoles.Instructor], cancellationToken);
        var student = await EnsureUserAsync(
            "student@learningportal.local", "Student123!", "Demo Student",
            [ApplicationRoles.Student], cancellationToken);

        var skill = await context.Skills.SingleOrDefaultAsync(
            value => value.Slug == "aspnet-core", cancellationToken);
        if (skill is null)
        {
            skill = Skill.Create(
                "ASP.NET Core",
                "Dependency injection, middleware, Minimal APIs, and authentication.");
            await context.Skills.AddAsync(skill, cancellationToken);
        }

        var course = await context.Courses.SingleOrDefaultAsync(
            value => value.Slug == CourseSlug, cancellationToken);
        if (course is null)
        {
            course = Course.Create(
                "ASP.NET Core Fundamentals",
                CourseSlug,
                "Introduction to dependency injection, middleware, Minimal APIs, and authentication.",
                "Software Engineering",
                null,
                instructor.Id);
            course.TrySetSkill(skill.Id);
            course.TryPublish();
            await context.Courses.AddAsync(course, cancellationToken);
        }

        await EnsureLessonsAsync(course.Id, cancellationToken);
        var quiz = await EnsureQuizAsync(course.Id, skill.Id, cancellationToken);

        if (!await context.Enrollments.AnyAsync(
                value => value.CourseId == course.Id && value.StudentId == student.Id,
                cancellationToken))
        {
            await context.Enrollments.AddAsync(
                Enrollment.Create(course.Id, student.Id, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        if (!await context.InstructorEligibility.AnyAsync(
                value => value.UserId == instructor.Id && value.SkillId == skill.Id,
                cancellationToken))
        {
            await context.InstructorEligibility.AddAsync(
                InstructorEligibility.Create(
                    instructor.Id,
                    skill.Id,
                    quiz.Id,
                    100m,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Development learning data is ready. Users: {UserCount}; course: {CourseSlug}.",
            3,
            CourseSlug);
        _ = administrator;
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string email,
        string password,
        string displayName,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsEnabled = true
            };
            var result = await userManager.CreateAsync(user, password);
            EnsureSucceeded(result, "create a development user");
        }
        else
        {
            var updateRequired = false;
            if (!user.IsEnabled) { user.IsEnabled = true; updateRequired = true; }
            if (!user.EmailConfirmed) { user.EmailConfirmed = true; updateRequired = true; }
            if (string.IsNullOrWhiteSpace(user.DisplayName))
            {
                user.DisplayName = displayName;
                updateRequired = true;
            }
            if (updateRequired)
            {
                EnsureSucceeded(
                    await userManager.UpdateAsync(user),
                    "update a development user");
            }
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                EnsureSucceeded(
                    await userManager.AddToRoleAsync(user, role),
                    "assign a development role");
            }
        }

        return user;
    }

    private async Task EnsureLessonsAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            ("Dependency Injection", "Learn service lifetimes and constructor injection.",
                "ASP.NET Core includes dependency injection. Register services as transient, scoped, or singleton and request them through constructors."),
            ("Middleware Pipeline", "Understand request pipeline ordering.",
                "Middleware processes HTTP requests in order. Each component can handle the request or call the next component."),
            ("Minimal APIs", "Build concise HTTP endpoints.",
                "Minimal APIs map routes directly to handlers while supporting dependency injection, authorization, validation, and typed results."),
            ("Authentication and Authorization", "Distinguish identity from access control.",
                "Authentication establishes identity. Authorization evaluates roles, policies, and claims before protected operations run.")
        };
        var existing = await context.Lessons
            .Where(value => value.CourseId == courseId)
            .Select(value => value.Title)
            .ToListAsync(cancellationToken);
        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            if (existing.Contains(definition.Item1, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var lesson = Lesson.Create(
                courseId,
                definition.Item1,
                definition.Item2,
                index + 1,
                15,
                LessonType.Article,
                definition.Item3,
                null,
                VideoProvider.None);
            lesson.TryPublish();
            await context.Lessons.AddAsync(lesson, cancellationToken);
        }
    }

    private async Task<Quiz> EnsureQuizAsync(
        Guid courseId,
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var existing = await context.Quizzes
            .Include(value => value.Questions)
            .ThenInclude(value => value.AnswerChoices)
            .SingleOrDefaultAsync(
                value => value.CourseId == courseId
                    && value.Title == "ASP.NET Core Fundamentals Assessment",
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var quiz = Quiz.Create(
            courseId,
            null,
            "ASP.NET Core Fundamentals Assessment",
            "Answer all questions using the published lesson material.",
            70m,
            3,
            required: true);
        quiz.TryConfigureInstructorAssessment(true, skillId);
        AddSingleChoice(
            quiz,
            "Which service lifetime creates one instance per request?",
            ["Transient", "Scoped", "Singleton"],
            2,
            1);
        AddSingleChoice(
            quiz,
            "What controls the order of HTTP request processing?",
            ["Middleware registration order", "Database table order", "CSS order"],
            1,
            2);
        AddSingleChoice(
            quiz,
            "What is authorization responsible for?",
            ["Hashing passwords", "Determining allowed actions", "Creating databases"],
            2,
            3);
        if (!quiz.TryPublish())
        {
            throw new InvalidOperationException("The development quiz could not be published.");
        }

        await context.Quizzes.AddAsync(quiz, cancellationToken);
        return quiz;
    }

    private static void AddSingleChoice(
        Quiz quiz,
        string text,
        IReadOnlyList<string> choices,
        int correctOrder,
        int questionOrder)
    {
        var question = QuizQuestion.Create(
            quiz.Id,
            text,
            QuestionType.SingleChoice,
            1m,
            questionOrder,
            "Review the corresponding published lesson.");
        for (var index = 0; index < choices.Count; index++)
        {
            question.TryAddAnswerChoice(QuizAnswerChoice.Create(
                question.Id,
                choices[index],
                index + 1 == correctOrder,
                index + 1));
        }
        if (!quiz.TryAddQuestion(question))
        {
            throw new InvalidOperationException("The development quiz question is invalid.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to {operation}: "
                + string.Join("; ", result.Errors.Select(error => error.Code)));
        }
    }
}
