namespace LearningPortal.Shared.Enrollments;

/// <summary>Represents a complete enrollment.</summary>
public sealed record EnrollmentResponse(
    Guid Id, Guid CourseId, Guid StudentId, string Status, DateTimeOffset EnrolledAtUtc,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? WithdrawnAtUtc,
    string RowVersion);

/// <summary>Represents an enrollment list row.</summary>
public sealed record EnrollmentListItemResponse(
    Guid Id, Guid CourseId, string CourseTitle, string CourseSlug, Guid StudentId,
    string StudentDisplayName, string StudentEmail, string Status, DateTimeOffset EnrolledAtUtc);

/// <summary>Represents a catalog course card.</summary>
public sealed record CourseCatalogItemResponse(
    Guid CourseId, string Title, string Slug, string DescriptionSummary, string InstructorName,
    int EstimatedMinutes, int PublishedLessonCount, string? EnrollmentStatus, Guid? EnrollmentId,
    bool IsEnrolled, bool CanEnroll, bool CanContinue);

/// <summary>Represents a published lesson outline item.</summary>
public sealed record PublishedLessonSummaryResponse(
    Guid Id, string Title, int Order, int EstimatedMinutes, string LessonType);

/// <summary>Represents published course details for an employee.</summary>
public sealed record CourseDetailsResponse(
    Guid CourseId, string Title, string Slug, string Description, string Category,
    string? ThumbnailUrl, string InstructorName, int EstimatedMinutes, int PublishedLessonCount,
    IReadOnlyCollection<PublishedLessonSummaryResponse> Lessons, string? EnrollmentStatus,
    Guid? EnrollmentId, bool CanEnroll, bool CanContinue);

/// <summary>Represents one course in My Learning.</summary>
public sealed record MyLearningItemResponse(
    Guid EnrollmentId, Guid CourseId, string CourseTitle, string CourseSlug,
    string DescriptionSummary, string Status, DateTimeOffset EnrolledAtUtc,
    int EstimatedMinutes, int PublishedLessonCount, bool CanContinue);

/// <summary>Contains a page of catalog courses.</summary>
public sealed record PagedCourseCatalogResponse(
    IReadOnlyCollection<CourseCatalogItemResponse> Items, int PageNumber, int PageSize,
    int TotalCount, int TotalPages);

/// <summary>Contains a page of employee enrollments.</summary>
public sealed record PagedMyLearningResponse(
    IReadOnlyCollection<MyLearningItemResponse> Items, int PageNumber, int PageSize,
    int TotalCount, int TotalPages);

/// <summary>Contains a page of course enrollments.</summary>
public sealed record PagedEnrollmentsResponse(
    IReadOnlyCollection<EnrollmentListItemResponse> Items, int PageNumber, int PageSize,
    int TotalCount, int TotalPages);

/// <summary>Supplies the expected enrollment concurrency value.</summary>
public sealed record WithdrawEnrollmentRequest(string RowVersion);

/// <summary>Supplies common catalog filters.</summary>
public sealed record GetCatalogRequest(string? Search = null, string? Category = null, int PageNumber = 1, int PageSize = 12);

/// <summary>Supplies common enrollment filters.</summary>
public sealed record GetEnrollmentsRequest(string? Search = null, string? Status = null, int PageNumber = 1, int PageSize = 10);
