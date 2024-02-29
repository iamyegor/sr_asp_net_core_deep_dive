using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.Helpers;

// ReSharper disable once InconsistentNaming
public static class IQueryableExtensions
{
    public static IQueryable<Author> ApplyFiltering(
        this IQueryable<Author> query,
        AuthorParameters parameters
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(parameters);

        if (!string.IsNullOrWhiteSpace(parameters.MainCategory))
        {
            return query.Where(x => x.MainCategory == parameters.MainCategory);
        }

        return query;
    }

    public static IQueryable<Author> ApplySearching(
        this IQueryable<Author> query,
        string? searchQuery
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            searchQuery = searchQuery.ToLower();
            return query.Where(x =>
                x.FirstName.ToLower().Contains(searchQuery)
                || x.LastName.ToLower().Contains(searchQuery)
                || x.MainCategory.ToLower().Contains(searchQuery)
            );
        }

        return query;
    }
}
