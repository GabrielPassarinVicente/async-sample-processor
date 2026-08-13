using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Domain.Entities;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class GetJobStatusServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithBlankJobId_ReturnsInvalidJobId()
    {
        var service = new GetJobStatusService(new FakeJobRepository());

        var result = await service.ExecuteAsync("");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_JOB_ID", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownJobId_ReturnsJobNotFound()
    {
        var service = new GetJobStatusService(new FakeJobRepository());

        var result = await service.ExecuteAsync("missing-job");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithKnownJobId_ReturnsMappedStatus()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-5", 3);
        await repository.AddAsync(job);
        var service = new GetJobStatusService(repository);

        var result = await service.ExecuteAsync("job-5");

        Assert.True(result.IsSuccess);
        Assert.Equal("job-5", result.Value!.JobId);
        Assert.Equal("processing", result.Value!.Status);
        Assert.Equal(3, result.Value!.TotalSamples);
    }
}
