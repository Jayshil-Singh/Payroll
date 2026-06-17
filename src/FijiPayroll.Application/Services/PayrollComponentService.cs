using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollComponents.Commands.CreatePayrollComponent;
using FijiPayroll.Application.Features.PayrollComponents.Commands.DeletePayrollComponent;
using FijiPayroll.Application.Features.PayrollComponents.Commands.DuplicatePayrollComponent;
using FijiPayroll.Application.Features.PayrollComponents.Commands.TogglePayrollComponentActive;
using FijiPayroll.Application.Features.PayrollComponents.Commands.UpdatePayrollComponent;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentById;
using FijiPayroll.Application.Features.PayrollComponents.Queries.GetPayrollComponentList;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using MediatR;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Orchestration service implementing <see cref="IPayrollComponentService"/>.
/// Translates ViewModel service calls into MediatR CQRS commands and queries,
/// managing database transaction boundaries.
/// </summary>
public sealed class PayrollComponentService : IPayrollComponentService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initialises a new instance of the <see cref="PayrollComponentService"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator instance.</param>
    /// <param name="unitOfWork">The Unit of Work instance for transaction boundaries.</param>
    public PayrollComponentService(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Result<PayrollComponentDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollComponentByIdQuery(id);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<PayrollComponentSummaryDto>>> GetListAsync(
        int companyId,
        string? searchTerm = null,
        ComponentType? typeFilter = null,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollComponentListQuery(
            CompanyId: companyId,
            SearchTerm: searchTerm,
            ComponentTypeFilter: typeFilter,
            ActiveOnly: activeOnly,
            PageNumber: pageNumber,
            PageSize: pageSize);

        return await _mediator.Send(query, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<int>> CreateAsync(
        int companyId,
        string componentCode,
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new CreatePayrollComponentCommand(
                companyId,
                componentCode,
                componentName,
                componentType,
                calculationMethod,
                calculationValue,
                formula,
                isTaxable,
                isFnpfApplicable,
                displayOrder,
                description);

            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateAsync(
        int id,
        string componentName,
        ComponentType componentType,
        CalculationMethod calculationMethod,
        decimal? calculationValue,
        string? formula,
        bool isTaxable,
        bool isFnpfApplicable,
        int displayOrder,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new UpdatePayrollComponentCommand(
                id,
                componentName,
                componentType,
                calculationMethod,
                calculationValue,
                formula,
                isTaxable,
                isFnpfApplicable,
                displayOrder,
                description);

            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ToggleActiveAsync(
        int id,
        bool setActive,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new TogglePayrollComponentActiveCommand(id, setActive);
            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> DuplicateAsync(
        int sourceId,
        string newCode,
        string newName,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new DuplicatePayrollComponentCommand(sourceId, newCode, newName);
            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new DeletePayrollComponentCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
