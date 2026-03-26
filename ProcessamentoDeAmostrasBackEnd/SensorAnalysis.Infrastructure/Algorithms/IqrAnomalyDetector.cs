using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Algorithms;

internal sealed class IqrAnomalyDetector : IAnomalyDetector
{
    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples)
    {
        var anomalies = new HashSet<string>();

        if (validSamples.Count < 4) return anomalies;

        var temperatures = validSamples.Select(s => s.Temperature!.Value).ToList();
        var tempBounds = CalculateBounds(temperatures);

        var humidities = validSamples.Select(s => s.Humidity!.Value).ToList();
        var humBounds = CalculateBounds(humidities);

        foreach (var sample in validSamples)
        {
            bool isTempAnomaly = sample.Temperature < tempBounds.Lower || sample.Temperature > tempBounds.Upper;
            bool isHumAnomaly = sample.Humidity < humBounds.Lower || sample.Humidity > humBounds.Upper;

            if (isTempAnomaly || isHumAnomaly)
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
