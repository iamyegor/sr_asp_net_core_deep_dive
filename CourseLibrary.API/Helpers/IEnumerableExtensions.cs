using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers;

// ReSharper disable once InconsistentNaming
public static class IEnumerableExtensions
{
    public static IEnumerable<ExpandoObject> ShapeData<T>(
        this IEnumerable<T> sourceList,
        string? requestedFields
    )
    {
        ArgumentNullException.ThrowIfNull(sourceList);

        List<ExpandoObject> dataShapedObjects = [];
        List<PropertyInfo> propertyInfos = [];

        if (string.IsNullOrWhiteSpace(requestedFields))
        {
            IEnumerable<PropertyInfo> retrievedPropertyInfos = typeof(T).GetProperties(
                BindingFlags.Instance | BindingFlags.Public
            );

            propertyInfos.AddRange(retrievedPropertyInfos);
        }
        else
        {
            IEnumerable<string> fields = requestedFields.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var field in fields)
            {
                PropertyInfo? propertyInfo = typeof(T).GetProperty(
                    field.Trim(),
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
                );

                if (propertyInfo == null)
                {
                    throw new Exception(
                        $"{typeof(T).Name} doesn't have a property with name {fields}"
                    );
                }

                propertyInfos.Add(propertyInfo);
            }
        }

        foreach (var sourceObject in sourceList)
        {
            ExpandoObject dataShapedObject = new ExpandoObject();
            foreach (var propertyInfo in propertyInfos)
            {
                ((IDictionary<string, object?>)dataShapedObject).Add(
                    propertyInfo.Name,
                    propertyInfo.GetValue(sourceObject)
                );
            }

            dataShapedObjects.Add(dataShapedObject);
        }

        return dataShapedObjects;
    }
}
