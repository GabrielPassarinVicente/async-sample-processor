using System.Text.Json.Serialization;
using SensorAnalysis.Domain.Enums;

namespace SensorAnalysis.Application.DTOs;

public class AnalyzedSampleDto
{
    [JsonPropertyName("sensor_id")]
    public string SensorId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("dew_point")]
    public double? DewPoint { get; set; }

    [JsonPropertyName("analysis")]
    public AnalysisDto Analysis { get; set; } = new();
}

public class AnalysisDto
{
    [JsonPropertyName("temperature")]
    public MetricDto Temperature { get; set; } = new();

    [JsonPropertyName("humidity")]
    public MetricDto Humidity { get; set; } = new();

    [JsonPropertyName("dew_point")]
    public MetricDto DewPoint { get; set; } = new();

    [JsonPropertyName("anomaly")]
    public AnomalyDto Anomaly { get; set; } = new();
}

public class MetricDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "normal";

    [JsonPropertyName("limit_type")]
    public string? LimitType { get; set; }

    [JsonPropertyName("threshold_value")]
    public double? ThresholdValue { get; set; }
}

public class AnomalyDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "normal";
}

