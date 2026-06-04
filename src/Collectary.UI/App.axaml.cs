using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Collectary.Infrastructure.Persistence;
using Collectary.Presentation.DI;
using Collectary.UI.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;
using Microsoft.EntityFrameworkCore;

namespace Collectary.UI;

public partial class App : Application
{
    private IContainer? _container;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLogger.Log.Fatal(e.ExceptionObject as Exception, "Unhandled AppDomain exception");

        var prefs = AppPreferences.Load();
        LocalizationService.Instance.Apply(prefs.Language);
        ThemeService.Instance.Apply(prefs.Theme);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            _container = BuildContainer();

            using (var scope = _container.BeginLifetimeScope())
            {
                var db = scope.Resolve<InventoryDbContext>();
                EnsureMigrationsCompatibility(db);
                db.Database.Migrate();
                DropObsoleteColumns(db);
            }

            AppLogger.Log.Information("Application started");

            var mainWindowVm = _container.Resolve<MainWindowViewModel>();
            _ = mainWindowVm.InitializeAsync();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = _container.Resolve<MainWindow>();
                window.DataContext = mainWindowVm;
                mainWindowVm.Host = window;
                desktop.MainWindow = window;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                var view = new MainView();
                view.DataContext = mainWindowVm;
                singleView.MainView = view;
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Fatal(ex, "Startup failed");
            throw;
        }
    }

    private static void EnsureMigrationsCompatibility(InventoryDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();
        try
        {
            using var historyCheck = connection.CreateCommand();
            historyCheck.CommandText =
                "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var historyExists = historyCheck.ExecuteScalar() is not null;

            if (!historyExists)
            {
                using var tableCheck = connection.CreateCommand();
                tableCheck.CommandText =
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='Presets'";
                var tablesExist = tableCheck.ExecuteScalar() is not null;

                if (tablesExist)
                {
                    using var createHistory = connection.CreateCommand();
                    createHistory.CommandText = """
                        CREATE TABLE __EFMigrationsHistory (
                            MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                            ProductVersion TEXT NOT NULL
                        );
                        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                        VALUES ('20260601221542_InitialCreate', '9.0.16');
                        """;
                    createHistory.ExecuteNonQuery();
                }
            }
        }
        finally
        {
            connection.Close();
        }
    }

    private static void DropObsoleteColumns(InventoryDbContext db)
    {
        var obsolete = new (string Table, string Column)[]
        {
            ("ListFieldDefinitions", "EntryEditMode"),
        };

        var connection = db.Database.GetDbConnection();
        connection.Open();
        try
        {
            foreach (var (table, column) in obsolete)
            {
                using var pragma = connection.CreateCommand();
                pragma.CommandText = $"PRAGMA table_info('{table}')";
                var exists = false;
                using (var reader = pragma.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (!exists) continue;

                using var drop = connection.CreateCommand();
                drop.CommandText = $"ALTER TABLE \"{table}\" DROP COLUMN \"{column}\"";
                drop.ExecuteNonQuery();
                AppLogger.Log.Information("Dropped obsolete column {Table}.{Column}", table, column);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to drop obsolete columns");
        }
        finally
        {
            connection.Close();
        }
    }

    private static IContainer BuildContainer()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appData, "Collectary", "collectary.db");
        var imagePath = Path.Combine(appData, "Collectary", "images");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule());
        builder.RegisterModule(new InfrastructureModule(dbPath, imagePath));
        builder.RegisterModule(new UiModule());
        return builder.Build();
    }
}
