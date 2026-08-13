using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Algorithms;

internal sealed class IqrAnomalyDetector : IAnomalyDetector
{
    private static readonly (string Name, Func<SensorSample, double> Select)[] Metrics =
    [
        ("Temperature", s => s.Temperature!.Value),
        ("Humidity",    s => s.Humidity!.Value),
        ("DewPoint",    s => s.DewPoint!.Value),
    ];

    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples)
    {
        var anomalies = new HashSet<string>();

        if (validSamples.Count < 4) return anomalies;

        var boundsByMetric = Metrics.ToDictionary(
            m => m.Name,
            m => CalculateBounds(validSamples.Select(m.Select).ToList()));

        foreach (var sample in validSamples)
        {
            bool isAnomaly = Metrics.Any(m =>
            {
                var value = m.Select(sample);
                var bounds = boundsByMetric[m.Name];
                return value < bounds.Lower || value > bounds.Upper;
            });

            if (isAnomaly)
                anomalies.Add($"{sample.SensorId}_{sample.Timestamp:O}");
        }

        return anomalies;
    }

    private static (double Lower, double Upper) CalculateBounds(List<double> values)
    {
        values.Sort();
        int n = values.Count;

        double q1 = values[n / 4];
        double q3 = values[(n * 3) / 4];
        double iqr = q3 - q1;

        return (q1 - 1.5 * iqr, q3 + 1.5 * iqr);
    }
}
