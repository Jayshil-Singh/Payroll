using FluentValidation;

namespace FijiPayroll.Application.Features.Lookups.Commands.ArchiveLookup;

/// <summary>
/// Validator for <see cref="ArchiveLookupCommand"/>.
/// </summary>
public sealed class ArchiveLookupCommandValidator : AbstractValidator<ArchiveLookupCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public ArchiveLookupCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be a positive integer.");

        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be a positive integer.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Archive reason is required.")
            .MaximumLength(500)
            .WithMessage("Archive reason must not exceed 500 characters.");
    }
}
