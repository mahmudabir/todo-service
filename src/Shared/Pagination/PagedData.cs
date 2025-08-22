namespace Shared.Pagination;

public class PagedData
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPage { get; set; }
    public int TotalRecord { get; set; }
}

public class PagedData<T> : PagedData
{
    public PagedData() { }

    public PagedData(PagedData pagedData)
    {
        PageNumber = pagedData.PageNumber;
        PageSize = pagedData.PageSize;
        TotalPage = pagedData.TotalPage;
        TotalRecord = pagedData.TotalRecord;
    }

    public required IEnumerable<T> Content { get; set; } = [];
}