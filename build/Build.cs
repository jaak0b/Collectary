using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

                var patterns = string.Join(" ", relative.Select(f => $"--mutate \"{f}\""));
                DotNet($"stryker -p \"{RootDirectory / "src" / project / $"{project}.csproj"}\" {patterns}",
                    workingDirectory: RootDirectory);
            }
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
}
