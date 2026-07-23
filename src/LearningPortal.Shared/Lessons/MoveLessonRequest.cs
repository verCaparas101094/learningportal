namespace LearningPortal.Shared.Lessons;

/// <summary>Contains an adjacent target order and concurrency token.</summary>
public sealed record MoveLessonRequest(int NewOrder, string RowVersion);
