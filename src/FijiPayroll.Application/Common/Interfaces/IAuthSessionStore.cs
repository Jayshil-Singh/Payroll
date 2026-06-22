using FijiPayroll.Application.Common.Models;

namespace FijiPayroll.Application.Common.Interfaces;

/// <summary>
/// Holds the active authenticated session for the desktop shell.
/// </summary>
public interface IAuthSessionStore
{
    AuthenticatedSession Current { get; }
    void Establish(AuthenticatedSession session);
    void Clear();
}
