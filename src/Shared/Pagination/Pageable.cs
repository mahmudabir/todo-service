using System.Text.Json.Serialization;

namespace Shared.Pagination;

public class Pageable
{
    private int _defaultPageSize = 20;
    private int _defaultPageNumber = 1;

    public bool AsPage { get; set; } = true;
    public bool AsDropdown { get; set; } = false;

    public int PageNumber
    {
        get
        {
            return _defaultPageNumber;
        }
        set
        {
            _defaultPageNumber = value <= 0 ? 1 : value;
        }
    }

    public int PageSize
    {
        get
        {
            return _defaultPageSize;
        }
        set
        {
            _defaultPageSize = value <= 0 ? 20 : value;
        }
    }

    public IQueryable<TEntity> ApplyPagination<TEntity>(IQueryable<TEntity> query)
    {
        query = query.Skip((PageNumber - 1) * PageSize)
                     .Take(PageSize);

        return query;
    }
}