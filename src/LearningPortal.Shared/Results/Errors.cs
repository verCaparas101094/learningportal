using System.Globalization;

namespace LearningPortal.Shared.Results;

/// <summary>
/// Provides reusable, consistently coded application errors.
/// </summary>
public static class Errors
{
    /// <summary>
    /// Provides reusable validation errors.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Creates an error containing one or more validation failure messages.
        /// </summary>
        /// <param name="message">The safe, aggregated validation message.</param>
        /// <returns>A validation failure error.</returns>
        public static Error Failed(string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            return new Error(
                "Validation.Failed",
                message,
                ErrorType.Validation);
        }

        /// <summary>
        /// Creates an error indicating that a required field has no value.
        /// </summary>
        /// <param name="field">The field name used in the error code and message.</param>
        /// <returns>A validation error for the required field.</returns>
        public static Error Required(string field)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);

            return new Error(
                $"Validation.{field}.Required",
                $"The {field} field is required.",
                ErrorType.Validation);
        }

        /// <summary>
        /// Creates an error indicating that a field value is invalid.
        /// </summary>
        /// <param name="field">The field name used in the error code and message.</param>
        /// <returns>A validation error for the invalid field.</returns>
        public static Error Invalid(string field)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(field);

            return new Error(
                $"Validation.{field}.Invalid",
                $"The {field} field is invalid.",
                ErrorType.Validation);
        }
    }

    /// <summary>
    /// Provides reusable errors shared across application features.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Creates an error indicating that an entity could not be found.
        /// </summary>
        /// <param name="entity">The entity type or display name.</param>
        /// <param name="id">The requested entity identifier.</param>
        /// <returns>A not-found error for the requested entity.</returns>
        public static Error NotFound(string entity, object id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity);
            ArgumentNullException.ThrowIfNull(id);

            return new Error(
                $"{entity}.NotFound",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The {0} with identifier '{1}' was not found.",
                    entity,
                    id),
                ErrorType.NotFound);
        }

        /// <summary>
        /// Creates an error indicating that an entity already exists.
        /// </summary>
        /// <param name="entity">The entity type or display name.</param>
        /// <returns>A conflict error for the duplicate entity.</returns>
        public static Error Duplicate(string entity)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entity);

            return new Error(
                $"{entity}.Duplicate",
                $"The {entity} already exists.",
                ErrorType.Conflict);
        }

        /// <summary>
        /// Creates an expected operation failure.
        /// </summary>
        /// <param name="code">The stable, machine-readable error code.</param>
        /// <param name="message">The safe, human-readable error message.</param>
        /// <returns>An expected failure error.</returns>
        public static Error Failure(string code, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            return new Error(code, message, ErrorType.Failure);
        }

        /// <summary>
        /// Creates an unexpected technical failure without exposing implementation details.
        /// </summary>
        /// <returns>An unexpected failure error.</returns>
        public static Error Unexpected() => new(
            "Common.Unexpected",
            "An unexpected error occurred.",
            ErrorType.Unexpected);
    }

    /// <summary>
    /// Provides reusable authentication errors.
    /// </summary>
    public static class Authentication
    {
        /// <summary>
        /// Creates an error indicating that authentication is required or has failed.
        /// </summary>
        /// <returns>An unauthorized error.</returns>
        public static Error Unauthorized() => new(
            "Authentication.Unauthorized",
            "Authentication is required to access this resource.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for invalid credentials without identifying which credential failed.
        /// </summary>
        /// <returns>An invalid-credentials error.</returns>
        public static Error InvalidCredentials() => new(
            "Authentication.InvalidCredentials",
            "The email address or password is incorrect.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for a refresh token that is unknown or malformed.
        /// </summary>
        /// <returns>An invalid refresh-token error.</returns>
        public static Error InvalidRefreshToken() => new(
            "Authentication.InvalidRefreshToken",
            "The refresh token is invalid.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for an expired refresh token.
        /// </summary>
        /// <returns>An expired refresh-token error.</returns>
        public static Error RefreshTokenExpired() => new(
            "Authentication.RefreshTokenExpired",
            "The refresh token has expired.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for an explicitly revoked refresh token.
        /// </summary>
        /// <returns>A revoked refresh-token error.</returns>
        public static Error RefreshTokenRevoked() => new(
            "Authentication.RefreshTokenRevoked",
            "The refresh token is no longer valid.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error when a rotated refresh token is presented again.
        /// </summary>
        /// <returns>A refresh-token replay error.</returns>
        public static Error RefreshTokenReused() => new(
            "Authentication.RefreshTokenReplayDetected",
            "The refresh token is no longer valid.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for a user currently locked by ASP.NET Identity.
        /// </summary>
        /// <returns>A locked-user error.</returns>
        public static Error UserLocked() => new(
            "Authentication.UserLocked",
            "The account is temporarily unavailable.",
            ErrorType.Unauthorized);

        /// <summary>
        /// Creates an error for a disabled or otherwise unavailable user.
        /// </summary>
        /// <returns>An unavailable-user error.</returns>
        public static Error UserUnavailable() => new(
            "Authentication.UserUnavailable",
            "The account is unavailable.",
            ErrorType.Unauthorized);
    }

    /// <summary>
    /// Provides reusable authorization errors.
    /// </summary>
    public static class Authorization
    {
        /// <summary>
        /// Creates an error indicating that the caller lacks permission for an operation.
        /// </summary>
        /// <returns>A forbidden error.</returns>
        public static Error Forbidden() => new(
            "Authorization.Forbidden",
            "You do not have permission to perform this operation.",
            ErrorType.Forbidden);
    }

    /// <summary>
    /// Provides reusable administrator user-management errors.
    /// </summary>
    public static class UserManagement
    {
        /// <summary>Creates an error for an unknown user identifier.</summary>
        public static Error UserNotFound(Guid userId) => new(
            "UserManagement.UserNotFound",
            $"The user with identifier '{userId}' was not found.",
            ErrorType.NotFound);

        /// <summary>Creates an error for a role outside the application allowlist.</summary>
        public static Error InvalidRole() => new(
            "UserManagement.InvalidRole",
            "The specified role is not a valid application role.",
            ErrorType.Validation);

        /// <summary>Creates a safe failure for an Identity user update that could not be completed.</summary>
        public static Error UpdateFailed() => new(
            "UserManagement.UpdateFailed",
            "The user could not be updated.",
            ErrorType.Conflict);

        /// <summary>Creates a safe failure for an Identity role assignment that could not be completed.</summary>
        public static Error RoleAssignmentFailed() => new(
            "UserManagement.RoleAssignmentFailed",
            "The role could not be assigned to the user.",
            ErrorType.Conflict);
    }

    /// <summary>Provides course-management errors.</summary>
    public static class CourseManagement
    {
        /// <summary>Creates an error for an unknown course.</summary>
        public static Error NotFound(Guid courseId) => new(
            "Course.NotFound",
            $"The course with identifier '{courseId}' was not found.",
            ErrorType.NotFound);

        /// <summary>Creates an error for a duplicate normalized slug.</summary>
        public static Error DuplicateSlug() => new(
            "Course.DuplicateSlug",
            "A course with the specified slug already exists.",
            ErrorType.Conflict);

        /// <summary>Creates an error for an invalid assigned instructor.</summary>
        public static Error InvalidInstructor() => new(
            "Course.InvalidInstructor",
            "A valid enabled Instructor is required.",
            ErrorType.Validation);

        /// <summary>Creates an error for an invalid lifecycle operation.</summary>
        public static Error InvalidState(string operation) => new(
            "Course.InvalidState",
            $"The course cannot be {operation} in its current state.",
            ErrorType.Conflict);

        /// <summary>Creates an optimistic-concurrency conflict.</summary>
        public static Error ConcurrencyConflict() => new(
            "Course.ConcurrencyConflict",
            "The course was modified by another request. Reload it and try again.",
            ErrorType.Conflict);
    }

    /// <summary>Provides lesson-management errors.</summary>
    public static class LessonManagement
    {
        /// <summary>Creates a missing lesson error.</summary>
        public static Error NotFound(Guid id) => new("Lesson.NotFound", $"The lesson with identifier '{id}' was not found.", ErrorType.NotFound);
        /// <summary>Creates a duplicate order error.</summary>
        public static Error DuplicateOrder() => new("Lesson.DuplicateOrder", "A lesson with the specified order already exists.", ErrorType.Conflict);
        /// <summary>Creates an invalid lifecycle error.</summary>
        public static Error InvalidState(string operation) => new("Lesson.InvalidState", $"The lesson cannot be {operation} in its current state.", ErrorType.Conflict);
        /// <summary>Creates an invalid order error.</summary>
        public static Error InvalidOrder() => new("Lesson.InvalidOrder", "The requested lesson order is invalid.", ErrorType.Validation);
        /// <summary>Creates a concurrency conflict error.</summary>
        public static Error ConcurrencyConflict() => new("Lesson.ConcurrencyConflict", "The lesson was modified by another request. Reload it and try again.", ErrorType.Conflict);
        /// <summary>Creates a lesson content validation error.</summary>
        public static Error InvalidContent(string message) => new("Lesson.InvalidContent", message, ErrorType.Validation);
    }
}
