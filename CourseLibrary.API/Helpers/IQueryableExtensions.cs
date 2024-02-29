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
        if (!string.IsNullOrWhiteSpace(parameters.MainCategory))
        {
            return query.Where(x => x.MainCategory == parameters.MainCategory);
        }

        return query;
    }
}
