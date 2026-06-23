using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Domain.Entities.Company;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Settings.Queries;

// ── DTO ───────────────────────────────────────────────────────────────────────

public sealed class SystemSettingsDto
{
    public string DefaultPayFrequency    { get; set; } = "Weekly";
    public string DefaultPayrollCalendar { get; set; } = "Standard 2026";
    public string NegativePayPolicy      { get; set; } = "PartialDeduction";

    public string DefaultSubmissionPaths { get; set; } = @"C:\FijiPayroll\Submissions";
    public string BackupDirectory        { get; set; } = @"C:\FijiPayroll\Backups";
    public string ExportDirectory        { get; set; } = @"C:\FijiPayroll\Exports";
    public string ImportDirectory        { get; set; } = @"C:\FijiPayroll\Imports";

    public string SmtpHost       { get; set; } = string.Empty;
    public int    SmtpPort       { get; set; } = 587;
    public string SmtpUsername   { get; set; } = string.Empty;
    public string SmtpPassword   { get; set; } = string.Empty;
    public bool   SmtpSslEnabled { get; set; } = true;
}

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the SystemSettings for the currently active company, creating a default
/// record if one does not yet exist.
/// </summary>
public sealed record GetSystemSettingsQuery(int CompanyId) : IRequest<Result<SystemSettingsDto>>;

public sealed class GetSystemSettingsQueryHandler
    : IRequestHandler<GetSystemSettingsQuery, Result<SystemSettingsDto>>
{
    private readonly ISystemSettingsRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemSettingsQueryHandler(ISystemSettingsRepository repo, IUnitOfWork unitOfWork)
    {
        _repo      = repo ?? throw new ArgumentNullException(nameof(repo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<SystemSettingsDto>> Handle(
        GetSystemSettingsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _repo.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

            if (settings == null)
            {
                // Seed defaults on first access
                settings = SystemSettings.Create(request.CompanyId);
                await _repo.AddAsync(settings, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<SystemSettingsDto>.Success(MapToDto(settings));
        }
        catch (Exception ex)
        {
            return Result<SystemSettingsDto>.Failure($"Failed to load settings: {ex.Message}");
        }
    }

    private static SystemSettingsDto MapToDto(SystemSettings s) => new()
    {
        DefaultPayFrequency    = s.DefaultPayFrequency,
        DefaultPayrollCalendar = s.DefaultPayrollCalendar,
        NegativePayPolicy      = s.NegativePayPolicy,
        DefaultSubmissionPaths = s.DefaultSubmissionPaths,
        BackupDirectory        = s.BackupDirectory,
        ExportDirectory        = s.ExportDirectory,
        ImportDirectory        = s.ImportDirectory,
        SmtpHost               = s.SmtpHost,
        SmtpPort               = s.SmtpPort,
        SmtpUsername           = s.SmtpUsername,
        SmtpPassword           = s.SmtpPassword,
        SmtpSslEnabled         = s.SmtpSslEnabled,
    };
}
