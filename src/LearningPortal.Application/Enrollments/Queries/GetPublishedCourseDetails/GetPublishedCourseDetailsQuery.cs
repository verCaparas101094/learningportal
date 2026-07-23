using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Enrollments.Queries.GetPublishedCourseDetails;

/// <summary>Requests published course details by slug.</summary>
public sealed record GetPublishedCourseDetailsQuery(string Slug) : IQuery<Result<CourseDetailsResponse>>;
