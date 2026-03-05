using System.Text.Json.Serialization;

namespace SensorAnalysis.Application.DTOs;

public class DownloadResultDto
{
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("total_samples")]
    public int TotalSamples { get; set; }

    [JsonPropertyName("processed_samples")]
    public int ProcessedSamples { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime CompletedAt { get; set; }

    [JsonPropertyName("results")]
    public List<AnalyzedSampleDto> Results { get; set; } = new();
}
