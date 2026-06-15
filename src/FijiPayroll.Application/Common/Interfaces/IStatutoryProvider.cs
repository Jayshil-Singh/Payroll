using System;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Contract for versioned, time-bound statutory compliance rules (PAYE tax, FNPF).
/// </summary>
public interface IStatutoryProvider
{
    /// <summary>Unique name/version code of the rule engine.</summary>
    string RuleVersion { get; }

    /// <summary>The start of the calculation rule validity period.</summary>
    DateTime EffectiveFrom { get; }

    /// <summary>The end of the calculation rule validity period.</summary>
    DateTime EffectiveTo { get; }

    /// <summary>Calculates the Pay-As-You-Earn (PAYE) withholding tax according to FRCS rules.</summary>
    decimal CalculatePaye(decimal taxableIncome, string residencyStatus);

    /// <summary>Calculates the Fiji National Provident Fund (FNPF) contribution.</summary>
    decimal CalculateFnpf(decimal grossPay, bool isExempt);
}
