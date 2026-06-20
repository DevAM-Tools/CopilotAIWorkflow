// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool.Tests;

/// <summary>Tests for <see cref="RunIsolation"/> and <see cref="CoberturaPathFinder"/>.</summary>
public sealed class TestRunnerTests
{
    [Test]
    public async Task RunIsolation_DefaultWorkDirectories_AreUnique()
    {
        string first = RunIsolation.CreateDefaultWorkDirectory();
        string second = RunIsolation.CreateDefaultWorkDirectory();

        await Assert.That(first).IsNotEqualTo(second);
        await Assert.That(Directory.Exists(first)).IsTrue();
        await Assert.That(Directory.Exists(second)).IsTrue();
    }

    [Test]
    public async Task RunIsolation_RelativeOutput_ResolvesUnderWorkDirectory()
    {
        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        string? resolved = RunIsolation.ResolveOutputPath("reports/out.json", workDirectory);

        await Assert.That(resolved).IsEqualTo(Path.GetFullPath(Path.Combine(workDirectory, "reports", "out.json")));
    }

    [Test]
    public async Task CoberturaPathFinder_PicksNewestInDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"cobertura-find-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string older = Path.Combine(root, "older.cobertura.xml");
        string newer = Path.Combine(root, "nested", "newer.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(newer)!);
        await File.WriteAllTextAsync(older, "<coverage><packages /></coverage>");
        await File.WriteAllTextAsync(newer, "<coverage><packages /></coverage>");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-5));

        bool success = CoberturaPathFinder.TryFindNewest(root, out string? path, out string? _);

        await Assert.That(success).IsTrue();
        await Assert.That(path).IsEqualTo(newer);
    }

    [Test]
    public async Task RunIsolation_TryCleanupWorkDirectory_DeletesWhenNotKept()
    {
        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        await Assert.That(RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _)).IsTrue();

        RunIsolation.TryCleanupWorkDirectory(workDirectory, keepWorkDir: false);

        await Assert.That(Directory.Exists(workDirectory)).IsFalse();
    }

    [Test]
    public async Task TryReserveWorkDirectory_RejectsActiveLock()
    {
        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        await Assert.That(RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _)).IsTrue();

        bool second = RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out string? error);

        await Assert.That(second).IsFalse();
        await Assert.That(error).IsNotNull();
        RunIsolation.ReleaseWorkDirectory(workDirectory);
    }

    [Test]
    public async Task TryReserveWorkDirectory_AllowsReuseAfterRelease()
    {
        string workDirectory = RunIsolation.CreateDefaultWorkDirectory();
        await Assert.That(RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _)).IsTrue();
        RunIsolation.ReleaseWorkDirectory(workDirectory);

        await Assert.That(RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _)).IsTrue();
        RunIsolation.ReleaseWorkDirectory(workDirectory);
    }

    [Test]
    public async Task TryReserveWorkDirectory_ExplicitPath_OnlyOneParallelReservationSucceeds()
    {
        string workDirectory = Path.Combine(Path.GetTempPath(), $"gap-lock-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);

        int successCount = 0;
        await Task.WhenAll(
            Task.Run(() =>
            {
                if (RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _))
                {
                    Interlocked.Increment(ref successCount);
                }
            }),
            Task.Run(() =>
            {
                if (RunIsolation.TryReserveWorkDirectory(workDirectory, out _, out _))
                {
                    Interlocked.Increment(ref successCount);
                }
            }));

        await Assert.That(successCount).IsEqualTo(1);
        RunIsolation.ReleaseWorkDirectory(workDirectory);
        if (Directory.Exists(workDirectory))
        {
            Directory.Delete(workDirectory, recursive: true);
        }
    }
}
