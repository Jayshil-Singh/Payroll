using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Persistence.Repositories;
using FijiPayroll.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
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
        // Register the audit interceptor. It depends on ICurrentUserAccessor, which is
        // registered in the Infrastructure layer.
        services.AddScoped<AuditableEntityInterceptor>();

        // Register the EF Core DbContext using SQL Server
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Register repositories
        services.AddScoped<IPayrollComponentRepository, PayrollComponentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IPayrollRunRepository, PayrollRunRepository>();
        services.AddScoped<ITaxBracketRepository, TaxBracketRepository>();

        // Register the Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Seeder services
        services.AddScoped<PayrollComponentSeeder>();
        services.AddScoped<TaxBracketSeeder>();
        services.AddScoped<EmployeeSeeder>();

        return services;
    }
}
