using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Exceptions;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Leave;

/// <summary>
/// Represents a company-specific leave type configuration (e.g., "Annual Leave", "Sick Leave").
/// Maps to <c>leave.LeaveTypes</c> in the database.
/// Inherits soft-delete capability from <see cref="SoftDeleteEntity"/>.
/// </summary>
public sealed class LeaveType : SoftDeleteEntity
{
    private string _typeName = string.Empty;
    private string? _description;

    private LeaveType() { }

    /// <summary>Gets the company this leave type belongs to (multi-tenant isolation).</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the display name of the leave type (e.g., "Annual Leave").</summary>
    public string TypeName
    {
        get => _typeName;
        private set => _typeName = Guard.AgainstNullOrWhiteSpace(value, nameof(TypeName));
    }

    /// <summary>Gets the leave category classification.</summary>
    public LeaveCategory Category { get; private set; }

    /// <summary>
    /// Gets the annual entitlement in days.
    /// For example: Annual Leave = 10, Sick Leave = 10, Maternity = 84 calendar days.
    /// </summary>
    public decimal EntitlementDays { get; private set; }

    /// <summary>Gets whether leave pay is applicable (false for unpaid leave).</summary>
    public bool IsPaid { get; private set; }

    /// <summary>
    /// Gets whether 25% leave loading applies when this leave type is taken.
    /// Per PayrollRules.md §10.1, applies only to Annual Leave if configured.
    /// </summary>
    public bool ApplyLeaveLoading { get; private set; }

    /// <summary>
    /// Gets the maximum number of days that can be carried over to the next period.
    /// Null means unlimited carry-over.
    /// </summary>
    public decimal? MaxCarryOverDays { get; private set; }

    /// <summary>Gets whether a medical certificate is required (e.g., Sick Leave > 2 days).</summary>
    public bool RequiresMedicalCertificate { get; private set; }

    /// <summary>Gets the number of consecutive days after which a medical certificate is mandatory.</summary>
    public int? MedicalCertificateAfterDays { get; private set; }

    /// <summary>Gets whether this leave type is active and available for requests.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the optional description providing additional detail.</summary>
    public string? Description
    {
        get => _description;
        private set => _description = value;
    }

    /// <summary>
    /// Factory method to create a new <see cref="LeaveType"/> instance.
    /// Validates all inputs before creating.
    /// </summary>
    /// <param name="companyId">Company owning this leave type.</param>
    /// <param name="typeName">Display name (must not be empty).</param>
    /// <param name="category">Leave category classification.</param>
    /// <param name="entitlementDays">Annual entitlement in working days.</param>
    /// <param name="isPaid">Whether the leave is paid.</param>
    /// <param name="applyLeaveLoading">Whether 25% leave loading applies.</param>
    /// <param name="maxCarryOverDays">Optional cap on days carried to next period.</param>
    /// <param name="requiresMedicalCertificate">Whether a medical certificate is needed.</param>
    /// <param name="medicalCertificateAfterDays">Days threshold before certificate required.</param>
    /// <param name="description">Optional descriptive text.</param>
    /// <returns>A new, valid <see cref="LeaveType"/>.</returns>
    /// <exception cref="DomainException">Thrown when business rules are violated.</exception>
    public static LeaveType Create(
        int companyId,
        string typeName,
        LeaveCategory category,
        decimal entitlementDays,
        bool isPaid = true,
        bool applyLeaveLoading = false,
        decimal? maxCarryOverDays = null,
        bool requiresMedicalCertificate = false,
        int? medicalCertificateAfterDays = null,
        string? description = null)
    {
        if (companyId <= 0)
            throw new DomainException("CompanyId must be a positive integer.");

        if (entitlementDays < 0)
            throw new DomainException("EntitlementDays must not be negative.");

        if (maxCarryOverDays.HasValue && maxCarryOverDays.Value < 0)
            throw new DomainException("MaxCarryOverDays must not be negative.");

        if (medicalCertificateAfterDays.HasValue && medicalCertificateAfterDays.Value < 1)
            throw new DomainException("MedicalCertificateAfterDays must be at least 1.");

        // Unpaid leave must not apply loading
        if (!isPaid && applyLeaveLoading)
            throw new DomainException("Leave loading cannot apply to unpaid leave types.");

        return new LeaveType
        {
            CompanyId = companyId,
            TypeName = typeName,
            Category = category,
            EntitlementDays = entitlementDays,
            IsPaid = isPaid,
            ApplyLeaveLoading = applyLeaveLoading,
            MaxCarryOverDays = maxCarryOverDays,
            RequiresMedicalCertificate = requiresMedicalCertificate,
            MedicalCertificateAfterDays = medicalCertificateAfterDays,
            Description = description,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the configuration of this leave type.
    /// </summary>
    /// <param name="typeName">New display name.</param>
    /// <param name="entitlementDays">New annual entitlement days.</param>
    /// <param name="isPaid">New paid status.</param>
    /// <param name="applyLeaveLoading">New leave loading flag.</param>
    /// <param name="maxCarryOverDays">New carry-over cap (null = unlimited).</param>
    /// <param name="requiresMedicalCertificate">New medical cert requirement.</param>
    /// <param name="medicalCertificateAfterDays">New medical cert threshold.</param>
    /// <param name="description">New optional description.</param>
    /// <exception cref="DomainException">Thrown when rules are violated or entity is deleted.</exception>
    public void Update(
        string typeName,
        decimal entitlementDays,
        bool isPaid,
        bool applyLeaveLoading,
        decimal? maxCarryOverDays,
        bool requiresMedicalCertificate,
        int? medicalCertificateAfterDays,
        string? description)
    {
        if (IsDeleted)
            throw new DomainException("Cannot update a deleted leave type.");

        if (entitlementDays < 0)
            throw new DomainException("EntitlementDays must not be negative.");

        if (!isPaid && applyLeaveLoading)
            throw new DomainException("Leave loading cannot apply to unpaid leave types.");

        TypeName = typeName;
        EntitlementDays = entitlementDays;
        IsPaid = isPaid;
        ApplyLeaveLoading = applyLeaveLoading;
        MaxCarryOverDays = maxCarryOverDays;
        RequiresMedicalCertificate = requiresMedicalCertificate;
        MedicalCertificateAfterDays = medicalCertificateAfterDays;
        Description = description;
    }

    /// <summary>Deactivates this leave type (no new requests allowed).</summary>
    /// <exception cref="DomainException">Thrown if already inactive or deleted.</exception>
    public void Deactivate()
    {
        if (IsDeleted)
            throw new DomainException("Cannot deactivate a deleted leave type.");

        if (!IsActive)
            throw new DomainException("Leave type is already inactive.");

        IsActive = false;
    }

    /// <summary>Reactivates a previously deactivated leave type.</summary>
    /// <exception cref="DomainException">Thrown if deleted.</exception>
    public void Activate()
    {
        if (IsDeleted)
            throw new DomainException("Cannot activate a deleted leave type.");

        IsActive = true;
    }
}
