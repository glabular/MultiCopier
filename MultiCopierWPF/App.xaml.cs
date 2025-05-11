using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiCopierWPF.Data;
using MultiCopierWPF.Interfaces;
using MultiCopierWPF.Services;
using MultiCopierWPF.ViewModels;
using System.Diagnostics;
using System.IO;
using MultiCopierWPF.Shared;
using System.Windows;
using Application = System.Windows.Application;
using Microsoft.Extensions.Logging;

namespace MultiCopierWPF;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Trace);

                // Override EF Core namespaces to show only warnings or above
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                logging.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Warning);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            })
            .ConfigureServices((hostContext, services) =>
            {
                ConfigureDatabase(services);
                services.AddTransient<MainWindowViewModel>();
                services.AddScoped<IBackupService, BackupService>();
                services.AddSingleton<ISettingsService, SettingsManager>();
                services.AddSingleton<IFolderSyncService, FolderSyncService>();
                services.AddTransient<IHashCalculator, Sha512HashCalculator>();

            }).Build();
    }

    private static void ConfigureDatabase(IServiceCollection services)
    {
        var dbPath = GetDatabasePath();
        Debug.WriteLine($"Database path in use: {dbPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<BackupDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
    }

    private static string GetDatabasePath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(folder, "MultiCopier", "MultiCopier.db");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        using var scope = AppHost.Services.CreateScope();        
        var db = scope.ServiceProvider.GetRequiredService<BackupDbContext>();

        try
        {
            db.Database.Migrate();
        }
        catch (Exception)
        {
            throw;
        }

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}
