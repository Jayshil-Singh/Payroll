using FijiPayroll.Application;
using FijiPayroll.Application.Common.Interfaces;
using FijiPayroll.Persistence;
using FijiPayroll.Persistence.Context;
using FijiPayroll.Persistence.Seeders;
using FijiPayroll.Persistence.Interceptors;
using FijiPayroll.Infrastructure;
using FijiPayroll.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using FijiPayroll.WPF.Infrastructure;
using FijiPayroll.WPF.Services;
using FijiPayroll.WPF.Views;
using FijiPayroll.WPF.Views.Auth;
using FijiPayroll.WPF.ViewModels;
using FijiPayroll.WPF.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FijiPayroll.WPF;

/// <summary>
/// Application bootstrapper with full enterprise hardening:
/// - DI container initialization
/// - Database migration + seeding
/// - State recovery (persisted + validated)
/// - Monitor/watchdog startup
/// - 5-second graceful shutdown with forced termination fallback
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;
    private string _currentTheme = "Dark";
    private readonly string _themeConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_config.txt");

    // Monitor references for orderly shutdown
    private SystemHealthMonitor? _healthMonitor;
    private SystemIntegrityValidator? _integrityValidator;
    private MemorySmoothingScheduler? _memoryScheduler;
    private PriorityDispatcherQueue? _priorityQueue;
    private ComplianceJobProcessor? _complianceJobProcessor;
    private SessionManager? _sessionManager;

    public IServiceProvider? ServiceProvider => _serviceProvider;
    public string CurrentTheme => _currentTheme;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Check for headless migration CLI arguments first
        if (e.Args.Contains("--migrate") || e.Args.Contains("/migrate") || e.Args.Contains("--bootstrap") || e.Args.Contains("/bootstrap"))
        {
            RunHeadlessMigration();
            return;
        }

        // Initialize and show splash screen
        var splash = new SplashWindow();
        splash.Show();

        Task.Run(async () =>
        {
            try
            {
                // ── Stage 1: DI Container ───────────────────────────────────────────
                splash.UpdateProgress(20, "Bootstrapping Container", "Loading services and features...");
                await Task.Delay(300);

                var services = new ServiceCollection();

                // Load configuration from appsettings.json
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var configuration = configBuilder.Build();
                services.AddSingleton<IConfiguration>(configuration);

                string? connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    string dbDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll");
                    if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
                    connectionString = $"Server=(localdb)\\mssqllocaldb;Database=FijiPayrollDb;Trusted_Connection=True;" +
                        $"MultipleActiveResultSets=true;AttachDbFileName={Path.Combine(dbDir, "FijiPayrollDb.mdf")}";
                }

                // Shared log buffer — must be registered before DI build
                var logBuffer = new LogBuffer();
                services.AddSingleton<ILogBuffer>(logBuffer);
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new InMemoryLoggerProvider(logBuffer));
                });

                // Infrastructure monitors (singletons, registered before AddPresentation)
                _priorityQueue = new PriorityDispatcherQueue();
                services.AddSingleton(_priorityQueue);
                services.AddSingleton<SystemHealthMonitor>();
                services.AddSingleton<SystemIntegrityValidator>();
                services.AddSingleton<MemorySmoothingScheduler>();
                services.AddSingleton<ViewModelLeakDetector>();

                // Application layers
                services.AddApplication();
                services.AddPersistence(connectionString);
                services.AddInfrastructure(configuration);


                // WPF presentation layer (ViewModels, Views, Shell services)
                services.AddPresentation();

                _serviceProvider = services.BuildServiceProvider();
                _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                _logger.LogInformation("DI Container successfully initialized.");

                // ── Stage 1.5: Licensing ─────────────────────────────────────────────
                splash.UpdateProgress(30, "Checking License", "Validating application license...");
                var licenseValidator = _serviceProvider.GetRequiredService<FijiPayroll.Infrastructure.Services.Licensing.LicenseValidator>();
                await licenseValidator.InitializeAsync();

                if (!licenseValidator.IsLicensed)
                {
                    _logger.LogWarning("License validation failed: {Error}", licenseValidator.ErrorMessage);
                    
                    bool licenseLoaded = false;
                    Dispatcher.Invoke(() =>
                    {
                        splash.Hide();
                        
                        var openFileDialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "License Files (*.fplic)|*.fplic",
                            Title = $"Select Fiji Payroll License File"
                        };

                        if (openFileDialog.ShowDialog() == true)
                        {
                            try
                            {
                                string licenseContent = File.ReadAllText(openFileDialog.FileName);
                                var task = licenseValidator.ValidateLicenseFileAsync(licenseContent);
                                task.Wait();
                                if (task.Result)
                                {
                                    // Save the license file to the app data directory
                                    string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll", "License");
                                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                                    string targetPath = Path.Combine(targetDir, "license.fplic");
                                    File.WriteAllText(targetPath, licenseContent);
                                    licenseLoaded = true;
                                    MessageBox.Show("License validated and registered successfully.", "License Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show($"Selected license is invalid: {licenseValidator.ErrorMessage}", "License Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to load selected license: {ex.Message}", "License Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        
                        if (licenseLoaded)
                        {
                            splash.Show();
                        }
                    });

                    if (!licenseLoaded)
                    {
                        _logger.LogWarning("No valid license provided. Exiting.");
                        Dispatcher.Invoke(() =>
                        {
                            splash.Close();
                            Shutdown(-1);
                        });
                        return;
                    }
                }

                // ── Stage 2: Database ────────────────────────────────────────────────
                splash.UpdateProgress(40, "Connecting Database", "Opening local SQL Server database...");
                await Task.Delay(300);

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
                
                // Initialize database tables for dynamic plugins
                var pluginLoader = _serviceProvider.GetRequiredService<PluginLoader>();
                await pluginLoader.InitializePluginsDatabaseAsync(scope.ServiceProvider);

                // Run PII plaintext to AES-256 migration
                splash.UpdateProgress(50, "Securing PII Data", "Migrating plaintext records to AES-256...");
                await context.MigratePlaintextToAesAsync();

                _logger.LogInformation("Database connected, schema validated, plugin schemas registered, and dynamic PII encrypted.");

                // ── Stage 3: Seeders ─────────────────────────────────────────────────
                splash.UpdateProgress(60, "Running Seeders", "Loading Fiji tax tables & company components...");
                await Task.Delay(300);

                var ruleModuleSeeder = scope.ServiceProvider.GetRequiredService<RuleModuleSeeder>();
                await ruleModuleSeeder.SeedAsync();

                var taxSeeder = scope.ServiceProvider.GetRequiredService<TaxBracketSeeder>();
                await taxSeeder.SeedAsync();

                var componentSeeder = scope.ServiceProvider.GetRequiredService<PayrollComponentSeeder>();
                await componentSeeder.SeedAsync();

                var employeeSeeder = scope.ServiceProvider.GetRequiredService<EmployeeSeeder>();
                await employeeSeeder.SeedAsync();

                var complianceSeeder = scope.ServiceProvider.GetRequiredService<ComplianceSeeder>();
                await complianceSeeder.SeedAsync();

                var userSeeder = scope.ServiceProvider.GetRequiredService<UserAccountSeeder>();
                await userSeeder.SeedAsync();

                _logger.LogInformation("FRCS tax tables, employee structures, compliance rules/layouts, and admin credentials seeded successfully.");

                // ── Stage 4: State Recovery ──────────────────────────────────────────
                splash.UpdateProgress(75, "Recovering State", "Restoring last session...");
                await Task.Delay(200);

                var stateStore = _serviceProvider.GetRequiredService<IApplicationStateStore>();
                await stateStore.LoadPersistedStateAsync();
                _logger.LogInformation("Application state restored.");

                bool loginSuccess = false;
                Dispatcher.Invoke(() =>
                {
                    splash.Hide();
                    var loginView = _serviceProvider.GetRequiredService<LoginView>();
                    loginSuccess = loginView.ShowDialog() == true;
                    if (loginSuccess)
                    {
                        splash.Show();
                    }
                });

                if (!loginSuccess)
                {
                    _logger.LogWarning("Authentication cancelled. Shutting down.");
                    Dispatcher.Invoke(() =>
                    {
                        splash.Close();
                        Shutdown();
                    });
                    return;
                }

                _logger.LogInformation("Authenticated session established for company {CompanyId}.", stateStore.CurrentCompanyId);

                // ── Stage 5: Start Monitors ──────────────────────────────────────────
                splash.UpdateProgress(90, "Starting Services", "Launching watchdog monitors...");

                _healthMonitor = _serviceProvider.GetRequiredService<SystemHealthMonitor>();
                _healthMonitor.Start();

                _integrityValidator = _serviceProvider.GetRequiredService<SystemIntegrityValidator>();
                _integrityValidator.Start();

                _memoryScheduler = _serviceProvider.GetRequiredService<MemorySmoothingScheduler>();
                _memoryScheduler.Start();

                _complianceJobProcessor = _serviceProvider.GetRequiredService<ComplianceJobProcessor>();
                _complianceJobProcessor.Start();

                _sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
                _sessionManager.StartTracking();

                _logger.LogInformation("All background monitors started.");

                // ── Stage 6: Open Shell ──────────────────────────────────────────────
                splash.UpdateProgress(100, "Opening Shell", "Launching main dashboard...");
                await Task.Delay(200);

                Dispatcher.Invoke(() =>
                {
                    LoadThemeSettings();

                    var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
                    var sessionStore = _serviceProvider.GetRequiredService<IAuthSessionStore>();

                    bool isEmployeeSelfService =
                        sessionStore.Current.Roles.Contains("Employee", StringComparer.OrdinalIgnoreCase) &&
                        sessionStore.Current.Roles.Count == 1; // Pure employee accounts only

                    if (isEmployeeSelfService)
                    {
                        // Employee Self-Service portal — full-screen dedicated ESS shell
                        _logger?.LogInformation("ESS mode: navigating to Employee Self-Service portal.");
                        navigationService.NavigateTo<ESSHomeViewModel>();

                        var essShell = _serviceProvider.GetRequiredService<ESSShellWindow>();
                        essShell.Show();
                    }
                    else
                    {
                        // Staff / admin — normal payroll shell
                        navigationService.NavigateTo<DashboardViewModel>();
                        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                        mainWindow.Show();
                    }

                    splash.Close();
                });

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to complete application boot sequence.");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Startup Error: {ex.Message}\n\n{ex.StackTrace}",
                        "Fatal Initialization Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    splash.Close();
                    Shutdown(-1);
                });
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application shutdown initiated.");

        // Graceful shutdown: give monitors up to 5 seconds to stop cleanly
        var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            Task.WhenAll(
                Task.Run(() => _healthMonitor?.Dispose()),
                Task.Run(() => _integrityValidator?.Dispose()),
                Task.Run(() => _memoryScheduler?.Dispose()),
                Task.Run(() => _priorityQueue?.Dispose()),
                Task.Run(() => _complianceJobProcessor?.Dispose()),
                Task.Run(() => _sessionManager?.Dispose())
            ).Wait(shutdownCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Graceful shutdown timed out — forcing termination.");
        }
        finally
        {
            shutdownCts.Dispose();
        }

        // Persist final application state
        if (_serviceProvider != null)
        {
            try
            {
                var stateStore = _serviceProvider.GetRequiredService<IApplicationStateStore>();
                stateStore.PersistCurrentStateAsync().Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
        }

        _logger?.LogInformation("Application exited cleanly.");
        base.OnExit(e);
    }

    // ─── Theme ────────────────────────────────────────────────────────────────

    public void LoadThemeSettings()
    {
        try
        {
            if (File.Exists(_themeConfigPath))
            {
                string theme = File.ReadAllText(_themeConfigPath).Trim();
                if (theme == "Dark" || theme == "Light")
                    SetTheme(theme);
            }
        }
        catch { }
    }

    public void SetTheme(string themeName)
    {
        var mergedDicts = Resources.MergedDictionaries;
        var themeDict = mergedDicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("Theme.Dark.xaml") ||
             d.Source.OriginalString.Contains("Theme.Light.xaml")));

        if (themeDict != null) mergedDicts.Remove(themeDict);

        string sourcePath = themeName == "Light" ? "Styles/Theme.Light.xaml" : "Styles/Theme.Dark.xaml";
        mergedDicts.Insert(0, new ResourceDictionary { Source = new Uri(sourcePath, UriKind.RelativeOrAbsolute) });
        _currentTheme = themeName;

        try { File.WriteAllText(_themeConfigPath, themeName); }
        catch { }
    }

    public void ToggleTheme() => SetTheme(_currentTheme == "Dark" ? "Light" : "Dark");

    // ─── Exception Handlers ───────────────────────────────────────────────────

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleException(e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            HandleException(ex);
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        HandleException(e.Exception);
    }

    private void HandleException(Exception exception)
    {
        _logger?.LogError(exception, "Fatal unhandled exception occurred in application shell.");

        Dispatcher.Invoke(() =>
        {
            try
            {
                var errorWindow = new ErrorRecoveryWindow(exception);
                errorWindow.ShowDialog();
            }
            catch
            {
                SafeRestart();
            }
        });
    }

    private void SafeRestart()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
                Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true });
        }
        catch { }
        Shutdown(-1);
    }

    private void RunHeadlessMigration()
    {
        string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Fiji Payroll");
        string logsDir = Path.Combine(commonDataDir, "Logs");
        if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
        string bootstrapLogPath = Path.Combine(logsDir, "bootstrap.log");

        void Log(string message)
        {
            string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {message}{Environment.NewLine}";
            File.AppendAllText(bootstrapLogPath, line);
        }

        Log("======================================================================");
        Log("Headless Database Bootstrap & Migration Started.");
        try
        {
            // Load configuration from appsettings.json
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true);
            var configuration = configBuilder.Build();

            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                string dbDir = commonDataDir;
                if (!Directory.Exists(dbDir)) Directory.Exists(dbDir); // Wait, make sure we create it
                if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
                connectionString = $"Server=(localdb)\\mssqllocaldb;Database=FijiPayrollDb;Trusted_Connection=True;MultipleActiveResultSets=true;AttachDbFileName={Path.Combine(dbDir, "FijiPayrollDb.mdf")}";
            }

            Log($"Resolved Connection String: {connectionString}");

            var services = new ServiceCollection();
            
            // Shared log buffer
            var logBuffer = new LogBuffer();
            services.AddSingleton<ILogBuffer>(logBuffer);
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new InMemoryLoggerProvider(logBuffer));
            });

            // Register standard monitors required by infrastructure / plugins
            var priorityQueue = new PriorityDispatcherQueue();
            services.AddSingleton(priorityQueue);
            services.AddSingleton<SystemHealthMonitor>();
            services.AddSingleton<SystemIntegrityValidator>();
            services.AddSingleton<MemorySmoothingScheduler>();
            services.AddSingleton<ViewModelLeakDetector>();

            // Layer services
            services.AddApplication();
            services.AddPersistence(connectionString);
            services.AddInfrastructure(configuration);
            services.AddPresentation();
            services.AddSingleton<IConfiguration>(configuration);

            using var serviceProvider = services.BuildServiceProvider();
            Log("DI Container successfully built.");

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            Log("Executing Entity Framework Migrations...");
            context.Database.Migrate();
            Log("Migrations completed successfully.");

            Log("Initializing dynamic plugin schemas...");
            var pluginLoader = serviceProvider.GetRequiredService<PluginLoader>();
            pluginLoader.InitializePluginsDatabaseAsync(scope.ServiceProvider).Wait();
            Log("Plugin schemas registered.");

            Log("Securing PII Data (plaintext to AES-256 migration)...");
            context.MigratePlaintextToAesAsync().Wait();
            Log("PII data encryption complete.");

            Log("Running Database Seeders...");
            
            Log("Seeding Rule Modules...");
            scope.ServiceProvider.GetRequiredService<RuleModuleSeeder>().SeedAsync().Wait();
            
            Log("Seeding Tax Brackets...");
            scope.ServiceProvider.GetRequiredService<TaxBracketSeeder>().SeedAsync().Wait();
            
            Log("Seeding Payroll Components...");
            scope.ServiceProvider.GetRequiredService<PayrollComponentSeeder>().SeedAsync().Wait();
            
            Log("Seeding Default Employee structures...");
            scope.ServiceProvider.GetRequiredService<EmployeeSeeder>().SeedAsync().Wait();
            
            Log("Seeding Compliance templates...");
            scope.ServiceProvider.GetRequiredService<ComplianceSeeder>().SeedAsync().Wait();
            
            Log("Seeding Default Administrator account...");
            scope.ServiceProvider.GetRequiredService<UserAccountSeeder>().SeedAsync().Wait();

            Log("Database seeding complete.");
            Log("Headless Database Bootstrap & Migration Completed Successfully.");
            Log("======================================================================");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR during bootstrap: {ex.Message}");
            Log($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Log($"Inner Exception: {ex.InnerException.Message}");
            }
            Log("======================================================================");
            Environment.Exit(1);
        }
    }
}
