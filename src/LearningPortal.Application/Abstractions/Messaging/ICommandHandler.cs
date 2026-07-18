namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>Handles a state-changing request.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>Executes the command asynchronously.</summary>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
