using FijiPayroll.Application.Common.Models;
using MediatR;
using System;

namespace FijiPayroll.Application.Features.Lookups.Commands.CreateLookup;

/// <summary>
/// CQRS Command to create a new polymorphic master lookup.
/// </summary>
public sealed record CreateLookupCommand(
    int CompanyId,
    string Category,
    string Code,
    string Name,
    DateTime EffectiveFrom,
    DateTime EffectiveTo,
    int? ParentId = null,
    int DisplayOrder = 0,
    bool IsActive = true
) : IRequest<Result<int>>;
