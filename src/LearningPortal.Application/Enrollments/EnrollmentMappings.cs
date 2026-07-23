using LearningPortal.Domain.Enrollments;
using LearningPortal.Shared.Enrollments;

namespace LearningPortal.Application.Enrollments;

internal static class EnrollmentMappings
{
    internal static EnrollmentResponse ToResponse(this Enrollment enrollment) => new(
        enrollment.Id, enrollment.CourseId, enrollment.StudentId, enrollment.Status.ToString(),
        enrollment.EnrolledAtUtc, enrollment.StartedAtUtc, enrollment.CompletedAtUtc,
        enrollment.WithdrawnAtUtc, Convert.ToBase64String(enrollment.RowVersion));

    internal static string Summary(string value) =>
        value.Length <= 180 ? value : string.Concat(value.AsSpan(0, 177), "...");
}
