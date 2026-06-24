using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.Persistence;

/// <summary>
/// Extension methods to register all Persistence layer services (EF Core DbContext,
/// repositories, unit of work, and interceptors) with the dependency injection container.
/// Called from the WPF composition root.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers database connection, EF Core interceptors, and repositories in the DI container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="connectionString">The SQL Server database connection string.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        // Register the audit interceptors. They depend on ICurrentUserAccessor, which is
        // registered in the Infrastructure layer.
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        // Register the EF Core DbContext using SQL Server
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            string? conn = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conn))
            {
                conn = connectionString;
            }
            options.UseSqlServer(conn);
        });

        // Register repositories
        services.AddScoped<IPayrollComponentRepository, PayrollComponentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
        services.AddScoped<ITaxBracketRepository, TaxBracketRepository>();
        services.AddScoped<IMasterLookupRepository, MasterLookupRepository>();
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<ISearchIndexRepository, SearchIndexRepository>();
        services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();
        services.AddScoped<IComplianceRepository, ComplianceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();

        // Register the Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Approval Engine
        services.AddScoped<IApprovalEngine, ApprovalEngine>();

        // Register Seeder services
        services.AddScoped<PayrollComponentSeeder>();
        services.AddScoped<TaxBracketSeeder>();
        services.AddScoped<EmployeeSeeder>();
        services.AddScoped<RuleModuleSeeder>();
        services.AddScoped<ComplianceSeeder>();
        services.AddScoped<UserAccountSeeder>();
        services.AddScoped<IJsonSeedLoader, JsonSeedLoader>();

        // Register the Reference Data Cache
        services.AddSingleton<IReferenceDataCache, ReferenceDataCache>();

        return services;
    }
}
