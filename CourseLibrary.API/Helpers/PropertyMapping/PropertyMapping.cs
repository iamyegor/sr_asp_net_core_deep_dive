namespace CourseLibrary.API.Helpers.PropertyMapping;

public class PropertyMapping<TSource, TDestination> : IPropertyMapping
{
    public Dictionary<string, PropertyMappingValue> Mappings { get; }

    public PropertyMapping(Dictionary<string, PropertyMappingValue> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        Mappings = mappings;
    }
}
