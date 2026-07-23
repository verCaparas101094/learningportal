using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using LearningPortal.Blazor.Models;
using LearningPortal.Blazor.Services;
using LearningPortal.Shared.Lessons;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Lessons;

/// <summary>Verifies lesson client routes and form validation.</summary>
public sealed class LessonApiClientTests
{
    /// <summary>Verifies course lesson search and pagination.</summary>
    [Fact]
    public async Task GetLessons_SendsCourseSearchAndPaging()
    {
        var courseId = Guid.NewGuid();
        var handler = new Handler(_ => Json(new PagedLessonsResponse([], 2, 25, 0, 0)));
        var client = Client(handler);
        await client.GetLessonsAsync(courseId, new() { Search = "intro", PageNumber = 2, PageSize = 25 });
        Assert.Equal($"/api/courses/{courseId:D}/lessons?PageNumber=2&PageSize=25&Search=intro", handler.Uri?.PathAndQuery);
    }

    /// <summary>Verifies lifecycle and reorder routes.</summary>
    [Theory]
    [InlineData("publish", "PUT")]
    [InlineData("move", "PUT")]
    [InlineData("delete", "DELETE")]
    public async Task Actions_SendExpectedRoutes(string action, string method)
    {
        var id = Guid.NewGuid();
        var response = new LessonResponse(id, Guid.NewGuid(), "Title", "", 1, 10, "Article", "# Content", null,
            "None", null, false, "<h1>Content</h1>", "Draft", DateTimeOffset.UtcNow, null, "AQ==");
        var handler = new Handler(_ => action == "delete" ? new(HttpStatusCode.NoContent) : Json(response));
        var client = Client(handler);
        if (action == "publish") await client.PublishLessonAsync(id);
        else if (action == "move") await client.MoveLessonAsync(id, new(2, "AQ=="));
        else await client.DeleteLessonAsync(id);
        Assert.Equal(method, handler.Method?.Method);
        Assert.Equal($"/api/lessons/{id:D}{(action == "delete" ? "" : $"/{action}")}", handler.Uri?.AbsolutePath);
    }

    /// <summary>Verifies create/edit form constraints.</summary>
    [Fact]
    public void Form_RejectsRequiredAndNumericValues()
    {
        var model = new LessonFormModel { Order = 0, EstimatedMinutes = 0 };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new(model), errors, true));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.Title)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.Order)));
    }

    /// <summary>Verifies preview request and playback metadata parsing.</summary>
    [Fact]
    public async Task Preview_SendsContentAndParsesVideoMetadata()
    {
        var preview = new LessonContentPreviewResponse("Video", null, "https://youtu.be/abc12345",
            "YouTube", "https://www.youtube-nocookie.com/embed/abc12345", false);
        var handler = new Handler(_ => Json(preview));
        var result = await Client(handler).PreviewLessonAsync(new("Video", null, "https://youtu.be/abc12345"));
        Assert.Equal("/api/lessons/preview", handler.Uri?.AbsolutePath);
        Assert.Equal("YouTube", result.VideoProvider);
    }

    private static LearningPortalApiClient Client(Handler handler) =>
        new(new HttpClient(handler) { BaseAddress = new("https://localhost/") });
    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public HttpMethod? Method { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri; Method = request.Method;
            return Task.FromResult(response(request));
        }
    }
}
