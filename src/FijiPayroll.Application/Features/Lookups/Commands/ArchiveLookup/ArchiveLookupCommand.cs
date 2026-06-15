using FijiPayroll.Application.Common.Models;
using MediatR;

namespace FijiPayroll.Application.Features.Lookups.Commands.ArchiveLookup;

/// <summary>
/// CQRS Command to archive an existing master lookup item.
/// </summary>
public sealed record ArchiveLookupCommand(
    int Id,
    int CompanyId,
    string Reason
) : IRequest<Result>;
