# Sensor Analysis API

REST API desenvolvida em **.NET 10** para recebimento, processamento assíncrono e análise de amostras de sensores ambientais. A API aceita arquivos JSON com leituras de temperatura, umidade e ponto de orvalho, processa os dados em background via algoritmo estatístico **IQR (Interquartile Range)** para detecção de anomalias e publica eventos de alerta em uma fila **RabbitMQ**.

O estado dos jobs é gerenciado in-memory via `ConcurrentDictionary`, sem dependência de banco de dados externo.

---

## Tecnologias

| Tecnologia | Versão | Finalidade |
|---|---|---|
| C# | 14 | Linguagem principal |
| .NET | 10 | Runtime e SDK |
| ASP.NET Core | 10 | Web API com Controllers |
| RabbitMQ | 3.x | Broker para publicação de eventos de anomalia |
| RabbitMQ.Client | latest | Client AMQP oficial |

---

## Como rodar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Instância do RabbitMQ acessível (veja abaixo como subir via Docker)

**Subindo o RabbitMQ:**

```bash
docker run -d --name rabbitmq \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management-alpine
```

### Executando a API

```bash
# 1. Restaurar dependências
dotnet restore

# 2. Compilar
dotnet build

# 3. Executar
dotnet run --project SensorAnalysis.API
```

A API estará disponível em `http://localhost:8080`.

### Configuração

As credenciais do RabbitMQ são definidas em `SensorAnalysis.API/appsettings.json`:

```json
"RabbitMqSettings": {
  "HostName": "localhost",
  "UserName": "admin",
  "Password": "admin123",
  "QueueName": "log_notifications"
}
```

Em produção, sobrescreva via variáveis de ambiente:

```
RabbitMqSettings__HostName
RabbitMqSettings__UserName
RabbitMqSettings__Password
RabbitMqSettings__QueueName
```

---

## Endpoints

| Método | Rota | HTTP Status | Descrição |
|---|---|---|---|
| `GET` | `/api/sensor/health` | `200` | Verifica se a API está no ar |
| `POST` | `/api/sensor/upload` | `202` | Envia o arquivo JSON; retorna um `jobId` |
| `GET` | `/api/sensor/status/{jobId}` | `200` | Consulta estado e progresso do job |
| `GET` | `/api/sensor/download/{jobId}` | `200` | Baixa o resultado da análise em JSON |

O fluxo é assíncrono por design: `POST /upload` retorna `202 Accepted` imediatamente com um `jobId`. O cliente faz polling em `/status/{jobId}` até o estado ser `Completed` e consome o resultado via `/download/{jobId}`.

---

## Arquitetura

O projeto segue **Clean Architecture** com **DDD**, organizado em quatro camadas com dependências apontando sempre para o centro:

```
SensorAnalysis.API
  └── SensorAnalysis.Application
        └── SensorAnalysis.Infrastructure
              └── SensorAnalysis.Domain   ← sem dependências externas
```

### Domain

Núcleo isolado da aplicação. Zero referência a frameworks, ORMs ou bibliotecas externas. Representa o modelo de negócio puro.

- **Entities** — `JobStatus` é o Aggregate Root: gerencia o ciclo de vida completo de um job via factory method, controla transições de estado (`Processing → Completed | Failed`) e protege invariantes (ex: impede `Completed → Failed`). `SensorSample` encapsula e valida uma leitura de sensor na criação.
- **Value Objects** — `SampleAnalysis` e `MetricAnalysis` são imutáveis e representam o resultado da avaliação de cada métrica. `MetricThresholds` define os limites de alerta e crítico por grandeza.
- **Domain Services** — `SensorEvaluator` avalia cada amostra individualmente contra os thresholds e produz um `SampleAnalysis`.
- **Domain Events** — `SensorAnomalyDetected` é emitido para cada amostra classificada como anômala ou crítica.
- **Interfaces** — `IJobRepository` e `IAnomalyDetector` são contratos implementados pela Infrastructure, mantendo o Domain desacoplado de detalhes técnicos.
- **Common** — `Result<T>` e `Error` implementam retorno explícito de falhas sem uso de exceções como controle de fluxo.

### Application

Orquestra os casos de uso coordenando domínio e infraestrutura. Não contém regras de negócio.

- **`ProcessSensorFileUseCase`** — recebe as amostras parseadas, cria o `JobStatus`, dispara o processamento em background (fire-and-forget com tratamento interno de exceção) e persiste o estado final ao término.
- **`DownloadResultsUseCase`** — valida o estado do job e retorna os resultados encapsulados em `Result<DownloadResultDto>`.
- **`SensorFileParser`** — deserializa o stream JSON recebido pelo controller e constrói as entidades de domínio.
- **DTOs e Mappers** — isolam a representação interna do domínio do contrato público da API.

### Infrastructure

Implementa os contratos definidos pelo Domain. Aqui residem os detalhes técnicos e as dependências externas.

- **`IqrAnomalyDetector`** — implementa `IAnomalyDetector`. Calcula Q1 e Q3 sobre o conjunto de amostras válidas, deriva os limites via `IQR × 1.5` e marca como anomalia qualquer leitura fora dos bounds.
- **`RabbitMqPublisher`** — implementa `IMessagePublisher`. Estabelece conexão AMQP, declara a fila como `durable` e publica `SensorAnomalyDetected` serializado em JSON.
- **`InMemoryJobRepository`** — implementa `IJobRepository` com `ConcurrentDictionary`. `AddAsync` garante unicidade via `TryAdd`; `UpdateAsync` rejeita explicitamente jobs inexistentes.

### API

Ponto de entrada HTTP. Delega toda a lógica aos casos de uso e trata apenas aspectos de transporte.

- **`SensorController`** — expõe os quatro endpoints, registra logs estruturados por etapa e mapeia os `Error.Code` do domínio para os HTTP status corretos.
- **`Program.cs`** — registra dependências no container de DI, configura CORS, Swagger e monta o pipeline de middlewares.

---
```