using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FijiPayroll.Application.Features.Employees.EventHandlers;

/// <summary>
/// Background MediatR handler to index employee data for fast search.
/// Listens to EmployeeCreatedEvent, EmployeeUpdatedEvent, and EmployeeTerminatedEvent.
/// </summary>
public sealed class EmployeeSearchIndexHandler :
    INotificationHandler<MediatRNotificationWrapper<EmployeeCreatedEvent>>,
    INotificationHandler<MediatRNotificationWrapper<EmployeeUpdatedEvent>>,
    INotificationHandler<MediatRNotificationWrapper<EmployeeTerminatedEvent>>
{
    private readonly ISearchService _searchService;
    private readonly ILogger<EmployeeSearchIndexHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public EmployeeSearchIndexHandler(ISearchService searchService, ILogger<EmployeeSearchIndexHandler> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(MediatRNotificationWrapper<EmployeeCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var employee = notification.Event.Employee;
        _logger.LogInformation("Indexing newly created employee {EmployeeId} ({Name})", employee.Id, employee.FullName);

        var searchData = JsonSerializer.Serialize(new
        {
            Title = employee.FullName,
            Snippet = $"{employee.Position} - {employee.Department} ({employee.Branch})",
            EmployeeName = employee.FullName,
            Department = employee.Department,
            Notes = $"Branch: {employee.Branch}, Position: {employee.Position}",
            Other = $"TIN: {employee.Tin}, FNPF: {employee.FnpfNumber}, Email: {employee.Email}, Type: {employee.EmploymentType}"
        });

        await _searchService.IndexEntityAsync("Employee", employee.Id.ToString(), searchData, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Handle(MediatRNotificationWrapper<EmployeeUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var employee = notification.Event.Employee;
        _logger.LogInformation("Indexing updated employee {EmployeeId} ({Name})", employee.Id, employee.FullName);

        string title = employee.IsActive ? employee.FullName : $"{employee.FullName} (Terminated)";
        string statusText = employee.IsActive ? "Active" : "Terminated";
        string snippet = $"{employee.Position} - {employee.Department} ({employee.Branch}) - {statusText}";

        var searchData = JsonSerializer.Serialize(new
        {
            Title = title,
            Snippet = snippet,
            EmployeeName = employee.FullName,
            Department = employee.Department,
            Notes = $"Branch: {employee.Branch}, Position: {employee.Position}, Status: {statusText}",
            Other = $"TIN: {employee.Tin}, FNPF: {employee.FnpfNumber}, Email: {employee.Email}, Type: {employee.EmploymentType}"
        });

        await _searchService.IndexEntityAsync("Employee", employee.Id.ToString(), searchData, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Handle(MediatRNotificationWrapper<EmployeeTerminatedEvent> notification, CancellationToken cancellationToken)
    {
        var employee = notification.Event.Employee;
        _logger.LogInformation("Indexing terminated employee {EmployeeId} ({Name})", employee.Id, employee.FullName);

        var searchData = JsonSerializer.Serialize(new
        {
            Title = $"{employee.FullName} (Terminated)",
            Snippet = $"{employee.Position} - {employee.Department} ({employee.Branch}) - Terminated",
            EmployeeName = employee.FullName,
            Department = employee.Department,
            Notes = $"Branch: {employee.Branch}, Position: {employee.Position}, Status: Terminated",
            Other = $"TIN: {employee.Tin}, FNPF: {employee.FnpfNumber}, Email: {employee.Email}, Type: {employee.EmploymentType}"
        });

        await _searchService.IndexEntityAsync("Employee", employee.Id.ToString(), searchData, cancellationToken);
    }
}
