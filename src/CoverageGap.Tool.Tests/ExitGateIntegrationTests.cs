// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>End-to-end exit-point gate tests for <see cref="Program"/>.</summary>
public sealed class ExitGateIntegrationTests
{
    [Test]
    public async Task ExitGate_PartialCoverage_ReportsExitGapAndFailsWithoutNoFail()
    {
        await using TempGapSample sample = await TempGapSample.CreateAsync(includeSecondReturnTest: false);

        int reportExitCode = await _RunReportAsync(sample, sample.ReportOutputPath);
        int exitGapCount = await _ReadExitGapCountAsync(sample.ReportOutputPath);

        await Assert.That(reportExitCode).IsEqualTo(1);
        await Assert.That(exitGapCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExitGate_FullCoverage_PassesExitGate()
    {
        await using TempGapSample sample = await TempGapSample.CreateAsync(includeSecondReturnTest: true);

        int reportExitCode = await _RunReportAsync(sample, sample.ReportOutputPath);
        int exitGapCount = await _ReadExitGapCountAsync(sample.ReportOutputPath);

        await Assert.That(reportExitCode).IsEqualTo(0);
        await Assert.That(exitGapCount).IsEqualTo(0);
    }

    private static async Task<int> _RunReportAsync(TempGapSample sample, string outputPath)
    {
        return await Program.Main(
        [
            "report",
            "project",
            sample.LibraryProjectPath,
            "--cobertura",
            sample.CoberturaPath,
            "--repo-root",
            sample.RootPath,
            "--scope",
            "GapSample",
            "--no-build",
            "--format",
            "agent",
            "-o",
            outputPath,
        ]);
    }

    private static async Task<int> _ReadExitGapCountAsync(string reportPath)
    {
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        return document.RootElement.GetProperty("summary").GetProperty("exitGapCount").GetInt32();
    }

    private sealed class TempGapSample : IAsyncDisposable
    {
        private TempGapSample(string rootPath, string libraryProjectPath, string coberturaPath, string reportOutputPath)
        {
            RootPath = rootPath;
            LibraryProjectPath = libraryProjectPath;
            CoberturaPath = coberturaPath;
            ReportOutputPath = reportOutputPath;
        }

        public string RootPath { get; }

        public string LibraryProjectPath { get; }

        public string CoberturaPath { get; }

        public string ReportOutputPath { get; }

        public static async Task<TempGapSample> CreateAsync(bool includeSecondReturnTest)
        {
            string repositoryRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            string rootPath = Path.Combine(Path.GetTempPath(), $"exit-gate-{Guid.NewGuid():N}");
            string libraryDirectory = Path.Combine(rootPath, "GapSample");
            string testsDirectory = Path.Combine(rootPath, "GapSample.Tests");
            Directory.CreateDirectory(libraryDirectory);
            Directory.CreateDirectory(testsDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(rootPath, "global.json"),
                await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));

            string libraryProjectPath = Path.Combine(libraryDirectory, "GapSample.csproj");
            string testsProjectPath = Path.Combine(testsDirectory, "GapSample.Tests.csproj");
            await File.WriteAllTextAsync(libraryProjectPath, _LibraryProjectXml);
            await File.WriteAllTextAsync(testsProjectPath, _TestsProjectXml);
            await File.WriteAllTextAsync(Path.Combine(libraryDirectory, "Calculator.cs"), _CalculatorSource);
            await File.WriteAllTextAsync(
                Path.Combine(testsDirectory, "CalculatorTests.cs"),
                includeSecondReturnTest ? _FullCoverageTestsSource : _PartialCoverageTestsSource);
            await File.WriteAllTextAsync(
                Path.Combine(testsDirectory, "GlobalUsings.cs"),
                """
                global using TUnit.Assertions;
                """);

            int restoreCode = await _RunDotNetAsync($"restore \"{testsProjectPath}\"", rootPath);
            await Assert.That(restoreCode).IsEqualTo(0);

            int buildCode = await _RunDotNetAsync($"build \"{testsProjectPath}\" -c Release --no-restore", rootPath);
            await Assert.That(buildCode).IsEqualTo(0);

            await _RunDotNetAsync(
                $"test \"{testsProjectPath}\" -c Release --no-build -- --reflection --coverage --coverage-output-format cobertura",
                rootPath);

            string coberturaPath = Directory
                .EnumerateFiles(testsDirectory, "*.cobertura.xml", SearchOption.AllDirectories)
                .OrderByDescending(static path => File.GetLastWriteTimeUtc(path))
                .First();

            return new TempGapSample(
                rootPath,
                libraryProjectPath,
                coberturaPath,
                Path.Combine(rootPath, "report.json"));
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }

        private static async Task<int> _RunDotNetAsync(string arguments, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using Process process = Process.Start(startInfo)!;
            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                string details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new InvalidOperationException($"dotnet {arguments} failed ({process.ExitCode}): {details}");
            }

            return process.ExitCode;
        }

        private const string _LibraryProjectXml =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>14</LangVersion>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """;

        private const string _TestsProjectXml =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="TUnit" Version="1.54.0" />
                <PackageReference Include="coverlet.collector" Version="6.0.4">
                  <PrivateAssets>all</PrivateAssets>
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                </PackageReference>
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="..\GapSample\GapSample.csproj" />
              </ItemGroup>
            </Project>
            """;

        private const string _CalculatorSource =
            """
            namespace GapSample;

            public sealed class Calculator
            {
                public int Pick(bool useFirst)
                {
                    if (useFirst)
                    {
                        return 1;
                    }

                    return 2;
                }
            }
            """;

        private const string _PartialCoverageTestsSource =
            """
            namespace GapSample.Tests;

            public sealed class CalculatorTests
            {
                [Test]
                public async Task Pick_UseFirst_ReturnsOne()
                {
                    Calculator calculator = new Calculator();
                    await Assert.That(calculator.Pick(true)).IsEqualTo(1);
                }
            }
            """;

        private const string _FullCoverageTestsSource =
            """
            namespace GapSample.Tests;

            public sealed class CalculatorTests
            {
                [Test]
                public async Task Pick_UseFirst_ReturnsOne()
                {
                    Calculator calculator = new Calculator();
                    await Assert.That(calculator.Pick(true)).IsEqualTo(1);
                }

                [Test]
                public async Task Pick_UseSecond_ReturnsTwo()
                {
                    Calculator calculator = new Calculator();
                    await Assert.That(calculator.Pick(false)).IsEqualTo(2);
                }
            }
            """;
    }
}
