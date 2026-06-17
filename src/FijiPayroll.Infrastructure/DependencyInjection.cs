using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Domain.Interfaces;
using FijiPayroll.Infrastructure.Services;
using FijiPayroll.Infrastructure.Services.BankGenerators;
using FijiPayroll.SDK.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FijiPayroll.Infrastructure;

/// <summary>
/// Extension methods to register all Infrastructure layer services (EventBus, FileStorage, PluginLoader)
/// with the dependency injection container. Called from the WPF composition root.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers infrastructure services and the plugin loader mechanism.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the EventBus
        services.AddScoped<IEventBus, EventBus>();

        // Register the CorrelationContext
        services.AddScoped<ICorrelationContext, CorrelationContext>();

        // Register the File Storage Provider
        services.AddScoped<IFileStorageProvider, FileStorageProvider>();

        // Register the Import Engine
        services.AddScoped<IImportEngine, ImportEngine>();

        // Register the Search Service
        services.AddSingleton<ISearchService, SearchService>();

        // Register the Compliance services
        services.AddScoped<IComplianceFileService, ComplianceFileService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Register Bank generators
        services.AddScoped<IBankFileGenerator, BSPBankGenerator>();
        services.AddScoped<IBankFileGenerator, ANZBankGenerator>();
        services.AddScoped<IBankFileGenerator, WestpacBankGenerator>();
        services.AddScoped<IBankFileGenerator, BREDBankGenerator>();
        services.AddScoped<IBankFileGenerator, HFCBankGenerator>();
        services.AddScoped<IBankFileGenerator, KontikiBankGenerator>();

        // Register background job processor as singleton
        services.AddSingleton<ComplianceJobProcessor>();

        // Register the License Fingerprint Provider
        services.AddSingleton<ILicenseFingerprintProvider, LicenseFingerprintProvider>();

        // Register Rule Cache and IMemoryCache
        services.AddMemoryCache();
        services.AddSingleton<FijiPayroll.Shared.Formula.IFormulaCache, MemoryFormulaCache>();

        // Register the Plugin Loader as a singleton
        var pluginLoader = new PluginLoader();
        pluginLoader.DiscoverAndRegisterPlugins(services, configuration);
        services.AddSingleton(pluginLoader);

        // Register Compliance Evidence Pack Generator services
        services.AddScoped<FijiPayroll.Infrastructure.Services.ComplianceEvidence.ReportSnapshotRegistry>();
        services.AddScoped<FijiPayroll.Infrastructure.Services.ComplianceEvidence.SSRSReportSnapshotService>();
        services.AddScoped<FijiPayroll.Infrastructure.Services.ComplianceEvidence.SimplePdfGenerator>();
        services.AddScoped<FijiPayroll.Infrastructure.Services.ComplianceEvidence.FileArchiveManager>();
        services.AddScoped<FijiPayroll.Infrastructure.Services.ComplianceEvidence.ComplianceMetadataAssembler>();
        services.AddScoped<IBuildVersionProvider, FijiPayroll.Infrastructure.Services.ComplianceEvidence.BuildVersionProvider>();
        services.AddScoped<IEvidencePackSignatureService, FijiPayroll.Infrastructure.Services.ComplianceEvidence.EvidencePackSignatureService>();
        services.AddScoped<ISignatureVerifierService, FijiPayroll.Infrastructure.Services.ComplianceEvidence.SignatureVerifierService>();
        services.AddScoped<IEvidencePackGeneratorService, FijiPayroll.Infrastructure.Services.ComplianceEvidence.EvidencePackGeneratorService>();

        return services;
    }
}
