using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Domain.Common;

public sealed record ProcessingJob(JobStatus Job, IReadOnlyList<SensorSample> Samples);
