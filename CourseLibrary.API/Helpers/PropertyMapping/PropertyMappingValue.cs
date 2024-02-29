using Ardalis.GuardClauses;

namespace CourseLibrary.API.Helpers.PropertyMapping;

public class PropertyMappingValue
{
    public IEnumerable<string> DestinationProperties { get; }
    public bool IsDirectionReversed { get; }

    public PropertyMappingValue(
        IEnumerable<string> destinationProperties,
        bool isDirectionReversed = false
    )
    {
        DestinationProperties = Guard.Against.Null(destinationProperties);
        IsDirectionReversed = isDirectionReversed;
    }
}
