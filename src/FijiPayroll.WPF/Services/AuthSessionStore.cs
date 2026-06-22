using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Application.Common.Models;

namespace FijiPayroll.WPF.Services;

/// <summary>
/// Thread-safe store for the active authenticated session.
/// </summary>
public sealed class AuthSessionStore : IAuthSessionStore
{
    private readonly object _lock = new();
    private AuthenticatedSession _current = new();

    public AuthenticatedSession Current
    {
        get
        {
            lock (_lock) return _current;
        }
    }

    public void Establish(AuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsAuthenticated)
        {
            throw new InvalidOperationException("Cannot establish an unauthenticated session.");
        }

        lock (_lock) { _current = session; }
    }

    public void Clear()
    {
        lock (_lock) { _current = new AuthenticatedSession(); }
    }
}
