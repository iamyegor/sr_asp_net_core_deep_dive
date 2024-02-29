using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers;

public static class ObjectExtensions
{
    public static ExpandoObject ShapeData<T>(this T sourceObject, string? joinedFields)
    {
        ArgumentNullException.ThrowIfNull(sourceObject);

        List<PropertyInfo> propertyInfos = [];

        if (string.IsNullOrWhiteSpace(joinedFields))
        {
            IEnumerable<PropertyInfo> retrievedPropertyInfos = typeof(T).GetProperties(
                BindingFlags.Public | BindingFlags.Instance
            );

            propertyInfos.AddRange(retrievedPropertyInfos);
        }
        else
        {
            IEnumerable<string> fields = joinedFields
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim());

            foreach (var field in fields)
            {
                PropertyInfo? retrievedPropertyInfo = typeof(T).GetProperty(
                    field,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
                );

                if (retrievedPropertyInfo == null)
                {
                    throw new Exception($"{typeof(T).Name} doesn't contain a property {field}");
                }

                propertyInfos.Add(retrievedPropertyInfo);
            }
        }

        ExpandoObject dataShapedObject = new();
        foreach (var propertyInfo in propertyInfos)
        {
            ((IDictionary<string, object?>)dataShapedObject).Add(
                propertyInfo.Name,
                propertyInfo.GetValue(sourceObject)
            );
        }

        return dataShapedObject;
    }
}
