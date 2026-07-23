#pragma warning disable CS1591
using LearningPortal.Application.InstructorEligibility;
using LearningPortal.Blazor.Services;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using LearningPortal.Domain.Skills;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Repositories;
using LearningPortal.Shared.InstructorEligibility;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.InstructorEligibility;

public sealed class InstructorEligibilityTests
{
    [Fact]
    public async Task ExactlyThreshold_Qualifies_AndRepeatedProcessingDoesNotDuplicate()
    {
        var skill = Skill.Create("Cloud Engineering");
        var (quiz, attempt) = CreateResult(skill.Id, correctQuestions: 4, totalQuestions: 5);
        var repository = new FakeEligibilityRepository();

        await InstructorEligibilityCalculator.ApplyAsync(attempt, quiz, repository, 80, UtcNow, default);
        await InstructorEligibilityCalculator.ApplyAsync(attempt, quiz, repository, 80, UtcNow, default);

        var value = Assert.Single(repository.Values);
        Assert.Equal(80, value.BestPercentage);
        Assert.True(value.IsEligible);
    }

    [Fact]
    public async Task BelowThreshold_DoesNotQualify()
    {
        var skill = Skill.Create("Data");
        var (quiz, attempt) = CreateResult(skill.Id, 3, 5);
        var repository = new FakeEligibilityRepository();
        await InstructorEligibilityCalculator.ApplyAsync(attempt, quiz, repository, 80, UtcNow, default);
        Assert.Empty(repository.Values);
    }

    [Fact]
    public async Task AboveThreshold_Qualifies_ButFailedAttemptDoesNot()
    {
        var skill = Skill.Create("Architecture");
        var repository = new FakeEligibilityRepository();
        var (qualifiedQuiz, qualifiedAttempt) = CreateResult(skill.Id, 5, 5);
        await InstructorEligibilityCalculator.ApplyAsync(qualifiedAttempt, qualifiedQuiz, repository, 80, UtcNow, default);
        Assert.Single(repository.Values);

        var failedQuiz = CreateQuiz(skill.Id, assessment: true, publish: true, questions: 5, passingPercentage: 100);
        var failedAttempt = CreateSubmittedAttempt(failedQuiz, 4, 5);
        Assert.False(failedAttempt.Passed);
        await InstructorEligibilityCalculator.ApplyAsync(failedAttempt, failedQuiz, repository, 80, UtcNow, default);
        Assert.Equal(100, repository.Values.Single().BestPercentage);
        Assert.True(repository.Values.Single().IsEligible);
    }

    [Fact]
    public async Task OrdinaryDraftAndArchivedQuizzes_DoNotQualify()
    {
        var skill = Skill.Create("Security");
        var repository = new FakeEligibilityRepository();
        var (ordinary, ordinaryAttempt) = CreateResult(skill.Id, 5, 5, assessment: false);
        await InstructorEligibilityCalculator.ApplyAsync(ordinaryAttempt, ordinary, repository, 80, UtcNow, default);
        var (archived, archivedAttempt) = CreateResult(skill.Id, 5, 5, archive: true);
        await InstructorEligibilityCalculator.ApplyAsync(archivedAttempt, archived, repository, 80, UtcNow, default);
        var draft = CreateQuiz(skill.Id, assessment: true, publish: false);
        var draftAttempt = CreateSubmittedAttempt(draft, 5, 5);
        await InstructorEligibilityCalculator.ApplyAsync(draftAttempt, draft, repository, 80, UtcNow, default);
        Assert.Empty(repository.Values);
    }

    [Fact]
    public void LaterLowerResult_DoesNotReduceOrRevokeEligibility()
    {
        var user = Guid.NewGuid();
        var skill = Guid.NewGuid();
        var value = Domain.Skills.InstructorEligibility.Create(user, skill, Guid.NewGuid(), 95, UtcNow);
        Assert.False(value.TryUpdateBest(Guid.NewGuid(), 82));
        Assert.Equal(95, value.BestPercentage);
        Assert.True(value.IsEligible);
    }

    [Fact]
    public async Task Eligibility_IsScopedToOneSkill()
    {
        var user = Guid.NewGuid();
        var first = Guid.NewGuid();
        var repository = new FakeEligibilityRepository();
        await repository.AddAsync(Domain.Skills.InstructorEligibility.Create(user, first, Guid.NewGuid(), 90, UtcNow));
        Assert.True(await repository.IsEligibleAsync(user, first));
        Assert.False(await repository.IsEligibleAsync(user, Guid.NewGuid()));
    }

    [Fact]
    public void Model_HasUniqueUserSkillIndex()
    {
        using var context = Context();
        var entity = context.Model.FindEntityType(typeof(Domain.Skills.InstructorEligibility))!;
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Domain.Skills.InstructorEligibility.UserId), nameof(Domain.Skills.InstructorEligibility.SkillId)]));
    }

    [Fact]
    public async Task ReadOnlyEligibilityQuery_DoesNotTrack()
    {
        await using var context = Context();
        var repository = new InstructorEligibilityRepository(context);
        await repository.GetByUserAsync(Guid.NewGuid());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ApiClient_UsesEligibilityRoutes()
    {
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => Json<IReadOnlyList<InstructorEligibilityResponse>>([]));
        var client = new LearningPortalApiClient(new HttpClient(handler) { BaseAddress = new("https://localhost/") });
        await client.RecalculateInstructorEligibilityAsync(userId);
        Assert.Equal($"/api/users/{userId:D}/instructor-eligibility/recalculate", handler.Uri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.Method);
    }

    private static readonly DateTimeOffset UtcNow = new(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
    private static (Quiz Quiz, QuizAttempt Attempt) CreateResult(
        Guid skillId, int correctQuestions, int totalQuestions, bool assessment = true, bool archive = false)
    {
        var quiz = CreateQuiz(skillId, assessment, publish: true, totalQuestions);
        if (archive) quiz.TryArchive();
        return (quiz, CreateSubmittedAttempt(quiz, correctQuestions, totalQuestions));
    }
    private static Quiz CreateQuiz(
        Guid skillId, bool assessment, bool publish, int questions = 5, decimal passingPercentage = 1)
    {
        var quiz = Quiz.Create(Guid.NewGuid(), null, "Assessment", "", passingPercentage, null, false);
        if (assessment) quiz.TryConfigureInstructorAssessment(true, skillId);
        for (var index = 1; index <= questions; index++)
        {
            var question = QuizQuestion.Create(quiz.Id, $"Q{index}", QuestionType.SingleChoice, 1, index);
            question.TryAddAnswerChoice(QuizAnswerChoice.Create(question.Id, "Yes", true, 1));
            question.TryAddAnswerChoice(QuizAnswerChoice.Create(question.Id, "No", false, 2));
            quiz.TryAddQuestion(question);
        }
        if (publish) Assert.True(quiz.TryPublish());
        return quiz;
    }
    private static QuizAttempt CreateSubmittedAttempt(Quiz quiz, int correctQuestions, int totalQuestions)
    {
        var attempt = QuizAttempt.Start(quiz.Id, Guid.NewGuid(), Guid.NewGuid(), 1, UtcNow);
        var answers = quiz.Questions.OrderBy(value => value.Order).Select((question, index) =>
            QuizAttemptAnswer.Create(attempt.Id, question,
                [question.AnswerChoices.Single(choice => choice.IsCorrect == (index < correctQuestions)).Id])).ToArray();
        Assert.True(attempt.TrySubmit(answers, quiz.PassingPercentage, UtcNow));
        Assert.Equal(totalQuestions, answers.Length);
        return attempt;
    }
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class FakeEligibilityRepository : IInstructorEligibilityRepository
    {
        public List<Domain.Skills.InstructorEligibility> Values { get; } = [];
        public Task<Domain.Skills.InstructorEligibility?> GetAsync(Guid userId, Guid skillId, CancellationToken ct = default) =>
            Task.FromResult(Values.SingleOrDefault(value => value.UserId == userId && value.SkillId == skillId));
        public Task<IReadOnlyList<Domain.Skills.InstructorEligibility>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Domain.Skills.InstructorEligibility>>(Values.Where(value => value.UserId == userId).ToArray());
        public Task<IReadOnlyList<Domain.Skills.InstructorEligibility>> GetEligibleBySkillAsync(Guid skillId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Domain.Skills.InstructorEligibility>>(Values.Where(value => value.SkillId == skillId).ToArray());
        public Task<bool> IsEligibleAsync(Guid userId, Guid skillId, CancellationToken ct = default) =>
            Task.FromResult(Values.Any(value => value.UserId == userId && value.SkillId == skillId && value.IsEligible));
        public Task<QuizAttempt?> GetBestAttemptAsync(Guid userId, Guid skillId, CancellationToken ct = default) => Task.FromResult<QuizAttempt?>(null);
        public Task AddAsync(Domain.Skills.InstructorEligibility value, CancellationToken ct = default) { Values.Add(value); return Task.CompletedTask; }
    }
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Uri = request.RequestUri; Method = request.Method; return Task.FromResult(response(request)); }
    }
}
