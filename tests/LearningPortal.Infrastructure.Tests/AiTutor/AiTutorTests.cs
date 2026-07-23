#pragma warning disable CS1591

using System.Net;
using System.Text;
using LearningPortal.Application.AiTutor;
using LearningPortal.Domain.AiTutor;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Lessons;
using LearningPortal.Infrastructure.AiTutor;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.AiTutor;

public sealed class AiTutorTests
{
    [Fact]
    public void Conversation_PersistsOnlyOrderedVisibleExchange()
    {
        var conversation = AiTutorConversation.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Secure coding",
            DateTimeOffset.UtcNow);

        var added = conversation.TryAddExchange(
            "What is least privilege?",
            "It means granting only necessary access.",
            DateTimeOffset.UtcNow);

        Assert.True(added);
        Assert.Collection(
            conversation.Messages.OrderBy(message => message.Sequence),
            message =>
            {
                Assert.Equal(AiTutorMessageRole.User, message.Role);
                Assert.Equal(1, message.Sequence);
            },
            message =>
            {
                Assert.Equal(AiTutorMessageRole.Assistant, message.Role);
                Assert.Equal(2, message.Sequence);
            });
        Assert.DoesNotContain(
            conversation.Messages,
            message => message.Role.ToString() == "System");
    }

    [Fact]
    public void ArchivedConversation_RejectsNewMessages_AndArchiveIsIdempotent()
    {
        var conversation = CreateConversation();

        Assert.True(conversation.TryArchive());
        Assert.True(conversation.TryArchive());
        Assert.False(conversation.TryAddExchange(
            "Question", "Reply", DateTimeOffset.UtcNow));
        Assert.Equal(AiTutorConversationStatus.Archived, conversation.Status);
    }

    [Fact]
    public async Task Repository_ReadOnlyQueries_DoNotTrackAndPreserveOrdering()
    {
        await using var context = CreateContext();
        var conversation = CreateConversation();
        conversation.TryAddExchange("First", "Second", DateTimeOffset.UtcNow);
        await context.AiTutorConversations.AddAsync(conversation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new AiTutorConversationRepository(context);

        var loaded = await repository.GetAsync(
            conversation.Id, readOnly: true, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal([1, 2], loaded.Messages.Select(message => message.Sequence));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public void Model_HasUniqueConversationSequenceAndRestrictiveHistoricalLinks()
    {
        using var context = CreateContext();
        var message = context.Model.FindEntityType(typeof(AiTutorMessage))!;
        var conversation = context.Model.FindEntityType(typeof(AiTutorConversation))!;

        Assert.Contains(
            message.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(["ConversationId", "Sequence"]));
        Assert.All(
            conversation.GetForeignKeys(),
            relationship => Assert.Equal(DeleteBehavior.Restrict, relationship.DeleteBehavior));
    }

    [Fact]
    public async Task OllamaClient_MapsSuccessfulChatReply()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"message":{"role":"assistant","content":"Grounded answer"}}""");
        var client = CreateClient(handler);

        var result = await client.GenerateAsync(
            "Course content",
            [],
            "Question",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Grounded answer", result.Content);
        Assert.Contains("COURSE MATERIAL (UNTRUSTED)", handler.LastRequestBody);
        Assert.DoesNotContain("localhost", handler.LastRequestBody);
    }

    [Theory]
    [InlineData("""{"message":{"content":""}}""", "InvalidResponse")]
    [InlineData("""{"unexpected":true}""", "InvalidResponse")]
    [InlineData("not-json", "InvalidResponse")]
    public async Task OllamaClient_RejectsInvalidResponses(string body, string expected)
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, body));

        var result = await client.GenerateAsync(
            "Context", [], "Question", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(expected, result.ErrorCode);
    }

    [Fact]
    public async Task OllamaClient_MapsModelNotFound()
    {
        var client = CreateClient(
            new StubHandler(HttpStatusCode.NotFound, """{"error":"model not found"}"""));

        var result = await client.GenerateAsync(
            "Context", [], "Question", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("ModelNotFound", result.ErrorCode);
    }

    [Fact]
    public async Task OllamaClient_Disabled_DoesNotMakeHttpRequest()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var options = ValidOptions();
        options.Enabled = false;
        var client = new OllamaClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/") },
            options,
            NullLogger<OllamaClient>.Instance);

        var result = await client.GenerateAsync(
            "Context", [], "Question", CancellationToken.None);

        Assert.Equal("Disabled", result.ErrorCode);
        Assert.Null(handler.LastRequestBody);
    }

    [Fact]
    public async Task OllamaHealth_ReportsConfiguredModelAvailability()
    {
        var client = CreateClient(new StubHandler(
            HttpStatusCode.OK,
            """{"models":[{"name":"llama3.2:3b"}]}"""));

        var health = await client.CheckHealthAsync(CancellationToken.None);

        Assert.True(health.Enabled);
        Assert.True(health.Reachable);
        Assert.True(health.ModelAvailable);
        Assert.Equal("Healthy", health.Status);
    }

    [Fact]
    public async Task ContextBuilder_IncludesCurrentLesson_AndExcludesDraftContent()
    {
        await using var context = CreateContext();
        var studentId = Guid.NewGuid();
        var course = Course.Create(
            "Secure coding", "secure-coding", "Course summary", "Security", null, Guid.NewGuid());
        Assert.True(course.TryPublish());
        var current = Lesson.Create(
            course.Id, "Current", "Current summary", 1, 10, LessonType.Article,
            "VISIBLE CURRENT MATERIAL", null, VideoProvider.None);
        Assert.True(current.TryPublish());
        var draft = Lesson.Create(
            course.Id, "Draft", "Draft summary", 2, 10, LessonType.Article,
            "PRIVATE DRAFT MATERIAL", null, VideoProvider.None);
        var enrollment = Enrollment.Create(course.Id, studentId, DateTimeOffset.UtcNow);
        context.AddRange(course, current, draft, enrollment);
        await context.SaveChangesAsync();
        var builder = new AiTutorContextBuilder(
            new CourseRepository(context),
            new LessonRepository(context),
            new EnrollmentRepository(context),
            new OllamaOptions { MaxContextCharacters = 30_000 });

        var result = await builder.BuildAsync(
            studentId, course.Id, current.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("CURRENT LESSON", result.Context);
        Assert.Contains("VISIBLE CURRENT MATERIAL", result.Context);
        Assert.DoesNotContain("PRIVATE DRAFT MATERIAL", result.Context);
        Assert.DoesNotContain(studentId.ToString(), result.Context);
    }

    private static AiTutorConversation CreateConversation() =>
        AiTutorConversation.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Course tutor",
            DateTimeOffset.UtcNow);

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static OllamaClient CreateClient(StubHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:11434/"),
                Timeout = TimeSpan.FromSeconds(5)
            },
            ValidOptions(),
            NullLogger<OllamaClient>.Instance);

    private static OllamaOptions ValidOptions() => new();

    private sealed class StubHandler(HttpStatusCode status, string body)
        : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
