using LearningPortal.Domain.Common.Events;

namespace LearningPortal.Domain.Courses.Events;

/// <summary>Represents publication of a course.</summary>
public sealed record CoursePublishedDomainEvent(Guid CourseId) : DomainEvent;
