using FluentValidation;

namespace FijiPayroll.Application.Features.CompanySetup.Queries.ValidateCompanyWizard;

/// <summary>
/// Validator for <see cref="ValidateCompanyWizardQuery"/>.
/// </summary>
public sealed class ValidateCompanyWizardQueryValidator : AbstractValidator<ValidateCompanyWizardQuery>
{
    /// <summary>
    /// Initialises validation rules.
    /// </summary>
    public ValidateCompanyWizardQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");
    }
}
