using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.Events;
using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using SensorAnalysis.Domain.ValueObjects;

namespace SensorAnalysis.Domain.Entities;

// Aggregate Root: guardião de todas as invariantes do ciclo de vida de um processamento
public sealed class JobStatus
{
    private readonly List<SampleAnalysisResult> _results = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public string JobId { get; private set; }
    public JobState State { get; private set; }
    public int TotalSamples { get; private set; }
    public int ProcessedSamples { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<SampleAnalysisResult> Results => _results.AsReadOnly();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private JobStatus(string jobId, int totalSamples)
    {
        JobId = jobId;
        TotalSamples = totalSamples;
        ProcessedSamples = 0;
        State = JobState.Processing;
        CreatedAt = DateTime.UtcNow;
    }

    // Chamado por: ProcessSensorFileService (Application) — cria o Agregado
    public static JobStatus Create(string jobId, int totalSamples)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new DomainException("JobId cannot be null or empty");

        if (totalSamples <= 0)
            throw new DomainException("TotalSamples must be greater than zero");

        return new JobStatus(jobId, totalSamples);
    }

    // Método rico do Aggregate Root: encapsula toda a lógica de processamento
    // Chamado por: ProcessSensorFileService (Application) — Passo 2 do fluxo DDD
    // Recebe Domain Services como parâmetros (padrão Evans: "domain service passed as parameter")
    public void Process(
        IReadOnlyList<SensorSample> samples,
        SensorEvaluator evaluator,
        IAnomalyDetector anomalyDetector)
    {
        if (State != JobState.Processing)
            throw new InvalidJobOperationException("Only a job in processing state can be processed.");

        // Domain Service: detecta quais amostras são anomalias estatísticas (IQR)
        var validSamples = samples.Where(s => s.IsValid()).ToList();
        var anomalyKeys = anomalyDetector.DetectAnomalies(validSamples);

        foreach (var sample in samples)
        {
            // Domain Service: avalia cada métrica contra os limiares configurados
            var analysis = evaluator.Evaluate(sample);

            var key = $"{sample.SensorId}_{sample.Timestamp:O}";
            if (anomalyKeys.Contains(key))
                analysis = analysis.AsAnomaly(); // Value Object imutável: retorna nova instância

            _results.Add(SampleAnalysisResult.Create(sample, analysis));
            IncrementProgress();
        }

        // Invariante: levanta Domain Events para qualquer amostra crítica ou anômala
        RaiseAnomalyEvents();

        State = JobState.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    // Chamado por: ProcessSensorFileService (Application) quando uma exceção não tratada ocorre
    public void MarkAsFailed(string errorMessage)
    {
        if (State == JobState.Completed)
            throw new InvalidJobOperationException("A completed job cannot be marked as failed.");

        State = JobState.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    // Chamado por: ProcessSensorFileService (Application) após despachar os Domain Events
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool IsCompleted => State == JobState.Completed;
    public bool IsFailed => State == JobState.Failed;
    public bool IsProcessing => State == JobState.Processing;

    public int GetProgressPercentage()
    {
        if (TotalSamples == 0) return 0;
        return (ProcessedSamples * 100) / TotalSamples;
    }

    public bool CanDownload() => IsCompleted && _results.Count > 0;

    // Detalhe interno: progresso é gerenciado pelo próprio Agregado
    private void IncrementProgress()
    {
        if (ProcessedSamples < TotalSamples)
            ProcessedSamples++;
    }

    // Invariante de domínio: toda amostra crítica ou anômala gera um evento
    private void RaiseAnomalyEvents()
    {
        foreach (var result in _results)
        {
            if (result.Analysis.IsCritical() || result.Analysis.IsAnomaly())
            {
                var reason = result.Analysis.IsCritical() ? "critical" : "anomaly";
                _domainEvents.Add(SensorAnomalyDetected.Create(result.SensorId, result.Timestamp, reason));
            }
        }
    }
}

