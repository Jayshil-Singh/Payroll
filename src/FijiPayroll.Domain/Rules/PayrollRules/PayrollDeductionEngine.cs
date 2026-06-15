using System;

namespace FijiPayroll.Domain.Rules.PayrollRules;

/// <summary>
/// Pure stateless engine for FNPF deductions and contributions calculations.
/// </summary>
public static class PayrollDeductionEngine
{
    private const decimal FnpfEmployeeRate = 0.08m;
    private const decimal FnpfEmployerRate = 0.10m;

    /// <summary>
    /// Calculates the FNPF employee portion (8% of applicable gross) rounded half up.
    /// </summary>
    public static decimal CalculateEmployeeFnpf(decimal fnpfApplicableGross, bool isExempt)
    {
        if (isExempt || fnpfApplicableGross <= 0)
        {
            return 0m;
        }

        decimal contribution = fnpfApplicableGross * FnpfEmployeeRate;
        return Math.Round(contribution, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates the FNPF employer portion (10% of applicable gross) rounded half up.
    /// </summary>
    public static decimal CalculateEmployerFnpf(decimal fnpfApplicableGross, bool isExempt)
    {
        if (isExempt || fnpfApplicableGross <= 0)
        {
            return 0m;
        }

        decimal contribution = fnpfApplicableGross * FnpfEmployerRate;
        return Math.Round(contribution, 2, MidpointRounding.AwayFromZero);
    }
}
