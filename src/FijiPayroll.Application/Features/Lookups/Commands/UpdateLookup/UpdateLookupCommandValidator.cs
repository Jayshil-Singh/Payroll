using FluentValidation;

namespace FijiPayroll.Application.Features.Lookups.Commands.UpdateLookup;

/// <summary>
/// Validator for <see cref="UpdateLookupCommand"/>.
/// </summary>
public sealed class UpdateLookupCommandValidator : AbstractValidator<UpdateLookupCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public UpdateLookupCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be a positive integer.");

        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

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
