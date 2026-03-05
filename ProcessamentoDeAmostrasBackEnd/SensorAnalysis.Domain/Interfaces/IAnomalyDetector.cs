using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Domain.Interfaces;

public interface IAnomalyDetector
{
    void Detect(List<SensorSample> validSamples, Dictionary<string, SampleAnalysis> analysisResults);
}