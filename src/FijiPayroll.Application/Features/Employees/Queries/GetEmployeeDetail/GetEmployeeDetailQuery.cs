using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FijiPayroll.Application.Features.Employees.Queries.GetEmployeeDetail;

/// <summary>
/// Query to retrieve detailed profile details for a specific employee.
/// </summary>
public sealed record GetEmployeeDetailQuery(int EmployeeId) : IRequest<Result<EmployeeDetailDto>>;

/// <summary>
/// Detailed employee profile including payment configurations.
/// </summary>
public sealed class EmployeeDetailDto
{
    /// <summary>The employee's unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Formatted employee code.</summary>
    public string EmployeeCode => $"EMP-{Id:D3}";

    /// <summary>The employee's full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Tax Identification Number (TIN).</summary>
    public string Tin { get; set; } = string.Empty;

    /// <summary>FNPF number.</summary>
    public string FnpfNumber { get; set; } = string.Empty;

    /// <summary>Residency status ("Resident" or "NonResident").</summary>
    public string ResidencyStatus { get; set; } = string.Empty;

    /// <summary>Employee's department.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Base salary or rate value.</summary>
    public decimal BaseSalary { get; set; }

    /// <summary>Target pay frequency type.</summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>Is exempt from FNPF contributions.</summary>
    public bool IsFnpfExempt { get; set; }

    /// <summary>Is exempt from PAYE taxes.</summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>Active status flag.</summary>
    public bool IsActive { get; set; }

    /// <summary>Employment type enum string.</summary>
    public string EmploymentType { get; set; } = string.Empty;

    /// <summary>Employee's branch location.</summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>Employee's position title.</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Employee's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Employee's cost centre assignment.</summary>
    public string CostCentre { get; set; } = string.Empty;

    /// <summary>Data quality score (0-100).</summary>
    public double DataQualityScore { get; set; }

    /// <summary>Assigned payment configurations.</summary>
    public IReadOnlyList<EmployeePaymentMethodDto> PaymentMethods { get; set; } = System.Array.Empty<EmployeePaymentMethodDto>();
}

/// <summary>
/// Detail DTO representing a payment method allocation.
/// </summary>
public sealed class EmployeePaymentMethodDto
{
    /// <summary>The payment method type string.</summary>
    public string MethodType { get; set; } = string.Empty;

    /// <summary>The bank name.</summary>
    public string? BankName { get; set; }

    /// <summary>The bank account number.</summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>The bank sort/routing code.</summary>
    public string? BankSortCode { get; set; }

    /// <summary>Mobile wallet phone number.</summary>
    public string? MobileNumber { get; set; }

    /// <summary>Allocation percentage.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Primary payment method selector.</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Handler for <see cref="GetEmployeeDetailQuery"/>.
/// </summary>
public sealed class GetEmployeeDetailQueryHandler : IRequestHandler<GetEmployeeDetailQuery, Result<EmployeeDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetEmployeeDetailQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<EmployeeDetailDto>> Handle(GetEmployeeDetailQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(PermissionConstants.EmployeesView))
        {
            return Result<EmployeeDetailDto>.Failure("Forbidden: You do not have permission to view employee details.");
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<EmployeeDetailDto>.Failure($"Employee with ID {request.EmployeeId} was not found.");
        }

        if (!_currentUser.HasCompanyAccess(employee.CompanyId))
        {
            return Result<EmployeeDetailDto>.Failure("Forbidden: You do not have access to this company's records.");
        }

        var dto = new EmployeeDetailDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Tin = employee.Tin,
            FnpfNumber = employee.FnpfNumber,
            ResidencyStatus = employee.ResidencyStatus,
            Department = employee.Department,
            BaseSalary = employee.BaseSalary,
            Frequency = employee.Frequency.ToString(),
            IsFnpfExempt = employee.IsFnpfExempt,
            IsTaxExempt = employee.IsTaxExempt,
            IsActive = employee.IsActive,
            EmploymentType = employee.EmploymentType.ToString(),
            Branch = employee.Branch,
            Position = employee.Position,
            Email = employee.Email,
            CostCentre = employee.CostCentre,
            DataQualityScore = employee.DataQualityScore,
            PaymentMethods = employee.PaymentMethods.Select(pm => new EmployeePaymentMethodDto
            {
                MethodType = pm.MethodType.ToString(),
                BankName = pm.BankName,
                BankAccountNumber = pm.BankAccountNumber,
                BankSortCode = pm.BankSortCode,
                MobileNumber = pm.MobileNumber,
                Percentage = pm.Percentage,
                IsPrimary = pm.IsPrimary
            }).ToList().AsReadOnly()
        };

        return Result<EmployeeDetailDto>.Success(dto);
    }
}
