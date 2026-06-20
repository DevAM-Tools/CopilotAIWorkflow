// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

/// <summary>Per-invocation isolated directories and output paths for parallel-safe runs.</summary>
internal static class RunIsolation
{
    private const string _LockFileName = ".coveragegap.lock";

    /// <summary>Creates or reserves a work directory for this invocation.</summary>
    /// <param name="requestedWorkDirectory">Optional user <c>--work-dir</c>; unique temp path when null.</param>
    /// <param name="workDirectory">Reserved absolute work directory path.</param>
    /// <param name="error">Error when reservation fails.</param>
    /// <returns><see langword="true"/> when the directory is reserved for this process.</returns>
    public static bool TryReserveWorkDirectory(string? requestedWorkDirectory, out string? workDirectory, out string? error)
    {
        workDirectory = null;
        error = null;

        if (string.IsNullOrWhiteSpace(requestedWorkDirectory))
        {
            workDirectory = CreateDefaultWorkDirectory();
            return _TryCreateLockFileExclusive(workDirectory, out error);
        }

        workDirectory = Path.GetFullPath(requestedWorkDirectory);
        Directory.CreateDirectory(workDirectory);
        return _TryCreateLockFileExclusive(workDirectory, out error);
    }

    /// <summary>Creates a unique work directory under the system temp folder.</summary>
    /// <returns>Absolute path to the created directory.</returns>
    public static string CreateDefaultWorkDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            CliConstants.WorkDirFolderName,
            $"{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Releases the work-directory lock file.</summary>
    /// <param name="workDirectory">Invocation work root.</param>
    public static void ReleaseWorkDirectory(string workDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);
        string lockPath = Path.Combine(workDirectory, _LockFileName);
        if (!File.Exists(lockPath))
        {
            return;
        }

        if (_TryReadLockOwnerPid(workDirectory, out int ownerPid) && ownerPid == Environment.ProcessId)
        {
            File.Delete(lockPath);
        }
    }

    /// <summary>Creates the test-results subdirectory for one production project.</summary>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <param name="productionProjectName">Production project file name without extension.</param>
    /// <returns>Absolute path to the test results directory.</returns>
    public static string CreateTestResultsDirectory(string workDirectory, string productionProjectName)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);
        ArgumentException.ThrowIfNullOrEmpty(productionProjectName);

        string path = Path.Combine(workDirectory, "test", productionProjectName);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Resolves an optional output path; relative paths are rooted under the work directory.</summary>
    /// <param name="outputPath">User-supplied <c>-o</c> value, or <see langword="null"/> for stdout-only.</param>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <returns>Absolute output path, or <see langword="null"/> when writing to stdout.</returns>
    public static string? ResolveOutputPath(string? outputPath, string workDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        if (Path.IsPathRooted(outputPath))
        {
            return Path.GetFullPath(outputPath);
        }

        return Path.GetFullPath(Path.Combine(workDirectory, outputPath));
    }

    /// <summary>Resolves the directory used for multi-file report output.</summary>
    /// <param name="outputPath">User <c>-o</c> path (file or directory).</param>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <returns>Directory that will receive per-project JSON files.</returns>
    public static string ResolveOutputDirectory(string? outputPath, string workDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return workDirectory;
        }

        string resolved = ResolveOutputPath(outputPath, workDirectory)!;
        if (resolved.EndsWith(Path.DirectorySeparatorChar)
            || resolved.EndsWith(Path.AltDirectorySeparatorChar)
            || Directory.Exists(resolved))
        {
            Directory.CreateDirectory(resolved);
            return resolved;
        }

        string? parent = Path.GetDirectoryName(resolved);
        if (string.IsNullOrEmpty(parent))
        {
            return workDirectory;
        }

        Directory.CreateDirectory(parent);
        return parent;
    }

    /// <summary>Returns the default summary file path inside the work directory.</summary>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <returns>Absolute path to <c>summary.json</c>.</returns>
    public static string DefaultSummaryPath(string workDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);
        return Path.Combine(workDirectory, CliConstants.SummaryFileName);
    }

    /// <summary>Releases the lock and optionally deletes the work directory.</summary>
    /// <param name="workDirectory">Invocation work root.</param>
    /// <param name="keepWorkDir">When <see langword="true"/>, only the lock file is removed.</param>
    public static void TryCleanupWorkDirectory(string workDirectory, bool keepWorkDir)
    {
        ArgumentException.ThrowIfNullOrEmpty(workDirectory);
        ReleaseWorkDirectory(workDirectory);

        if (keepWorkDir || !Directory.Exists(workDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(workDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool _TryCreateLockFileExclusive(string workDirectory, out string? error)
    {
        error = null;
        string lockPath = Path.Combine(workDirectory, _LockFileName);
        if (_TryWriteLockExclusive(lockPath, out error))
        {
            return true;
        }

        if (error is not null)
        {
            return false;
        }

        if (File.Exists(lockPath))
        {
            File.Delete(lockPath);
        }

        return _TryWriteLockExclusive(lockPath, out error);
    }

    private static bool _TryWriteLockExclusive(string lockPath, out string? error)
    {
        error = null;
        string payload = $"{Environment.ProcessId}{Environment.NewLine}{DateTimeOffset.UtcNow:O}";
        try
        {
            using FileStream stream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            stream.Write(bytes, 0, bytes.Length);
            return true;
        }
        catch (IOException)
        {
            string? directory = Path.GetDirectoryName(lockPath);
            if (string.IsNullOrEmpty(directory))
            {
                error = $"Failed to reserve work directory: {lockPath}";
                return false;
            }

            if (_TryReadLockOwnerPid(directory, out int ownerPid) && _IsProcessRunning(ownerPid))
            {
                error = $"Work directory is in use by another coveragegap instance (PID {ownerPid}): {directory}";
                return false;
            }

            return false;
        }
        catch (UnauthorizedAccessException accessException)
        {
            error = $"Failed to reserve work directory: {accessException.Message}";
            return false;
        }
    }

    private static bool _TryReadLockOwnerPid(string workDirectory, out int ownerPid)
    {
        ownerPid = 0;
        string lockPath = Path.Combine(workDirectory, _LockFileName);
        if (!File.Exists(lockPath))
        {
            return false;
        }

        string firstLine = File.ReadLines(lockPath).FirstOrDefault() ?? string.Empty;
        return int.TryParse(firstLine, out ownerPid);
    }

    private static bool _IsProcessRunning(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
