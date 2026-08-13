using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class DownloadResultsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithBlankJobId_ReturnsInvalidJobId()
    {
        var service = new DownloadResultsService(new FakeJobRepository());

        var result = await service.ExecuteAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_JOB_ID", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownJobId_ReturnsJobNotFound()
    {
        var service = new DownloadResultsService(new FakeJobRepository());

        var result = await service.ExecuteAsync("missing-job");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedJob_ReturnsJobFailed()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-1", 1);
        job.MarkAsFailed("boom");
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-1");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_FAILED", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithProcessingJob_ReturnsJobNotCompleted()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-2", 1);
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-2");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_COMPLETED", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedJobWithoutResults_ReturnsNoResults()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-3", 1);
        job.Process(new List<SensorSample>(), new SensorEvaluator(), new StubAnomalyDetector());
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-3");

        Assert.True(result.IsFailure);
        Assert.Equal("NO_RESULTS", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedJobWithResults_ReturnsSuccess()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-4", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-4");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Results);
    }
}
