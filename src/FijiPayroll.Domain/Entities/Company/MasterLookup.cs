using FijiPayroll.Domain.Entities.Common;
using System;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing polymorphic reference lookup elements (Departments, Banks, positions, etc.).
/// Supports parenting, ordering, and date-range validity.
/// </summary>
public sealed class MasterLookup : ArchivableEntity
{
    private string _category = string.Empty;
    private string _code = string.Empty;
    private string _name = string.Empty;

    private MasterLookup() { }

    /// <summary>Gets the owner company ID.</summary>
    public int CompanyId { get; private set; }

    /// <summary>Gets the category name (e.g., DEPARTMENTS, BANKS).</summary>
    public string Category
    {
        get => _category;
        private set => _category = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the unique code of the lookup item.</summary>
    public string Code
    {
        get => _code;
        private set => _code = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the descriptive name of the lookup item.</summary>
    public string Name
    {
        get => _name;
        private set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the UTC start date of validity.</summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>Gets the UTC expiration date of validity.</summary>
    public DateTime EffectiveTo { get; private set; }

    /// <summary>Gets the parent lookup item ID for hierarchical data relationships.</summary>
    public int? ParentId { get; private set; }

    /// <summary>Gets the priority/sorting value.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets a value indicating whether this lookup item is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Factory method to create a lookup data entry.</summary>
    public static MasterLookup Create(
        int companyId,
        string category,
        string code,
        string name,
        DateTime effectiveFrom,
        DateTime effectiveTo,
        int? parentId = null,
        int displayOrder = 0,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new MasterLookup
        {
            CompanyId = companyId,
            Category = category.ToUpperInvariant(),
            Code = code.ToUpperInvariant(),
            Name = name,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
    }

    /// <summary>Updates properties of the lookup data entry.</summary>
    public void Update(string name, DateTime effectiveFrom, DateTime effectiveTo, int? parentId, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        ParentId = parentId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }
}
