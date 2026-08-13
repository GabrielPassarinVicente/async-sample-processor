using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

internal sealed class StubAnomalyDetector : IAnomalyDetector
{
    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples) => new HashSet<string>();
}

public class JobStatusTests
{
    [Fact]
    public void Create_WithBlankJobId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JobStatus.Create("  ", 1));
    }

    [Fact]
    public void Create_WithNonPositiveTotalSamples_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JobStatus.Create("job-1", 0));
    }

    [Fact]
    public void Process_WhenNotInProcessingState_ThrowsInvalidJobOperationException()
    {
        var job = JobStatus.Create("job-2", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.Throws<InvalidJobOperationException>(
            () => job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector()));
    }

    [Fact]
    public void Process_CompletesJobAndTracksProgress()
    {
        var job = JobStatus.Create("job-3", 2);
        var samples = new List<SensorSample>
        {
            SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10),
            SensorSample.Create("s2", "env", DateTime.UtcNow, 21, 51, 11),
        };

        job.Process(samples, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.True(job.IsCompleted);
        Assert.Equal(2, job.ProcessedSamples);
        Assert.Equal(100, job.GetProgressPercentage());
        Assert.True(job.CanDownload());
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyCompleted_ThrowsInvalidJobOperationException()
    {
        var job = JobStatus.Create("job-4", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.Throws<InvalidJobOperationException>(() => job.MarkAsFailed("boom"));
    }

    [Fact]
    public void MarkAsFailed_WhenProcessing_SetsFailedStateAndMessage()
    {
        var job = JobStatus.Create("job-5", 1);

        job.MarkAsFailed("boom");

        Assert.True(job.IsFailed);
        Assert.Equal("boom", job.ErrorMessage);
    }
}
