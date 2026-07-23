using System.Net.Http.Json;
using System.Text.Json;
using LearningPortal.Blazor.Models;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Enrollments;
using LearningPortal.Shared.Learning;
using LearningPortal.Shared.UserManagement;
using LearningPortal.Shared.Quizzes;
using LearningPortal.Shared.InstructorEligibility;
using LearningPortal.Shared.AiTutor;

namespace LearningPortal.Blazor.Services;

/// <summary>Provides typed, asynchronous access to the Learning Portal API.</summary>
public sealed class LearningPortalApiClient(HttpClient httpClient)
{
    /// <summary>Gets the API liveness state.</summary>
    public async Task<ApiHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<ApiHealthResponse>("health/live", cancellationToken);
        return response ?? throw new InvalidOperationException("The API returned an empty health response.");
    }

    /// <summary>Gets one filtered, paginated page of administrator-safe users.</summary>
    /// <param name="request">The search and pagination values.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The matching page of users.</returns>
    public async Task<PagedUsersResponse> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = $"api/users?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            requestUri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        }

        var response = await httpClient.GetFromJsonAsync<PagedUsersResponse>(
            requestUri,
            cancellationToken);

        return response ?? throw new InvalidOperationException("The API returned an empty users response.");
    }

    /// <summary>Enables one user account.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> EnableUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        PutAsync<UserResponse>($"api/users/{userId:D}/enable", null, cancellationToken);

    /// <summary>Disables one user account.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> DisableUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        PutAsync<UserResponse>($"api/users/{userId:D}/disable", null, cancellationToken);

    /// <summary>Adds one valid application role to a user.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The role assignment.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The updated user.</returns>
    public Task<UserResponse> AssignUserRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UserResponse>($"api/users/{userId:D}/roles", request, cancellationToken);
    }

    /// <summary>Gets one filtered course page.</summary>
    public async Task<PagedCoursesResponse> GetCoursesAsync(
        GetCoursesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            values.Add($"Search={Uri.EscapeDataString(request.Search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            values.Add($"Status={Uri.EscapeDataString(request.Status.Trim())}");
        }

        values.Add($"PageNumber={request.PageNumber}");
        values.Add($"PageSize={request.PageSize}");

        using var response = await httpClient.GetAsync(
            $"api/courses?{string.Join('&', values)}",
            cancellationToken);
        return await ReadResponseAsync<PagedCoursesResponse>(response, cancellationToken);
    }

    /// <summary>Gets one course by identifier.</summary>
    public async Task<CourseResponse> GetCourseByIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/courses/{courseId:D}",
            cancellationToken);
        return await ReadResponseAsync<CourseResponse>(response, cancellationToken);
    }

    /// <summary>Creates a Draft course.</summary>
    public async Task<CourseResponse> CreateCourseAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await httpClient.PostAsJsonAsync(
            "api/courses",
            request,
            cancellationToken);
        return await ReadResponseAsync<CourseResponse>(response, cancellationToken);
    }

    /// <summary>Updates a Draft course.</summary>
    public Task<CourseResponse> UpdateCourseAsync(
        Guid courseId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<CourseResponse>($"api/courses/{courseId:D}", request, cancellationToken);

    /// <summary>Publishes a Draft course.</summary>
    public Task<CourseResponse> PublishCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        PutAsync<CourseResponse>($"api/courses/{courseId:D}/publish", null, cancellationToken);

    /// <summary>Archives a Published course.</summary>
    public Task<CourseResponse> ArchiveCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default) =>
        PutAsync<CourseResponse>($"api/courses/{courseId:D}/archive", null, cancellationToken);

    /// <summary>Deletes a Draft course.</summary>
    public async Task DeleteCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/courses/{courseId:D}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>Gets a filtered lesson page for a course.</summary>
    public async Task<PagedLessonsResponse> GetLessonsAsync(Guid courseId, GetLessonsRequest request, CancellationToken cancellationToken = default)
    {
        var uri = $"api/courses/{courseId:D}/lessons?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search)) uri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        return await ReadResponseAsync<PagedLessonsResponse>(response, cancellationToken);
    }
    /// <summary>Gets one lesson.</summary>
    public async Task<LessonResponse> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/lessons/{lessonId:D}", cancellationToken);
        return await ReadResponseAsync<LessonResponse>(response, cancellationToken);
    }
    /// <summary>Creates a lesson.</summary>
    public async Task<LessonResponse> CreateLessonAsync(Guid courseId, CreateLessonRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/courses/{courseId:D}/lessons", request, cancellationToken);
        return await ReadResponseAsync<LessonResponse>(response, cancellationToken);
    }
    /// <summary>Updates a Draft lesson.</summary>
    public Task<LessonResponse> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<LessonResponse>($"api/lessons/{lessonId:D}", request, cancellationToken);
    /// <summary>Publishes a lesson.</summary>
    public Task<LessonResponse> PublishLessonAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        PutAsync<LessonResponse>($"api/lessons/{lessonId:D}/publish", null, cancellationToken);
    /// <summary>Moves a Draft lesson.</summary>
    public Task<LessonResponse> MoveLessonAsync(Guid lessonId, MoveLessonRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<LessonResponse>($"api/lessons/{lessonId:D}/move", request, cancellationToken);
    /// <summary>Deletes a Draft lesson.</summary>
    public async Task DeleteLessonAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/lessons/{lessonId:D}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }
    /// <summary>Builds a non-persisted safe lesson preview.</summary>
    public async Task<LessonContentPreviewResponse> PreviewLessonAsync(
        LessonContentPreviewRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/lessons/preview", request, cancellationToken);
        return await ReadResponseAsync<LessonContentPreviewResponse>(response, cancellationToken);
    }

    /// <summary>Gets the published employee course catalog.</summary>
    public async Task<PagedCourseCatalogResponse> GetCatalogAsync(
        GetCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var uri = $"api/catalog/courses?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search)) uri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        return await ReadResponseAsync<PagedCourseCatalogResponse>(response, cancellationToken);
    }

    /// <summary>Gets published course details.</summary>
    public async Task<CourseDetailsResponse> GetCatalogCourseAsync(
        string slug, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/catalog/courses/{Uri.EscapeDataString(slug)}", cancellationToken);
        return await ReadResponseAsync<CourseDetailsResponse>(response, cancellationToken);
    }

    /// <summary>Enrolls the current employee in a course.</summary>
    public async Task<EnrollmentResponse> EnrollAsync(
        Guid courseId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/courses/{courseId:D}/enroll", null, cancellationToken);
        return await ReadResponseAsync<EnrollmentResponse>(response, cancellationToken);
    }

    /// <summary>Withdraws an enrollment using optimistic concurrency.</summary>
    public Task<EnrollmentResponse> WithdrawAsync(
        Guid enrollmentId, string rowVersion, CancellationToken cancellationToken = default) =>
        PutAsync<EnrollmentResponse>(
            $"api/enrollments/{enrollmentId:D}/withdraw", new WithdrawEnrollmentRequest(rowVersion), cancellationToken);

    /// <summary>Gets the current employee's enrollments.</summary>
    public async Task<PagedMyLearningResponse> GetMyEnrollmentsAsync(
        GetEnrollmentsRequest request, CancellationToken cancellationToken = default)
    {
        var uri = $"api/enrollments/me?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search)) uri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        if (!string.IsNullOrWhiteSpace(request.Status)) uri += $"&Status={Uri.EscapeDataString(request.Status.Trim())}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        return await ReadResponseAsync<PagedMyLearningResponse>(response, cancellationToken);
    }

    /// <summary>Gets an authorized enrollment.</summary>
    public async Task<EnrollmentResponse> GetEnrollmentAsync(
        Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/enrollments/{enrollmentId:D}", cancellationToken);
        return await ReadResponseAsync<EnrollmentResponse>(response, cancellationToken);
    }

    /// <summary>Gets an authorized course enrollment page.</summary>
    public async Task<PagedEnrollmentsResponse> GetCourseEnrollmentsAsync(
        Guid courseId,
        GetEnrollmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/courses/{courseId:D}/enrollments?PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search)) uri += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        if (!string.IsNullOrWhiteSpace(request.Status)) uri += $"&Status={Uri.EscapeDataString(request.Status.Trim())}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        return await ReadResponseAsync<PagedEnrollmentsResponse>(response, cancellationToken);
    }

    /// <summary>Gets learner-safe lesson player data.</summary>
    public async Task<LessonPlayerResponse> GetLessonPlayerAsync(string courseSlug, Guid lessonId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/learning/courses/{Uri.EscapeDataString(courseSlug)}/lessons/{lessonId:D}", cancellationToken);
        return await ReadResponseAsync<LessonPlayerResponse>(response, cancellationToken);
    }
    /// <summary>Records learner access to a lesson.</summary>
    public async Task<CourseProgressResponse> AccessLessonAsync(Guid enrollmentId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/learning/enrollments/{enrollmentId:D}/lessons/{lessonId:D}/access", null, cancellationToken);
        return await ReadResponseAsync<CourseProgressResponse>(response, cancellationToken);
    }
    /// <summary>Completes a lesson idempotently.</summary>
    public Task<CompleteLessonResponse> CompleteLessonAsync(Guid enrollmentId, Guid lessonId, CancellationToken cancellationToken = default) =>
        PutAsync<CompleteLessonResponse>($"api/learning/enrollments/{enrollmentId:D}/lessons/{lessonId:D}/complete", null, cancellationToken);
    /// <summary>Resolves the learner's next appropriate lesson.</summary>
    public async Task<ContinueLearningDestinationResponse?> GetContinueLearningAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/learning/enrollments/{enrollmentId:D}/continue", cancellationToken);
        return await ReadResponseAsync<ContinueLearningDestinationResponse?>(response, cancellationToken);
    }

    /// <summary>Gets quizzes for course administration.</summary>
    public async Task<IReadOnlyList<QuizAdministrationResponse>> GetCourseQuizzesAsync(
        Guid courseId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/courses/{courseId:D}/quizzes", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<QuizAdministrationResponse>>(response, cancellationToken);
    }

    /// <summary>Gets an editable quiz graph.</summary>
    public async Task<QuizAdministrationResponse> GetQuizAdministrationAsync(
        Guid quizId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/quizzes/{quizId:D}/administration", cancellationToken);
        return await ReadResponseAsync<QuizAdministrationResponse>(response, cancellationToken);
    }

    /// <summary>Creates a draft quiz.</summary>
    public async Task<QuizAdministrationResponse> CreateQuizAsync(
        Guid courseId, SaveQuizRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/courses/{courseId:D}/quizzes", request, cancellationToken);
        return await ReadResponseAsync<QuizAdministrationResponse>(response, cancellationToken);
    }

    /// <summary>Updates draft quiz details.</summary>
    public Task<QuizAdministrationResponse> UpdateQuizAsync(
        Guid quizId, SaveQuizRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<QuizAdministrationResponse>($"api/quizzes/{quizId:D}", request, cancellationToken);

    /// <summary>Adds a validated question and its answer choices.</summary>
    public async Task<QuizAdministrationResponse> AddQuizQuestionAsync(
        Guid quizId, SaveQuizQuestionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/quizzes/{quizId:D}/questions", request, cancellationToken);
        return await ReadResponseAsync<QuizAdministrationResponse>(response, cancellationToken);
    }

    /// <summary>Updates a draft question and its answer-choice graph.</summary>
    public Task<QuizAdministrationResponse> UpdateQuizQuestionAsync(
        Guid quizId,
        Guid questionId,
        SaveQuizQuestionRequest request,
        CancellationToken cancellationToken = default) =>
        PutAsync<QuizAdministrationResponse>(
            $"api/quizzes/{quizId:D}/questions/{questionId:D}", request, cancellationToken);

    /// <summary>Publishes a valid draft quiz.</summary>
    public Task<QuizAdministrationResponse> PublishQuizAsync(Guid quizId, CancellationToken cancellationToken = default) =>
        PutAsync<QuizAdministrationResponse>($"api/quizzes/{quizId:D}/publish", null, cancellationToken);

    /// <summary>Archives a published quiz.</summary>
    public Task<QuizAdministrationResponse> ArchiveQuizAsync(Guid quizId, CancellationToken cancellationToken = default) =>
        PutAsync<QuizAdministrationResponse>($"api/quizzes/{quizId:D}/archive", null, cancellationToken);

    /// <summary>Gets learner-safe quiz content without correctness flags.</summary>
    public async Task<QuizResponse> GetQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/learning/quizzes/{quizId:D}", cancellationToken);
        return await ReadResponseAsync<QuizResponse>(response, cancellationToken);
    }

    /// <summary>Starts or resumes the single active attempt.</summary>
    public async Task<StartQuizAttemptResponse> StartQuizAttemptAsync(
        Guid quizId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/learning/quizzes/{quizId:D}/attempts", null, cancellationToken);
        return await ReadResponseAsync<StartQuizAttemptResponse>(response, cancellationToken);
    }

    /// <summary>Submits learner selections for server scoring.</summary>
    public async Task<QuizAttemptResponse> SubmitQuizAttemptAsync(
        Guid attemptId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/learning/quiz-attempts/{attemptId:D}/submit", request, cancellationToken);
        return await ReadResponseAsync<QuizAttemptResponse>(response, cancellationToken);
    }

    /// <summary>Gets an owned attempt or result.</summary>
    public async Task<QuizAttemptResponse> GetQuizAttemptAsync(
        Guid attemptId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/learning/quiz-attempts/{attemptId:D}", cancellationToken);
        return await ReadResponseAsync<QuizAttemptResponse>(response, cancellationToken);
    }

    /// <summary>Gets the current learner's attempt history.</summary>
    public async Task<IReadOnlyList<QuizAttemptResponse>> GetMyQuizAttemptsAsync(
        Guid quizId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/learning/quizzes/{quizId:D}/attempts/me", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<QuizAttemptResponse>>(response, cancellationToken);
    }

    /// <summary>Gets the signed-in user's skill qualifications.</summary>
    public async Task<IReadOnlyList<InstructorEligibilityResponse>> GetMyInstructorEligibilityAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/instructor-eligibility/me", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<InstructorEligibilityResponse>>(response, cancellationToken);
    }

    /// <summary>Gets administrator-visible eligibility for a user.</summary>
    public async Task<IReadOnlyList<InstructorEligibilityResponse>> GetUserInstructorEligibilityAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/users/{userId:D}/instructor-eligibility", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<InstructorEligibilityResponse>>(response, cancellationToken);
    }

    /// <summary>Gets active skills.</summary>
    public async Task<IReadOnlyList<SkillResponse>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/skills", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<SkillResponse>>(response, cancellationToken);
    }

    /// <summary>Gets enabled eligible instructors for one skill.</summary>
    public async Task<IReadOnlyList<EligibleInstructorResponse>> GetEligibleInstructorsAsync(
        Guid skillId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/skills/{skillId:D}/eligible-instructors", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<EligibleInstructorResponse>>(response, cancellationToken);
    }

    /// <summary>Recalculates a user's qualifications from submitted attempts.</summary>
    public async Task<IReadOnlyList<InstructorEligibilityResponse>> RecalculateInstructorEligibilityAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/users/{userId:D}/instructor-eligibility/recalculate", null, cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<InstructorEligibilityResponse>>(response, cancellationToken);
    }

    /// <summary>Assigns an eligible instructor to a matching course skill.</summary>
    public Task<CourseResponse> AssignCourseInstructorAsync(
        Guid courseId, AssignCourseInstructorRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<CourseResponse>($"api/courses/{courseId:D}/instructor", request, cancellationToken);

    /// <summary>Starts a learner-owned AI Tutor conversation.</summary>
    public async Task<AiTutorConversationResponse> StartAiTutorConversationAsync(
        StartAiTutorConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/ai-tutor/conversations", request, cancellationToken);
        return await ReadResponseAsync<AiTutorConversationResponse>(
            response, cancellationToken);
    }

    /// <summary>Gets the signed-in learner's AI Tutor conversations.</summary>
    public async Task<IReadOnlyList<AiTutorConversationListItemResponse>>
        GetMyAiTutorConversationsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/ai-tutor/conversations", cancellationToken);
        return await ReadResponseAsync<IReadOnlyList<AiTutorConversationListItemResponse>>(
            response, cancellationToken);
    }

    /// <summary>Gets one learner-owned AI Tutor conversation.</summary>
    public async Task<AiTutorConversationResponse> GetAiTutorConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/ai-tutor/conversations/{conversationId:D}", cancellationToken);
        return await ReadResponseAsync<AiTutorConversationResponse>(
            response, cancellationToken);
    }

    /// <summary>Sends a question to the local AI Tutor.</summary>
    public async Task<AiTutorReplyResponse> SendAiTutorMessageAsync(
        Guid conversationId,
        SendAiTutorMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"api/ai-tutor/conversations/{conversationId:D}/messages",
            request,
            cancellationToken);
        return await ReadResponseAsync<AiTutorReplyResponse>(
            response, cancellationToken);
    }

    /// <summary>Archives a learner-owned AI Tutor conversation.</summary>
    public async Task<AiTutorConversationResponse> ArchiveAiTutorConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/ai-tutor/conversations/{conversationId:D}/archive",
            null,
            cancellationToken);
        return await ReadResponseAsync<AiTutorConversationResponse>(
            response, cancellationToken);
    }

    /// <summary>Gets administrator-visible local Ollama health.</summary>
    public async Task<OllamaHealthResponse> GetOllamaHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/admin/ai-tutor/health", cancellationToken);
        return await ReadResponseAsync<OllamaHealthResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PutAsync<TResponse>(
        string requestUri,
        object? request,
        CancellationToken cancellationToken)
    {
        using var response = request is null
            ? await httpClient.PutAsync(requestUri, content: null, cancellationToken)
            : await httpClient.PutAsJsonAsync(requestUri, request, cancellationToken);

        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<TResponse> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        return result ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = "The API request could not be completed.";
        string? errorCode = null;

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("title", out var title)
                    && !string.IsNullOrWhiteSpace(title.GetString()))
                {
                    message = title.GetString()!;
                }

                if (root.TryGetProperty("code", out var code))
                {
                    errorCode = code.GetString();
                }
                else if (root.TryGetProperty("errorCode", out var legacyCode))
                {
                    errorCode = legacyCode.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Preserve the safe fallback for a malformed error response.
        }

        throw new ApiProblemException(response.StatusCode, message, errorCode);
    }
}
