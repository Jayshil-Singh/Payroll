using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ApprovePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ProcessBatchPayroll;
using FijiPayroll.Application.Features.PayrollRuns.Commands.CreatePayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.PostPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.ResetPayrollRun;
using FijiPayroll.Application.Features.PayrollRuns.Commands.AdminOverrideLock;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunById;
using FijiPayroll.Application.Features.PayrollRuns.Queries.GetPayrollRunList;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service coordinating transactions and forwarding presentations calls to CQRS.
/// </summary>
public sealed class PayrollRunService : IPayrollRunService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public PayrollRunService(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<PayrollRunDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollRunByIdQuery(id);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<PayrollRunSummaryDto>>> GetListAsync(
        int companyId,
        PayrollFrequencyType? frequencyFilter = null,
        PayrollRunStatus? statusFilter = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPayrollRunListQuery(companyId, frequencyFilter, statusFilter, pageNumber, pageSize);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<int>> CreateAsync(
        int companyId,
        string runCode,
        string periodName,
        DateTime startDate,
        DateTime endDate,
        DateTime paymentDate,
        PayrollFrequencyType frequency,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new CreatePayrollRunCommand(companyId, runCode, periodName, startDate, endDate, paymentDate, frequency, description);
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

    /// <inheritdoc />
    public async Task<Result> CalculateAsync(int id, Guid calculationRequestId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new ProcessBatchPayrollCommand(id, calculationRequestId);
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

    /// <inheritdoc />
    public async Task<Result> ResetAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new ResetPayrollRunCommand(id);
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

    /// <inheritdoc />
    public async Task<Result> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new ApprovePayrollRunCommand(id);
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

    /// <inheritdoc />
    public async Task<Result> PostAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new PostPayrollRunCommand(id);
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

    /// <inheritdoc />
    public async Task<Result> AdminOverrideLockAsync(int id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new AdminOverrideLockCommand(id);
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
