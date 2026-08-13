# Correção de Arquitetura e Clean Code do Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corrigir os 6 problemas reais de arquitetura/clean code identificados na auditoria do backend `.NET` (Clean Architecture + DDD) e adicionar a base de testes que faltava, sem introduzir infraestrutura nova.

**Architecture:** Move a porta `IMessagePublisher` para `Domain.Interfaces` (removendo a referência de `Infrastructure` a `Application`); substitui a `Task` de fire-and-forget por uma fila em memória (`Channel<ProcessingJob>`) consumida por um `BackgroundService`; padroniza o contrato JSON da API em camelCase; torna o CORS configurável; corrige o bug do detector de anomalias IQR (ignorava Ponto de Orvalho); adiciona dois projetos de teste xUnit (`SensorAnalysis.Domain.Tests`, `SensorAnalysis.Application.Tests`).

**Tech Stack:** .NET 10 / C# 14, ASP.NET Core (Controllers), RabbitMQ.Client, xUnit (sem framework de mock — fakes escritos à mão, pois as interfaces têm 1-3 métodos).

## Global Constraints

- Não introduzir banco de dados, novo message broker, ou bibliotecas de mocking (Moq/NSubstitute) — as interfaces do projeto (`IJobRepository`, `IJobProcessingQueue`, `IAnomalyDetector`) têm poucos métodos e são triviais de "fakear" à mão.
- Manter o padrão `Result<T>` já usado no projeto para retorno de erros — não introduzir exceções como controle de fluxo em código novo.
- Todo código novo em C# segue o estilo já usado no repositório: classes `sealed` quando não há necessidade de herança, factory methods estáticos em vez de construtores públicos em Domain, comentários apenas quando explicam o "porquê" (não o "o quê").
- O contrato JSON da API (`/upload`, `/status`, `/download`) deve ficar 100% consistente em camelCase — nenhum endpoint deve devolver snake_case.
- `RabbitMqNotificationDto` (mensagem publicada no RabbitMQ) fica fora de escopo — é o formato da mensagem de fila, não o contrato HTTP, e não é o que a auditoria apontou como inconsistente.

---

## Visão geral dos arquivos

**Backend — `ProcessamentoDeAmostrasBackEnd/`:**

| Arquivo | Ação | Task |
|---|---|---|
| `SensorAnalysis.Domain/Interfaces/IMessagePublisher.cs` | Criar | 1 |
| `SensorAnalysis.Application/Interfaces/IMessagePublisher.cs` | Remover | 1 |
| `SensorAnalysis.Infrastructure/Messaging/RabbitMqPublisher.cs` | Modificar (using) | 1 |
| `SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs` | Modificar (using, depois DI da fila) | 1, 2 |
| `SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj` | Modificar (remover ProjectReference) | 1 |
| `SensorAnalysis.Domain/Common/ProcessingJob.cs` | Criar | 2 |
| `SensorAnalysis.Domain/Interfaces/IJobProcessingQueue.cs` | Criar | 2 |
| `SensorAnalysis.Infrastructure/Processing/ChannelJobProcessingQueue.cs` | Criar | 2 |
| `SensorAnalysis.Infrastructure/Processing/JobProcessingBackgroundService.cs` | Criar | 2 |
| `SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj` | Modificar (add `Microsoft.Extensions.Hosting.Abstractions`) | 2 |
| `SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs` | Modificar (refatorar `StartAsync`) | 2 |
| `SensorAnalysis.Application/DTOs/AnalyzedSampleDto.cs` | Modificar (remover `[JsonPropertyName]`) | 3 |
| `SensorAnalysis.Application/DTOs/DownloadResultDto.cs` | Modificar (remover `[JsonPropertyName]`) | 3 |
| `SensorAnalysis.API/Program.cs` | Modificar (`AddJsonOptions`, depois CORS) | 3, 5 |
| `SensorAnalysis.API/Controller/SensorController.cs` | Modificar (usar `JsonOptions` injetado) | 3 |
| `SensorAnalysis.API/appsettings.json` | Modificar (add `Cors:Origins`) | 5 |
| `SensorAnalysis.Infrastructure/Algorithms/IqrAnomalyDetector.cs` | Modificar (corrigir bug + eliminar duplicação) | 6 |
| `SensorAnalysis.Infrastructure/AssemblyInfo.cs` | Criar (`InternalsVisibleTo`) | 7 |
| `SensorAnalysis.Domain.Tests/` (projeto novo) | Criar | 7 |
| `SensorAnalysis.Application.Tests/` (projeto novo) | Criar | 8 |
| `SensorAnalysis.slnx` | Modificar (registrar os 2 projetos de teste) | 7, 8 |
| `SensorAnalysis.API/README.md` (repo: `ProcessamentoDeAmostrasBackEnd/README.md`) | Modificar (diagrama + nomes de classes) | 9 |
| `README.md` (raiz) | Modificar (nomes de classes + trade-off do RabbitMQ) | 9 |

**Frontend — `ProcessamentoDeAmostrasFrontEnd/`:**

| Arquivo | Ação | Task |
|---|---|---|
| `app/services/sensorService.ts` | Modificar (simplificar normalização) | 4 |

---

### Task 1: Mover a porta `IMessagePublisher` para `Domain.Interfaces`

**Files:**
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IMessagePublisher.cs`
- Delete: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/Interfaces/IMessagePublisher.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Messaging/RabbitMqPublisher.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs`

**Interfaces:**
- Produces: `SensorAnalysis.Domain.Interfaces.IMessagePublisher` com `Task PublishAsync(SensorAnomalyDetected domainEvent)` — mesma assinatura de antes, novo namespace.

- [ ] **Step 1: Criar a interface no novo local**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IMessagePublisher.cs`:

```csharp
using SensorAnalysis.Domain.Events;

namespace SensorAnalysis.Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(SensorAnomalyDetected domainEvent);
}
```

- [ ] **Step 2: Remover a interface do local antigo**

Delete o arquivo `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/Interfaces/IMessagePublisher.cs`.

- [ ] **Step 3: Atualizar `RabbitMqPublisher.cs`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Messaging/RabbitMqPublisher.cs`, troque:

```csharp
using SensorAnalysis.Application.Interfaces;
```

por:

```csharp
using SensorAnalysis.Domain.Interfaces;
```

- [ ] **Step 4: Atualizar `InfrastructureServiceExtensions.cs`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs`, remova a linha:

```csharp
using SensorAnalysis.Application.Interfaces;
```

(o arquivo já tem `using SensorAnalysis.Domain.Interfaces;`, que agora também resolve `IMessagePublisher`).

- [ ] **Step 5: Remover a referência de projeto Infrastructure → Application**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj`, remova a linha:

```xml
<ProjectReference Include="..\SensorAnalysis.Application\SensorAnalysis.Application.csproj" />
```

O `<ItemGroup>` de `ProjectReference` deve conter apenas a referência a `SensorAnalysis.Domain.csproj`.

- [ ] **Step 6: Atualizar `ProcessSensorFileService.cs`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs`, remova a linha:

```csharp
using SensorAnalysis.Application.Interfaces;
```

(`IMessagePublisher` já resolve via `using SensorAnalysis.Domain.Interfaces;`, que este arquivo já importa).

- [ ] **Step 7: Compilar e verificar**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build`
Expected: build com sucesso, 0 erros. Isso confirma que `SensorAnalysis.Infrastructure` compila sem a referência a `SensorAnalysis.Application`.

- [ ] **Step 8: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IMessagePublisher.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/Interfaces/IMessagePublisher.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Messaging/RabbitMqPublisher.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs
git commit -m "refactor: move IMessagePublisher to Domain.Interfaces"
```

---

### Task 2: Fila de processamento gerenciada (`Channel` + `BackgroundService`)

**Files:**
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Common/ProcessingJob.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IJobProcessingQueue.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Processing/ChannelJobProcessingQueue.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Processing/JobProcessingBackgroundService.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs`

**Interfaces:**
- Consumes: `IMessagePublisher`, `IAnomalyDetector`, `IJobRepository` (`SensorAnalysis.Domain.Interfaces`, de Task 1); `JobStatus.Process(IReadOnlyList<SensorSample>, SensorEvaluator, IAnomalyDetector)`, `JobStatus.MarkAsFailed(string)`, `JobStatus.ClearDomainEvents()`, `JobStatus.DomainEvents` (já existentes em `SensorAnalysis.Domain.Entities.JobStatus`).
- Produces: `SensorAnalysis.Domain.Common.ProcessingJob` (record `ProcessingJob(JobStatus Job, IReadOnlyList<SensorSample> Samples)`); `SensorAnalysis.Domain.Interfaces.IJobProcessingQueue` com `ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default)` e `IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default)` — usados pela Task 8 (testes de `ProcessSensorFileService`) via fake.

- [ ] **Step 1: Criar o record `ProcessingJob` no Domain**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Common/ProcessingJob.cs`:

```csharp
using SensorAnalysis.Domain.Entities;

namespace SensorAnalysis.Domain.Common;

public sealed record ProcessingJob(JobStatus Job, IReadOnlyList<SensorSample> Samples);
```

- [ ] **Step 2: Criar a interface `IJobProcessingQueue` no Domain**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IJobProcessingQueue.cs`:

```csharp
using SensorAnalysis.Domain.Common;

namespace SensorAnalysis.Domain.Interfaces;

public interface IJobProcessingQueue
{
    ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Adicionar o pacote `Microsoft.Extensions.Hosting.Abstractions` à Infrastructure**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet add SensorAnalysis.Infrastructure package Microsoft.Extensions.Hosting.Abstractions`
Expected: `PackageReference` adicionado ao `SensorAnalysis.Infrastructure.csproj`.

- [ ] **Step 4: Implementar `ChannelJobProcessingQueue`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Processing/ChannelJobProcessingQueue.cs`:

```csharp
using System.Threading.Channels;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Processing;

internal sealed class ChannelJobProcessingQueue : IJobProcessingQueue
{
    private readonly Channel<ProcessingJob> _channel = Channel.CreateUnbounded<ProcessingJob>();

    public ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
```

- [ ] **Step 5: Implementar `JobProcessingBackgroundService`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Processing/JobProcessingBackgroundService.cs`:

```csharp
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

                foreach (var domainEvent in item.Job.DomainEvents.OfType<SensorAnomalyDetected>())
                    await _messagePublisher.PublishAsync(domainEvent);

                item.Job.ClearDomainEvents();
                await _jobRepository.UpdateAsync(item.Job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar job {JobId}", item.Job.JobId);
                item.Job.MarkAsFailed(ex.Message);
                await _jobRepository.UpdateAsync(item.Job);
            }
        }
    }
}
```

- [ ] **Step 6: Registrar a fila e o `BackgroundService` na Infrastructure**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs`, adicione o using e as duas linhas de registro:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Infrastructure.Algorithms;
using SensorAnalysis.Infrastructure.Messaging;
using SensorAnalysis.Infrastructure.Persistence;
using SensorAnalysis.Infrastructure.Processing;

namespace SensorAnalysis.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IAnomalyDetector, IqrAnomalyDetector>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton<IJobProcessingQueue, ChannelJobProcessingQueue>();
        services.AddHostedService<JobProcessingBackgroundService>();

        return services;
    }
}
```

- [ ] **Step 7: Refatorar `ProcessSensorFileService` para apenas enfileirar**

Substitua o conteúdo de `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs` por:

```csharp
using SensorAnalysis.Application.Services;
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.ApplicationServices;

// Chamado por: SensorController (Apresentação) via StartAsync()
// Responsabilidade: orquestração pura — sem regras de negócio
public class ProcessSensorFileService
{
    private readonly SensorFileParser _fileParser;
    private readonly IJobRepository _jobRepository;
    private readonly IJobProcessingQueue _jobProcessingQueue;

    public ProcessSensorFileService(
        SensorFileParser fileParser,
        IJobRepository jobRepository,
        IJobProcessingQueue jobProcessingQueue)
    {
        _fileParser = fileParser;
        _jobRepository = jobRepository;
        _jobProcessingQueue = jobProcessingQueue;
    }

    // Chamado por: SensorController.UploadFile()
    // Enfileira o job e retorna o jobId imediatamente; o processamento ocorre
    // em background via JobProcessingBackgroundService (Infrastructure)
    public async Task<Result<string>> StartAsync(Stream fileStream)
    {
        var parseResult = await _fileParser.ParseAsync(fileStream);

        if (parseResult.IsFailure)
            return Result<string>.Failure(parseResult.Error!);

        var samples = parseResult.Value!;
        var jobId = Guid.NewGuid().ToString();

        var job = JobStatus.Create(jobId, samples.Count);
        await _jobRepository.AddAsync(job);

        await _jobProcessingQueue.EnqueueAsync(new ProcessingJob(job, samples));

        return Result<string>.Success(jobId);
    }
}
```

- [ ] **Step 8: Compilar**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build`
Expected: build com sucesso, 0 erros.

- [ ] **Step 9: Verificação manual do fluxo assíncrono**

Run (em um terminal): `cd ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API && dotnet run`

Em outro terminal, com a API rodando (ajuste a porta conforme o output do `dotnet run`, ex. `http://localhost:5279`):

```bash
cat > /tmp/sample.json <<'EOF'
[
  { "sensor_id": "s1", "type": "env", "timestamp": "2026-01-01T00:00:00Z", "temperature": 20, "humidity": 50, "dew_point": 10 },
  { "sensor_id": "s2", "type": "env", "timestamp": "2026-01-01T00:01:00Z", "temperature": 21, "humidity": 51, "dew_point": 11 }
]
EOF
curl -s -X POST http://localhost:5279/api/sensor/upload -F "file=@/tmp/sample.json"
```

Expected: resposta `202` com `{"jobId": "...", "message": "..."}`. Copie o `jobId` e rode:

```bash
curl -s http://localhost:5279/api/sensor/status/<jobId>
```

Expected: depois de alguns milissegundos, `isCompleted: true` e `results` com 2 itens — confirma que o `BackgroundService` processou o job enfileirado (sem a antiga `Task` solta). Pare a API com Ctrl+C.

- [ ] **Step 10: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Common/ProcessingJob.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain/Interfaces/IJobProcessingQueue.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Processing/ \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/InfrastructureServiceExtensions.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/SensorAnalysis.Infrastructure.csproj \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/ApplicationServices/ProcessSensorFileService.cs
git commit -m "feat: replace fire-and-forget task with managed Channel+BackgroundService queue"
```

---

### Task 3: Contrato JSON consistente (camelCase) na API

**Files:**
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/AnalyzedSampleDto.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/DownloadResultDto.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Controller/SensorController.cs`

**Interfaces:**
- Consumes: nenhuma nova — apenas remove atributos e centraliza `JsonSerializerOptions`.
- Produces: `AnalyzedSampleDto`, `DownloadResultDto` sem atributos `[JsonPropertyName]` — todas as respostas da API passam a usar o `PropertyNamingPolicy` padrão (camelCase) do ASP.NET Core.

- [ ] **Step 1: Remover os atributos `[JsonPropertyName]` de `AnalyzedSampleDto.cs`**

Substitua o conteúdo de `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/AnalyzedSampleDto.cs` por:

```csharp
namespace SensorAnalysis.Application.DTOs;

public class AnalyzedSampleDto
{
    public string SensorId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? DewPoint { get; set; }
    public AnalysisDto Analysis { get; set; } = new();
}

public class AnalysisDto
{
    public MetricDto Temperature { get; set; } = new();
    public MetricDto Humidity { get; set; } = new();
    public MetricDto DewPoint { get; set; } = new();
    public AnomalyDto Anomaly { get; set; } = new();
}

public class MetricDto
{
    public string Status { get; set; } = "normal";
    public string? LimitType { get; set; }
    public double? ThresholdValue { get; set; }
}

public class AnomalyDto
{
    public string Status { get; set; } = "normal";
}
```

- [ ] **Step 2: Remover os atributos `[JsonPropertyName]` de `DownloadResultDto.cs`**

Substitua o conteúdo de `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/DownloadResultDto.cs` por:

```csharp
namespace SensorAnalysis.Application.DTOs;

public class DownloadResultDto
{
    public string JobId { get; set; } = string.Empty;
    public int TotalSamples { get; set; }
    public int ProcessedSamples { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<AnalyzedSampleDto> Results { get; set; } = new();
}
```

- [ ] **Step 3: Configurar `WriteIndented` globalmente em `Program.cs`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs`, troque:

```csharp
builder.Services.AddControllers();
```

por:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);
```

- [ ] **Step 4: Usar os `JsonSerializerOptions` do framework em `SensorController.DownloadResults`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Controller/SensorController.cs`, adicione o using e o campo, e ajuste o construtor:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SensorAnalysis.Application.ApplicationServices;

namespace SensorAnalysis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ProcessSensorFileService _processService;
    private readonly DownloadResultsService _downloadService;
    private readonly GetJobStatusService _getJobStatusService;
    private readonly JsonSerializerOptions _downloadJsonOptions;
    private readonly ILogger<SensorController> _logger;

    public SensorController(
        ProcessSensorFileService processService,
        DownloadResultsService downloadService,
        GetJobStatusService getJobStatusService,
        IOptions<JsonOptions> jsonOptions,
        ILogger<SensorController> logger)
    {
        _processService = processService;
        _downloadService = downloadService;
        _getJobStatusService = getJobStatusService;
        _downloadJsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _logger = logger;
    }
```

Em seguida, no método `DownloadResults`, troque:

```csharp
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var json = JsonSerializer.Serialize(result.Value, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"sensor_analysis_{jobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        return File(bytes, "application/json", fileName);
```

por:

```csharp
        var json = JsonSerializer.Serialize(result.Value, _downloadJsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"sensor_analysis_{jobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        return File(bytes, "application/json", fileName);
```

- [ ] **Step 5: Compilar**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build`
Expected: build com sucesso, 0 erros.

- [ ] **Step 6: Verificação manual do contrato**

Com a API rodando (`dotnet run` em `SensorAnalysis.API`), repita o upload do Step 9 da Task 2 e rode:

```bash
curl -s http://localhost:5279/api/sensor/status/<jobId> | grep -o '"dewPoint"' 
curl -s http://localhost:5279/api/sensor/status/<jobId> | grep -o '"dew_point"'
```

Expected: a primeira busca encontra ocorrências (`dewPoint`); a segunda não encontra nenhuma (`dew_point` não deve mais aparecer em lugar nenhum da resposta). Repita para `/download/<jobId>` e confirme o mesmo padrão.

- [ ] **Step 7: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/AnalyzedSampleDto.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application/DTOs/DownloadResultDto.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Controller/SensorController.cs
git commit -m "fix: standardize API JSON contract on camelCase across all endpoints"
```

---

### Task 4: Simplificar a normalização defensiva no frontend

**Files:**
- Modify: `ProcessamentoDeAmostrasFrontEnd/app/services/sensorService.ts`

**Interfaces:**
- Consumes: `SensorReading`, `UploadResponse`, `JobStatusResponse` (`~/types/sensor`, sem alteração).
- Produces: mesma API pública (`uploadFile`, `getJobStatus`, `downloadResults`) — comportamento externo idêntico, apenas menos casing defensivo internamente.

- [ ] **Step 1: Substituir o conteúdo de `sensorService.ts`**

Substitua `ProcessamentoDeAmostrasFrontEnd/app/services/sensorService.ts` por:

```typescript
import axios from 'axios'
import type { SensorReading, UploadResponse, JobStatusResponse } from '~/types/sensor'

function normalizeReading(raw: Partial<SensorReading>): SensorReading {
  return {
    timestamp:   raw.timestamp   ?? '',
    type:        raw.type        ?? 'unknown',
    temperature: raw.temperature ?? null,
    humidity:    raw.humidity    ?? null,
    dewPoint:    raw.dewPoint    ?? null,
    analysis: {
      temperature: raw.analysis?.temperature ?? { status: 'normal', limitType: null },
      humidity:    raw.analysis?.humidity    ?? { status: 'normal', limitType: null },
      dewPoint:    raw.analysis?.dewPoint    ?? { status: 'normal', limitType: null },
      anomaly:     raw.analysis?.anomaly     ?? { status: 'normal' },
    },
  }
}

export function createSensorService(baseUrl: string) {
  const api = axios.create({ baseURL: baseUrl })

  async function uploadFile(file: File): Promise<UploadResponse> {
    const formData = new FormData()
    formData.append('file', file)
    const { data } = await api.post<UploadResponse>('/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  }

  async function getJobStatus(jobId: string): Promise<JobStatusResponse> {
    const { data } = await api.get<{
      isCompleted:      boolean
      processedSamples: number
      totalSamples:     number
      results:          Partial<SensorReading>[] | null
    }>(`/status/${jobId}`)

    return {
      isCompleted:      data.isCompleted ?? false,
      processedSamples: data.processedSamples ?? 0,
      totalSamples:     data.totalSamples ?? 0,
      results:          (data.results ?? []).map(normalizeReading),
    }
  }

  async function downloadResults(jobId: string): Promise<void> {
    try {
      const response = await api.get(`/download/${jobId}`, {
        responseType: 'blob',
      })

      if (!import.meta.client) throw new Error('Download requer ambiente de browser.')
      const blob = new Blob([response.data], { type: 'application/json' })
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')

      link.href = url
      link.download = `analysis_results_${jobId}.json`
      document.body.appendChild(link)
      link.click()

      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        throw new Error('Endpoint de download não encontrado. Verifique se o backend está rodando e se a rota está configurada corretamente.')
      }
      throw error
    }
  }

  return { uploadFile, getJobStatus, downloadResults }
}
```

- [ ] **Step 2: Verificação manual no browser**

Com o backend das Tasks 1-3 rodando (`dotnet run` em `SensorAnalysis.API`) e o frontend (`cd ProcessamentoDeAmostrasFrontEnd && yarn install && yarn dev`), abra `http://localhost:3000`, faça upload do mesmo `sample.json` usado na Task 2, e confirme visualmente que o dashboard mostra KPIs e gráficos preenchidos normalmente (sem valores zerados/"unknown" indevidos) — confirma que a simplificação não quebrou a leitura dos dados agora que o backend responde em camelCase puro.

- [ ] **Step 3: Commit**

```bash
git add ProcessamentoDeAmostrasFrontEnd/app/services/sensorService.ts
git commit -m "refactor: simplify frontend response normalization now that backend contract is consistent"
```

---

### Task 5: CORS configurável

**Files:**
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/appsettings.json`

**Interfaces:**
- Consumes: `builder.Configuration["Cors:Origins"]` (populado por `CORS__Origins` no `docker-compose.yml`, já existente).
- Produces: nenhuma nova — apenas altera o comportamento da policy de CORS já registrada.

- [ ] **Step 1: Adicionar um default local em `appsettings.json`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/appsettings.json`, adicione a seção `Cors` (mantendo o restante do arquivo):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.AspNetCore.Cors": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "Origins": "http://localhost:3000"
  },
  "RabbitMqSettings": {
    "HostName": "localhost",
    "UserName": "",
    "Password": "",
    "QueueName": "log_notifications"
  }
}
```

- [ ] **Step 2: Ler a configuração em `Program.cs`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs`, troque:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});
```

por:

```csharp
var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});
```

- [ ] **Step 3: Compilar**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build`
Expected: build com sucesso, 0 erros.

- [ ] **Step 4: Verificação manual do CORS**

Com a API rodando (`dotnet run` em `SensorAnalysis.API`, porta ex. `5279`):

```bash
curl -s -i -X OPTIONS http://localhost:5279/api/sensor/health \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET" | grep -i "access-control-allow-origin"

curl -s -i -X OPTIONS http://localhost:5279/api/sensor/health \
  -H "Origin: http://evil.example.com" \
  -H "Access-Control-Request-Method: GET" | grep -i "access-control-allow-origin"
```

Expected: o primeiro comando imprime `Access-Control-Allow-Origin: http://localhost:3000`; o segundo não imprime nada (origem não permitida não recebe o header).

- [ ] **Step 5: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/Program.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.API/appsettings.json
git commit -m "fix: make CORS origins configurable instead of allowing any origin"
```

---

### Task 6: Corrigir o bug do `IqrAnomalyDetector` (Ponto de Orvalho ignorado)

**Files:**
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Algorithms/IqrAnomalyDetector.cs`

**Interfaces:**
- Consumes: `SensorSample.Temperature/Humidity/DewPoint` (`SensorAnalysis.Domain.Entities`, sem alteração).
- Produces: `IqrAnomalyDetector.DetectAnomalies` (assinatura inalterada) — a verificação automatizada desta correção acontece na Task 7 (`IqrAnomalyDetectorTests`), pois o projeto de testes ainda não existe nesta task.

- [ ] **Step 1: Substituir o conteúdo de `IqrAnomalyDetector.cs`**

Substitua `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Algorithms/IqrAnomalyDetector.cs` por:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Infrastructure.Algorithms;

internal sealed class IqrAnomalyDetector : IAnomalyDetector
{
    private static readonly (string Name, Func<SensorSample, double> Select)[] Metrics =
    [
        ("Temperature", s => s.Temperature!.Value),
        ("Humidity",    s => s.Humidity!.Value),
        ("DewPoint",    s => s.DewPoint!.Value),
    ];

    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples)
    {
        var anomalies = new HashSet<string>();

        if (validSamples.Count < 4) return anomalies;

        var boundsByMetric = Metrics.ToDictionary(
            m => m.Name,
            m => CalculateBounds(validSamples.Select(m.Select).ToList()));

        foreach (var sample in validSamples)
        {
            bool isAnomaly = Metrics.Any(m =>
            {
                var value = m.Select(sample);
                var bounds = boundsByMetric[m.Name];
                return value < bounds.Lower || value > bounds.Upper;
            });

            if (isAnomaly)
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
```

- [ ] **Step 2: Compilar**

Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build`
Expected: build com sucesso, 0 erros.

- [ ] **Step 3: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/Algorithms/IqrAnomalyDetector.cs
git commit -m "fix: include dew point in IQR anomaly detection bounds"
```

---

### Task 7: Projeto de testes `SensorAnalysis.Domain.Tests`

**Files:**
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/AssemblyInfo.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/SensorAnalysis.Domain.Tests.csproj`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/JobStatusTests.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/SensorEvaluatorTests.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/MetricThresholdsTests.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/IqrAnomalyDetectorTests.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx`

**Interfaces:**
- Consumes: `JobStatus`, `SensorSample` (`SensorAnalysis.Domain.Entities`), `SensorEvaluator` (`SensorAnalysis.Domain.Services`), `MetricThresholds` (`SensorAnalysis.Domain.ValueObjects`), `IAnomalyDetector` (`SensorAnalysis.Domain.Interfaces`), `IqrAnomalyDetector` (`SensorAnalysis.Infrastructure.Algorithms`, `internal` — acessível via `InternalsVisibleTo`), `DomainException`/`InvalidJobOperationException` (`SensorAnalysis.Domain.Exceptions`) — todos já existentes.

- [ ] **Step 1: Permitir acesso a tipos `internal` da Infrastructure**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SensorAnalysis.Domain.Tests")]
```

- [ ] **Step 2: Criar e configurar o projeto de teste**

Run:
```bash
cd ProcessamentoDeAmostrasBackEnd
dotnet new xunit -n SensorAnalysis.Domain.Tests -o SensorAnalysis.Domain.Tests
rm SensorAnalysis.Domain.Tests/UnitTest1.cs
```

Abra `SensorAnalysis.Domain.Tests/SensorAnalysis.Domain.Tests.csproj` gerado e:
1. Confirme que `<TargetFramework>` está como `net10.0` (ajuste se o template gerou outra versão).
2. Confirme `<ImplicitUsings>enable</ImplicitUsings>` e `<Nullable>enable</Nullable>` (adicione se não estiverem presentes, para seguir o padrão dos demais `.csproj` do repositório).
3. Adicione, antes do `</Project>` final, um novo `<ItemGroup>`:

```xml
  <ItemGroup>
    <ProjectReference Include="..\SensorAnalysis.Domain\SensorAnalysis.Domain.csproj" />
    <ProjectReference Include="..\SensorAnalysis.Infrastructure\SensorAnalysis.Infrastructure.csproj" />
  </ItemGroup>
```

- [ ] **Step 3: Registrar o projeto no `.slnx`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx`, adicione a linha antes de `</Solution>`:

```xml
  <Project Path="SensorAnalysis.Domain.Tests/SensorAnalysis.Domain.Tests.csproj" />
```

- [ ] **Step 4: Escrever `JobStatusTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/JobStatusTests.cs`:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.Interfaces;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

internal sealed class StubAnomalyDetector : IAnomalyDetector
{
    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples) => new HashSet<string>();
}

public class JobStatusTests
{
    [Fact]
    public void Create_WithBlankJobId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JobStatus.Create("  ", 1));
    }

    [Fact]
    public void Create_WithNonPositiveTotalSamples_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => JobStatus.Create("job-1", 0));
    }

    [Fact]
    public void Process_WhenNotInProcessingState_ThrowsInvalidJobOperationException()
    {
        var job = JobStatus.Create("job-2", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.Throws<InvalidJobOperationException>(
            () => job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector()));
    }

    [Fact]
    public void Process_CompletesJobAndTracksProgress()
    {
        var job = JobStatus.Create("job-3", 2);
        var samples = new List<SensorSample>
        {
            SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10),
            SensorSample.Create("s2", "env", DateTime.UtcNow, 21, 51, 11),
        };

        job.Process(samples, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.True(job.IsCompleted);
        Assert.Equal(2, job.ProcessedSamples);
        Assert.Equal(100, job.GetProgressPercentage());
        Assert.True(job.CanDownload());
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyCompleted_ThrowsInvalidJobOperationException()
    {
        var job = JobStatus.Create("job-4", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());

        Assert.Throws<InvalidJobOperationException>(() => job.MarkAsFailed("boom"));
    }

    [Fact]
    public void MarkAsFailed_WhenProcessing_SetsFailedStateAndMessage()
    {
        var job = JobStatus.Create("job-5", 1);

        job.MarkAsFailed("boom");

        Assert.True(job.IsFailed);
        Assert.Equal("boom", job.ErrorMessage);
    }
}
```

Run: `dotnet test SensorAnalysis.Domain.Tests --filter FullyQualifiedName~JobStatusTests`
Expected: 5 testes, todos `Passed`.

- [ ] **Step 5: Escrever `SensorEvaluatorTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/SensorEvaluatorTests.cs`:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Enums;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class SensorEvaluatorTests
{
    private readonly SensorEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_WithMissingMetric_ReturnsInvalid()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, null, 50, 10);

        var analysis = _evaluator.Evaluate(sample);

        Assert.True(analysis.IsInvalid());
    }

    [Fact]
    public void Evaluate_WithinNormalRange_ReturnsNormal()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Normal, analysis.Temperature.Status);
        Assert.Equal(StatusLevel.Normal, analysis.Humidity.Status);
        Assert.Equal(StatusLevel.Normal, analysis.DewPoint.Status);
    }

    [Fact]
    public void Evaluate_AboveAlertMaxBelowCriticalMax_ReturnsAlert()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 32, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Alert, analysis.Temperature.Status);
        Assert.Equal(LimitType.Max, analysis.Temperature.LimitType);
    }

    [Fact]
    public void Evaluate_AboveCriticalMax_ReturnsCritical()
    {
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 40, 50, 15);

        var analysis = _evaluator.Evaluate(sample);

        Assert.Equal(StatusLevel.Critical, analysis.Temperature.Status);
        Assert.Equal(LimitType.Max, analysis.Temperature.LimitType);
    }
}
```

Run: `dotnet test SensorAnalysis.Domain.Tests --filter FullyQualifiedName~SensorEvaluatorTests`
Expected: 4 testes, todos `Passed`.

- [ ] **Step 6: Escrever `MetricThresholdsTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/MetricThresholdsTests.cs`:

```csharp
using SensorAnalysis.Domain.Exceptions;
using SensorAnalysis.Domain.ValueObjects;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class MetricThresholdsTests
{
    [Fact]
    public void Create_WithCriticalMinAboveAlertMin_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MetricThresholds.Create(alertMin: 10, alertMax: null, criticalMin: 15, criticalMax: null));
    }

    [Fact]
    public void Create_WithCriticalMaxBelowAlertMax_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => MetricThresholds.Create(alertMin: null, alertMax: 30, criticalMin: null, criticalMax: 25));
    }

    [Fact]
    public void Create_WithConsistentBounds_Succeeds()
    {
        var thresholds = MetricThresholds.Create(alertMin: 15, alertMax: 30, criticalMin: 10, criticalMax: 35);

        Assert.Equal(15, thresholds.AlertMin);
        Assert.Equal(35, thresholds.CriticalMax);
    }
}
```

Run: `dotnet test SensorAnalysis.Domain.Tests --filter FullyQualifiedName~MetricThresholdsTests`
Expected: 3 testes, todos `Passed`.

- [ ] **Step 7: Escrever `IqrAnomalyDetectorTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/IqrAnomalyDetectorTests.cs`:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Infrastructure.Algorithms;
using Xunit;

namespace SensorAnalysis.Domain.Tests;

public class IqrAnomalyDetectorTests
{
    private readonly IqrAnomalyDetector _detector = new();

    [Fact]
    public void DetectAnomalies_WithFewerThanFourSamples_ReturnsEmpty()
    {
        var samples = new List<SensorSample>
        {
            SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10),
        };

        var anomalies = _detector.DetectAnomalies(samples);

        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectAnomalies_WithDewPointOutlier_FlagsSample()
    {
        var baseline = new[] { 10.0, 10.5, 11.0, 10.2, 10.8, 10.4 };
        var samples = new List<SensorSample>();

        for (int i = 0; i < baseline.Length; i++)
            samples.Add(SensorSample.Create($"s{i}", "env", DateTime.UtcNow.AddMinutes(i), 20, 50, baseline[i]));

        var outlierTimestamp = DateTime.UtcNow.AddMinutes(99);
        var outlier = SensorSample.Create("s-outlier", "env", outlierTimestamp, 20, 50, 90.0);
        samples.Add(outlier);

        var anomalies = _detector.DetectAnomalies(samples);

        Assert.Contains($"s-outlier_{outlierTimestamp:O}", anomalies);
    }
}
```

Run: `dotnet test SensorAnalysis.Domain.Tests --filter FullyQualifiedName~IqrAnomalyDetectorTests`
Expected: 2 testes, todos `Passed`. Se `DetectAnomalies_WithDewPointOutlier_FlagsSample` falhar, revise a Task 6 — o bug pode não ter sido corrigido corretamente.

- [ ] **Step 8: Rodar a suíte completa do projeto**

Run: `dotnet test SensorAnalysis.Domain.Tests`
Expected: 14 testes, todos `Passed`.

- [ ] **Step 9: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Infrastructure/AssemblyInfo.cs \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Domain.Tests/ \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx
git commit -m "test: add SensorAnalysis.Domain.Tests covering JobStatus, SensorEvaluator, MetricThresholds and IqrAnomalyDetector"
```

---

### Task 8: Projeto de testes `SensorAnalysis.Application.Tests`

**Files:**
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/SensorAnalysis.Application.Tests.csproj`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/FakeJobRepository.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/FakeJobProcessingQueue.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/StubAnomalyDetector.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/ProcessSensorFileServiceTests.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/DownloadResultsServiceTests.cs`
- Create: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/GetJobStatusServiceTests.cs`
- Modify: `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx`

**Interfaces:**
- Consumes: `ProcessSensorFileService`, `DownloadResultsService`, `GetJobStatusService` (`SensorAnalysis.Application.ApplicationServices`, de Task 2); `SensorFileParser` (`SensorAnalysis.Application.Services`); `IJobRepository`, `IJobProcessingQueue`, `IAnomalyDetector` (`SensorAnalysis.Domain.Interfaces`); `JobStatus`, `SensorSample` (`SensorAnalysis.Domain.Entities`); `SensorEvaluator` (`SensorAnalysis.Domain.Services`); `ProcessingJob` (`SensorAnalysis.Domain.Common`) — todos já existentes após as Tasks 1-2.
- Produces: `FakeJobRepository` (implementa `IJobRepository` com `Dictionary<string, JobStatus>` interno, expõe `Stored(string jobId)` para inspeção), `FakeJobProcessingQueue` (implementa `IJobProcessingQueue`, expõe `List<ProcessingJob> Enqueued`), `StubAnomalyDetector` (implementa `IAnomalyDetector`, sempre retorna conjunto vazio) — usados apenas dentro deste projeto de teste.

- [ ] **Step 1: Criar e configurar o projeto de teste**

Run:
```bash
cd ProcessamentoDeAmostrasBackEnd
dotnet new xunit -n SensorAnalysis.Application.Tests -o SensorAnalysis.Application.Tests
rm SensorAnalysis.Application.Tests/UnitTest1.cs
```

Abra `SensorAnalysis.Application.Tests/SensorAnalysis.Application.Tests.csproj` gerado e:
1. Confirme `<TargetFramework>net10.0</TargetFramework>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Nullable>enable</Nullable>`.
2. Adicione, antes do `</Project>` final:

```xml
  <ItemGroup>
    <ProjectReference Include="..\SensorAnalysis.Domain\SensorAnalysis.Domain.csproj" />
    <ProjectReference Include="..\SensorAnalysis.Application\SensorAnalysis.Application.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Registrar o projeto no `.slnx`**

Em `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx`, adicione a linha antes de `</Solution>`:

```xml
  <Project Path="SensorAnalysis.Application.Tests/SensorAnalysis.Application.Tests.csproj" />
```

- [ ] **Step 3: Criar os fakes/stubs**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/FakeJobRepository.cs`:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class FakeJobRepository : IJobRepository
{
    private readonly Dictionary<string, JobStatus> _jobs = new();

    public JobStatus? Stored(string jobId) => _jobs.GetValueOrDefault(jobId);

    public Task<JobStatus?> GetByIdAsync(string jobId)
        => Task.FromResult(_jobs.GetValueOrDefault(jobId));

    public Task AddAsync(JobStatus job)
    {
        _jobs[job.JobId] = job;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(JobStatus job)
    {
        _jobs[job.JobId] = job;
        return Task.CompletedTask;
    }
}
```

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/FakeJobProcessingQueue.cs`:

```csharp
using SensorAnalysis.Domain.Common;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class FakeJobProcessingQueue : IJobProcessingQueue
{
    public List<ProcessingJob> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(ProcessingJob job, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(job);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not used in Application-layer tests.");
}
```

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/StubAnomalyDetector.cs`:

```csharp
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Interfaces;

namespace SensorAnalysis.Application.Tests;

internal sealed class StubAnomalyDetector : IAnomalyDetector
{
    public IReadOnlySet<string> DetectAnomalies(IReadOnlyList<SensorSample> validSamples) => new HashSet<string>();
}
```

- [ ] **Step 4: Escrever `ProcessSensorFileServiceTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/ProcessSensorFileServiceTests.cs`:

```csharp
using System.Text;
using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Application.Services;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class ProcessSensorFileServiceTests
{
    private static Stream JsonStream(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    [Fact]
    public async Task StartAsync_WithValidFile_EnqueuesJobAndReturnsJobId()
    {
        var repository = new FakeJobRepository();
        var queue = new FakeJobProcessingQueue();
        var service = new ProcessSensorFileService(new SensorFileParser(), repository, queue);

        const string json = """
        [
            { "sensor_id": "s1", "type": "env", "timestamp": "2026-01-01T00:00:00Z", "temperature": 20, "humidity": 50, "dew_point": 10 }
        ]
        """;

        var result = await service.StartAsync(JsonStream(json));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
        Assert.Single(queue.Enqueued);
        Assert.Equal(result.Value, queue.Enqueued[0].Job.JobId);
        Assert.NotNull(repository.Stored(result.Value!));
    }

    [Fact]
    public async Task StartAsync_WithEmptyFile_ReturnsFailureWithoutEnqueueing()
    {
        var repository = new FakeJobRepository();
        var queue = new FakeJobProcessingQueue();
        var service = new ProcessSensorFileService(new SensorFileParser(), repository, queue);

        var result = await service.StartAsync(JsonStream("[]"));

        Assert.True(result.IsFailure);
        Assert.Equal("EMPTY_FILE", result.Error!.Code);
        Assert.Empty(queue.Enqueued);
    }
}
```

Run: `dotnet test SensorAnalysis.Application.Tests --filter FullyQualifiedName~ProcessSensorFileServiceTests`
Expected: 2 testes, todos `Passed`.

- [ ] **Step 5: Escrever `DownloadResultsServiceTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/DownloadResultsServiceTests.cs`:

```csharp
using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Domain.Entities;
using SensorAnalysis.Domain.Services;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class DownloadResultsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithBlankJobId_ReturnsInvalidJobId()
    {
        var service = new DownloadResultsService(new FakeJobRepository());

        var result = await service.ExecuteAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_JOB_ID", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownJobId_ReturnsJobNotFound()
    {
        var service = new DownloadResultsService(new FakeJobRepository());

        var result = await service.ExecuteAsync("missing-job");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedJob_ReturnsJobFailed()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-1", 1);
        job.MarkAsFailed("boom");
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-1");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_FAILED", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithProcessingJob_ReturnsJobNotCompleted()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-2", 1);
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-2");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_COMPLETED", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedJobWithoutResults_ReturnsNoResults()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-3", 1);
        job.Process(new List<SensorSample>(), new SensorEvaluator(), new StubAnomalyDetector());
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-3");

        Assert.True(result.IsFailure);
        Assert.Equal("NO_RESULTS", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedJobWithResults_ReturnsSuccess()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-4", 1);
        var sample = SensorSample.Create("s1", "env", DateTime.UtcNow, 20, 50, 10);
        job.Process(new List<SensorSample> { sample }, new SensorEvaluator(), new StubAnomalyDetector());
        await repository.AddAsync(job);
        var service = new DownloadResultsService(repository);

        var result = await service.ExecuteAsync("job-4");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Results);
    }
}
```

Run: `dotnet test SensorAnalysis.Application.Tests --filter FullyQualifiedName~DownloadResultsServiceTests`
Expected: 6 testes, todos `Passed`.

- [ ] **Step 6: Escrever `GetJobStatusServiceTests.cs`**

Crie `ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/GetJobStatusServiceTests.cs`:

```csharp
using SensorAnalysis.Application.ApplicationServices;
using SensorAnalysis.Domain.Entities;
using Xunit;

namespace SensorAnalysis.Application.Tests;

public class GetJobStatusServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithBlankJobId_ReturnsInvalidJobId()
    {
        var service = new GetJobStatusService(new FakeJobRepository());

        var result = await service.ExecuteAsync("");

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_JOB_ID", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownJobId_ReturnsJobNotFound()
    {
        var service = new GetJobStatusService(new FakeJobRepository());

        var result = await service.ExecuteAsync("missing-job");

        Assert.True(result.IsFailure);
        Assert.Equal("JOB_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WithKnownJobId_ReturnsMappedStatus()
    {
        var repository = new FakeJobRepository();
        var job = JobStatus.Create("job-5", 3);
        await repository.AddAsync(job);
        var service = new GetJobStatusService(repository);

        var result = await service.ExecuteAsync("job-5");

        Assert.True(result.IsSuccess);
        Assert.Equal("job-5", result.Value!.JobId);
        Assert.Equal("processing", result.Value!.Status);
        Assert.Equal(3, result.Value!.TotalSamples);
    }
}
```

Run: `dotnet test SensorAnalysis.Application.Tests --filter FullyQualifiedName~GetJobStatusServiceTests`
Expected: 3 testes, todos `Passed`.

- [ ] **Step 7: Rodar a suíte completa do projeto e depois a solução inteira**

Run: `dotnet test SensorAnalysis.Application.Tests`
Expected: 11 testes, todos `Passed`.

Run: `dotnet test` (na raiz de `ProcessamentoDeAmostrasBackEnd`, executa `SensorAnalysis.slnx`)
Expected: 25 testes no total (14 de `Domain.Tests` + 11 de `Application.Tests`), todos `Passed`; nenhum erro de build nos demais projetos da solução.

- [ ] **Step 8: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/SensorAnalysis.Application.Tests/ \
        ProcessamentoDeAmostrasBackEnd/SensorAnalysis.slnx
git commit -m "test: add SensorAnalysis.Application.Tests covering ProcessSensorFileService, DownloadResultsService and GetJobStatusService"
```

---

### Task 9: Atualizar documentação

**Files:**
- Modify: `ProcessamentoDeAmostrasBackEnd/README.md`
- Modify: `README.md` (raiz)

**Interfaces:** nenhuma — apenas texto.

- [ ] **Step 1: Corrigir o diagrama de camadas em `ProcessamentoDeAmostrasBackEnd/README.md`**

Troque:

```markdown
O projeto segue **Clean Architecture** com **DDD**, organizado em quatro camadas com dependências apontando sempre para o centro:

```
SensorAnalysis.API
  └── SensorAnalysis.Application
        └── SensorAnalysis.Infrastructure
              └── SensorAnalysis.Domain   ← sem dependências externas
```
```

por:

```markdown
O projeto segue **Clean Architecture** com **DDD**, organizado em quatro camadas com dependências apontando sempre para o centro:

```
SensorAnalysis.Domain          ← sem dependências externas
  ↑
SensorAnalysis.Application     ← depende apenas do Domain
  ↑
SensorAnalysis.Infrastructure  ← implementa os contratos do Domain; depende de Domain + Application
  ↑
SensorAnalysis.API             ← composition root; referencia as três camadas para configurar a injeção de dependência
```
```

- [ ] **Step 2: Corrigir a lista de Interfaces do Domain**

Troque:

```markdown
- **Interfaces** — `IJobRepository` e `IAnomalyDetector` são contratos implementados pela Infrastructure, mantendo o Domain desacoplado de detalhes técnicos.
```

por:

```markdown
- **Interfaces** — `IJobRepository`, `IAnomalyDetector`, `IMessagePublisher` e `IJobProcessingQueue` são contratos implementados pela Infrastructure, mantendo o Domain desacoplado de detalhes técnicos.
```

- [ ] **Step 3: Corrigir os nomes de classes da Application**

Troque:

```markdown
- **`ProcessSensorFileUseCase`** — recebe as amostras parseadas, cria o `JobStatus`, dispara o processamento em background (fire-and-forget com tratamento interno de exceção) e persiste o estado final ao término.
- **`DownloadResultsUseCase`** — valida o estado do job e retorna os resultados encapsulados em `Result<DownloadResultDto>`.
- **`SensorFileParser`** — deserializa o stream JSON recebido pelo controller e constrói as entidades de domínio.
- **DTOs e Mappers** — isolam a representação interna do domínio do contrato público da API.
```

por:

```markdown
- **`ProcessSensorFileService`** — recebe as amostras parseadas, cria o `JobStatus`, persiste o estado inicial e enfileira o job via `IJobProcessingQueue` — não executa mais o processamento diretamente.
- **`DownloadResultsService`** / **`GetJobStatusService`** — validam o estado do job e retornam os resultados/status encapsulados em `Result<T>`.
- **`SensorFileParser`** — deserializa o stream JSON recebido pelo controller e constrói as entidades de domínio.
- **DTOs e Mappers** — isolam a representação interna do domínio do contrato público da API (camelCase, consistente em todos os endpoints).
```

- [ ] **Step 4: Adicionar a fila gerenciada à lista da Infrastructure**

Troque:

```markdown
- **`IqrAnomalyDetector`** — implementa `IAnomalyDetector`. Calcula Q1 e Q3 sobre o conjunto de amostras válidas, deriva os limites via `IQR × 1.5` e marca como anomalia qualquer leitura fora dos bounds.
- **`RabbitMqPublisher`** — implementa `IMessagePublisher`. Estabelece conexão AMQP, declara a fila como `durable` e publica `SensorAnomalyDetected` serializado em JSON.
- **`InMemoryJobRepository`** — implementa `IJobRepository` com `ConcurrentDictionary`. `AddAsync` garante unicidade via `TryAdd`; `UpdateAsync` rejeita explicitamente jobs inexistentes.
```

por:

```markdown
- **`IqrAnomalyDetector`** — implementa `IAnomalyDetector`. Calcula Q1 e Q3 (Temperatura, Umidade e Ponto de Orvalho) sobre o conjunto de amostras válidas, deriva os limites via `IQR × 1.5` e marca como anomalia qualquer leitura fora dos bounds.
- **`RabbitMqPublisher`** — implementa `IMessagePublisher`. Estabelece conexão AMQP, declara a fila como `durable` e publica `SensorAnomalyDetected` serializado em JSON.
- **`InMemoryJobRepository`** — implementa `IJobRepository` com `ConcurrentDictionary`. `AddAsync` garante unicidade via `TryAdd`; `UpdateAsync` rejeita explicitamente jobs inexistentes.
- **`ChannelJobProcessingQueue`** + **`JobProcessingBackgroundService`** — implementam `IJobProcessingQueue`. Um `Channel<ProcessingJob>` recebe os jobs enfileirados pela Application e um `BackgroundService` do próprio host os processa, com ciclo de vida gerenciado pelo ASP.NET Core (substitui o antigo fire-and-forget).
```

- [ ] **Step 5: Corrigir os nomes de classes no `README.md` raiz**

Troque:

```markdown
- **Application** orquestra os casos de uso (`ProcessSensorFileUseCase`, `DownloadResultsUseCase`) sem conter regras de negócio.
```

por:

```markdown
- **Application** orquestra os casos de uso (`ProcessSensorFileService`, `DownloadResultsService`, `GetJobStatusService`) sem conter regras de negócio.
```

- [ ] **Step 6: Corrigir o trade-off do RabbitMQ no `README.md` raiz**

Troque:

```markdown
* **Mensageria com RabbitMQ (Desacoplamento):**
  O processamento de arquivos `.json` com milhares de leituras pode ser custoso. Se a API processasse isso de forma síncrona, a requisição HTTP ficaria presa, prejudicando a UX e o uso de recursos do servidor. O RabbitMQ entra para garantir o padrão *Fire and Forget* na ingestão: a API recebe o arquivo, publica o evento na fila e responde instantaneamente ao cliente. O processamento pesado ocorre em *background*, garantindo resiliência sob carga.
```

por:

```markdown
* **Processamento em background (Channel + BackgroundService) e RabbitMQ para notificações:**
  O processamento de arquivos `.json` com milhares de leituras pode ser custoso. Se a API processasse isso de forma síncrona, a requisição HTTP ficaria presa, prejudicando a UX e o uso de recursos do servidor. Por isso, o upload apenas enfileira o job em um `Channel` em memória e responde instantaneamente ao cliente; um `BackgroundService` do próprio host consome a fila e faz o processamento pesado, com ciclo de vida gerenciado pelo ASP.NET Core. O RabbitMQ entra depois, exclusivamente para publicar notificações de anomalia detectada — não participa da ingestão do job.
```

- [ ] **Step 7: Commit**

```bash
git add ProcessamentoDeAmostrasBackEnd/README.md README.md
git commit -m "docs: fix backend README to match the actual layer dependencies and class names"
```

---

## Verificação final

- [ ] Run: `cd ProcessamentoDeAmostrasBackEnd && dotnet build && dotnet test`
  Expected: build com sucesso em todos os projetos; 25 testes `Passed`, 0 `Failed`.
- [ ] Run manual: com `dotnet run` em `SensorAnalysis.API` e `yarn dev` no frontend, repita o fluxo completo (upload → polling de status → download) pelo browser em `http://localhost:3000` e confirme que o dashboard renderiza normalmente.
