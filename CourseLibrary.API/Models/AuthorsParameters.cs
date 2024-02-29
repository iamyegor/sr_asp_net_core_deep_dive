namespace CourseLibrary.API.Models;

public class AuthorsParameters
{
    private const int MaxPageSize = 20;
    public string? MainCategory { get; set; }
    public string? SearchQuery { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    private int _pageSize = MaxPageSize;
    public string? OrderBy { get; set; }
}
