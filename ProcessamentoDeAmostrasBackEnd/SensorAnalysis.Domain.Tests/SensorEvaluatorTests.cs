using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class SensorEvaluatorTests
{
    private readonly SensorEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_WithMissingMetric_ReturnsInvalid()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, null, 50, 10);

        var analysis = _evaluator.Evaluate(sample);

        Assert.True(analysis.IsInvalid());
    }

    [Fact]
    public void Evaluate_WithinNormalRange_ReturnsNormal()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Normal, analysis.Temperature.Status);
        Assert.Equal(StatusLevel.Normal, analysis.Humidity.Status);
        Assert.Equal(StatusLevel.Normal, analysis.DewPoint.Status);
    }

    [Fact]
    public void Evaluate_AboveAlertMaxBelowCriticalMax_ReturnsAlert()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 32, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Alert, analysis.Temperature.Status);
        Assert.Equal(LimitType.Max, analysis.Temperature.LimitType);
    }

    [Fact]
    public void Evaluate_AboveCriticalMax_ReturnsCritical()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 40, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Critical, analysis.Temperature.Status);
        Assert.Equal(LimitType.Max, analysis.Temperature.LimitType);
    }
}
