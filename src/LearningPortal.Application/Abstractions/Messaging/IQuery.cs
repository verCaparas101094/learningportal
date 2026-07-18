namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>Marks a request that reads application state.</summary>
/// <typeparam name="TResponse">The query response type.</typeparam>
public interface IQuery<TResponse>;
