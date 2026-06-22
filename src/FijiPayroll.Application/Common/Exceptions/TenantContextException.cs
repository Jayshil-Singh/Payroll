namespace FijiPayroll.Application.Common.Exceptions;

/// <summary>
/// Thrown when a tenant (company) context is required but not established.
/// </summary>
public sealed class TenantContextException : InvalidOperationException
{
    public TenantContextException(string message) : base(message) { }
}
