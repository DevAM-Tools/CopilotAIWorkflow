// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Run;

/// <summary>Tests for <see cref="ParallelRunCoordinator"/>.</summary>
public sealed class ParallelRunCoordinatorTests
{
    [Test]
    public async Task RunAll_CompletesAllTasks_WithBoundedParallelism()
    {
        int maxConcurrent = 0;
        int currentConcurrent = 0;
        int enteredCount = 0;
        int itemCount = 12;
        int maxParallelism = 4;
        TaskCompletionSource releaseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource allSlotsEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<RunSummaryAccumulator> runTask = ParallelRunCoordinator.RunAllAsync(
            Enumerable.Range(0, itemCount).ToList(),
            async (item, cancellationToken) =>
            {
                int started = Interlocked.Increment(ref currentConcurrent);
                int observedMax = Volatile.Read(ref maxConcurrent);
                while (started > observedMax)
                {
                    Interlocked.CompareExchange(ref maxConcurrent, started, observedMax);
                    observedMax = Volatile.Read(ref maxConcurrent);
                }

                if (Interlocked.Increment(ref enteredCount) == maxParallelism)
                {
                    allSlotsEntered.TrySetResult();
                }

                await releaseGate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                Interlocked.Decrement(ref currentConcurrent);

                ProjectSummaryEntry entry = new ProjectSummaryEntry(
                    Invariant($"project-{item}"),
                    null,
                    true,
                    0,
                    0,
                    0,
                    Invariant($"project-{item}.json"),
                    false);
                return new ProjectRunOutcome(entry, null);
            },
            maxParallelism,
            CancellationToken.None);

        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        bool reachedCap = true;
        try
        {
            await allSlotsEntered.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            reachedCap = false;
        }

        releaseGate.TrySetResult();
        RunSummaryAccumulator accumulator = await runTask.ConfigureAwait(false);

        await Assert.That(reachedCap).IsTrue();
        await Assert.That(accumulator.GetEntries().Count).IsEqualTo(itemCount);
        await Assert.That(maxConcurrent).IsLessThanOrEqualTo(maxParallelism);
        await Assert.That(maxConcurrent).IsGreaterThan(1);
    }

    [Test]
    public async Task RunAll_FirstFailure_RecordsToolFailure()
    {
        List<int> items = [1, 2, 3];
        RunSummaryAccumulator accumulator = await ParallelRunCoordinator.RunAllAsync(
            items,
            async (item, cancellationToken) =>
            {
                await Task.CompletedTask.ConfigureAwait(false);
                if (item == 2)
                {
                    return new ProjectRunOutcome(
                        new ProjectSummaryEntry("p2", null, false, 0, 0, 1, null, true),
                        "failed");
                }

                return new ProjectRunOutcome(
                    new ProjectSummaryEntry(Invariant($"p{item}"), null, true, 0, 0, 0, null, false),
                    null);
            },
            2,
            CancellationToken.None);

        await Assert.That(accumulator.HasFailure).IsTrue();
        await Assert.That(accumulator.GetEntries().Count).IsEqualTo(3);
    }
}
