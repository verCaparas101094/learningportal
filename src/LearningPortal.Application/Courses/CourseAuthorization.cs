using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Courses;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses;

internal static class CourseAuthorization
{
    public static Error? ValidateManager(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Errors.Authentication.Unauthorized();
        }

        return IsAdministrator(currentUser) || IsInstructor(currentUser)
            ? null
            : Errors.Authorization.Forbidden();
    }

    public static bool CanAccess(ICurrentUserService currentUser, Course course) =>
        IsAdministrator(currentUser)
        || (IsInstructor(currentUser) && currentUser.UserId == course.InstructorId);

    public static bool IsAdministrator(ICurrentUserService currentUser) =>
        currentUser.HasRole(ApplicationRoles.Administrator);

    public static bool IsInstructor(ICurrentUserService currentUser) =>
        currentUser.HasRole(ApplicationRoles.Instructor);
}
