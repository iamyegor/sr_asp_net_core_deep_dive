using System.Reflection;

namespace CourseLibrary.API.Helpers;

public class PropertyChecker
{
    public bool HasPropertiesForDataShaping<T>(string? joinedProperties)
    {
        if (string.IsNullOrWhiteSpace(joinedProperties))
        {
            return true;
        }

        IEnumerable<string> properties = joinedProperties
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim());

        foreach (var property in properties)
        {
            PropertyInfo? propertyInfo = typeof(T).GetProperty(
                property,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (propertyInfo == null)
            {
                return false;
            }
        }

        return true;
    }
}
