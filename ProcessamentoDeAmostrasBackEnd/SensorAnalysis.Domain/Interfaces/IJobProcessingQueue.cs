using SensorAnalysis.Domain.Common;

namespace SensorAnalysis.Domain.Interfaces;

public interface IJobProcessingQueue
{
    ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default);
}
