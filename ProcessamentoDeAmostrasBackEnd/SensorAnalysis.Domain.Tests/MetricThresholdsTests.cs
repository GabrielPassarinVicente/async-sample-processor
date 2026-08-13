using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.ValueObjects;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class MetricThresholdsTests
{
    [Fact]
    public void Create_WithCriticalMinAboveAlertMin_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MetricThresholds.Create(alertMin: 10, alertMax: null, criticalMin: 15, criticalMax: null));
    }

    [Fact]
    public void Create_WithCriticalMaxBelowAlertMax_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MetricThresholds.Create(alertMin: null, alertMax: 30, criticalMin: null, criticalMax: 25));
    }

    [Fact]
    public void Create_WithConsistentBounds_Succeeds()
    {
        var thresholds = MetricThresholds.Create(alertMin: 15, alertMax: 30, criticalMin: 10, criticalMax: 35);

        Assert.Equal(15, thresholds.AlertMin);
        Assert.Equal(35, thresholds.CriticalMax);
    }
}
