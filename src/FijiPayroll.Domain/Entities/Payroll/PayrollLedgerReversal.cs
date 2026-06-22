using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Payroll;

/// <summary>
/// Tracing link showing the relationship between an original ledger entry and its correction reversal ledger entry.
/// </summary>
public sealed class PayrollLedgerReversal : BaseEntity
{
    public int CompanyId { get; private set; }
    public int OriginalLedgerId { get; private set; }
    public int ReversalLedgerId { get; private set; }
    public DateTime ReversalDate { get; private set; }
    public string ReversalReason { get; private set; } = string.Empty;
    public string User { get; private set; } = string.Empty;

    private PayrollLedgerReversal() { } // For EF Core

    public static PayrollLedgerReversal Create(
        int companyId,
        int originalLedgerId,
        int reversalLedgerId,
        string reversalReason,
        string user)
    {
        if (companyId <= 0) throw new ArgumentOutOfRangeException(nameof(companyId));
        if (originalLedgerId <= 0) throw new ArgumentOutOfRangeException(nameof(originalLedgerId));
        if (reversalLedgerId <= 0) throw new ArgumentOutOfRangeException(nameof(reversalLedgerId));
        if (string.IsNullOrWhiteSpace(reversalReason)) throw new ArgumentException("Reversal reason is required.", nameof(reversalReason));

        return new PayrollLedgerReversal
        {
            CompanyId = companyId,
            OriginalLedgerId = originalLedgerId,
            ReversalLedgerId = reversalLedgerId,
            ReversalDate = DateTime.UtcNow,
            ReversalReason = reversalReason,
            User = user ?? "System"
        };
    }
}
