using System.Linq.Expressions;

namespace Shared.Pagination;

public class Sortable
{
    public string? OrderBy { get; set; }
    public bool IsAscending { get; set; }

    public IQueryable<TEntity> ApplySorting<TEntity>(IQueryable<TEntity> query)
    {
        if (string.IsNullOrWhiteSpace(OrderBy))
            return query;

        try
        {
            // Create parameter expression (x)
            var parameter = Expression.Parameter(typeof(TEntity), "x");

            // Try to get property by name
            var property = Expression.Property(parameter, OrderBy);

            // Create lambda expression
            var lambda = Expression.Lambda(property, parameter);

            // Determine sorting method based on a direction
            string methodName = IsAscending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

            // Create method call expression
            var resultExpression = Expression.Call(typeof(Queryable),
                                                   methodName,
                                                   [typeof(TEntity), property.Type],
                                                   query.Expression,
                                                   Expression.Quote(lambda)
                                                  );

            // Apply sorting to query
            return query.Provider.CreateQuery<TEntity>(resultExpression);
        }
        catch
        {
            // Fallback to default sorting or return unsorted query
            return query;
        }
    }
}