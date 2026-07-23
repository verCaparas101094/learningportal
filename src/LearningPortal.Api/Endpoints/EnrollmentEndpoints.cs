using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Enrollments.Commands.EnrollInCourse;
using LearningPortal.Application.Enrollments.Commands.WithdrawEnrollment;
using LearningPortal.Application.Enrollments.Queries.GetCourseEnrollments;
using LearningPortal.Application.Enrollments.Queries.GetEnrollmentById;
using LearningPortal.Application.Enrollments.Queries.GetMyEnrollments;
using LearningPortal.Application.Enrollments.Queries.GetPublishedCourseCatalog;
using LearningPortal.Application.Enrollments.Queries.GetPublishedCourseDetails;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps employee catalog and enrollment endpoints.</summary>
public static class EnrollmentEndpoints
{
    /// <summary>Maps catalog and enrollment routes.</summary>
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/catalog/courses", GetCatalogAsync).WithTags("Catalog").RequireAuthorization();
        endpoints.MapGet("/api/catalog/courses/{slug}", GetCourseDetailsAsync).WithTags("Catalog").RequireAuthorization();
        endpoints.MapPost("/api/courses/{courseId:guid}/enroll", EnrollAsync).WithTags("Enrollments").RequireAuthorization();
        endpoints.MapPut("/api/enrollments/{enrollmentId:guid}/withdraw", WithdrawAsync).WithTags("Enrollments").RequireAuthorization();
        endpoints.MapGet("/api/enrollments/me", GetMineAsync).WithTags("Enrollments").RequireAuthorization();
        endpoints.MapGet("/api/enrollments/{enrollmentId:guid}", GetByIdAsync).WithTags("Enrollments").RequireAuthorization();
        endpoints.MapGet("/api/courses/{courseId:guid}/enrollments", GetCourseEnrollmentsAsync)
            .WithTags("Enrollments").RequireAdminOrInstructor();
        return endpoints;
    }

    private static async Task<IResult> GetCatalogAsync(
        [AsParameters] GetCatalogRequest request,
        IQueryHandler<GetPublishedCourseCatalogQuery, Result<PagedCourseCatalogResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new(request.Search, request.PageNumber, request.PageSize), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetCourseDetailsAsync(
        string slug, IQueryHandler<GetPublishedCourseDetailsQuery, Result<CourseDetailsResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new(slug), cancellationToken)).ToHttpResult();

    private static async Task<IResult> EnrollAsync(
        Guid courseId, ICommandDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var result = await dispatcher.SendAsync<EnrollInCourseCommand, EnrollmentResponse>(
            new(courseId), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/enrollments/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    private static async Task<IResult> WithdrawAsync(
        Guid enrollmentId, WithdrawEnrollmentRequest request, ICommandDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync<WithdrawEnrollmentCommand, EnrollmentResponse>(
            new(enrollmentId, request.RowVersion), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetMineAsync(
        [AsParameters] GetEnrollmentsRequest request,
        IQueryHandler<GetMyEnrollmentsQuery, Result<PagedMyLearningResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new(request.Search, request.Status, request.PageNumber, request.PageSize), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetByIdAsync(
        Guid enrollmentId, IQueryHandler<GetEnrollmentByIdQuery, Result<EnrollmentResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new(enrollmentId), cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetCourseEnrollmentsAsync(
        Guid courseId, [AsParameters] GetEnrollmentsRequest request,
        IQueryHandler<GetCourseEnrollmentsQuery, Result<PagedEnrollmentsResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(
            new(courseId, request.Search, request.Status, request.PageNumber, request.PageSize),
            cancellationToken)).ToHttpResult();
}
