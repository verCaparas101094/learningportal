using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Commands.EnrollInCourse;

/// <summary>Enrolls the current employee in a published course.</summary>
public sealed record EnrollInCourseCommand(Guid CourseId) : ICommand<Result<EnrollmentResponse>>;
