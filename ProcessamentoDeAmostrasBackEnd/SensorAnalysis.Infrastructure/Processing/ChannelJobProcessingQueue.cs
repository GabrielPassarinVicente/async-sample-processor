using System.Threading.Channels;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Processing;

internal sealed class ChannelJobProcessingQueue : IJobProcessingQueue
{
    private readonly Channel<ProcessingJob> _channel = Channel.CreateUnbounded<ProcessingJob>();

    public ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
