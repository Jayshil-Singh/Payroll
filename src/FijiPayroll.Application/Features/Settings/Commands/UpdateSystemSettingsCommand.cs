using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Settings.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Creates or updates the SystemSettings record for the specified company.
/// All fields are optional updates; existing values are preserved if not provided.
/// </summary>
public sealed class UpdateSystemSettingsCommand : IRequest<Result<Unit>>
{
    public int CompanyId { get; init; }

    // Payroll defaults
    public string? DefaultPayFrequency    { get; init; }
    public string? DefaultPayrollCalendar { get; init; }
    public string? NegativePayPolicy      { get; init; }

    // Directories
    public string? DefaultSubmissionPaths { get; init; }
    public string? BackupDirectory        { get; init; }
    public string? ExportDirectory        { get; init; }
    public string? ImportDirectory        { get; init; }

    // SMTP
    public string? SmtpHost       { get; init; }
    public int?    SmtpPort       { get; init; }
    public string? SmtpUsername   { get; init; }
    public string? SmtpPassword   { get; init; }
    public bool?   SmtpSslEnabled { get; init; }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateSystemSettingsCommandHandler
    : IRequestHandler<UpdateSystemSettingsCommand, Result<Unit>>
{
    private readonly ISystemSettingsRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSystemSettingsCommandHandler(ISystemSettingsRepository repo, IUnitOfWork unitOfWork)
    {
        _repo       = repo ?? throw new ArgumentNullException(nameof(repo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit>> Handle(
        UpdateSystemSettingsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _repo.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

            if (settings == null)
            {
                settings = SystemSettings.Create(request.CompanyId);
                await _repo.AddAsync(settings, cancellationToken);
            }

            // Apply changes — only update fields provided in the command
            if (request.DefaultPayFrequency    != null) settings.DefaultPayFrequency    = request.DefaultPayFrequency;
            if (request.DefaultPayrollCalendar != null) settings.DefaultPayrollCalendar = request.DefaultPayrollCalendar;
            if (request.NegativePayPolicy      != null) settings.NegativePayPolicy      = request.NegativePayPolicy;
            if (request.DefaultSubmissionPaths != null) settings.DefaultSubmissionPaths = request.DefaultSubmissionPaths;
            if (request.BackupDirectory        != null) settings.BackupDirectory        = request.BackupDirectory;
            if (request.ExportDirectory        != null) settings.ExportDirectory        = request.ExportDirectory;
            if (request.ImportDirectory        != null) settings.ImportDirectory        = request.ImportDirectory;
            if (request.SmtpHost               != null) settings.SmtpHost              = request.SmtpHost;
            if (request.SmtpPort               != null) settings.SmtpPort              = request.SmtpPort.Value;
            if (request.SmtpUsername           != null) settings.SmtpUsername           = request.SmtpUsername;
            if (request.SmtpPassword           != null) settings.SmtpPassword           = request.SmtpPassword;
            if (request.SmtpSslEnabled         != null) settings.SmtpSslEnabled         = request.SmtpSslEnabled.Value;

            _repo.Update(settings);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Failure($"Failed to save settings: {ex.Message}");
        }
    }
}
