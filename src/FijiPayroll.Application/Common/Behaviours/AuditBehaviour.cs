using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FijiPayroll.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior that automatically logs successful command executions
/// into the database AuditLogs table.
/// </summary>
public sealed class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<AuditBehaviour<TRequest, TResponse>> _logger;

    public AuditBehaviour(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ICorrelationContext correlationContext,
        ILogger<AuditBehaviour<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableCommand auditableCommand)
        {
            try
            {
                var requestName = typeof(TRequest).Name;
                _logger.LogInformation("Creating command-level audit log for request {RequestName}", requestName);

                // Try to serialize request parameters safely (avoiding sensitive fields)
                string changesJson;
                try
                {
                    // Basic serialization, skipping password fields for security
                    var requestType = request.GetType();
                    var properties = requestType.GetProperties();
                    var dict = new System.Collections.Generic.Dictionary<string, object?>();
                    foreach (var prop in properties)
                    {
                        if (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase))
                        {
                            dict[prop.Name] = "***MASKED***";
                        }
                        else
                        {
                            dict[prop.Name] = prop.GetValue(request);
                        }
                    }
                    changesJson = JsonSerializer.Serialize(dict);
                }
                catch (Exception serializeEx)
                {
                    _logger.LogWarning(serializeEx, "Failed to serialize request parameters for command audit log.");
                    changesJson = "{\"error\":\"Serialization failed\"}";
                }

                // Resolve company ID safely
                int companyId = 0;
                try
                {
                    companyId = _tenantProvider.GetCurrentCompanyId();
                }
                catch
                {
                    // Fallback if no tenant context established yet
                    var companyIdProp = request.GetType().GetProperty("CompanyId");
                    if (companyIdProp != null && companyIdProp.GetValue(request) is int cid)
                    {
                        companyId = cid;
                    }
                }

                var auditLog = AuditLog.Create(
                    companyId: companyId,
                    userId: _currentUserService.Username,
                    entityName: auditableCommand.AuditEntity,
                    entityId: "0", // Command execution doesn't target a single saved ID at pipeline level
                    action: auditableCommand.AuditAction,
                    changes: changesJson,
                    timestamp: DateTime.UtcNow,
                    correlationId: _correlationContext.CorrelationId
                );

                // Add audit log via UnitOfWork and persist it.
                await _unitOfWork.AddAuditLogAsync(auditLog, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write command audit log for request.");
            }
        }

        return response;
    }
}
