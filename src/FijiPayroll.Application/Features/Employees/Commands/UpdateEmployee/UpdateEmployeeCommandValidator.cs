using FluentValidation;
using System.Linq;

namespace FijiPayroll.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// Validator for <see cref="UpdateEmployeeCommand"/>.
/// </summary>
public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Employee Id must be a positive integer.");

        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(200)
            .WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.ResidencyStatus)
            .NotEmpty()
            .WithMessage("Residency status is required.")
            .Must(x => x == "Resident" || x == "NonResident")
            .WithMessage("Residency status must be either 'Resident' or 'NonResident'.");

        RuleFor(x => x.Tin)
            .NotEmpty()
            .WithMessage("TIN is required.")
            .Matches(@"^\d{9,10}$")
            .WithMessage("TIN must be exactly 9 or 10 digits.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Frequency)
            .IsInEnum()
            .WithMessage("A valid payroll frequency must be selected.");

        RuleFor(x => x.EmploymentType)
            .IsInEnum()
            .WithMessage("A valid employment type must be selected.");

        RuleFor(x => x.Branch)
            .MaximumLength(100)
            .WithMessage("Branch must not exceed 100 characters.");

        RuleFor(x => x.Position)
            .MaximumLength(100)
            .WithMessage("Position must not exceed 100 characters.");

        RuleFor(x => x.BaseSalary)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Base salary must be greater than or equal to zero.");

        // FNPF Number is required when not exempt
        RuleFor(x => x.FnpfNumber)
            .NotEmpty()
            .WithMessage("FNPF number is required unless employee is exempt.")
            .Matches(@"^[a-zA-Z0-9-\s]+$")
            .WithMessage("FNPF number may only contain alphanumeric characters, hyphens, and spaces.")
            .When(x => !x.IsFnpfExempt);

        // Payment Methods validations
        RuleFor(x => x.PaymentMethods)
            .NotEmpty()
            .WithMessage("At least one payment method is required.");

        RuleFor(x => x.PaymentMethods)
            .Must(methods => methods != null && methods.Count(pm => pm.IsPrimary) == 1)
            .WithMessage("Exactly one primary payment method must be configured.")
            .When(x => x.PaymentMethods != null && x.PaymentMethods.Count > 0);

        RuleFor(x => x.PaymentMethods)
            .Must(methods => methods != null && methods.Sum(pm => pm.Percentage) == 100m)
            .WithMessage("Total allocation percentage across all payment methods must equal exactly 100%.")
            .When(x => x.PaymentMethods != null && x.PaymentMethods.Count > 0);
    }
}
