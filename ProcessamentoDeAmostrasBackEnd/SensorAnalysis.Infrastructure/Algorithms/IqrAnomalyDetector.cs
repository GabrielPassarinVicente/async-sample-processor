using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Infrastructure.Algorithms;

public class IqrAnomalyDetector : IAnomalyDetector
{
    public void Detect(List<SensorSample> validSamples, Dictionary<string, SampleAnalysis> analysisResults)
    {
        if (validSamples.Count < 4) return;

        var temperatures = validSamples.Select(s => s.Temperature!.Value).ToList();
        var tempBounds = CalculateBounds(temperatures);

        var humidities = validSamples.Select(s => s.Humidity!.Value).ToList();
        var humBounds = CalculateBounds(humidities);

        foreach (var sample in validSamples)
        {
            string key = $"{sample.SensorId}_{sample.Timestamp:O}";
            var analysis = analysisResults[key];

            bool isTempAnomaly = sample.Temperature < tempBounds.Lower || sample.Temperature > tempBounds.Upper;
            bool isHumAnomaly = sample.Humidity < humBounds.Lower || sample.Humidity > humBounds.Upper;

            if (isTempAnomaly || isHumAnomaly)
            {
                analysis.MarkAsAnomaly();
            }
        }
    }

    private (double Lower, double Upper) CalculateBounds(List<double> values)
    {
        values.Sort();
        int n = values.Count;

        double q1 = values[n / 4];
        double q3 = values[(n * 3) / 4];

        double iqr = q3 - q1;

        double lowerBound = q1 - 1.5 * iqr;
        double upperBound = q3 + 1.5 * iqr;

        return (lowerBound, upperBound);
    }
}
