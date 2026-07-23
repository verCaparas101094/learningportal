namespace LearningPortal.Shared.Learning;

/// <summary>Represents learner progress for one published lesson.</summary>
public sealed record LessonProgressResponse(Guid LessonId, string Status, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? LastAccessedAtUtc, string? RowVersion);
/// <summary>Represents a learner-safe lesson outline row.</summary>
public sealed record LearningLessonOutlineItemResponse(Guid Id, string Title, int Order, int EstimatedMinutes, string LessonType, string ProgressStatus, bool IsCurrent);
/// <summary>Represents aggregate progress for an enrollment.</summary>
public sealed record CourseProgressResponse(Guid EnrollmentId, string EnrollmentStatus, int CompletedLessonCount, int TotalPublishedLessonCount, int CompletionPercentage, bool IsCompleted);
/// <summary>Represents the next learner lesson destination, when one exists.</summary>
public sealed record ContinueLearningDestinationResponse(Guid EnrollmentId, string CourseSlug, Guid LessonId, string LessonTitle);
/// <summary>Contains learner content, navigation, outline, and progress.</summary>
public sealed record LessonPlayerResponse(Guid EnrollmentId, Guid CourseId, string CourseTitle, string CourseSlug, Guid LessonId, string LessonTitle, string LessonType, string? MarkdownHtml, string? SourceUrl, string? EmbedUrl, bool IsDirectVideo, IReadOnlyCollection<LearningLessonOutlineItemResponse> Lessons, Guid? PreviousLessonId, Guid? NextLessonId, CourseProgressResponse Progress);
/// <summary>Contains the post-completion learner state.</summary>
public sealed record CompleteLessonResponse(LessonProgressResponse LessonProgress, CourseProgressResponse CourseProgress, Guid? NextLessonId);
