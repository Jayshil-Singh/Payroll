namespace FijiPayroll.Domain.Enumerations;

/// <summary>
/// Categories of leave available to employees as defined in PayrollRules.md §10.
/// Maps directly to <c>company.LeaveTypes.Category</c> column.
/// </summary>
public enum LeaveCategory
{
    /// <summary>Annual leave entitlement (minimum 10 working days/year).</summary>
    AnnualLeave,

    /// <summary>Sick leave entitlement (10 days/year).</summary>
    SickLeave,

    /// <summary>Maternity leave (84 days paid for primary carer).</summary>
    MaternityLeave,

    /// <summary>Paternity leave (5 days paid).</summary>
    PaternityLeave,

    /// <summary>Bereavement leave (3 days paid for immediate family).</summary>
    BereavementLeave,

    /// <summary>Jury duty (full pay).</summary>
    JuryDuty,

    /// <summary>Unpaid leave — no pay during period.</summary>
    UnpaidLeave,

    /// <summary>Any other leave type not covered by standard categories.</summary>
    Other
}
