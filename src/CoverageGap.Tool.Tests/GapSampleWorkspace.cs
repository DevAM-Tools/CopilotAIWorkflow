// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>Shared temporary GapSample workspace for CLI integration tests.</summary>
internal sealed class GapSampleWorkspace : IAsyncDisposable
{
    private GapSampleWorkspace(
        string rootPath,
        string libraryProjectPath,
        string testsProjectPath,
        string workDirectory)
    {
        RootPath = rootPath;
        LibraryProjectPath = libraryProjectPath;
        TestsProjectPath = testsProjectPath;
        WorkDirectory = workDirectory;
    }

    public string RootPath { get; }

    public string LibraryProjectPath { get; }

    public string TestsProjectPath { get; }

    public string WorkDirectory { get; }

    public static async Task<GapSampleWorkspace> CreateAsync(
        bool includeSecondReturnTest,
        bool includeTestClass = true)
    {
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string rootPath = Path.Combine(Path.GetTempPath(), $"gap-sample-{Guid.NewGuid():N}");
        string libraryDirectory = Path.Combine(rootPath, "GapSample");
        string testsDirectory = Path.Combine(rootPath, "GapSample.Tests");
        Directory.CreateDirectory(libraryDirectory);
        Directory.CreateDirectory(testsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(rootPath, "global.json"),
            await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));

        string libraryProjectPath = Path.Combine(libraryDirectory, "GapSample.csproj");
        string testsProjectPath = Path.Combine(testsDirectory, "GapSample.Tests.csproj");
        await File.WriteAllTextAsync(libraryProjectPath, LibraryProjectXml);
        await File.WriteAllTextAsync(testsProjectPath, TestsProjectXml);
        await File.WriteAllTextAsync(Path.Combine(libraryDirectory, "Calculator.cs"), CalculatorSource);
        if (includeTestClass)
        {
            await File.WriteAllTextAsync(
                Path.Combine(testsDirectory, "CalculatorTests.cs"),
                includeSecondReturnTest ? FullCoverageTestsSource : _PartialCoverageTestsSource);
        }

        await File.WriteAllTextAsync(
            Path.Combine(testsDirectory, "GlobalUsings.cs"),
            """
            global using TUnit.Assertions;
            """);

        int restoreCode = await _RunDotNetAsync($"restore \"{testsProjectPath}\"", rootPath);
        await Assert.That(restoreCode).IsEqualTo(0);

        int buildCode = await _RunDotNetAsync($"build \"{testsProjectPath}\" -c Release --no-restore", rootPath);
        await Assert.That(buildCode).IsEqualTo(0);

        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        return new GapSampleWorkspace(rootPath, libraryProjectPath, testsProjectPath, workDirectory);
    }

    public static async Task<GapSampleWorkspace> CreateUnpairedLibraryAsync()
    {
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string rootPath = Path.Combine(Path.GetTempPath(), $"gap-unpaired-{Guid.NewGuid():N}");
        string libraryDirectory = Path.Combine(rootPath, "GapSample");
        Directory.CreateDirectory(libraryDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(rootPath, "global.json"),
            await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "global.json")));

        string libraryProjectPath = Path.Combine(libraryDirectory, "GapSample.csproj");
        await File.WriteAllTextAsync(libraryProjectPath, LibraryProjectXml);
        await File.WriteAllTextAsync(Path.Combine(libraryDirectory, "Calculator.cs"), CalculatorSource);

        int restoreCode = await _RunDotNetAsync($"restore \"{libraryProjectPath}\"", rootPath);
        await Assert.That(restoreCode).IsEqualTo(0);

        int buildCode = await _RunDotNetAsync($"build \"{libraryProjectPath}\" -c Release --no-restore", rootPath);
        await Assert.That(buildCode).IsEqualTo(0);

        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        return new GapSampleWorkspace(rootPath, libraryProjectPath, string.Empty, workDirectory);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }

        if (Directory.Exists(WorkDirectory))
        {
            Directory.Delete(WorkDirectory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    private static async Task<int> _RunDotNetAsync(string arguments, string workingDirectory)
    {
        DotNetProcessResult result = await DotNetProcess.RunAsync(arguments, workingDirectory);
        if (result.ExitCode != 0)
        {
            string details = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            throw new InvalidOperationException($"dotnet {arguments} failed ({result.ExitCode}): {details}");
        }

        return result.ExitCode;
    }

    internal const string LibraryProjectXml =
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

    internal const string TestsProjectXml =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="TUnit" Version="1.56.0" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\GapSample\GapSample.csproj" />
          </ItemGroup>
        </Project>
        """;

    internal const string CalculatorSource =
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

    internal const string FullCoverageTestsSource =
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
