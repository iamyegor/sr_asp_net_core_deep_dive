using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;

namespace CourseLibrary.API.Validations;

public class CustomResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IActionResult CreateActionResult(
        ActionExecutingContext context,
        ValidationProblemDetails? validationProblemDetails
    )
    {
        if (validationProblemDetails == null)
        {
            return new OkResult();
        }

        var options = context.HttpContext.RequestServices.GetRequiredService<
            IOptions<ApiBehaviorOptions>
        >();

        return (ActionResult)options.Value.InvalidModelStateResponseFactory(context);
    }
}
