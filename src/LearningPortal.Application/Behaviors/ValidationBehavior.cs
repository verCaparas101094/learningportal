using FluentValidation;
using FluentValidation.Results;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Application.Behaviors;

/// <summary>
/// Validates commands before their handlers execute and returns failed Results for invalid requests.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TValue">The successful result value type.</typeparam>
public sealed class ValidationBehavior<TCommand, TValue>(
    IEnumerable<IValidator<TCommand>> validators,
    ILogger<ValidationBehavior<TCommand, TValue>> logger)
    : ICommandPipelineBehavior<TCommand, TValue>
    where TCommand : ICommand<Result<TValue>>
{
    private readonly IReadOnlyList<IValidator<TCommand>> _validators = validators.ToArray();

    /// <inheritdoc />
    public async Task<Result<TValue>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TValue> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Count == 0)
        {
            return await next();
        }

        var failures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            failures.AddRange(validationResult.Errors.Where(failure => failure is not null));
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        var messages = failures
            .Select(failure => failure.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var message = messages.Length > 0
            ? string.Join(" ", messages)
            : "One or more validation errors occurred.";

        logger.LogInformation(
            "Validation failed for command {CommandType}. Invalid properties: {InvalidProperties}.",
            typeof(TCommand).Name,
            failures.Select(failure => failure.PropertyName).Distinct(StringComparer.Ordinal).ToArray());

        return Result<TValue>.Failure(Errors.Validation.Failed(message));
    }
}
