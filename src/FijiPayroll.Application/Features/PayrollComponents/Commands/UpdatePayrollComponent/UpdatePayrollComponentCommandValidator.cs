using FluentValidation;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.UpdatePayrollComponent;

/// <summary>
/// FluentValidation validator for <see cref="UpdatePayrollComponentCommand"/>.
/// </summary>
public sealed class UpdatePayrollComponentCommandValidator
    : AbstractValidator<UpdatePayrollComponentCommand>
{
    /// <summary>Initialises all validation rules.</summary>
    public UpdatePayrollComponentCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Component ID must be a positive integer.");

        RuleFor(x => x.ComponentName)
            .NotEmpty()
            .WithMessage("Component Name is required.")
            .MaximumLength(200)
            .WithMessage("Component Name must not exceed 200 characters.");

        RuleFor(x => x.ComponentType)
            .IsInEnum()
            .WithMessage("A valid Component Type must be selected.");

        RuleFor(x => x.CalculationMethod)
            .IsInEnum()
            .WithMessage("A valid Calculation Method must be selected.");

        When(x => x.CalculationMethod is CalculationMethod.Fixed
                                      or CalculationMethod.Percentage, () =>
        {
            RuleFor(x => x.CalculationValue)
                .NotNull()
                .WithMessage("Calculation Value is required for Fixed and Percentage methods.")
                .GreaterThan(0m)
                .WithMessage("Calculation Value must be greater than zero.");
        });

        When(x => x.CalculationMethod == CalculationMethod.Formula, () =>
        {
            RuleFor(x => x.Formula)
                .NotEmpty()
                .WithMessage("A Formula expression is required for Formula components.")
                .MaximumLength(4000)
                .WithMessage("Formula must not exceed 4000 characters.");
        });

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Display Order must be zero or greater.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
