using LearningPortal.Domain.Common.Events;

namespace LearningPortal.Domain.Courses.Events;

/// <summary>Represents deletion of a Draft course.</summary>
public sealed record CourseDeletedDomainEvent(Guid CourseId) : DomainEvent;
