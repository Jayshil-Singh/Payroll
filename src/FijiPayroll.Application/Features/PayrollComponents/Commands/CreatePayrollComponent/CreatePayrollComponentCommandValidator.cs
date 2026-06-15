using FluentValidation;
using FijiPayroll.Domain.Enumerations;

namespace FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;

/// <summary>
/// FluentValidation validator for <see cref="CreatePayrollComponentCommand"/>.
/// Enforces UI-level rules as defined in Phase05-Configuration.md §9.2 and
/// domain-level rules from Database.md §5.6.
/// </summary>
public sealed class CreatePayrollComponentCommandValidator
    : AbstractValidator<CreatePayrollComponentCommand>
{
    /// <summary>Initialises all validation rules.</summary>
    public CreatePayrollComponentCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

        RuleFor(x => x.ComponentCode)
            .NotEmpty()
            .WithMessage("Component Code is required.")
            .MaximumLength(20)
            .WithMessage("Component Code must not exceed 20 characters.")
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("Component Code may only contain uppercase letters, digits, underscores, and hyphens.");

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

        // CalculationValue is required and must be positive for Fixed and Percentage methods
        When(x => x.CalculationMethod is CalculationMethod.Fixed
                                      or CalculationMethod.Percentage, () =>
        {
            RuleFor(x => x.CalculationValue)
                .NotNull()
                .WithMessage("Calculation Value is required for Fixed and Percentage methods.")
                .GreaterThan(0m)
                .WithMessage("Calculation Value must be greater than zero.");
        });

        // Formula is required (and non-empty) for Formula method
        When(x => x.CalculationMethod == CalculationMethod.Formula, () =>
        {
            RuleFor(x => x.Formula)
                .NotEmpty()
                .WithMessage("A Formula expression is required for Formula components.")
                .MaximumLength(4000)
                .WithMessage("Formula must not exceed 4000 characters.");
        });

        // CalculationValue and Formula should be null for Manual method
        When(x => x.CalculationMethod == CalculationMethod.Manual, () =>
        {
            RuleFor(x => x.CalculationValue)
                .Null()
                .WithMessage("Calculation Value must be empty for Manual components.");
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
