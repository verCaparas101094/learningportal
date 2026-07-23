#pragma warning disable CS1591
using LearningPortal.Domain.Quizzes;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Repositories;
using LearningPortal.Shared.Quizzes;
using LearningPortal.Shared.Results;
using LearningPortal.Blazor.Services;
using LearningPortal.Application.Quizzes;
using LearningPortal.Application;
using LearningPortal.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Quizzes;

public sealed class QuizSubsystemTests
{
    [Fact]
    public void MultipleChoice_UsesExactMatchScoring()
    {
        var quizId = Guid.NewGuid();
        var question = QuizQuestion.Create(quizId, "Select both.", QuestionType.MultipleChoice, 5, 1);
        var first = QuizAnswerChoice.Create(question.Id, "A", true, 1);
        var second = QuizAnswerChoice.Create(question.Id, "B", true, 2);
        var distractor = QuizAnswerChoice.Create(question.Id, "C", false, 3);
        question.TryAddAnswerChoice(first);
        question.TryAddAnswerChoice(second);
        question.TryAddAnswerChoice(distractor);
        var attempt = QuizAttempt.Start(quizId, Guid.NewGuid(), Guid.NewGuid(), 1, DateTimeOffset.UtcNow);

        var partial = QuizAttemptAnswer.Create(attempt.Id, question, [first.Id]);
        var exact = QuizAttemptAnswer.Create(attempt.Id, question, [second.Id, first.Id]);

        Assert.False(partial.IsCorrect);
        Assert.Equal(0, partial.PointsAwarded);
        Assert.True(exact.IsCorrect);
        Assert.Equal(5, exact.PointsAwarded);
    }

    [Fact]
    public void SubmittedAttempt_IsImmutableAndCalculatesServerFields()
    {
        var question = QuizQuestion.Create(Guid.NewGuid(), "True?", QuestionType.TrueFalse, 4, 1);
        var correct = QuizAnswerChoice.Create(question.Id, "True", true, 1);
        question.TryAddAnswerChoice(correct);
        question.TryAddAnswerChoice(QuizAnswerChoice.Create(question.Id, "False", false, 2));
        var attempt = QuizAttempt.Start(question.QuizId, Guid.NewGuid(), Guid.NewGuid(), 1, DateTimeOffset.UtcNow);
        var answer = QuizAttemptAnswer.Create(attempt.Id, question, [correct.Id]);

        Assert.True(attempt.TrySubmit([answer], 80, DateTimeOffset.UtcNow));
        Assert.False(attempt.TrySubmit([answer], 80, DateTimeOffset.UtcNow));
        Assert.Equal(4, attempt.Score);
        Assert.Equal(4, attempt.MaximumScore);
        Assert.Equal(100, attempt.Percentage);
        Assert.True(attempt.Passed);
    }

    [Fact]
    public void LearnerChoiceContract_DoesNotExposeCorrectness()
    {
        Assert.DoesNotContain(
            typeof(QuizChoiceResponse).GetProperties(),
            property => property.Name.Equals("IsCorrect", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AttemptRepository_EnforcesOwnershipFiltersAndReadOnlyQueries()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var repository = new QuizAttemptRepository(context);
        var student = Guid.NewGuid();
        var quiz = Guid.NewGuid();
        var attempt = QuizAttempt.Start(quiz, Guid.NewGuid(), student, 1, DateTimeOffset.UtcNow);
        await repository.AddAsync(attempt);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await repository.GetByQuizAndStudentAsync(quiz, student);

        Assert.Single(loaded);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await repository.GetByQuizAndStudentAsync(quiz, Guid.NewGuid()));
    }

    [Fact]
    public void SubmitValidator_RejectsDuplicateQuestionsAndChoices()
    {
        var questionId = Guid.NewGuid();
        var choiceId = Guid.NewGuid();
        var command = new SubmitQuizAttempt(
            Guid.NewGuid(),
            new([
                new(questionId, [choiceId, choiceId]),
                new(questionId, [])
            ]));

        var result = new SubmitQuizAttemptValidator().Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.ErrorMessage.Contains("question", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, failure => failure.ErrorMessage.Contains("Duplicate choices", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApiClient_UsesAuthenticatedQuizAttemptRoutes()
    {
        var quizId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/attempts", StringComparison.Ordinal))
                return Json(new StartQuizAttemptResponse(attemptId, 1, false));
            return Json(new QuizAttemptResponse(
                attemptId, quizId, 1, "Submitted", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                1, 1, 100, true, []));
        });
        var client = new LearningPortalApiClient(new HttpClient(handler) { BaseAddress = new("https://localhost/") });

        await client.StartQuizAttemptAsync(quizId);
        Assert.Equal($"/api/learning/quizzes/{quizId:D}/attempts", handler.Uri!.AbsolutePath);
        await client.SubmitQuizAttemptAsync(attemptId, new([]));
        Assert.Equal($"/api/learning/quiz-attempts/{attemptId:D}/submit", handler.Uri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.Method);
    }

    [Fact]
    public async Task ApiClient_UsesEnrollmentAuthorizedCourseQuizRoute()
    {
        var courseId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => Json<IReadOnlyList<QuizListItemResponse>>([]));
        var client = new LearningPortalApiClient(
            new HttpClient(handler) { BaseAddress = new("https://localhost/") });

        await client.GetCourseQuizzesForLearnerAsync(courseId);

        Assert.Equal(
            $"/api/learning/courses/{courseId:D}/quizzes",
            handler.Uri!.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Method);
    }

    [Fact]
    public void LearnerCourseQuizQueryHandler_IsRegisteredForMinimalApiInjection()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(
                IQueryHandler<
                    GetCourseQuizzes,
                    Result<IReadOnlyList<QuizListItemResponse>>>));
    }

    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            Method = request.Method;
            return Task.FromResult(response(request));
        }
    }
}
