using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Domain.Interfaces;

public interface IAnomalyDetector
{
    IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples);
}
