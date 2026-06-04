using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Collectary.Core.Ports;
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

    /// <summary>
    /// Extra DI modules contributed by the platform entry point (e.g. desktop registers cloud sync).
    /// Set during <c>AppBuilder.AfterSetup</c> before the container is built. Kept empty on platforms
    /// (Browser) that must not pull in platform-specific SDKs.
    /// </summary>
    public IReadOnlyList<Autofac.Core.IModule> PlatformModules { get; set; } = Array.Empty<Autofac.Core.IModule>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLogger.Log.Fatal(e.ExceptionObject as Exception, "Unhandled AppDomain exception");

        var prefs = AppPreferences.Load();
        LocalizationService.Instance.Apply(prefs.Language);
        ThemeService.Instance.ApplySkin(prefs.Skin);
        ThemeService.Instance.ApplyColorTheme(prefs.EffectiveColorTheme());
        ThemeService.Instance.ApplyAccent(ParseAccent(prefs.AccentColor));
        ThemeService.Instance.ApplyCustomColors(prefs.CustomColors);
    }

    private static Avalonia.Media.Color? ParseAccent(string? hex) =>
        !string.IsNullOrWhiteSpace(hex) && Avalonia.Media.Color.TryParse(hex, out var color)
            ? color
            : null;

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            _container = BuildContainer();

            using (var scope = _container.BeginLifetimeScope())
            {
                var db = scope.Resolve<InventoryDbContext>();
                if (OperatingSystem.IsBrowser())
                {
                    db.Database.EnsureCreated();
                }
                else
                {
                    EnsureMigrationsCompatibility(db);
                    db.Database.Migrate();
                    DropObsoleteColumns(db);
                }
            }

            AppLogger.Log.Information("Application started");

            var prefs = AppPreferences.Load();
            var requireLogin = prefs.RequireLogin && !OperatingSystem.IsBrowser();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (requireLogin)
                {
                    ShowLoginThenMain(desktop);
                }
                else
                {
                    EnsureDefaultUser();
                    ShowMainWindow(desktop);
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                EnsureDefaultUser();
                var mainWindowVm = _container.Resolve<MainWindowViewModel>();
                _ = mainWindowVm.InitializeAsync();
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

    private void EnsureDefaultUser()
    {
        var bootstrapper = _container!.Resolve<IAccountBootstrapper>();
        var user = bootstrapper.EnsureDefaultUserAsync().GetAwaiter().GetResult();
        bootstrapper.BackfillOwnerlessAsync(user.Id).GetAwaiter().GetResult();
    }

    private void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = _container!.Resolve<MainWindowViewModel>();
        _ = vm.InitializeAsync();
        var window = _container.Resolve<MainWindow>();
        window.DataContext = vm;
        vm.Host = window;
        desktop.MainWindow = window;
    }

    private void ShowLoginThenMain(IClassicDesktopStyleApplicationLifetime desktop)
    {
        LoginWindow? login = null;
        var loginVm = new LoginViewModel(
            _container!.Resolve<IAuthService>(),
            _container.Resolve<IAccountBootstrapper>(),
            onAuthenticated: () =>
            {
                var vm = _container.Resolve<MainWindowViewModel>();
                _ = vm.InitializeAsync();
                var window = _container.Resolve<MainWindow>();
                window.DataContext = vm;
                vm.Host = window;
                desktop.MainWindow = window;
                window.Show();
                login?.Close();
            });

        login = new LoginWindow { DataContext = loginVm };
        desktop.MainWindow = login;
    }

    private IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule());

        if (OperatingSystem.IsBrowser())
        {
            builder.RegisterModule(new BrowserInfrastructureModule());
        }
        else
        {
            var dataRoot = AppDataPaths.Root;
            var dbPath = Path.Combine(dataRoot, "collectary.db");
            var imagePath = Path.Combine(dataRoot, "images");
            Directory.CreateDirectory(dataRoot);
            builder.RegisterModule(new InfrastructureModule(dbPath, imagePath));
        }

        foreach (var module in PlatformModules)
            builder.RegisterModule(module);

        builder.RegisterModule(new SecurityModule());
        builder.RegisterModule(new UiModule());
        return builder.Build();
    }
}
