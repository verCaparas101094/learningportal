#pragma warning disable CS1591
using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.Courses;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using LearningPortal.Domain.Skills;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.InstructorEligibility;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.InstructorEligibility;

public sealed record GetMyInstructorEligibility
    : IQuery<Result<IReadOnlyList<InstructorEligibilityResponse>>>;
public sealed record GetUserInstructorEligibility(Guid UserId)
    : IQuery<Result<IReadOnlyList<InstructorEligibilityResponse>>>;
public sealed record GetEligibleInstructorsForSkill(Guid SkillId)
    : IQuery<Result<IReadOnlyList<EligibleInstructorResponse>>>;
public sealed record RecalculateInstructorEligibility(Guid UserId)
    : ICommand<Result<IReadOnlyList<InstructorEligibilityResponse>>>;
public sealed record CheckInstructorEligibility(Guid UserId, Guid SkillId)
    : IQuery<Result<bool>>;
public sealed record AssignCourseInstructor(Guid CourseId, Guid InstructorId, string? RowVersion)
    : ICommand<Result<CourseResponse>>;
public sealed record GetSkills : IQuery<Result<IReadOnlyList<SkillResponse>>>;

public sealed class EligibilityIdValidator : AbstractValidator<GetEligibleInstructorsForSkill>
{
    public EligibilityIdValidator() => RuleFor(query => query.SkillId).NotEmpty();
}
public sealed class RecalculateInstructorEligibilityValidator : AbstractValidator<RecalculateInstructorEligibility>
{
    public RecalculateInstructorEligibilityValidator() => RuleFor(command => command.UserId).NotEmpty();
}
public sealed class AssignCourseInstructorValidator : AbstractValidator<AssignCourseInstructor>
{
    public AssignCourseInstructorValidator()
    {
        RuleFor(command => command.CourseId).NotEmpty();
        RuleFor(command => command.InstructorId).NotEmpty();
    }
}

public sealed class InstructorEligibilityHandler(
    IInstructorEligibilityRepository eligibility,
    ISkillRepository skills,
    IQuizRepository quizzes,
    ICourseRepository courses,
    IUnitOfWork unit,
    ICurrentUserService currentUser,
    IUserManagementService users,
    ISystemClock clock,
    InstructorEligibilityOptions options)
    : IQueryHandler<GetMyInstructorEligibility, Result<IReadOnlyList<InstructorEligibilityResponse>>>,
      IQueryHandler<GetUserInstructorEligibility, Result<IReadOnlyList<InstructorEligibilityResponse>>>,
      IQueryHandler<GetEligibleInstructorsForSkill, Result<IReadOnlyList<EligibleInstructorResponse>>>,
      IQueryHandler<CheckInstructorEligibility, Result<bool>>,
      IQueryHandler<GetSkills, Result<IReadOnlyList<SkillResponse>>>,
      ICommandHandler<RecalculateInstructorEligibility, Result<IReadOnlyList<InstructorEligibilityResponse>>>,
      ICommandHandler<AssignCourseInstructor, Result<CourseResponse>>
{
    public Task<Result<IReadOnlyList<InstructorEligibilityResponse>>> HandleAsync(
        GetMyInstructorEligibility query, CancellationToken ct = default) =>
        currentUser.UserId is Guid userId
            ? GetUserAsync(userId, false, ct)
            : Task.FromResult(Result<IReadOnlyList<InstructorEligibilityResponse>>.Failure(Errors.Authentication.Unauthorized()));

    public Task<Result<IReadOnlyList<InstructorEligibilityResponse>>> HandleAsync(
        GetUserInstructorEligibility query, CancellationToken ct = default) =>
        GetUserAsync(query.UserId, true, ct);

    public async Task<Result<IReadOnlyList<EligibleInstructorResponse>>> HandleAsync(
        GetEligibleInstructorsForSkill query, CancellationToken ct = default)
    {
        var error = RequireAdministrator();
        if (error is not null) return Result<IReadOnlyList<EligibleInstructorResponse>>.Failure(error);
        if (await skills.GetByIdAsync(query.SkillId, cancellationToken: ct) is null)
            return Result<IReadOnlyList<EligibleInstructorResponse>>.Failure(Errors.Common.NotFound("Skill", query.SkillId));
        var records = await eligibility.GetEligibleBySkillAsync(query.SkillId, ct);
        var userMap = await users.GetUsersByIdsAsync(records.Select(value => value.UserId).ToArray(), ct);
        return Result<IReadOnlyList<EligibleInstructorResponse>>.Success(records
            .Where(value => userMap.TryGetValue(value.UserId, out var user) && user.IsEnabled)
            .Select(value => new EligibleInstructorResponse(
                value.UserId, userMap[value.UserId].DisplayName, value.SkillId,
                value.BestPercentage, value.QualifiedAtUtc)).ToArray());
    }

    public async Task<Result<bool>> HandleAsync(CheckInstructorEligibility query, CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated) return Result<bool>.Failure(Errors.Authentication.Unauthorized());
        if (currentUser.UserId != query.UserId && !currentUser.HasRole(ApplicationRoles.Administrator))
            return Result<bool>.Failure(Errors.Authorization.Forbidden());
        return Result<bool>.Success(await eligibility.IsEligibleAsync(query.UserId, query.SkillId, ct));
    }

    public async Task<Result<IReadOnlyList<SkillResponse>>> HandleAsync(GetSkills query, CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated)
            return Result<IReadOnlyList<SkillResponse>>.Failure(Errors.Authentication.Unauthorized());
        var values = await skills.GetActiveAsync(ct);
        return Result<IReadOnlyList<SkillResponse>>.Success(values.Select(ToResponse).ToArray());
    }

    public async Task<Result<IReadOnlyList<InstructorEligibilityResponse>>> HandleAsync(
        RecalculateInstructorEligibility command, CancellationToken ct = default)
    {
        var error = RequireAdministrator();
        if (error is not null) return Result<IReadOnlyList<InstructorEligibilityResponse>>.Failure(error);
        var userResult = await users.GetUserByIdAsync(command.UserId, ct);
        if (userResult.IsFailure)
            return Result<IReadOnlyList<InstructorEligibilityResponse>>.Failure(
                Errors.Common.NotFound("User", command.UserId));
        foreach (var skill in await skills.GetActiveAsync(ct))
        {
            var attempt = await eligibility.GetBestAttemptAsync(command.UserId, skill.Id, ct);
            if (attempt is not null)
            {
                var quiz = await quizzes.GetByIdReadOnlyAsync(attempt.QuizId, ct);
                if (quiz is not null)
                    await InstructorEligibilityCalculator.ApplyAsync(
                        attempt, quiz, eligibility, options.QualificationThreshold, clock.UtcNow, ct);
            }
        }
        await unit.SaveChangesAsync(ct);
        return await GetUserAsync(command.UserId, false, ct);
    }

    public async Task<Result<CourseResponse>> HandleAsync(AssignCourseInstructor command, CancellationToken ct = default)
    {
        var error = RequireAdministrator();
        if (error is not null) return Result<CourseResponse>.Failure(error);
        var course = await courses.GetByIdAsync(command.CourseId, ct);
        if (course is null) return Result<CourseResponse>.Failure(Errors.CourseManagement.NotFound(command.CourseId));
        if (course.SkillId is not Guid skillId)
            return Result<CourseResponse>.Failure(Errors.Validation.Failed("The course must have a skill before assigning an instructor."));
        var userResult = await users.GetUserByIdAsync(command.InstructorId, ct);
        if (userResult.IsFailure || !userResult.Value.IsEnabled)
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidInstructor());
        if (!await eligibility.IsEligibleAsync(command.InstructorId, skillId, ct))
            return Result<CourseResponse>.Failure(Errors.Common.Failure(
                "InstructorEligibility.Required", "The user is not eligible to instruct this course's skill."));
        if (command.RowVersion is not null)
            courses.SetOriginalRowVersion(course, Convert.FromBase64String(command.RowVersion));
        if (!course.TryAssignInstructor(command.InstructorId))
            return Result<CourseResponse>.Failure(Errors.CourseManagement.InvalidState("assigned"));
        if (!userResult.Value.Roles.Contains(ApplicationRoles.Instructor, StringComparer.OrdinalIgnoreCase))
        {
            var roleResult = await users.AssignRoleAsync(command.InstructorId, ApplicationRoles.Instructor, ct);
            if (roleResult.IsFailure) return Result<CourseResponse>.Failure(roleResult.Error!);
        }
        await unit.SaveChangesAsync(ct);
        return Result<CourseResponse>.Success(course.ToResponse());
    }

    private async Task<Result<IReadOnlyList<InstructorEligibilityResponse>>> GetUserAsync(
        Guid userId, bool requireAdmin, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) return Result<IReadOnlyList<InstructorEligibilityResponse>>.Failure(Errors.Authentication.Unauthorized());
        if (requireAdmin && !currentUser.HasRole(ApplicationRoles.Administrator))
            return Result<IReadOnlyList<InstructorEligibilityResponse>>.Failure(Errors.Authorization.Forbidden());
        var records = await eligibility.GetByUserAsync(userId, ct);
        var skillMap = (await skills.GetActiveAsync(ct)).ToDictionary(value => value.Id);
        return Result<IReadOnlyList<InstructorEligibilityResponse>>.Success(records
            .Where(value => skillMap.ContainsKey(value.SkillId))
            .Select(value => new InstructorEligibilityResponse(
                value.UserId, value.SkillId, skillMap[value.SkillId].Name, value.IsEligible,
                value.BestPercentage, value.QualifiedAtUtc, value.QualifyingQuizId)).ToArray());
    }

    private Error? RequireAdministrator() =>
        !currentUser.IsAuthenticated ? Errors.Authentication.Unauthorized()
        : !currentUser.HasRole(ApplicationRoles.Administrator) ? Errors.Authorization.Forbidden() : null;
    private static SkillResponse ToResponse(Skill value) =>
        new(value.Id, value.Name, value.Slug, value.Description, value.IsActive);
}

public static class InstructorEligibilityCalculator
{
    public static async Task ApplyAsync(
        QuizAttempt attempt,
        Quiz quiz,
        IInstructorEligibilityRepository repository,
        decimal threshold,
        DateTimeOffset qualifiedAtUtc,
        CancellationToken ct)
    {
        if (attempt.Status != QuizAttemptStatus.Submitted
            || !attempt.Passed
            || attempt.Percentage < threshold
            || quiz.Status != QuizStatus.Published
            || !quiz.IsInstructorAssessment
            || quiz.SkillId is not Guid skillId
            || quiz.Id != attempt.QuizId)
            return;
        var record = await repository.GetAsync(attempt.StudentId, skillId, ct);
        if (record is null)
        {
            record = Domain.Skills.InstructorEligibility.Create(
                attempt.StudentId, skillId, attempt.QuizId, attempt.Percentage, qualifiedAtUtc);
            await repository.AddAsync(record, ct);
        }
        else
        {
            record.TryUpdateBest(attempt.QuizId, attempt.Percentage);
        }
    }
}
