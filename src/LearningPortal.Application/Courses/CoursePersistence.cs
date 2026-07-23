using LearningPortal.Domain.Courses.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Courses;

internal static class CoursePersistence
{
    public static async Task<Error?> SaveAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DuplicateCourseSlugException)
        {
            return Errors.CourseManagement.DuplicateSlug();
        }
        catch (CourseConcurrencyException)
        {
            return Errors.CourseManagement.ConcurrencyConflict();
        }
    }
}
