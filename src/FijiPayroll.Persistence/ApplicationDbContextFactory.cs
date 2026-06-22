using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantProvider, DesignTimeTenantProvider>();
        var tenantProvider = services.BuildServiceProvider().GetRequiredService<ITenantProvider>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=FijiPayrollDb_Migrations;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options;

        return new ApplicationDbContext(
            options,
            new AuditableEntityInterceptor(new DesignTimeUserAccessor()),
            tenantProvider);
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public int GetCurrentCompanyId() => 1;
        public string GetCurrentTenantSecurityKey() => "KEY_COMP_1";
    }

    private sealed class DesignTimeUserAccessor : ICurrentUserAccessor
    {
        public string Username => "migration@design";
    }
}
