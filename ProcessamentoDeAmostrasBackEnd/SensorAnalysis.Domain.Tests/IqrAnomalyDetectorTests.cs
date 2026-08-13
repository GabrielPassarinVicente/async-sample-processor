using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Infrastructure.Algorithms;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class IqrAnomalyDetectorTests
{
    private readonly IqrAnomalyDetector _detector = new();

    [Fact]
    public void DetectAnomalies_WithFewerThanFourSamples_ReturnsEmpty()
    {
        var samples = new List<SensorSample>
        {
            SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10),
        };

        var anomalies = _detector.DetectAnomalies(samples);

        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectAnomalies_WithDewPointOutlier_FlagsSample()
    {
        var baseline = new[] { 10.0, 10.5, 11.0, 10.2, 10.8, 10.4 };
        var samples = new List<SensorSample>();

        for (int i = 0; i < baseline.Length; i++)
            samples.Add(SensorSample.Create($"s{i}", "env", DateTime.UtcNow.AddMinutes(i), 20, 50, baseline[i]));

        var outlierTimestamp = DateTime.UtcNow.AddMinutes(99);
        var outlier = SensorSample.Create("s-outlier", "env", outlierTimestamp, 20, 50, 90.0);
        samples.Add(outlier);

        var anomalies = _detector.DetectAnomalies(samples);

        Assert.Contains($"s-outlier_{outlierTimestamp:O}", anomalies);
    }
}
