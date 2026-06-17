using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Enumerations;
using FijiPayroll.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Services;

/// <summary>
/// Service implementation for managing the guided Company Setup Wizard onboarding workflow.
/// </summary>
public sealed class SetupWorkflowService : ISetupWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICorrelationContext _correlationContext;

    /// <summary>
    /// Initialises a new instance of the <see cref="SetupWorkflowService"/> class.
    /// </summary>
    public SetupWorkflowService(IUnitOfWork unitOfWork, ICorrelationContext correlationContext)
    {
        _unitOfWork = unitOfWork;
        _correlationContext = correlationContext;
    }

    /// <inheritdoc />
    public async Task<CompanySetupState> GetOrCreateSetupStateAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var state = await _unitOfWork.Setup.GetSetupStateAsync(companyId, cancellationToken);

        if (state == null)
        {
            state = CompanySetupState.Create(companyId);
            await _unitOfWork.Setup.AddSetupStateAsync(state, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return state;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompanySetupTask>> GetSetupTasksAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Setup.GetSetupTasksAsync(companyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> CompleteStepAsync(int companyId, WizardStep step, string completedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetOrCreateSetupStateAsync(companyId, cancellationToken);
            if (state.IsCompleted)
            {
                return Result.Failure("SETUP_ERROR: The onboarding setup is already marked as completed.");
            }

            // Verify if task is already completed
            var existingTask = state.Tasks.FirstOrDefault(t => t.Step == step);
            if (existingTask == null)
            {
                var task = CompanySetupTask.Create(companyId, state.Id, step, completedBy);
                state.AssociateTask(task);
                await _unitOfWork.Setup.AddSetupTaskAsync(task, cancellationToken);
            }

            // Audit
            var ip = GetLocalIpAddress();
            var execId = Guid.NewGuid();
            var audit = CompanySetupAudit.Create(
                companyId,
                step.ToString(),
                "CompleteStep",
                $"Successfully completed step {step}",
                SetupAuditStatus.Success,
                ip,
                Environment.MachineName,
                "1.0.0",
                _correlationContext.CorrelationId,
                execId
            );
            await _unitOfWork.Setup.AddSetupAuditAsync(audit, cancellationToken);

            // Determine next step
            var nextStep = (WizardStep)((int)step + 1);
            if (nextStep > WizardStep.Completed || step == WizardStep.Validation)
            {
                state.CompleteSetup();

                var company = await _unitOfWork.Setup.GetCompanyByIdAsync(companyId, cancellationToken);
                if (company != null)
                {
                    company.MarkSetupCompleted();
                }
            }
            else
            {
                state.TransitionToStep(nextStep);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to complete step: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SkipStepAsync(int companyId, WizardStep step, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetOrCreateSetupStateAsync(companyId, cancellationToken);
            if (state.IsCompleted)
            {
                return Result.Failure("SETUP_ERROR: The onboarding setup is already marked as completed.");
            }

            var nextStep = (WizardStep)((int)step + 1);
            if (nextStep > WizardStep.Completed)
            {
                nextStep = WizardStep.Completed;
            }

            state.TransitionToStep(nextStep);

            // Audit
            var ip = GetLocalIpAddress();
            var execId = Guid.NewGuid();
            var audit = CompanySetupAudit.Create(
                companyId,
                step.ToString(),
                "SkipStep",
                $"Skipped step {step}",
                SetupAuditStatus.Warning,
                ip,
                Environment.MachineName,
                "1.0.0",
                _correlationContext.CorrelationId,
                execId
            );
            await _unitOfWork.Setup.AddSetupAuditAsync(audit, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to skip step: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ResetSetupAsync(int companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.Setup.RemoveSetupStateAsync(companyId, cancellationToken);

            var company = await _unitOfWork.Setup.GetCompanyByIdAsync(companyId, cancellationToken);
            if (company != null)
            {
                company.ResetSetupState();
            }

            await _unitOfWork.Setup.RemoveCheckpointsAsync(companyId, cancellationToken);
            await _unitOfWork.Setup.RemoveExecutionRecordsAsync(companyId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to reset setup: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResultDto> ValidateSetupAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 1. Validate Company Details
        var company = await _unitOfWork.Setup.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            errors.Add("Company profile not found.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(company.TradingName))
                errors.Add("Company trading name is required.");
            if (string.IsNullOrWhiteSpace(company.TIN) || company.TIN.Length != 9 || !company.TIN.All(char.IsDigit))
                errors.Add("Company TIN must be exactly a 9-digit numeric string.");
            if (string.IsNullOrWhiteSpace(company.FnpfEmployerNumber))
                errors.Add("Company FNPF employer number is required.");
        }

        // 2. Validate Fiscal Calendar
        var calendars = await _unitOfWork.Setup.GetFiscalCalendarsAsync(companyId, cancellationToken);
        if (!calendars.Any())
        {
            errors.Add("No fiscal calendar has been generated.");
        }
        else if (calendars.All(c => c.Periods.Count == 0))
        {
            errors.Add("Generated fiscal calendar contains no periods.");
        }

        // 3. Validate Payroll Frequencies & schedules
        var frequencies = await _unitOfWork.Setup.GetPayrollFrequencyDefinitionsAsync(companyId, cancellationToken);
        if (!frequencies.Any())
        {
            errors.Add("At least one active payroll frequency definition must be configured.");
        }
        else if (frequencies.All(f => f.Schedules.Count == 0))
        {
            errors.Add("Configured payroll frequencies have no generated pay schedules.");
        }

        // 4. Validate Corporate Bank Accounts
        var bankAccounts = await _unitOfWork.Setup.GetCompanyBankAccountsAsync(companyId, cancellationToken);
        if (!bankAccounts.Any())
        {
            errors.Add("At least one corporate bank account must be configured.");
        }

        // 5. Validate Approval Config
        var approvals = await _unitOfWork.Setup.GetApprovalConfigsAsync(companyId, cancellationToken);
        if (!approvals.Any())
        {
            errors.Add("At least one approval role routing must be configured.");
        }
        else
        {
            foreach (var app in approvals)
            {
                if (app.UserId == null && app.EmployeeId == null)
                {
                    errors.Add($"Approval configuration for role {app.Role} must have either a User or an Employee assigned.");
                }
            }
        }

        bool isValid = errors.Count == 0;
        return new ValidationResultDto(isValid, errors, warnings);
    }

    /// <inheritdoc />
    public async Task<Company?> GetCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Setup.GetCompanyByIdAsync(companyId, cancellationToken);
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Suppress DNS exception
        }
        return "127.0.0.1";
    }
}
