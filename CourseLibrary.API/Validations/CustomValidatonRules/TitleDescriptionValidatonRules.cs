using CourseLibrary.API.Models;
using FluentValidation;

namespace CourseLibrary.API.Validations.CustomValidatonRules;

public static class TitleDescriptionValidatonRules
{
    public static IRuleBuilderOptions<T, CourseForManipulationDto> TitleMustNotMatchDescription<T>(
        this IRuleBuilder<T, CourseForManipulationDto> ruleBuilder
    )
    {
        return ruleBuilder
            .Must(x => x.Title != x.Description)
            .WithMessage("Title mustn't match the description")
            .WithName("Course");
    }
}
