using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>
/// Dispatches commands through the configured application pipeline before invoking their handlers.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command through its pipeline and returns its result.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TValue">The successful result value type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result produced by the command pipeline.</returns>
    Task<Result<TValue>> SendAsync<TCommand, TValue>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<Result<TValue>>;
}
