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
    private string _branch = string.Empty;
    private string _position = string.Empty;
    private string _email = string.Empty;
    private readonly List<EmployeePaymentMethod> _paymentMethods = new();

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

    /// <summary>Gets the employment type.</summary>
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>Gets the employee's branch.</summary>
    public string Branch
    {
        get => _branch;
        private set => _branch = value ?? string.Empty;
    }

    /// <summary>Gets the employee's position.</summary>
    public string Position
    {
        get => _position;
        private set => _position = value ?? string.Empty;
    }

    /// <summary>Gets the employee's email address.</summary>
    public string Email
    {
        get => _email;
        private set => _email = value ?? string.Empty;
    }

    /// <summary>Gets the data quality score for the employee record.</summary>
    public double DataQualityScore { get; private set; }

    /// <summary>Gets the payment methods configured for this employee.</summary>
    public IReadOnlyCollection<EmployeePaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    /// <summary>Adds a payment method to the employee.</summary>
    public void AddPaymentMethod(EmployeePaymentMethod paymentMethod)
    {
        if (paymentMethod == null) throw new ArgumentNullException(nameof(paymentMethod));
        _paymentMethods.Add(paymentMethod);
        RecalculateDataQualityScore();
    }

    /// <summary>Clears all payment methods.</summary>
    public void ClearPaymentMethods()
    {
        _paymentMethods.Clear();
        RecalculateDataQualityScore();
    }

    /// <summary>Recalculates the data quality score for the employee record.</summary>
    public void RecalculateDataQualityScore()
    {
        double score = 0.0;
        if (!string.IsNullOrWhiteSpace(FullName)) score += 15.0;
        if (!string.IsNullOrWhiteSpace(Tin)) score += 15.0;
        if (IsFnpfExempt || !string.IsNullOrWhiteSpace(FnpfNumber)) score += 15.0;
        if (!string.IsNullOrWhiteSpace(Department) && !string.IsNullOrWhiteSpace(Branch) && !string.IsNullOrWhiteSpace(Position)) score += 15.0;
        if (_paymentMethods.Any(pm => pm.IsPrimary)) score += 20.0;
        if (!string.IsNullOrWhiteSpace(Email)) score += 20.0;
        DataQualityScore = score;
    }

    /// <summary>
    /// Factory method to create an Employee with optional new fields.
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
        bool isActive,
        EmploymentType employmentType = EmploymentType.Permanent,
        string branch = "",
        string position = "",
        string email = "")
    {
        var employee = new Employee
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
            IsActive = isActive,
            EmploymentType = Guard.AgainstInvalidEnum(employmentType),
            Branch = branch,
            Position = position,
            Email = email
        };
        employee.RecalculateDataQualityScore();
        return employee;
    }

    /// <summary>Updates employee properties.</summary>
    public void Update(
        string fullName,
        string tin,
        string fnpfNumber,
        string residencyStatus,
        string department,
        decimal baseSalary,
        PayrollFrequency frequency,
        bool isFnpfExempt,
        bool isTaxExempt,
        bool isActive,
        EmploymentType employmentType,
        string branch,
        string position,
        string email)
    {
        FullName = fullName;
        Tin = tin;
        FnpfNumber = fnpfNumber;
        ResidencyStatus = residencyStatus;
        Department = department;
        BaseSalary = baseSalary;
        Frequency = Guard.AgainstInvalidEnum(frequency);
        IsFnpfExempt = isFnpfExempt;
        IsTaxExempt = isTaxExempt;
        IsActive = isActive;
        EmploymentType = Guard.AgainstInvalidEnum(employmentType);
        Branch = branch;
        Position = position;
        Email = email;

        RecalculateDataQualityScore();
    }
}
