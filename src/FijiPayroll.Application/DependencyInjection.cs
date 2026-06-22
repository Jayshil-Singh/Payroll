using FijiPayroll.Application.Common.Behaviours;
using FijiPayroll.Application.Common.Models;
using FijiPayroll.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FijiPayroll.Application;

/// <summary>
/// Extension methods to register all Application layer services with the
/// dependency injection container. Called from the WPF composition root.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR, FluentValidation, and all MediatR pipeline behaviours
    /// defined in the Application layer.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR — scans this assembly for all IRequestHandler implementations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation validators — scans this assembly for all AbstractValidator<T>
        services.AddValidatorsFromAssembly(
            Assembly.GetExecutingAssembly(),
            lifetime: ServiceLifetime.Transient);

        // Register MediatR pipeline behaviours in execution order:
        // 1. Logging  →  2. Authorization  →  3. Validation  →  4. Transaction  →  5. Audit  →  Handler
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehaviour<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuthorizationBehaviour<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehaviour<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(TransactionBehaviour<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(AuditBehaviour<,>));

        // Register application services orchestrating MediatR
        services.AddScoped<IPayrollComponentService, PayrollComponentService>();
        services.AddScoped<IPayrollRunService, PayrollRunService>();
        services.AddScoped<PayrollCalculationEngine>();
        services.AddScoped<PayrollValidationService>();
        services.AddScoped<PayrollContextBuilder>();
        services.AddScoped<PayrollPipelineService>();
        services.AddScoped<IFiscalCalendarGenerator, FiscalCalendarGenerator>();
        services.AddScoped<IPayScheduleGenerator, PayScheduleGenerator>();
        services.AddScoped<ISetupWorkflowService, SetupWorkflowService>();

        // ── Rule Engine and Platform Services ───────────────────
        services.AddScoped<BusinessCalendarService>();
        services.AddScoped<RulePackageManager>();
        services.AddScoped<SimulationEngine>();
        services.AddScoped<RuleSimulationEngine>();
        services.AddScoped<ComplianceValidationService>();
        services.AddScoped<FijiPayroll.Shared.Formula.RuleExecutionPipeline>();

        // ── Phase 15 Enterprise Platform Services ───────────────
        services.AddSingleton<BatchProcessingCoordinator>();
        services.AddScoped<PayrollValidationPipeline>();
        services.AddScoped<PayrollReplayEngine>();
        services.AddScoped<PayrollDifferenceAnalyzer>();
        services.AddSingleton<BackgroundJobManager>();
        services.AddScoped<RollbackEngine>();

        return services;
    }
}
