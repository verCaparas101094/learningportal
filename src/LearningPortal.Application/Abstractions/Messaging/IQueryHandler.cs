namespace LearningPortal.Application.Abstractions.Messaging;

/// <summary>Handles a read-only request.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Executes the query asynchronously.</summary>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
