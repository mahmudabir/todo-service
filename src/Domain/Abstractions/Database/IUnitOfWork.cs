namespace Domain.Abstractions.Database;

public interface IUnitOfWork : IDisposable
{
    #region SqlServer

    // For Sql Server
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    // For Sql Server
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);

    #endregion SqlServer


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}