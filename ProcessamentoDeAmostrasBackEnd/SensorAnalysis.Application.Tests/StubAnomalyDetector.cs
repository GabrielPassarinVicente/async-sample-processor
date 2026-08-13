using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class StubAnomalyDetector : IAnomalyDetector
{
    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples) => new HashSet<string>();
}
