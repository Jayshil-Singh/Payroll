using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Shared.Constants;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Leave.Queries.GetLeaveTypes;

/// <summary>
/// Query to retrieve all active leave types for a company.
/// </summary>
public sealed record GetLeaveTypesQuery(int CompanyId) : IRequest<Result<IReadOnlyList<LeaveTypeDto>>>;

/// <summary>
/// DTO representing a leave type.
/// </summary>
public sealed class LeaveTypeDto
{
    public int Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal EntitlementDays { get; set; }
    public bool IsPaid { get; set; }
    public bool ApplyLeaveLoading { get; set; }
    public bool RequiresMedicalCertificate { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Handler for <see cref="GetLeaveTypesQuery"/>.
/// </summary>
public sealed class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, Result<IReadOnlyList<LeaveTypeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes dependencies.</summary>
    public GetLeaveTypesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveTypeDto>>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        // 1. Permission check
        if (!_currentUser.HasPermission(PermissionConstants.LeaveView))
        {
            return Result<IReadOnlyList<LeaveTypeDto>>.Failure("Forbidden: You do not have permission to view leave types.");
        }

        // 2. Company access check
        if (!_currentUser.HasCompanyAccess(request.CompanyId))
        {
            return Result<IReadOnlyList<LeaveTypeDto>>.Failure("Forbidden: You do not have access to this company.");
        }

        // 3. Load active leave types
        var types = await _unitOfWork.Leave.GetTypesByCompanyAsync(request.CompanyId, cancellationToken);
        var activeTypes = types.Where(t => t.IsActive).ToList();

        // 4. Map to DTOs
        var dtos = activeTypes.Select(t => new LeaveTypeDto
        {
            Id = t.Id,
            TypeName = t.TypeName,
            Category = t.Category.ToString(),
            EntitlementDays = t.EntitlementDays,
            IsPaid = t.IsPaid,
            ApplyLeaveLoading = t.ApplyLeaveLoading,
            RequiresMedicalCertificate = t.RequiresMedicalCertificate,
            Description = t.Description
        }).ToList();

        return Result<IReadOnlyList<LeaveTypeDto>>.Success(dtos.AsReadOnly());
    }
}
