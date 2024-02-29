using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;

namespace CourseLibrary.API.Helpers.PropertyMapping;

public class PropertyMappingService
{
    private readonly Dictionary<string, PropertyMappingValue> _authorMappings = new Dictionary<
        string,
        PropertyMappingValue
    >(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = new PropertyMappingValue(["Id"]),
        ["Name"] = new PropertyMappingValue(["FirstName", "LastName"]),
        ["Age"] = new PropertyMappingValue(["DateOfBirth"], true),
        ["MainCategory"] = new PropertyMappingValue(["MainCategory"])
    };

    private readonly List<IPropertyMapping> _propertyMappings = [];

    public PropertyMappingService()
    {
        _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorMappings));
    }

    public Dictionary<string, PropertyMappingValue> GetMappings<TSource, TDestination>()
    {
        List<PropertyMapping<TSource, TDestination>> mappings = _propertyMappings
            .OfType<PropertyMapping<TSource, TDestination>>()
            .ToList();

        if (mappings.Count > 1)
        {
            throw new Exception(
                $"There are more that one mapping for <{typeof(TSource).Name}, {typeof(TDestination).Name}>"
            );
        }

        return mappings.First().Mappings;
    }

    public bool HasMappingsFor<TSource, TDestination>(string? requestedProperties)
    {
        if (requestedProperties == null)
        {
            return true;
        }

        Dictionary<string, PropertyMappingValue> mappings = GetMappings<TSource, TDestination>();

        IEnumerable<string> orderByClauses = requestedProperties.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (var orderByClause in orderByClauses)
        {
            string trimmedClause = orderByClause.Trim();

            int indexOfWhiteSpace = trimmedClause.IndexOf(' ');
            string propertyName =
                indexOfWhiteSpace == -1 ? trimmedClause : trimmedClause.Remove(indexOfWhiteSpace);

            if (!mappings.ContainsKey(propertyName))
            {
                return false;
            }
        }

        return true;
    }
}
