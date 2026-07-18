using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.DependencyInjection;

namespace LearningPortal.Application.Messaging;

/// <summary>
/// Resolves command handlers and composes their registered pipeline behaviors.
/// </summary>
internal sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    /// <inheritdoc />
    public async Task<Result<TValue>> SendAsync<TCommand, TValue>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<Result<TValue>>
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, Result<TValue>>>();
        var behaviors = serviceProvider
            .GetServices<ICommandPipelineBehavior<TCommand, TValue>>()
            .Reverse()
            .ToArray();

        CommandHandlerDelegate<TValue> next = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var currentNext = next;
            next = () => behavior.HandleAsync(command, currentNext, cancellationToken);
        }

        return await next();
    }
}
