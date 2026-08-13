using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;

namespace SensorAnalysis.Infrastructure.Processing;

internal sealed class JobProcessingBackgroundService : BackgroundService
{
    private readonly IJobProcessingQueue _queue;
    private readonly SensorEvaluator _evaluator;
    private readonly IAnomalyDetector _anomalyDetector;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobProcessingBackgroundService> _logger;

    public JobProcessingBackgroundService(
        IJobProcessingQueue queue,
        SensorEvaluator evaluator,
        IAnomalyDetector anomalyDetector,
        IMessagePublisher messagePublisher,
        IJobRepository jobRepository,
        ILogger<JobProcessingBackgroundService> logger)
    {
        _queue = queue;
        _evaluator = evaluator;
        _anomalyDetector = anomalyDetector;
        _messagePublisher = messagePublisher;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                item.Job.Process(item.Samples, _evaluator, _anomalyDetector);
                await _jobRepository.UpdateAsync(item.Job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar job {JobId}", item.Job.JobId);

                if (!item.Job.IsCompleted)
                {
                    try
                    {
                        item.Job.MarkAsFailed(ex.Message);
                        await _jobRepository.UpdateAsync(item.Job);
                    }
                    catch (Exception recoveryEx)
                    {
                        _logger.LogError(recoveryEx, "Falha ao registrar falha do job {JobId}", item.Job.JobId);
                    }
                }

                continue;
            }

            try
            {
                foreach (var domainEvent in item.Job.DomainEvents.OfType<SensorAnomalyDetected>())
                    await _messagePublisher.PublishAsync(domainEvent);

                item.Job.ClearDomainEvents();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao publicar eventos de anomalia do job {JobId}", item.Job.JobId);
            }
        }
    }
}
