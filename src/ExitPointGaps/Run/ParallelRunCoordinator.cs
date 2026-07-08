// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Executes per-project work with bounded parallelism.</summary>
internal static class ParallelRunCoordinator
{
    private static readonly object _StderrLock = new();

    /// <summary>Runs all items with bounded concurrency and accumulates summaries.</summary>
    /// <typeparam name="TRecord">Project record type.</typeparam>
    /// <param name="items">Items to process.</param>
    /// <param name="runOneAsync">Per-item worker.</param>
    /// <param name="maxParallelism">Maximum concurrent workers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Thread-safe summary accumulator.</returns>
    public static async Task<RunSummaryAccumulator> RunAllAsync<TRecord>(
        IReadOnlyList<TRecord> items,
        Func<TRecord, CancellationToken, Task<ProjectRunOutcome?>> runOneAsync,
        int maxParallelism,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(runOneAsync);

        if (maxParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxParallelism), "Parallelism must be at least 1.");
        }

        RunSummaryAccumulator accumulator = new();
        if (items.Count == 0)
        {
            return accumulator;
        }

        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
        Task[] tasks = new Task[items.Count];

        for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            TRecord item = items[itemIndex];
            tasks[itemIndex] = _RunOneAsync(item, runOneAsync, semaphore, accumulator, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return accumulator;
    }

    /// <summary>Writes a synchronized stderr line.</summary>
    /// <param name="message">Message text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WriteErrorLineAsync(string message, CancellationToken cancellationToken)
    {
        lock (_StderrLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.Error.WriteLine(message);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task _RunOneAsync<TRecord>(
        TRecord item,
        Func<TRecord, CancellationToken, Task<ProjectRunOutcome?>> runOneAsync,
        SemaphoreSlim semaphore,
        RunSummaryAccumulator accumulator,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ProjectRunOutcome? outcome = await runOneAsync(item, cancellationToken).ConfigureAwait(false);
            if (outcome is null)
            {
                await WriteErrorLineAsync("Project run returned no outcome.", cancellationToken).ConfigureAwait(false);
                accumulator.AddProject(new ProjectSummaryEntry(
                    string.Empty,
                    null,
                    false,
                    0,
                    0,
                    1,
                    null,
                    true));
                return;
            }

            if (outcome.Entry is not null)
            {
                accumulator.AddProject(outcome.Entry);
            }

            if (!string.IsNullOrEmpty(outcome.Error))
            {
                await WriteErrorLineAsync(outcome.Error, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}
