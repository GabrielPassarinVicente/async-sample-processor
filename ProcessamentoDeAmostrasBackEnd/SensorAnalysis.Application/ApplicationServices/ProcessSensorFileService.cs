using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Application.Services;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Application.ApplicationServices;

public class ProcessSensorFileService
{
    private readonly SensorFileParser _fileParser;
    private readonly SensorEvaluator _evaluator;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IJobRepository _jobRepository;

    public ProcessSensorFileService(
        SensorFileParser fileParser,
        SensorEvaluator evaluator,
        IAnomalyDetector anomalyDetector,
        IMessagePublisher messagePublisher,
        IJobRepository jobRepository)
    {
        _fileParser = fileParser;
        _evaluator = evaluator;
        _anomalyDetector = anomalyDetector;
        _messagePublisher = messagePublisher;
        _jobRepository = jobRepository;
    }

    public async Task<Result<string>> StartAsync(Stream fileStream)
    {
        var parseResult = await _fileParser.ParseAsync(fileStream);

        if (parseResult.IsFailure)
            return Result<string>.Failure(parseResult.Error!);

        var samples = parseResult.Value!;
        var jobId = Guid.NewGuid().ToString();
        var job = JobStatus.Create(jobId, samples.Count);

        await _jobRepository.AddAsync(job);

        _ = ProcessInBackgroundAsync(job, samples);

        return Result<string>.Success(jobId);
    }

    private async Task ProcessInBackgroundAsync(JobStatus job, List<SensorSample> samples)
    {
        try
        {
            await ProcessAsync(job, samples);
        }
        catch (Exception ex)
        {
            job.MarkAsFailed(ex.Message);
            await _jobRepository.UpdateAsync(job);
        }
    }

    private async Task ProcessAsync(JobStatus job, List<SensorSample> samples)
    {
        var validSamples = samples.Where(s => s.IsValid()).ToList();
        var anomalyKeys = _anomalyDetector.DetectAnomalies(validSamples);

        var results = new List<SampleAnalysisResult>();

        foreach (var sample in samples)
        {
            var analysis = _evaluator.Evaluate(sample);

            var key = $"{sample.SensorId}_{sample.Timestamp:O}";
            if (anomalyKeys.Contains(key))
                analysis = analysis.AsAnomaly();

            results.Add(SampleAnalysisResult.Create(sample, analysis));
            job.IncrementProgress();
        }

        job.Complete(results);

        foreach (var domainEvent in job.DomainEvents.OfType<SensorAnomalyDetected>())
            await _messagePublisher.PublishAsync(domainEvent);

        job.ClearDomainEvents();

        await _jobRepository.UpdateAsync(job);
    }
}
