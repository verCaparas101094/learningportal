using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using LearningPortal.Blazor.Models;
using LearningPortal.Blazor.Services;
using LearningPortal.Shared.Courses;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Courses;

/// <summary>Verifies typed course API requests and safe problem parsing.</summary>
public sealed class CourseApiClientTests
{
    /// <summary>Verifies list filter and pagination query construction.</summary>
    [Fact]
    public async Task GetCoursesAsync_ConstructsSearchFilterAndPagination()
    {
        var handler = new RecordingHandler(_ => JsonResponse(new PagedCoursesResponse(
            [],
            2,
            25,
            0,
            0)));
        var client = CreateClient(handler);

        var response = await client.GetCoursesAsync(new GetCoursesRequest
        {
            Search = " leadership ",
            Status = "Draft",
            PageNumber = 2,
            PageSize = 25
        });

        Assert.Empty(response.Items);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal(
            "/api/courses?Search=leadership&Status=Draft&PageNumber=2&PageSize=25",
            handler.RequestUri?.PathAndQuery);
    }

    /// <summary>Verifies successful course creation.</summary>
    [Fact]
    public async Task CreateCourseAsync_ReturnsCreatedCourse()
    {
        var course = CreateResponse();
        var handler = new RecordingHandler(_ => JsonResponse(course, HttpStatusCode.Created));
        var client = CreateClient(handler);

        var response = await client.CreateCourseAsync(new CreateCourseRequest(
            "Course",
            "course",
            "Description",
            "Category",
            null,
            course.InstructorId));

        Assert.Equal(course.Id, response.Id);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/courses", handler.RequestUri?.AbsolutePath);
        Assert.Contains("\"slug\":\"course\"", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies API validation problem parsing.</summary>
    [Fact]
    public async Task CreateCourseAsync_ParsesValidationFailure()
    {
        var handler = new RecordingHandler(_ => ProblemResponse(
            HttpStatusCode.BadRequest,
            "Validation failed.",
            "Validation.Failed"));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            client.CreateCourseAsync(new CreateCourseRequest(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                null)));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Validation.Failed", exception.ErrorCode);
    }

    /// <summary>Verifies client-side form validation.</summary>
    [Fact]
    public void CourseFormModel_RejectsMissingRequiredValues()
    {
        var model = new CourseFormModel();
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Title)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Slug)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Category)));
    }

    /// <summary>Verifies Draft update method, route, and rowversion body.</summary>
    [Fact]
    public async Task UpdateCourseAsync_SendsConcurrencyProtectedRequest()
    {
        var course = CreateResponse();
        var handler = new RecordingHandler(_ => JsonResponse(course));
        var client = CreateClient(handler);

        await client.UpdateCourseAsync(course.Id, new UpdateCourseRequest(
            course.Title,
            course.Slug,
            course.Description,
            course.Category,
            course.ThumbnailUrl,
            course.RowVersion));

        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal($"/api/courses/{course.Id:D}", handler.RequestUri?.AbsolutePath);
        Assert.Contains("\"rowVersion\":\"AQ==\"", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies lifecycle and delete API routes.</summary>
    [Theory]
    [InlineData("publish", "PUT")]
    [InlineData("archive", "PUT")]
    [InlineData("delete", "DELETE")]
    public async Task CourseActions_UseExpectedRoutes(string operation, string method)
    {
        var course = CreateResponse();
        var handler = new RecordingHandler(_ =>
            operation == "delete"
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : JsonResponse(course));
        var client = CreateClient(handler);

        switch (operation)
        {
            case "publish":
                await client.PublishCourseAsync(course.Id);
                break;
            case "archive":
                await client.ArchiveCourseAsync(course.Id);
                break;
            default:
                await client.DeleteCourseAsync(course.Id);
                break;
        }

        Assert.Equal(method, handler.Method?.Method);
        var suffix = operation == "delete" ? string.Empty : $"/{operation}";
        Assert.Equal($"/api/courses/{course.Id:D}{suffix}", handler.RequestUri?.AbsolutePath);
    }

    /// <summary>Verifies concurrency conflict parsing.</summary>
    [Fact]
    public async Task UpdateCourseAsync_ParsesConcurrencyConflict()
    {
        var course = CreateResponse();
        var handler = new RecordingHandler(_ => ProblemResponse(
            HttpStatusCode.Conflict,
            "The course changed.",
            "Course.ConcurrencyConflict"));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            client.UpdateCourseAsync(course.Id, new UpdateCourseRequest(
                course.Title,
                course.Slug,
                course.Description,
                course.Category,
                null,
                course.RowVersion)));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("Course.ConcurrencyConflict", exception.ErrorCode);
    }

    private static LearningPortalApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });

    private static CourseResponse CreateResponse() => new(
        Guid.CreateVersion7(),
        "Course",
        "course",
        "Description",
        "Category",
        null,
        "Draft",
        Guid.CreateVersion7(),
        DateTimeOffset.UtcNow,
        Guid.CreateVersion7(),
        null,
        null,
        "AQ==");

    private static HttpResponseMessage JsonResponse<T>(
        T value,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(value) };

    private static HttpResponseMessage ProblemResponse(
        HttpStatusCode statusCode,
        string title,
        string code) =>
        new(statusCode)
        {
            Content = JsonContent.Create(new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}",
                title,
                status = (int)statusCode,
                code
            })
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
