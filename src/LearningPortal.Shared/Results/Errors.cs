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
}
