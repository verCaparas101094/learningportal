using System.Net.Http.Json;
using LearningPortal.Blazor.Services;
using LearningPortal.Shared.Enrollments;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Enrollments;

/// <summary>Verifies authenticated enrollment API request construction.</summary>
public sealed class EnrollmentApiClientTests
{
    /// <summary>Verifies My Learning filter and pagination parameters.</summary>
    [Fact]
    public async Task GetMyEnrollmentsAsync_ConstructsFiltersAndPagination()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler);

        await client.GetMyEnrollmentsAsync(new(" leadership ", "InProgress", 2, 20));

        Assert.Equal(
            "/api/enrollments/me?PageNumber=2&PageSize=20&Search=leadership&Status=InProgress",
            handler.RequestUri?.PathAndQuery);
    }

    /// <summary>Verifies course enrollment administration parameters.</summary>
    [Fact]
    public async Task GetCourseEnrollmentsAsync_ConstructsFiltersAndPagination()
    {
        var courseId = Guid.NewGuid();
        var handler = new RecordingHandler();
        var client = CreateClient(handler);

        await client.GetCourseEnrollmentsAsync(courseId, new(" student ", "Enrolled", 3, 25));

        Assert.Equal(
            $"/api/courses/{courseId:D}/enrollments?PageNumber=3&PageSize=25&Search=student&Status=Enrolled",
            handler.RequestUri?.PathAndQuery);
    }

    private static LearningPortalApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage
            {
                Content = JsonContent.Create(new PagedEnrollmentsResponse([], 1, 10, 0, 0))
            });
        }
    }
}
