using Shared.Pagination;

namespace Shared.Extensions;

public static class TypeExtension
{
    /// <summary>
    /// Null or Empty checking for any kind of data type & Class type as well
    /// </summary>
    public static bool IsNullOrEmpty<TSource>(this TSource? value)
    {
        if (value is null) return true;
        return value is string str && (string.IsNullOrWhiteSpace(str) || string.IsNullOrEmpty(str));
    }

    /// <summary>
    /// Get percentage value for decimal type variable only
    /// </summary>
    public static decimal Percentage(this decimal value, double percentage)
    {
        return value * ((decimal)percentage / 100);
    }

    /// <summary>
    /// Get percentage value for any number type variable except decimal
    /// </summary>
    public static double Percentage<TSource>(this TSource value, double percentage)
        where TSource : struct, IConvertible
    {
        double result = Convert.ToDouble(value);
        return result * (percentage / 100);
    }

    /// <summary>
    /// Check if the value is between the range
    /// </summary>
    public static bool IsBetween<TSource>(this TSource value, TSource minRange, TSource maxRange)
        where TSource : struct, IComparable
    {
        return value.CompareTo(minRange) >= 0 && value.CompareTo(maxRange) <= 0;
    }

    /// <summary>
    /// If any item is null or if no item is passed, then returns true
    /// </summary>
    public static bool HasAnyNull<TSource>(this IEnumerable<TSource>? source)
    {
        return source is null || source.Any(item => item == null);
    }

    /// <summary>
    /// If all items are null or if no item is passed, then returns true
    /// </summary>
    public static bool HasAllNull<TSource>(this IEnumerable<TSource>? source)
    {
        return source is null || source.All(item => item == null);
    }

    public static PagedData<T> ToPagedData<T>(this IEnumerable<T> source, int pageSize, int pageNumber, int totalPage, int totalRecord)
    {
        return new PagedData<T>
        {
            PageSize = pageSize,
            PageNumber = pageNumber,
            TotalPage = totalPage,
            TotalRecord = totalRecord,
            Content = source ?? Enumerable.Empty<T>()
        };
    }
}