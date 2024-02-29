using System.Linq.Dynamic.Core;
using CourseLibrary.API.Helpers.PropertyMapping;

namespace CourseLibrary.API.Helpers.IQueryableExtensions;

public static class ForGeneric
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? requestedSorting,
        Dictionary<string, PropertyMappingValue> mappings
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(requestedSorting))
        {
            return query;
        }

        string orderByString = string.Empty;
        IEnumerable<string> orderByClauses = requestedSorting.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (var orderByClause in orderByClauses)
        {
            bool isDescending = orderByClause.EndsWith(" desc");
            string trimmedClause = orderByClause.Trim();

            int indexOfWhiteSpace = trimmedClause.IndexOf(' ');
            string propertyName =
                indexOfWhiteSpace == -1 ? trimmedClause : trimmedClause.Remove(indexOfWhiteSpace);

            if (!mappings.ContainsKey(propertyName))
            {
                throw new Exception($"There are no mappings for property: {propertyName}");
            }

            PropertyMappingValue propertyMappingValue = mappings[propertyName];
            isDescending = propertyMappingValue.IsDirectionReversed ? !isDescending : isDescending;

            foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
            {
                if (!string.IsNullOrWhiteSpace(orderByString))
                {
                    orderByString += ",";
                }

                string direction = isDescending ? "descending" : "ascending";
                orderByString += $"{destinationProperty} {direction}";
            }
        }

        return query.OrderBy(orderByString);
    }
}
