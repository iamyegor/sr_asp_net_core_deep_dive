using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace CourseLibrary.API.ActionContraints;

public class ValidRequestHeaderMediaType : Attribute, IActionConstraint
{
    private readonly string _header;
    private readonly MediaTypeCollection _allowedMediaTypes;

    public ValidRequestHeaderMediaType(string header, params string[] allowedMediaTypes)
    {
        _header = Guard.Against.Null(header);

        if (allowedMediaTypes.Length == 0)
        {
            throw new Exception("You have to provide at least one allowed media type");
        }

        _allowedMediaTypes = [];
        foreach (var allowedMediaType in allowedMediaTypes)
        {
            _allowedMediaTypes.Add(allowedMediaType);
        }
    }

    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        IHeaderDictionary requestHeaders = context.RouteContext.HttpContext.Request.Headers;
        if (!requestHeaders.ContainsKey(_header))
        {
            return false;
        }

        MediaType requestMediaType = new MediaType(requestHeaders[_header]!);
        foreach (var allowedMediaType in _allowedMediaTypes)
        {
            MediaType parsedAllowedMediaType = new MediaType(allowedMediaType);
            if (requestMediaType.Equals(parsedAllowedMediaType))
            {
                return true;
            }
        }

        return false;
    }
}
