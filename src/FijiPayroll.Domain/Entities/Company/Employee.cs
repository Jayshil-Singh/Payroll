using FijiPayroll.Domain.Entities.Common;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Guards;

namespace FijiPayroll.Domain.Entities.Company;

/// <summary>
/// Domain entity representing an employee and their master salary/statutory settings.
/// </summary>
public sealed class Employee : SoftDeleteEntity
{
    private string _fullName = string.Empty;
    private string _tin = string.Empty;
    private string _fnpfNumber = string.Empty;
    private string _residencyStatus = string.Empty;
    private string _department = string.Empty;

    private Employee() { }

    /// <summary>
    /// Foreign key to Company.
    /// </summary>
    public int CompanyId { get; private set; }

    /// <summary>
    /// Employee's full name.
    /// </summary>
    public string FullName
    {
        get => _fullName;
        private set => _fullName = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Tax Identification Number (TIN) from FRCS.
    /// </summary>
    public string Tin
    {
        get => _tin;
        private set => _tin = value ?? string.Empty;
    }

    /// <summary>
    /// FNPF registration number.
    /// </summary>
    public string FnpfNumber
    {
        get => _fnpfNumber;
        private set => _fnpfNumber = value ?? string.Empty;
    }

    /// <summary>
    /// Residency status ("Resident" or "NonResident").
    /// </summary>
    public string ResidencyStatus
    {
        get => _residencyStatus;
        private set => _residencyStatus = Guard.AgainstNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Employee's department.
    /// </summary>
    public string Department
    {
        get => _department;
        private set => _department = value ?? string.Empty;
    }

    /// <summary>
    /// Base periodic salary or rate value.
    /// </summary>
    public decimal BaseSalary { get; private set; }

    /// <summary>
    /// Target pay frequency.
    /// </summary>
    public PayrollFrequency Frequency { get; private set; }

    /// <summary>
    /// True if employee is exempt from FNPF contributions.
    /// </summary>
    public bool IsFnpfExempt { get; private set; }

    /// <summary>
    /// True if employee is exempt from PAYE taxes.
    /// </summary>
    public bool IsTaxExempt { get; private set; }

    /// <summary>
    /// True if employee is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Factory method to create an Employee.
    /// </summary>
    public static Employee Create(
        int companyId,
        string fullName,
        string tin,
        string fnpfNumber,
        string residencyStatus,
        string department,
        decimal baseSalary,
        PayrollFrequency frequency,
        bool isFnpfExempt,
        bool isTaxExempt,
        bool isActive)
    {
        return new Employee
        {
            CompanyId = companyId,
            FullName = fullName,
            Tin = tin,
            FnpfNumber = fnpfNumber,
            ResidencyStatus = residencyStatus,
            Department = department,
            BaseSalary = baseSalary,
            Frequency = Guard.AgainstInvalidEnum(frequency),
            IsFnpfExempt = isFnpfExempt,
            IsTaxExempt = isTaxExempt,
            IsActive = isActive
        };
    }
}
