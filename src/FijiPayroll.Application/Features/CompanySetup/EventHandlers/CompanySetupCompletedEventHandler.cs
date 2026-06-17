using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.CompanySetup.EventHandlers;

/// <summary>
/// Background MediatR handler to react to the <see cref="CompanySetupCompletedEvent"/>.
/// </summary>
public sealed class CompanySetupCompletedEventHandler : INotificationHandler<MediatRNotificationWrapper<CompanySetupCompletedEvent>>
{
    private readonly ISearchService _searchService;
    private readonly ILogger<CompanySetupCompletedEventHandler> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="CompanySetupCompletedEventHandler"/> class.
    /// </summary>
    public CompanySetupCompletedEventHandler(ISearchService searchService, ILogger<CompanySetupCompletedEventHandler> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(MediatRNotificationWrapper<CompanySetupCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.Event;
        _logger.LogInformation("Company onboarding setup completed for company ID {CompanyId} by user '{User}'. ExecutionId: {ExecutionId}",
            domainEvent.CompanyId, domainEvent.CompletedBy, domainEvent.ExecutionId);

        // Index the setup complete status in the search indexes
        var searchData = JsonSerializer.Serialize(new
        {
            Title = "Company Setup Completed",
            Snippet = $"Company ID: {domainEvent.CompanyId}. Completed by: {domainEvent.CompletedBy}.",
            CompanyId = domainEvent.CompanyId,
            ExecutionId = domainEvent.ExecutionId,
            CompletedBy = domainEvent.CompletedBy,
            OccurredOn = domainEvent.OccurredOn
        });

        await _searchService.IndexEntityAsync("CompanySetup", domainEvent.CompanyId.ToString(), searchData, cancellationToken);
    }
}
