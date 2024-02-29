using Microsoft.EntityFrameworkCore;

namespace CourseLibrary.API.Helpers;

public class PagedList<T> : List<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    private PagedList(IEnumerable<T> items, int pageNumber, int pageSize, int totalElements)
    {
        AddRange(items);

        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalElements = totalElements;
        TotalPages = (int)Math.Ceiling(totalElements / (double)pageSize);
    }

    public static async Task<PagedList<T>> Create(
        IQueryable<T> collection,
        int pageNumber,
        int pageSize
    )
    {
        int totalElements = await collection.CountAsync();

        List<T> items = await collection
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(items, pageNumber, pageSize, totalElements);
    }
}
