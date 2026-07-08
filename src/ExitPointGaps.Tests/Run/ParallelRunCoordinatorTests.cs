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
        int itemCount = 12;
        int maxParallelism = 4;

        RunSummaryAccumulator accumulator = await ParallelRunCoordinator.RunAllAsync(
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

                await Task.Delay(25, cancellationToken).ConfigureAwait(false);
                Interlocked.Decrement(ref currentConcurrent);

                ProjectSummaryEntry entry = new ProjectSummaryEntry(
                    $"project-{item}",
                    null,
                    true,
                    0,
                    0,
                    0,
                    $"project-{item}.json",
                    false);
                return new ProjectRunOutcome(entry, null);
            },
            maxParallelism,
            CancellationToken.None);

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
                    new ProjectSummaryEntry($"p{item}", null, true, 0, 0, 0, null, false),
                    null);
            },
            2,
            CancellationToken.None);

        await Assert.That(accumulator.HasToolFailure).IsTrue();
        await Assert.That(accumulator.GetEntries().Count).IsEqualTo(3);
    }
}
