using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for projecting an IQueryable source to a target type using expression trees.
/// </summary>
public static class ProjectionExtensions
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, LambdaExpression>> ProjectionCache = new();

    /// <summary>
    /// Projects an <see cref="IQueryable{TSource}"/> to an <see cref="IQueryable{TTarget}"/> using dynamically generated expression trees.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TTarget">The target DTO type.</typeparam>
    /// <param name="source">The source queryable to project.</param>
    /// <param name="customMapping">Optional custom mapping expression for complex scenarios.</param>
    /// <returns>An <see cref="IQueryable{TTarget}"/> with the projected properties.</returns>
    public static IQueryable<TTarget> ProjectTo<TSource, TTarget>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, object>>? customMapping = null)
        where TSource : class
    {
        var projection = GetProjection<TSource, TTarget>(customMapping);
        return source.Select(projection);
    }

    /// <summary>
    /// Retrieves or generates a projection expression from the source type to the target type.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TTarget">The target DTO type.</typeparam>
    /// <param name="customMapping">Optional custom mapping expression for complex scenarios.</param>
    /// <returns>An expression representing the projection from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.</returns>
    private static Expression<Func<TSource, TTarget>> GetProjection<TSource, TTarget>(
        Expression<Func<TSource, object>>? customMapping = null)
        where TSource : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Check cache first
        if (ProjectionCache.TryGetValue(sourceType, out var targetCache) &&
            targetCache.TryGetValue(targetType, out var projection))
        {
            return (Expression<Func<TSource, TTarget>>)projection;
        }

        var parameter = Expression.Parameter(sourceType, "entity");
        var bindings = new List<MemberBinding>();
        var targetProperties = targetType.GetProperties();

        // Generate default property mappings
        foreach (var targetProperty in targetProperties)
        {
            var sourceProperty = sourceType.GetProperty(targetProperty.Name);

            if (sourceProperty == null || sourceProperty.PropertyType != targetProperty.PropertyType)
                continue;

            var propertyAccess = Expression.Property(parameter, sourceProperty);
            var binding = Expression.Bind(targetProperty, propertyAccess);
            bindings.Add(binding);
        }

        // Create the default projection
        var constructor = Expression.New(targetType);
        var memberInit = Expression.MemberInit(constructor, bindings);

        // If custom mapping is provided, combine it with the default projection
        if (customMapping != null)
        {
            // Convert the custom mapping expression to match the target type
            var customMappingBody = new CustomMappingVisitor(parameter).Visit(customMapping.Body);
            var customMappingExpression = Expression.Lambda<Func<TSource, TTarget>>(
                Expression.MemberInit(
                    constructor,
                    bindings.Concat(new[] { Expression.Bind(
                        targetType.GetProperty("CustomProperty"), // Replace with the actual target property
                        customMappingBody
                    )}).ToList()
                ),
                parameter
            );

            // Cache the projection
            if (!ProjectionCache.TryGetValue(sourceType, out var value))
            {
                value = new ConcurrentDictionary<Type, LambdaExpression>();
                ProjectionCache[sourceType] = value;
            }

            value[targetType] = customMappingExpression;

            return customMappingExpression;
        }
        else
        {
            // Cache the default projection
            var lambda = Expression.Lambda<Func<TSource, TTarget>>(memberInit, parameter);

            if (!ProjectionCache.TryGetValue(sourceType, out var value))
            {
                value = new ConcurrentDictionary<Type, LambdaExpression>();
                ProjectionCache[sourceType] = value;
            }

            value[targetType] = lambda;

            return lambda;
        }
    }

    /// <summary>
    /// A visitor to replace the parameter in the custom mapping expression with the projection parameter.
    /// </summary>
    private class CustomMappingVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        public CustomMappingVisitor(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace the original parameter with the projection parameter
            return _parameter;
        }
    }
}