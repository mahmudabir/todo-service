using System.Linq.Expressions;

using Domain.Entities;

using Microsoft.EntityFrameworkCore.Query;

using Shared.Pagination;

namespace Domain.Abstractions.Database;

public interface IRepository<TEntity, in TKey> : IRepositoryBase<TEntity, TKey> where TEntity : Entity
{
}

public interface IRepositoryBase<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression, bool asNoTracking = false, CancellationToken cancellationToken = default);



    Task<bool> ExistsByAsync(Expression<Func<TEntity, bool>> expression, bool asNoTracking = false, CancellationToken cancellationToken = default);



    Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? expression, Sortable? sortable = null, bool asNoTracking = false, CancellationToken cancellationToken = default);

    Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<TEntity, bool>>? expression, Sortable? sortable = null, bool asNoTracking = false, CancellationToken cancellationToken = default);



    Task<List<TEntity>> GetAllAsync([NotParameterized] FormattableString sql, Sortable? sortable = null, CancellationToken cancellationToken = default);

    Task<List<TResult>> GetAllAsync<TResult>([NotParameterized] FormattableString sql, Sortable? sortable = null, CancellationToken cancellationToken = default);


    Task<PagedData<TEntity>> GetAllPagedAsync(Expression<Func<TEntity, bool>>? expression, Pageable pageable, Sortable? sortable = null, bool asNoTracking = false, CancellationToken cancellationToken = default);

    Task<PagedData<TResult>> GetAllPagedAsync<TResult>(Expression<Func<TEntity, bool>>? expression, Pageable pageable, Sortable? sortable = null, bool asNoTracking = false, CancellationToken cancellationToken = default);



    Task<PagedData<TEntity>> GetAllPagedAsync([NotParameterized] FormattableString sql, Pageable pageable, Sortable? sortable = null, CancellationToken cancellationToken = default);

    Task<PagedData<TResult>> GetAllPagedAsync<TResult>([NotParameterized] FormattableString sql, Pageable pageable, Sortable? sortable = null, CancellationToken cancellationToken = default);


    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);


    Task Update(TEntity entity);

    Task UpdateRange(IEnumerable<TEntity> entities);


    Task Remove(TEntity entity);

    Task RemoveRange(IEnumerable<TEntity> entities);

    Task RemoveByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task RemoveByIdRangeAsync(Expression<Func<TEntity, bool>> expression, bool asNoTracking = false, CancellationToken cancellationToken = default);



    Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);


    Task<int> ExecuteUpdateAsync(Expression<Func<TEntity, bool>> expression, TEntity model, CancellationToken cancellationToken = default);


    IAsyncEnumerable<TEntity> StreamAllAsync(Expression<Func<TEntity, bool>>? expression = null, bool asNoTracking = false, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TResult> StreamAllAsync<TResult>(Expression<Func<TEntity, bool>>? expression = null, bool asNoTracking = false, CancellationToken cancellationToken = default);


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}