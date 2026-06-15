using FluentValidation;

namespace FijiPayroll.Application.Features.Lookups.Commands.CreateLookup;

/// <summary>
/// Validator for <see cref="CreateLookupCommand"/>.
/// </summary>
public sealed class CreateLookupCommandValidator : AbstractValidator<CreateLookupCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateLookupCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.")
            .MaximumLength(50)
            .WithMessage("Category must not exceed 50 characters.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Code is required.")
            .MaximumLength(50)
            .WithMessage("Code must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Code may only contain letters, digits, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Display Order must be zero or greater.");
    }
}
