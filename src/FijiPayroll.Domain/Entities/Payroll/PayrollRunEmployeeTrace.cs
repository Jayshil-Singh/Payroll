using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Detailed step-level calculation trace logs for audit debug tracing.
/// Handled as append-only and immutable.
/// </summary>
public sealed class PayrollRunEmployeeTrace : BaseEntity
{
    private string _traceText = string.Empty;

    private PayrollRunEmployeeTrace() { }

    /// <summary>
    /// Foreign key back to the computed employee record.
    /// </summary>
    public int PayrollRunEmployeeId { get; private set; }

    /// <summary>Navigation to parent run employee for tenant-scoped queries.</summary>
    public PayrollRunEmployee PayrollRunEmployee { get; private set; } = null!;

    /// <summary>
    /// Trace payload listing progressive calculation details, tax rules hit, FNPF etc.
    /// </summary>
    public string TraceText
    {
        get => _traceText;
        private set => _traceText = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Factory method to create a trace record.
    /// </summary>
    public static PayrollRunEmployeeTrace Create(int payrollRunEmployeeId, string traceText)
    {
        return new PayrollRunEmployeeTrace
        {
            PayrollRunEmployeeId = payrollRunEmployeeId,
            TraceText = traceText
        };
    }
}
