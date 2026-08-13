using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class FakeJobProcessingQueue : IJobProcessingQueue
{
    public List<ProcessingJob> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(job);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used in Application-layer tests.");
}
