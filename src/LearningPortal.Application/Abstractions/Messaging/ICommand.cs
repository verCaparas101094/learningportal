namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>Marks a request that changes application state.</summary>
/// <typeparam name="TResponse">The command response type.</typeparam>
public interface ICommand<TResponse>;
