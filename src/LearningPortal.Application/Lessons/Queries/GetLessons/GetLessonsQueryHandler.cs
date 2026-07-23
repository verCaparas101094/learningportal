#pragma warning disable CS1591
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.Lessons.Queries.GetLessons;
public sealed class GetLessonsQueryHandler(ILessonRepository lessons, ICurrentUserService user)
    : IQueryHandler<GetLessonsQuery, Result<PagedLessonsResponse>>
{
    public async Task<Result<PagedLessonsResponse>> HandleAsync(GetLessonsQuery q, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated) return Result<PagedLessonsResponse>.Failure(Errors.Authentication.Unauthorized());
        if (!user.HasRole(ApplicationRoles.Administrator) && !user.HasRole(ApplicationRoles.Instructor))
            return Result<PagedLessonsResponse>.Failure(Errors.Authorization.Forbidden());
        var instructorId = user.HasRole(ApplicationRoles.Administrator) ? null : user.UserId;
        var page = await lessons.GetPageAsync(null, q.Search, q.PageNumber, q.PageSize, instructorId, ct);
        return Result<PagedLessonsResponse>.Success(new(page.Items.Select(x => x.ToListItem()).ToArray(),
            q.PageNumber, q.PageSize, page.TotalCount, (int)Math.Ceiling(page.TotalCount / (double)q.PageSize)));
    }
}
