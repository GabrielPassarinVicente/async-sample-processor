using System.Text;
using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Application.Services;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class ProcessSensorFileServiceTests
{
    private static Stream JsonStream(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task StartAsync_WithValidFile_EnqueuesJobAndReturnsJobId()
    {
        var repository = new FakeJobRepository();
        var queue = new FakeJobProcessingQueue();
        var service = new ProcessSensorFileService(new SensorFileParser(), repository, queue);

        const string json = """
        [
            { "sensor_id": "s1", "type": "env", "timestamp": "2026-01-01T00:00:00Z", "temperature": 20, "humidity": 50, "dew_point": 10 }
        ]
        """;

        var result = await service.StartAsync(JsonStream(json));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
        Assert.Single(queue.Enqueued);
        Assert.Equal(result.Value, queue.Enqueued[0].Job.JobId);
        Assert.NotNull(repository.Stored(result.Value!));
    }

    [Fact]
    public async Task StartAsync_WithEmptyFile_ReturnsFailureWithoutEnqueueing()
    {
        var repository = new FakeJobRepository();
        var queue = new FakeJobProcessingQueue();
        var service = new ProcessSensorFileService(new SensorFileParser(), repository, queue);

        var result = await service.StartAsync(JsonStream("[]"));

        Assert.True(result.IsFailure);
        Assert.Equal("EMPTY_FILE", result.Error!.Code);
        Assert.Empty(queue.Enqueued);
    }
}
