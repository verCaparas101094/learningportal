using LearningPortal.Domain.Common.Events;

namespace LearningPortal.Domain.Courses.Events;

/// <summary>Represents archival of a course.</summary>
public sealed record CourseArchivedDomainEvent(Guid CourseId) : DomainEvent;
