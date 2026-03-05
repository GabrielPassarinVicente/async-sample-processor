using SensorAnalysis.Application.DTOs;
using SensorAnalysis.Application.Interfaces;
using SensorAnalysis.Application.Mappers;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Application.UseCases;

public class ProcessSensorFileUseCase
{
    private readonly SensorEvaluator _evaluator;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IJobRepository _jobRepository;

    public ProcessSensorFileUseCase(
        SensorEvaluator evaluator,
        IAnomalyDetector anomalyDetector,
        IMessagePublisher messagePublisher,
        IJobRepository jobRepository)
    {
        _evaluator = evaluator;
        _anomalyDetector = anomalyDetector;
        _messagePublisher = messagePublisher;
        _jobRepository = jobRepository;
    }

    public async Task<Result<string>> StartAsync(List<SensorSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return Result<string>.Failure(Error.EmptyFile);

        string jobId = Guid.NewGuid().ToString();
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
        var resultsDictionary = new Dictionary<string, SampleAnalysis>();
        var validSamples = new List<SensorSample>();
        var finalOutput = new List<AnalyzedSampleDto>();

        foreach (var sample in samples)
        {
            var analysis = _evaluator.Evaluate(sample);
            string uniqueKey = $"{sample.SensorId}_{sample.Timestamp:O}";
            resultsDictionary[uniqueKey] = analysis;

            if (analysis.AnomalyStatus != Domain.Enums.StatusLevel.Invalid)
            {
                validSamples.Add(sample);
            }
        }

        _anomalyDetector.Detect(validSamples, resultsDictionary);

        foreach (var sample in samples)
        {
            string uniqueKey = $"{sample.SensorId}_{sample.Timestamp:O}";
            var analysis = resultsDictionary[uniqueKey];

            var dto = SensorMapper.ToDto(sample, analysis);
            finalOutput.Add(dto);

            if (analysis.IsCritical() || analysis.IsAnomaly())
            {
                var domainEvent = SensorAnomalyDetected.Create(
                    sample.SensorId,
                    sample.Timestamp,
                    analysis.IsCritical() ? "critical" : "anomaly"
                );
                await _messagePublisher.PublishAsync(domainEvent);
            }

            job.IncrementProgress();
        }

        job.Complete(finalOutput.Cast<object>().ToList());
        await _jobRepository.UpdateAsync(job);
    }
}

