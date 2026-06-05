using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build — default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Parameter("Minimum acceptable line coverage percentage for the Coverage gate")]
    readonly double CoverageThreshold = 95;

    AbsolutePath TestProjectsRoot => RootDirectory / "tests";
    AbsolutePath CoverageDirectory => RootDirectory / "TestResults" / "coverage";
    AbsolutePath CoverageReportDirectory => RootDirectory / "TestResults" / "CoverageReport";
    AbsolutePath CoverageSettings => RootDirectory / "coverlet.runsettings";

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
            foreach (var project in TestProjects)
                DotNetTest(s => s
                    .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild());
        });

    Target Coverage => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            CoverageReportDirectory.CreateOrCleanDirectory();

            foreach (var project in TestProjects)
                DotNetTest(s => s
                    .SetProjectFile(TestProjectsRoot / project / $"{project}.csproj")
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetDataCollector("XPlat Code Coverage")
                    .SetSettingsFile(CoverageSettings)
                    .SetResultsDirectory(CoverageDirectory));

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
        .DependsOn(Test)
        .Executes(() =>
        {
            foreach (var project in new[] { "Collectary.Core", "Collectary.Infrastructure", "Collectary.Infrastructure.Cloud", "Collectary.Presentation" })
                DotNet($"stryker -p \"{RootDirectory / "src" / project / $"{project}.csproj"}\"",
                    workingDirectory: RootDirectory);
        });
}
