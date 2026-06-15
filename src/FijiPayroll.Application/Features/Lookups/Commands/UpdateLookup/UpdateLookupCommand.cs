using FijiPayroll.Application.Common.Models;
using MediatR;
using System;

namespace FijiPayroll.Application.Features.Lookups.Commands.UpdateLookup;

/// <summary>
/// CQRS Command to update an existing master lookup item.
/// </summary>
public sealed record UpdateLookupCommand(
    int Id,
    int CompanyId,
    string Name,
    DateTime EffectiveFrom,
    DateTime EffectiveTo,
    int? ParentId = null,
    int DisplayOrder = 0,
    bool IsActive = true
) : IRequest<Result>;
