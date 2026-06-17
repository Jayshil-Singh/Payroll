using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Guards;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Represents a configuration-driven tax bracket for PAYE progressive tax calculations.
/// Ensures historical reproducibility and audit reconstruction.
/// </summary>
public sealed class TaxBracket : AuditableEntity
{
    private string _taxVersion = string.Empty;
    private string _residencyStatus = string.Empty;
    private decimal _lowerLimit;
    private decimal _upperLimit;
    private decimal _taxRate;
    private decimal _fixedTaxAmount;

    private TaxBracket() { }

    /// <summary>
    /// Version identifier of the tax ruleset (e.g., "2025-2026").
    /// </summary>
    public string TaxVersion
    {
        get => _taxVersion;
        private set => _taxVersion = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Residency status of the employee (e.g., "Resident", "NonResident").
    /// </summary>
    public string ResidencyStatus
    {
        get => _residencyStatus;
        private set => _residencyStatus = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Pay frequency of the pay group.
    /// </summary>
    public PayrollFrequencyType Frequency { get; private set; }

    /// <summary>
    /// Lower boundary of taxable income for this bracket.
    /// </summary>
    public decimal LowerLimit
    {
        get => _lowerLimit;
        private set => _lowerLimit = Guard.AgainstNegative(value);
    }

    /// <summary>
    /// Upper boundary of taxable income for this bracket.
    /// </summary>
    public decimal UpperLimit
    {
        get => _upperLimit;
        private set => _upperLimit = Guard.AgainstNegative(value);
    }

    /// <summary>
    /// Marginal tax rate for income falling within this bracket (expressed as decimal, e.g. 0.18).
    /// </summary>
    public decimal TaxRate
    {
        get => _taxRate;
        private set => _taxRate = Guard.AgainstNegative(value);
    }

    /// <summary>
    /// Fixed tax amount accumulated from previous tax brackets.
    /// </summary>
    public decimal FixedTaxAmount
    {
        get => _fixedTaxAmount;
        private set => _fixedTaxAmount = Guard.AgainstNegative(value);
    }

    /// <summary>
    /// Active flag.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Start date of this tax rule version validity.
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Factory method to build a new TaxBracket.
    /// </summary>
    public static TaxBracket Create(
        string taxVersion,
        string residencyStatus,
        PayrollFrequencyType frequency,
        decimal lowerLimit,
        decimal upperLimit,
        decimal taxRate,
        decimal fixedTaxAmount,
        bool isActive,
        DateTime effectiveDate)
    {
        return new TaxBracket
        {
            TaxVersion = taxVersion,
            ResidencyStatus = residencyStatus,
            Frequency = Guard.AgainstInvalidEnum(frequency),
            LowerLimit = lowerLimit,
            UpperLimit = upperLimit,
            TaxRate = taxRate,
            FixedTaxAmount = fixedTaxAmount,
            IsActive = isActive,
            EffectiveDate = effectiveDate
        };
    }
}
