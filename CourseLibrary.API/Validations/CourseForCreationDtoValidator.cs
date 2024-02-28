using CourseLibrary.API.Models;
using CourseLibrary.API.Validations.CustomValidatonRules;
using FluentValidation;

namespace CourseLibrary.API.Validations;

public class CourseForCreationDtoValidator : AbstractValidator<CourseForCreationDto>
{
    public CourseForCreationDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(1500);
        RuleFor(course => course).TitleMustNotMatchDescription();
    }
}