using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build — default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Parameter("Minimum acceptable line coverage percentage for the Coverage gate")]
    readonly double CoverageThreshold = 95;

    [Parameter("Git baseline that scopes mutation testing to your local changes. Defaults to 'HEAD' — only the code you have changed since your last commit. The Mutate target diffs against this with the git CLI and mutates just those files; running mutation across the whole codebase is intentionally not supported — it is far too slow.")]
    readonly string Since = "HEAD";

    AbsolutePath TestProjectsRoot => RootDirectory / "tests";
    AbsolutePath CoverageDirectory => RootDirectory / "TestResults" / "coverage";
    AbsolutePath CoverageReportDirectory => RootDirectory / "TestResults" / "CoverageReport";
    AbsolutePath CoverageSettings => RootDirectory / "coverlet.runsettings";
    AbsolutePath TestSettings => RootDirectory / "tests.runsettings";
    AbsolutePath DesktopProject => RootDirectory / "src" / "Collectary.UI.Desktop" / "Collectary.UI.Desktop.csproj";
    AbsolutePath AndroidProject => RootDirectory / "src" / "Collectary.UI.Android" / "Collectary.UI.Android.csproj";

    IReadOnlyList<CloudCredential> RequiredCredentials =>
    [
        new("COLLECTARY_ONEDRIVE_CLIENT_ID", "OneDrive (Azure Entra) public client id"),
        new("COLLECTARY_ANDROID_SIGNATURE_HASH", "Android OneDrive redirect signature hash"),
        new("COLLECTARY_GOOGLE_CLIENT_ID", "Google Drive OAuth client id (Windows desktop)"),
        new("COLLECTARY_GOOGLE_CLIENT_SECRET", "Google Drive OAuth client secret (Windows desktop)")
    ];

    string[] TestProjects =>
    [
        "Collectary.Core.Tests",
        "Collectary.Infrastructure.Tests",
        "Collectary.UI.Tests"
    ];

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "Collectary.slnx"));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "Collectary.slnx")
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetSettingsFile(TestSettings)
                    .CombineWith(TestProjects, (settings, project) => settings
                        .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")),
                degreeOfParallelism: TestProjects.Length,
                completeOnFailure: true);
        });

    Target Coverage => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetDataCollector("XPlat Code Coverage")
                    .SetSettingsFile(CoverageSettings)
                    .SetResultsDirectory(CoverageDirectory)
                    .CombineWith(TestProjects, (settings, project) => settings
                        .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")),
                degreeOfParallelism: TestProjects.Length,
                completeOnFailure: true);

            var coverageFiles = CoverageDirectory.GlobFiles("**/coverage.cobertura.xml");
            Assert.True(coverageFiles.Count > 0, "No coverage files were produced.");

            DotNet(
                $"reportgenerator " +
                $"\"-reports:{CoverageDirectory / "**" / "coverage.cobertura.xml"}\" " +
                $"\"-targetdir:{CoverageReportDirectory}\" " +
                $"\"-reporttypes:TextSummary;Cobertura;Html\"",
                workingDirectory: RootDirectory);

            var merged = CoverageReportDirectory / "Cobertura.xml";
            var lineRate = ReadLineRate(merged);
            var percent = Math.Round(lineRate * 100, 2);

            Log.Information("Merged line coverage: {Percent}% (threshold {Threshold}%)", percent, CoverageThreshold);

            Assert.True(percent >= CoverageThreshold,
                $"Line coverage {percent}% is below the required {CoverageThreshold}%.");
        });

    static double ReadLineRate(AbsolutePath coberturaFile)
    {
        var doc = XDocument.Load(coberturaFile);
        var coverage = doc.Root!;

        var covered = coverage.Attribute("lines-covered");
        var valid = coverage.Attribute("lines-valid");
        if (covered is not null && valid is not null &&
            double.TryParse(valid.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var validCount) &&
            validCount > 0 &&
            double.TryParse(covered.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var coveredCount))
        {
            return coveredCount / validCount;
        }

        var lineRate = coverage.Attribute("line-rate");
        return lineRate is not null &&
               double.TryParse(lineRate.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate)
            ? rate
            : 0;
    }

    Target Mutate => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var changed = ChangedMutableSourceFiles();
            if (changed.Count == 0)
            {
                Log.Information("Mutate: no changed source files since {Since}; nothing to mutate.", Since);
                return;
            }

            foreach (var project in new[] { "Collectary.Core", "Collectary.Infrastructure", "Collectary.Infrastructure.Cloud", "Collectary.Presentation" })
            {
                var prefix = $"src/{project}/";
                var relative = changed
                    .Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Substring(prefix.Length))
                    .ToArray();
                if (relative.Length == 0) continue;

                var csproj = RootDirectory / "src" / project / $"{project}.csproj";
                var patterns = string.Join(" ", relative.Select(f => $"--mutate \"**/{System.IO.Path.GetFileName(f)}\""));
                var arguments = $"stryker -p \"{csproj}\" {patterns}";

                var dotnet = ToolPathResolver.GetPathExecutable("dotnet");
                var process = ProcessTasks.StartProcess(dotnet, arguments, RootDirectory);
                process.WaitForExit();

                var text = string.Join("\n", process.Output.Select(o => o.Text));
                if (text.Contains("unable to calculate a mutation score", StringComparison.OrdinalIgnoreCase) ||
                    !text.Contains("final mutation score", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail(
                        $"Mutate: Stryker tested no mutants for {project} ({relative.Length} changed file(s): "
                        + $"{string.Join(", ", relative.Select(System.IO.Path.GetFileName))}). "
                        + "The changed files were Excluded instead of mutated — the mutation gate did not actually run. "
                        + "Check the --mutate glob arguments reaching Stryker.");
                }

                process.AssertZeroExitCode();
            }
        });

    Target CheckCredentials => _ => _
        .Executes(() =>
        {
            var results = RequiredCredentials
                .Select(c => (c.EnvVariable, c.Purpose, Value: ResolveCredential(c.EnvVariable)))
                .ToList();

            foreach (var (envVariable, purpose, value) in results)
            {
                var present = !string.IsNullOrWhiteSpace(value);
                // A user/machine var persisted after this shell launched is absent from the inherited
                // process block; copy it in so dependent targets and their child processes inherit it.
                if (present) Environment.SetEnvironmentVariable(envVariable, value);
                Log.Information("{Status}  {Var} — {Purpose}", present ? "ok     " : "MISSING", envVariable, purpose);
            }

            var missing = results.Where(r => string.IsNullOrWhiteSpace(r.Value)).Select(r => r.EnvVariable).ToList();
            Assert.True(missing.Count == 0,
                $"Missing cloud credentials: {string.Join(", ", missing)}. Persist them with "
                + "`.\\build.ps1 --target SetCredentials`, or set them as user/system environment variables.");
        });

    string ResolveCredential(string envVariable)
    {
        var process = Environment.GetEnvironmentVariable(envVariable);
        if (!string.IsNullOrWhiteSpace(process)) return process;
        if (!OperatingSystem.IsWindows()) return string.Empty;

        var user = Environment.GetEnvironmentVariable(envVariable, EnvironmentVariableTarget.User);
        if (!string.IsNullOrWhiteSpace(user)) return user;
        var machine = Environment.GetEnvironmentVariable(envVariable, EnvironmentVariableTarget.Machine);
        return string.IsNullOrWhiteSpace(machine) ? string.Empty : machine;
    }

    Target SetCredentials => _ => _
        .Executes(() =>
        {
            Assert.True(EnvironmentInfo.IsWin,
                "SetCredentials persists to the per-user Windows environment (HKCU), so it only runs on Windows.");
            Assert.True(!Console.IsInputRedirected,
                "SetCredentials prompts interactively — run it directly in a terminal, not through a redirected or CI context.");

            foreach (var credential in RequiredCredentials)
            {
                var value = PromptForCredential(credential);
                Environment.SetEnvironmentVariable(credential.EnvVariable, value, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(credential.EnvVariable, value);
                Log.Information("Persisted {Var} for the current user", credential.EnvVariable);
            }

            Log.Information("Persisted {Count} credential(s). Restart any running terminals/IDEs to pick them up.",
                RequiredCredentials.Count);
        });

    string PromptForCredential(CloudCredential credential)
    {
        while (true)
        {
            Console.Write($"{credential.EnvVariable} ({credential.Purpose}): ");
            var value = ReadHiddenLine().Trim();
            Console.WriteLine();
            if (value.Length > 0) return value;
            Console.WriteLine("  Nothing entered — a paste may not have worked. Please try again.");
        }
    }

    string ReadHiddenLine()
    {
        var builder = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) return builder.ToString();
            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }
            if (char.IsControl(key.KeyChar)) continue;
            builder.Append(key.KeyChar);
            Console.Write('*');
        }
    }

    Target RunDesktop => _ => _
        .DependsOn(Compile, CheckCredentials)
        .Executes(() =>
        {
            Assert.True(EnvironmentInfo.IsWin, "RunDesktop targets the Windows desktop head.");
            DotNetRun(s => s
                .SetProjectFile(DesktopProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target DeployAndroid => _ => _
        .DependsOn(CheckCredentials)
        .Executes(() =>
        {
            DotNet($"build \"{AndroidProject}\" --configuration {Configuration} -t:Install",
                workingDirectory: RootDirectory);
        });

    Target BuildApk => _ => _
        .DependsOn(CheckCredentials)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(AndroidProject)
                .SetConfiguration(Configuration));

            var apks = AndroidProject.Parent
                .GlobFiles($"bin/{Configuration}/**/*-Signed.apk", $"bin/{Configuration}/**/*.apk");
            foreach (var apk in apks.Distinct())
                Log.Information("APK: {Apk}", apk);
        });

    IReadOnlyList<string> ChangedMutableSourceFiles()
    {
        IEnumerable<string> Lines(string arguments) =>
            Git(arguments, workingDirectory: RootDirectory, logOutput: false)
                .Where(o => o.Type == OutputType.Std)
                .Select(o => o.Text);

        return Lines($"diff --name-only {Since}")
            .Concat(Lines("ls-files --others --exclude-standard"))
            .Select(p => p.Trim().Replace('\\', '/'))
            .Where(p => p.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                        && p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        && !IsExcludedFromMutation(p))
            .Distinct()
            .ToList();
    }

    bool IsExcludedFromMutation(string path) =>
        path.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/Views/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/Controls/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/DI/", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/CloudModule.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/InventoryDbContext.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/MainWindowViewModel.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/ThemeService.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/AppLogger.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("/DialogService.cs", StringComparison.OrdinalIgnoreCase);

    record CloudCredential(string EnvVariable, string Purpose);
}
