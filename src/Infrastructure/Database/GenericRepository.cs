using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Domain.Abstractions.Database;
using Domain.Entities;

using Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

using Shared.Extensions;
using Shared.Pagination;

namespace Infrastructure.Database;

public abstract class GenericRepository<TEntity, TKey>(ApplicationDbContext dbContext, ILogger<RepositoryBase<TEntity, TKey>> logger)
    : RepositoryBase<TEntity, TKey>(dbContext, logger)
    where TEntity : Entity
{
}

public abstract class RepositoryBase<TEntity, TKey> : IRepositoryBase<TEntity, TKey>
    where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;
    protected readonly DbSet<TEntity> _entity;
    private readonly ILogger<RepositoryBase<TEntity, TKey>> _logger;

    protected RepositoryBase(ApplicationDbContext dbContext, ILogger<RepositoryBase<TEntity, TKey>> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
        _entity = _dbContext.Set<TEntity>();
    }


    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _entity.FindAsync([id], cancellationToken);
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression,
                                         bool asNoTracking = false,
                                         CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        return await query.FirstOrDefaultAsync(expression, cancellationToken);
    }

    public virtual async Task<bool> ExistsByAsync(Expression<Func<TEntity, bool>> expression,
                                                  bool asNoTracking = false,
                                                  CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        return await query.AnyAsync(expression, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? expression,
                                                         Sortable? sortable = null,
                                                         bool asNoTracking = false,
                                                         CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var newQuery = query.Where(expression ?? (x => true));

        newQuery = sortable.ApplySorting(newQuery);

        return await newQuery.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<TEntity, bool>>? expression,
                                                                  Sortable? sortable = null,
                                                                  bool asNoTracking = false,
                                                                  CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var newQuery = query.Where(expression ?? (x => true)).ProjectTo<TEntity, TResult>();

        newQuery = sortable.ApplySorting(newQuery);

        return await newQuery.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync([NotParameterized] FormattableString sql,
                                                         Sortable? sortable = null,
                                                         CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = _entity.FromSqlInterpolated(sql);

        query = sortable.ApplySorting(query);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> GetAllAsync<TResult>([NotParameterized] FormattableString sql,
                                                                  Sortable? sortable = null,
                                                                  CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = _dbContext.Database.SqlQuery<TResult>(sql);

        query = sortable.ApplySorting(query);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedData<TEntity>> GetAllPagedAsync(Expression<Func<TEntity, bool>>? expression,
                                                                   Pageable pageable,
                                                                   Sortable? sortable = null,
                                                                   bool asNoTracking = false,
                                                                   CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var newQuery = query.Where(expression ?? (x => true));

        newQuery = sortable.ApplySorting(newQuery);

        return await GetAllPagedAsync(newQuery, pageable, cancellationToken);
    }

    public virtual async Task<PagedData<TResult>> GetAllPagedAsync<TResult>(Expression<Func<TEntity, bool>>? expression,
                                                                            Pageable pageable,
                                                                            Sortable? sortable = null,
                                                                            bool asNoTracking = false,
                                                                            CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var newQuery = query.Where(expression ?? (x => true)).ProjectTo<TEntity, TResult>();

        newQuery = sortable.ApplySorting(newQuery);

        return await GetAllPagedAsync(newQuery, pageable, cancellationToken);
    }

    public virtual async Task<PagedData<TEntity>> GetAllPagedAsync([NotParameterized] FormattableString sql,
                                                                   Pageable pageable,
                                                                   Sortable? sortable = null,
                                                                   CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = _entity.FromSqlInterpolated(sql);

        query = sortable.ApplySorting(query);

        return await GetAllPagedAsync(query, pageable, cancellationToken);
    }

    public virtual async Task<PagedData<TResult>> GetAllPagedAsync<TResult>([NotParameterized] FormattableString sql,
                                                                            Pageable pageable,
                                                                            Sortable? sortable = null,
                                                                            CancellationToken cancellationToken = default)
    {
        sortable ??= new Sortable();

        var query = _dbContext.Database.SqlQuery<TResult>(sql);

        query = sortable.ApplySorting(query);

        return await GetAllPagedAsync(query, pageable, cancellationToken);
    }

    public static async Task<PagedData<TResult>> GetAllPagedAsync<TResult>(IQueryable<TResult> query,
                                                                            Pageable? pageable,
                                                                            CancellationToken cancellationToken = default)
    {
        pageable ??= new Pageable();

        int totalRecord = await query.CountAsync(cancellationToken);
        int totalPage = 1;

        if (pageable.AsPage)
        {
            totalPage = (int)Math.Ceiling(totalRecord / (decimal)pageable.PageSize);
            query = pageable.ApplyPagination(query);
        }
        else
        {
            pageable.PageSize = totalRecord;
            pageable.PageNumber = 1;
        }

        var data = await query.ToListAsync(cancellationToken);
        return data.ToPagedData(pageable.PageSize, pageable.PageNumber, totalPage, totalRecord);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken
                                           cancellationToken = default)
    {
        await _entity.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities,
                                            CancellationToken cancellationToken = default)
    {
        await _entity.AddRangeAsync(entities, cancellationToken);
    }

    public virtual Task Update(TEntity entity)
    {
        _entity.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateRange(IEnumerable<TEntity> entities)
    {
        _entity.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task<int> ExecuteUpdateAsync(Expression<Func<TEntity, bool>> expression,
                                                      TEntity model,
                                                      CancellationToken cancellationToken = default)
    {
        await Update(model);
        return await SaveChangesAsync(cancellationToken);
    }

    public virtual Task Remove(TEntity entity)
    {
        _entity.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task RemoveRange(IEnumerable<TEntity> entities)
    {
        _entity.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task RemoveByIdAsync(TKey id,
                                              CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is not null)
        {
            _entity.Remove(item);
        }
    }

    public virtual async Task RemoveByIdRangeAsync(Expression<Func<TEntity, bool>> expression,
                                                   bool asNoTracking = false,
                                                   CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(expression, null, asNoTracking, cancellationToken: cancellationToken);
        if (items.Count > 0)
        {
            _entity.RemoveRange(items);
        }
    }

    public virtual async Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> expression,
                                                      CancellationToken cancellationToken = default)
    {
        return await _entity.Where(expression).ExecuteDeleteAsync(cancellationToken);
    }

    public virtual async IAsyncEnumerable<TEntity> StreamAllAsync(Expression<Func<TEntity, bool>>? expression = null,
                                                                  bool asNoTracking = false,
                                                                  [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var filtered = query.Where(expression ?? (x => true));

        await foreach (var item in filtered.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public virtual async IAsyncEnumerable<TResult> StreamAllAsync<TResult>(Expression<Func<TEntity, bool>>? expression = null,
                                                                           bool asNoTracking = false,
                                                                           [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _entity.AsNoTracking() : _entity;
        var filtered = query.Where(expression ?? (x => true)).ProjectTo<TEntity, TResult>();

        await foreach (var item in filtered.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }


    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}