using FluentValidation;
using System;

namespace FijiPayroll.Application.Features.CompanySetup.Commands.CreateCompanyWizard;

/// <summary>
/// Validator for <see cref="CreateCompanyWizardCommand"/>.
/// </summary>
public sealed class CreateCompanyWizardCommandValidator : AbstractValidator<CreateCompanyWizardCommand>
{
    /// <summary>
    /// Initialises validation rules.
    /// </summary>
    public CreateCompanyWizardCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

        RuleFor(x => x.ExecutionId)
            .NotEmpty()
            .WithMessage("ExecutionId must not be empty Guid.")
            .NotEqual(Guid.Empty)
            .WithMessage("ExecutionId cannot be empty Guid.");
    }
}
