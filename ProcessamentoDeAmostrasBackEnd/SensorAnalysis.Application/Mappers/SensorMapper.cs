using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Application.Mappers;

public static class SensorMapper
{
    public static AnalyzedSampleDto ToDto(SensorSample sample, SampleAnalysis analysis)
    {
        return new AnalyzedSampleDto
        {
            SensorId = sample.SensorId,
            Type = sample.Type,
            Timestamp = sample.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Temperature = sample.Temperature,
            Humidity = sample.Humidity,
            DewPoint = sample.DewPoint,
            Analysis = new AnalysisDto
            {
                Temperature = MapMetric(analysis.Temperature),
                Humidity = MapMetric(analysis.Humidity),
                DewPoint = MapMetric(analysis.DewPoint),
                Anomaly = new AnomalyDto 
                { 
                    Status = analysis.AnomalyStatus.ToString().ToLowerInvariant() 
                }
            }
        };
    }

    private static MetricDto MapMetric(MetricAnalysis metric)
    {
        return new MetricDto
        {
            Status = metric.Status.ToString().ToLowerInvariant(),
            LimitType = metric.LimitType?.ToString().ToLowerInvariant(),
            ThresholdValue = metric.ThresholdValue
        };
    }
}
