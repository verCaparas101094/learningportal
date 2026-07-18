using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>
/// Defines a component that runs before and/or after a command handler.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TValue">The successful result value type.</typeparam>
public interface ICommandPipelineBehavior<TCommand, TValue>
    where TCommand : ICommand<Result<TValue>>
{
    /// <summary>
    /// Processes a command and optionally invokes the next pipeline component.
    /// </summary>
    /// <param name="command">The command being processed.</param>
    /// <param name="next">The next component in the command pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The command result.</returns>
    Task<Result<TValue>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TValue> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the next executable component in a Result-based command pipeline.
/// </summary>
/// <typeparam name="TValue">The successful result value type.</typeparam>
/// <returns>The asynchronous command result.</returns>
public delegate Task<Result<TValue>> CommandHandlerDelegate<TValue>();
