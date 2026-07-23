using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.InstructorEligibility;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.InstructorEligibility;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps instructor eligibility and skill routes.</summary>
public static class InstructorEligibilityEndpoints
{
    /// <summary>Maps authenticated and administrator eligibility routes.</summary>
    public static IEndpointRouteBuilder MapInstructorEligibilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/instructor-eligibility/me", GetMineAsync).RequireAuthorization();
        endpoints.MapGet("/api/skills", GetSkillsAsync).RequireAuthorization();
        endpoints.MapGet("/api/users/{userId:guid}/instructor-eligibility", GetUserAsync)
            .RequireAuthorization(Policies.AdminOnly);
        endpoints.MapGet("/api/skills/{skillId:guid}/eligible-instructors", GetEligibleAsync)
            .RequireAuthorization(Policies.AdminOnly);
        endpoints.MapPost("/api/users/{userId:guid}/instructor-eligibility/recalculate", RecalculateAsync)
            .RequireAuthorization(Policies.AdminOnly);
        endpoints.MapPut("/api/courses/{courseId:guid}/instructor", AssignAsync)
            .RequireAuthorization(Policies.AdminOnly);
        return endpoints;
    }

    private static async Task<IResult> GetMineAsync(
        IQueryHandler<GetMyInstructorEligibility, Result<IReadOnlyList<InstructorEligibilityResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(), ct)).ToHttpResult();
    private static async Task<IResult> GetUserAsync(
        Guid userId,
        IQueryHandler<GetUserInstructorEligibility, Result<IReadOnlyList<InstructorEligibilityResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(userId), ct)).ToHttpResult();
    private static async Task<IResult> GetEligibleAsync(
        Guid skillId,
        IQueryHandler<GetEligibleInstructorsForSkill, Result<IReadOnlyList<EligibleInstructorResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(skillId), ct)).ToHttpResult();
    private static async Task<IResult> GetSkillsAsync(
        IQueryHandler<GetSkills, Result<IReadOnlyList<SkillResponse>>> handler,
        CancellationToken ct) => (await handler.HandleAsync(new(), ct)).ToHttpResult();
    private static async Task<IResult> RecalculateAsync(
        Guid userId, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<RecalculateInstructorEligibility, IReadOnlyList<InstructorEligibilityResponse>>(
            new(userId), ct)).ToHttpResult();
    private static async Task<IResult> AssignAsync(
        Guid courseId, AssignCourseInstructorRequest request, ICommandDispatcher dispatcher, CancellationToken ct) =>
        (await dispatcher.SendAsync<AssignCourseInstructor, CourseResponse>(
            new(courseId, request.InstructorId, request.RowVersion), ct)).ToHttpResult();
}
