using System;

namespace FijiPayroll.Domain.Rules.PayrollRules;

/// <summary>
/// Pure stateless engine for FNPF deductions and contributions calculations.
/// </summary>
public static class PayrollDeductionEngine
{
    /// <summary>
    /// Calculates the FNPF employee portion rounded half up.
    /// </summary>
    public static decimal CalculateEmployeeFnpf(
        decimal fnpfApplicableGross,
        bool isExempt,
        decimal employeeRate)
    {
        if (isExempt || fnpfApplicableGross <= 0 || employeeRate <= 0)
        {
            return 0m;
        }

        decimal contribution = fnpfApplicableGross * employeeRate;
        return Math.Round(contribution, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates the FNPF employer portion rounded half up.
    /// </summary>
    public static decimal CalculateEmployerFnpf(
        decimal fnpfApplicableGross,
        bool isExempt,
        decimal employerRate)
    {
        if (isExempt || fnpfApplicableGross <= 0 || employerRate <= 0)
        {
            return 0m;
        }

        decimal contribution = fnpfApplicableGross * employerRate;
        return Math.Round(contribution, 2, MidpointRounding.AwayFromZero);
    }
}
