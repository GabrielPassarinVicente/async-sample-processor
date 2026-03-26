using System.Text.Json;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Application.Services;

public class SensorFileParser
{
    public async Task<Result<List<SensorSample>>> ParseAsync(Stream fileStream)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var rawData = await JsonSerializer.DeserializeAsync<List<SensorSampleDto>>(fileStream, options);

            if (rawData == null || rawData.Count == 0)
                return Result<List<SensorSample>>.Failure(ApplicationErrors.EmptyFile);

            var samples = new List<SensorSample>();

            foreach (var dto in rawData)
            {
                try
                {
                    var sample = SensorSample.Create(
                        dto.SensorId ?? string.Empty,
                        dto.Type ?? string.Empty,
                        dto.Timestamp,
                        dto.Temperature,
                        dto.Humidity,
                        dto.DewPoint
                    );
                    samples.Add(sample);
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }

            if (samples.Count == 0)
                return Result<List<SensorSample>>.Failure(ApplicationErrors.EmptyFile);

            return Result<List<SensorSample>>.Success(samples);
        }
        catch (JsonException)
        {
            return Result<List<SensorSample>>.Failure(ApplicationErrors.InvalidFormat);
        }
    }
}

internal class SensorSampleDto
{
    public string? SensorId { get; set; }
    public string? Type { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? DewPoint { get; set; }
}

