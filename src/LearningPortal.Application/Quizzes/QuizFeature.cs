#pragma warning disable CS1591
using System.Text.Json;
using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Quizzes;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Quizzes;

public sealed record CreateQuizCommand(Guid CourseId, SaveQuizRequest Request)
    : ICommand<Result<QuizAdministrationResponse>>;
public sealed record UpdateQuizCommand(Guid QuizId, SaveQuizRequest Request)
    : ICommand<Result<QuizAdministrationResponse>>;
public sealed record PublishQuizCommand(Guid QuizId) : ICommand<Result<QuizAdministrationResponse>>;
public sealed record ArchiveQuizCommand(Guid QuizId) : ICommand<Result<QuizAdministrationResponse>>;
public sealed record AddQuizQuestionCommand(Guid QuizId, SaveQuizQuestionRequest Request)
    : ICommand<Result<QuizAdministrationResponse>>;
public sealed record UpdateQuizQuestionCommand(Guid QuizId, Guid QuestionId, SaveQuizQuestionRequest Request)
    : ICommand<Result<QuizAdministrationResponse>>;
public sealed record StartQuizAttempt(Guid QuizId) : ICommand<Result<StartQuizAttemptResponse>>;
public sealed record ResumeQuizAttempt(Guid AttemptId) : IQuery<Result<QuizAttemptResponse>>;
public sealed record SubmitQuizAttempt(Guid AttemptId, SubmitQuizAttemptRequest Request)
    : ICommand<Result<QuizAttemptResponse>>;
public sealed record GetQuiz(Guid QuizId) : IQuery<Result<QuizResponse>>;
public sealed record GetQuizAttempt(Guid AttemptId) : IQuery<Result<QuizAttemptResponse>>;
public sealed record GetMyAttempts(Guid QuizId) : IQuery<Result<IReadOnlyList<QuizAttemptResponse>>>;
public sealed record GetQuizAdministration(Guid QuizId) : IQuery<Result<QuizAdministrationResponse>>;
public sealed record GetCourseQuizAdministration(Guid CourseId)
    : IQuery<Result<IReadOnlyList<QuizAdministrationResponse>>>;

public sealed class SaveQuizRequestValidator : AbstractValidator<SaveQuizRequest>
{
    public SaveQuizRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.PassingPercentage).InclusiveBetween(1, 100);
        RuleFor(request => request.MaximumAttempts).GreaterThan(0).When(request => request.MaximumAttempts.HasValue);
    }
}

public sealed class SaveQuizQuestionRequestValidator : AbstractValidator<SaveQuizQuestionRequest>
{
    public SaveQuizQuestionRequestValidator()
    {
        RuleFor(request => request.Text).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.Points).GreaterThan(0);
        RuleFor(request => request.Order).GreaterThan(0);
        RuleFor(request => request.QuestionType).Must(value => Enum.TryParse<QuestionType>(value, true, out _));
        RuleForEach(request => request.Choices).ChildRules(choice =>
        {
            choice.RuleFor(value => value.Text).NotEmpty().MaximumLength(2000);
            choice.RuleFor(value => value.Order).GreaterThan(0);
        });
        RuleFor(request => request).Must(HasValidChoices)
            .WithMessage("Answer choices do not satisfy the selected question type.");
    }

    private static bool HasValidChoices(SaveQuizQuestionRequest request)
    {
        if (!Enum.TryParse<QuestionType>(request.QuestionType, true, out var type)
            || request.Choices.Select(choice => choice.Order).Distinct().Count() != request.Choices.Count)
        {
            return false;
        }

        var correct = request.Choices.Count(choice => choice.IsCorrect);
        return type switch
        {
            QuestionType.SingleChoice => request.Choices.Count > 0 && correct == 1,
            QuestionType.MultipleChoice => request.Choices.Count > 0 && correct > 0,
            QuestionType.TrueFalse => request.Choices.Count == 2 && correct == 1,
            _ => false
        };
    }
}

public sealed class SubmitQuizAttemptRequestValidator : AbstractValidator<SubmitQuizAttemptRequest>
{
    public SubmitQuizAttemptRequestValidator()
    {
        RuleFor(request => request.Answers).NotNull();
        RuleFor(request => request.Answers.Select(answer => answer.QuestionId))
            .Must(ids => ids.Distinct().Count() == ids.Count()).WithMessage("A question may only be answered once.");
        RuleForEach(request => request.Answers).ChildRules(answer =>
        {
            answer.RuleFor(value => value.QuestionId).NotEmpty();
            answer.RuleFor(value => value.SelectedChoiceIds).NotNull();
            answer.RuleFor(value => value.SelectedChoiceIds)
                .Must(ids => ids.Distinct().Count() == ids.Count()).WithMessage("Duplicate choices are not allowed.");
        });
    }
}

public sealed class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(command => command.CourseId).NotEmpty();
        RuleFor(command => command.Request).SetValidator(new SaveQuizRequestValidator());
    }
}

public sealed class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
{
    public UpdateQuizCommandValidator()
    {
        RuleFor(command => command.QuizId).NotEmpty();
        RuleFor(command => command.Request).SetValidator(new SaveQuizRequestValidator());
    }
}

public sealed class AddQuizQuestionCommandValidator : AbstractValidator<AddQuizQuestionCommand>
{
    public AddQuizQuestionCommandValidator()
    {
        RuleFor(command => command.QuizId).NotEmpty();
        RuleFor(command => command.Request).SetValidator(new SaveQuizQuestionRequestValidator());
    }
}

public sealed class UpdateQuizQuestionCommandValidator : AbstractValidator<UpdateQuizQuestionCommand>
{
    public UpdateQuizQuestionCommandValidator()
    {
        RuleFor(command => command.QuizId).NotEmpty();
        RuleFor(command => command.QuestionId).NotEmpty();
        RuleFor(command => command.Request).SetValidator(new SaveQuizQuestionRequestValidator());
    }
}

public sealed class SubmitQuizAttemptValidator : AbstractValidator<SubmitQuizAttempt>
{
    public SubmitQuizAttemptValidator()
    {
        RuleFor(command => command.AttemptId).NotEmpty();
        RuleFor(command => command.Request).SetValidator(new SubmitQuizAttemptRequestValidator());
    }
}

public sealed class QuizAdministrationHandler(
    IQuizRepository quizzes,
    ICourseRepository courses,
    ILessonRepository lessons,
    IUnitOfWork unit,
    ICurrentUserService user)
    : ICommandHandler<CreateQuizCommand, Result<QuizAdministrationResponse>>,
      ICommandHandler<UpdateQuizCommand, Result<QuizAdministrationResponse>>,
      ICommandHandler<PublishQuizCommand, Result<QuizAdministrationResponse>>,
      ICommandHandler<ArchiveQuizCommand, Result<QuizAdministrationResponse>>,
      ICommandHandler<AddQuizQuestionCommand, Result<QuizAdministrationResponse>>,
      ICommandHandler<UpdateQuizQuestionCommand, Result<QuizAdministrationResponse>>,
      IQueryHandler<GetQuizAdministration, Result<QuizAdministrationResponse>>,
      IQueryHandler<GetCourseQuizAdministration, Result<IReadOnlyList<QuizAdministrationResponse>>>
{
    public async Task<Result<QuizAdministrationResponse>> HandleAsync(CreateQuizCommand command, CancellationToken ct = default)
    {
        var course = await courses.GetByIdReadOnlyAsync(command.CourseId, ct);
        var error = Authorize(course, command.CourseId);
        if (error is not null) return Result<QuizAdministrationResponse>.Failure(error);
        if (!await LessonBelongsAsync(command.Request.LessonId, command.CourseId, ct))
            return Failure("Quiz.InvalidLesson", "The selected lesson does not belong to the quiz course.");
        var request = command.Request;
        var quiz = Quiz.Create(command.CourseId, request.LessonId, request.Title, request.Description,
            request.PassingPercentage, request.MaximumAttempts, request.IsRequired);
        await quizzes.AddAsync(quiz, ct);
        await unit.SaveChangesAsync(ct);
        return Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz));
    }

    public async Task<Result<QuizAdministrationResponse>> HandleAsync(UpdateQuizCommand command, CancellationToken ct = default)
    {
        var quiz = await quizzes.GetGraphAsync(command.QuizId, ct);
        if (quiz is null) return NotFound(command.QuizId);
        var course = await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct);
        var error = Authorize(course, quiz.CourseId);
        if (error is not null) return Result<QuizAdministrationResponse>.Failure(error);
        if (!await LessonBelongsAsync(command.Request.LessonId, quiz.CourseId, ct))
            return Failure("Quiz.InvalidLesson", "The selected lesson does not belong to the quiz course.");
        var request = command.Request;
        if (!quiz.TryUpdate(request.Title, request.Description, request.PassingPercentage,
                request.MaximumAttempts, request.IsRequired))
            return Failure("Quiz.InvalidState", "Only a draft quiz can be updated.");
        await unit.SaveChangesAsync(ct);
        return Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz));
    }

    public Task<Result<QuizAdministrationResponse>> HandleAsync(PublishQuizCommand command, CancellationToken ct = default) =>
        TransitionAsync(command.QuizId, quiz => quiz.TryPublish(), "published", ct);

    public Task<Result<QuizAdministrationResponse>> HandleAsync(ArchiveQuizCommand command, CancellationToken ct = default) =>
        TransitionAsync(command.QuizId, quiz => quiz.TryArchive(), "archived", ct);

    public async Task<Result<QuizAdministrationResponse>> HandleAsync(AddQuizQuestionCommand command, CancellationToken ct = default)
    {
        var quiz = await quizzes.GetGraphAsync(command.QuizId, ct);
        if (quiz is null) return NotFound(command.QuizId);
        var course = await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct);
        var error = Authorize(course, quiz.CourseId);
        if (error is not null) return Result<QuizAdministrationResponse>.Failure(error);
        if (!Enum.TryParse<QuestionType>(command.Request.QuestionType, true, out var type))
            return Failure("Quiz.InvalidQuestionType", "The question type is invalid.");
        var request = command.Request;
        var question = QuizQuestion.Create(quiz.Id, request.Text, type, request.Points, request.Order, request.Explanation);
        if (!request.IsActive) question.Deactivate();
        foreach (var item in request.Choices.OrderBy(choice => choice.Order))
        {
            question.TryAddAnswerChoice(QuizAnswerChoice.Create(question.Id, item.Text, item.IsCorrect, item.Order));
        }
        if (!question.HasValidAnswers(question.AnswerChoices) || !quiz.TryAddQuestion(question))
            return Failure("Quiz.InvalidQuestion", "The question is invalid or its order is already used.");
        await unit.SaveChangesAsync(ct);
        return Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz));
    }

    public async Task<Result<QuizAdministrationResponse>> HandleAsync(
        UpdateQuizQuestionCommand command,
        CancellationToken ct = default)
    {
        var quiz = await quizzes.GetGraphAsync(command.QuizId, ct);
        if (quiz is null) return NotFound(command.QuizId);
        var course = await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct);
        var error = Authorize(course, quiz.CourseId);
        if (error is not null) return Result<QuizAdministrationResponse>.Failure(error);
        if (quiz.Status != QuizStatus.Draft)
            return Failure("Quiz.InvalidState", "Only draft quiz questions can be updated.");
        var question = quiz.Questions.SingleOrDefault(value => value.Id == command.QuestionId);
        if (question is null)
            return Failure("Quiz.QuestionNotFound", "The quiz question was not found.");
        if (quiz.Questions.Any(value => value.Id != question.Id && value.Order == command.Request.Order))
            return Failure("Quiz.DuplicateQuestionOrder", "A question already uses the requested order.");
        if (!Enum.TryParse<QuestionType>(command.Request.QuestionType, true, out var type))
            return Failure("Quiz.InvalidQuestionType", "The question type is invalid.");
        var request = command.Request;
        if (!question.TryUpdate(request.Text, type, request.Points, request.Explanation)
            || !question.TryReorder(request.Order))
            return Failure("Quiz.InvalidQuestion", "The question values are invalid.");
        if (request.IsActive) question.Activate(); else question.Deactivate();
        foreach (var existing in question.AnswerChoices.ToArray())
            question.TryRemoveAnswerChoice(existing.Id);
        foreach (var item in request.Choices.OrderBy(choice => choice.Order))
            question.TryAddAnswerChoice(QuizAnswerChoice.Create(question.Id, item.Text, item.IsCorrect, item.Order));
        if (!question.HasValidAnswers(question.AnswerChoices))
            return Failure("Quiz.InvalidQuestion", "Answer choices do not satisfy the question type.");
        await unit.SaveChangesAsync(ct);
        return Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz));
    }

    public async Task<Result<QuizAdministrationResponse>> HandleAsync(GetQuizAdministration query, CancellationToken ct = default)
    {
        var quiz = await quizzes.GetGraphAsync(query.QuizId, ct);
        if (quiz is null) return NotFound(query.QuizId);
        var error = Authorize(await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct), quiz.CourseId);
        return error is null
            ? Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz))
            : Result<QuizAdministrationResponse>.Failure(error);
    }

    public async Task<Result<IReadOnlyList<QuizAdministrationResponse>>> HandleAsync(
        GetCourseQuizAdministration query,
        CancellationToken ct = default)
    {
        var course = await courses.GetByIdReadOnlyAsync(query.CourseId, ct);
        var error = Authorize(course, query.CourseId);
        if (error is not null) return Result<IReadOnlyList<QuizAdministrationResponse>>.Failure(error);
        var values = await quizzes.GetByCourseAsync(query.CourseId, ct);
        return Result<IReadOnlyList<QuizAdministrationResponse>>.Success(
            values.Select(QuizMappings.ToAdministration).ToArray());
    }

    private async Task<Result<QuizAdministrationResponse>> TransitionAsync(
        Guid quizId, Func<Quiz, bool> transition, string operation, CancellationToken ct)
    {
        var quiz = await quizzes.GetGraphAsync(quizId, ct);
        if (quiz is null) return NotFound(quizId);
        var error = Authorize(await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct), quiz.CourseId);
        if (error is not null) return Result<QuizAdministrationResponse>.Failure(error);
        if (!transition(quiz)) return Failure("Quiz.InvalidState", $"The quiz cannot be {operation}.");
        await unit.SaveChangesAsync(ct);
        return Result<QuizAdministrationResponse>.Success(QuizMappings.ToAdministration(quiz));
    }

    private Error? Authorize(Domain.Courses.Course? course, Guid courseId) =>
        course is null ? Errors.CourseManagement.NotFound(courseId)
        : CourseAuthorization.ValidateManager(user) ?? (!CourseAuthorization.CanAccess(user, course)
            ? Errors.Authorization.Forbidden() : null);

    private async Task<bool> LessonBelongsAsync(Guid? lessonId, Guid courseId, CancellationToken ct)
    {
        if (lessonId is null) return true;
        var lesson = await lessons.GetByIdReadOnlyAsync(lessonId.Value, ct);
        return lesson?.CourseId == courseId;
    }

    private static Result<QuizAdministrationResponse> NotFound(Guid id) =>
        Result<QuizAdministrationResponse>.Failure(Errors.Common.NotFound("Quiz", id));
    private static Result<QuizAdministrationResponse> Failure(string code, string message) =>
        Result<QuizAdministrationResponse>.Failure(Errors.Common.Failure(code, message));
}

public sealed class QuizAttemptHandler(
    IQuizRepository quizzes,
    IQuizAttemptRepository attempts,
    IEnrollmentRepository enrollments,
    ICourseRepository courses,
    ILessonRepository lessons,
    ILessonProgressRepository progress,
    IUnitOfWork unit,
    ICurrentUserService user,
    ISystemClock clock)
    : ICommandHandler<StartQuizAttempt, Result<StartQuizAttemptResponse>>,
      ICommandHandler<SubmitQuizAttempt, Result<QuizAttemptResponse>>,
      IQueryHandler<ResumeQuizAttempt, Result<QuizAttemptResponse>>,
      IQueryHandler<GetQuiz, Result<QuizResponse>>,
      IQueryHandler<GetQuizAttempt, Result<QuizAttemptResponse>>,
      IQueryHandler<GetMyAttempts, Result<IReadOnlyList<QuizAttemptResponse>>>
{
    public async Task<Result<StartQuizAttemptResponse>> HandleAsync(StartQuizAttempt command, CancellationToken ct = default)
    {
        var access = await LoadQuizAccessAsync(command.QuizId, ct);
        if (access.Error is not null) return Result<StartQuizAttemptResponse>.Failure(access.Error);
        var (quiz, enrollment, studentId) = access.Value!.Value;
        var active = await attempts.GetActiveAsync(quiz.Id, studentId, ct);
        if (active is not null)
            return Result<StartQuizAttemptResponse>.Success(new(active.Id, active.AttemptNumber, true));
        var count = await attempts.CountAsync(quiz.Id, studentId, ct);
        if (quiz.MaximumAttempts is int maximum && count >= maximum)
            return Result<StartQuizAttemptResponse>.Failure(Errors.Common.Failure(
                "Quiz.MaximumAttemptsExceeded", "The maximum number of attempts has been reached."));
        var attempt = QuizAttempt.Start(quiz.Id, enrollment.Id, studentId, count + 1, clock.UtcNow);
        await attempts.AddAsync(attempt, ct);
        enrollment.TryStart(clock.UtcNow);
        await unit.SaveChangesAsync(ct);
        return Result<StartQuizAttemptResponse>.Success(new(attempt.Id, attempt.AttemptNumber, false));
    }

    public async Task<Result<QuizAttemptResponse>> HandleAsync(SubmitQuizAttempt command, CancellationToken ct = default)
    {
        var attempt = await attempts.GetByIdAsync(command.AttemptId, false, ct);
        var ownership = ValidateOwnership(attempt);
        if (ownership is not null) return Result<QuizAttemptResponse>.Failure(ownership);
        if (attempt!.Status != QuizAttemptStatus.InProgress)
            return Result<QuizAttemptResponse>.Failure(Errors.Common.Failure("Quiz.AttemptSubmitted", "Submitted attempts are immutable."));
        var quiz = await quizzes.GetGraphAsync(attempt.QuizId, ct);
        if (quiz is null || quiz.Status != QuizStatus.Published)
            return Result<QuizAttemptResponse>.Failure(Errors.Common.Failure("Quiz.NotPublished", "Only published quizzes may be submitted."));
        var activeQuestions = quiz.Questions.Where(question => question.IsActive).OrderBy(question => question.Order).ToArray();
        if (command.Request.Answers.Select(answer => answer.QuestionId).Order()
            .SequenceEqual(activeQuestions.Select(question => question.Id).Order()) is false)
            return Result<QuizAttemptResponse>.Failure(Errors.Validation.Failed("Every active question must be answered exactly once."));
        try
        {
            var submitted = command.Request.Answers.ToDictionary(answer => answer.QuestionId);
            var snapshots = activeQuestions.Select(question =>
                QuizAttemptAnswer.Create(attempt.Id, question, submitted[question.Id].SelectedChoiceIds)).ToArray();
            if (!attempt.TrySubmit(snapshots, quiz.PassingPercentage, clock.UtcNow))
                return Result<QuizAttemptResponse>.Failure(Errors.Common.Failure("Quiz.InvalidSubmission", "The attempt could not be submitted."));
        }
        catch (ArgumentException exception)
        {
            return Result<QuizAttemptResponse>.Failure(Errors.Validation.Failed(exception.Message));
        }
        var enrollment = await enrollments.GetByIdAsync(attempt.EnrollmentId, ct);
        if (enrollment is not null)
            await CourseCompletion.TryCompleteAsync(
                enrollment, quizzes, attempts, lessons, progress, clock.UtcNow, attempt, ct);
        await unit.SaveChangesAsync(ct);
        return Result<QuizAttemptResponse>.Success(QuizMappings.ToAttempt(attempt));
    }

    public async Task<Result<QuizAttemptResponse>> HandleAsync(ResumeQuizAttempt query, CancellationToken ct = default) =>
        await GetOwnedAttemptAsync(query.AttemptId, ct);
    public async Task<Result<QuizAttemptResponse>> HandleAsync(GetQuizAttempt query, CancellationToken ct = default) =>
        await GetOwnedAttemptAsync(query.AttemptId, ct);

    public async Task<Result<QuizResponse>> HandleAsync(GetQuiz query, CancellationToken ct = default)
    {
        var access = await LoadQuizAccessAsync(query.QuizId, ct);
        return access.Error is null
            ? Result<QuizResponse>.Success(QuizMappings.ToLearner(access.Value!.Value.Quiz))
            : Result<QuizResponse>.Failure(access.Error);
    }

    public async Task<Result<IReadOnlyList<QuizAttemptResponse>>> HandleAsync(GetMyAttempts query, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is not Guid student)
            return Result<IReadOnlyList<QuizAttemptResponse>>.Failure(Errors.Authentication.Unauthorized());
        var values = await attempts.GetByQuizAndStudentAsync(query.QuizId, student, ct);
        return Result<IReadOnlyList<QuizAttemptResponse>>.Success(values.Select(QuizMappings.ToAttempt).ToArray());
    }

    private async Task<Result<QuizAttemptResponse>> GetOwnedAttemptAsync(Guid id, CancellationToken ct)
    {
        var attempt = await attempts.GetByIdAsync(id, true, ct);
        var error = ValidateOwnership(attempt);
        return error is null
            ? Result<QuizAttemptResponse>.Success(QuizMappings.ToAttempt(attempt!))
            : Result<QuizAttemptResponse>.Failure(error);
    }

    private Error? ValidateOwnership(QuizAttempt? attempt)
    {
        if (!user.IsAuthenticated || user.UserId is null) return Errors.Authentication.Unauthorized();
        if (attempt is null) return Errors.Common.NotFound("QuizAttempt", Guid.Empty);
        return attempt.StudentId == user.UserId ? null : Errors.Authorization.Forbidden();
    }

    private async Task<(Error? Error, (Quiz Quiz, Enrollment Enrollment, Guid StudentId)? Value)> LoadQuizAccessAsync(
        Guid quizId, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is not Guid student) return (Errors.Authentication.Unauthorized(), null);
        var quiz = await quizzes.GetGraphAsync(quizId, ct);
        if (quiz is null) return (Errors.Common.NotFound("Quiz", quizId), null);
        if (quiz.Status != QuizStatus.Published) return (Errors.Common.Failure("Quiz.NotPublished", "Only published quizzes may be attempted."), null);
        var course = await courses.GetByIdReadOnlyAsync(quiz.CourseId, ct);
        if (course?.Status != Domain.Courses.CourseStatus.Published) return (Errors.Authorization.Forbidden(), null);
        var enrollment = await enrollments.GetByCourseAndStudentAsync(quiz.CourseId, student, ct);
        if (enrollment is null || enrollment.Status is not (EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress or EnrollmentStatus.Completed))
            return (Errors.Authorization.Forbidden(), null);
        return (null, (quiz, enrollment, student));
    }
}

internal static class CourseCompletion
{
    internal static async Task TryCompleteAsync(
        Enrollment enrollment,
        IQuizRepository quizzes,
        IQuizAttemptRepository attempts,
        ILessonRepository lessons,
        ILessonProgressRepository progress,
        DateTimeOffset occurredAtUtc,
        QuizAttempt? currentAttempt,
        CancellationToken ct)
    {
        var publishedLessons = await lessons.GetPublishedByCourseAsync(enrollment.CourseId, ct);
        var lessonProgress = await progress.GetByEnrollmentAsync(enrollment.Id, ct);
        var lessonsComplete = publishedLessons.Count > 0
            && publishedLessons.All(lesson => lessonProgress.Any(item =>
                item.LessonId == lesson.Id && item.Status == Domain.Learning.LessonProgressStatus.Completed));
        if (!lessonsComplete) return;
        var required = await quizzes.GetRequiredPublishedByCourseAsync(enrollment.CourseId, ct);
        foreach (var quiz in required)
        {
            var passedByCurrent = currentAttempt is { Passed: true }
                && currentAttempt.QuizId == quiz.Id
                && currentAttempt.EnrollmentId == enrollment.Id;
            if (!passedByCurrent && !await attempts.HasPassedAsync(quiz.Id, enrollment.Id, ct)) return;
        }
        enrollment.TryComplete(occurredAtUtc);
    }
}

internal static class QuizMappings
{
    internal static QuizResponse ToLearner(Quiz quiz) => new(
        quiz.Id, quiz.CourseId, quiz.LessonId, quiz.Title, quiz.Description, quiz.PassingPercentage,
        quiz.MaximumAttempts, quiz.IsRequiredForCourseCompletion, quiz.Status.ToString(),
        quiz.Questions.Where(question => question.IsActive).OrderBy(question => question.Order)
            .Select(question => new QuizQuestionResponse(
                question.Id, question.Text, question.QuestionType.ToString(), question.Points, question.Order,
                question.AnswerChoices.OrderBy(choice => choice.Order)
                    .Select(choice => new QuizChoiceResponse(choice.Id, choice.Text, choice.Order)).ToArray()))
            .ToArray());

    internal static QuizAdministrationResponse ToAdministration(Quiz quiz) => new(
        quiz.Id, quiz.CourseId, quiz.LessonId, quiz.Title, quiz.Description, quiz.PassingPercentage,
        quiz.MaximumAttempts, quiz.IsRequiredForCourseCompletion, quiz.Status.ToString(),
        Convert.ToBase64String(quiz.RowVersion),
        quiz.Questions.OrderBy(question => question.Order).Select(question => new QuizAdminQuestionResponse(
            question.Id, question.Text, question.QuestionType.ToString(), question.Points, question.Order,
            question.Explanation, question.IsActive,
            question.AnswerChoices.OrderBy(choice => choice.Order)
                .Select(choice => new QuizAdminChoiceResponse(choice.Id, choice.Text, choice.IsCorrect, choice.Order))
                .ToArray())).ToArray());

    internal static QuizAttemptResponse ToAttempt(QuizAttempt attempt)
    {
        var submitted = attempt.Status == QuizAttemptStatus.Submitted;
        return new QuizAttemptResponse(
            attempt.Id, attempt.QuizId, attempt.AttemptNumber, attempt.Status.ToString(), attempt.StartedAtUtc,
            attempt.SubmittedAtUtc, submitted ? attempt.Score : null, submitted ? attempt.MaximumScore : null,
            submitted ? attempt.Percentage : null, submitted ? attempt.Passed : null,
            submitted ? attempt.Answers.Select(answer => new QuizAttemptAnswerResponse(
                answer.QuestionId, answer.QuestionText,
                JsonSerializer.Deserialize<Guid[]>(answer.SelectedChoiceIds) ?? [],
                answer.IsCorrect, answer.PointsAwarded, answer.MaximumPoints, answer.Explanation)).ToArray() : []);
    }
}
