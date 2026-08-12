# Design: Correção de Arquitetura e Clean Code do Backend

**Data:** 2026-08-12
**Contexto:** Revisão pós-entrevista apontou que a arquitetura do backend está "misturada". Este documento descreve os achados confirmados e o desenho da correção.

## Achados (auditoria)

Revisão completa de `ProcessamentoDeAmostrasBackEnd` (Clean Architecture + DDD, .NET 10) e `ProcessamentoDeAmostrasFrontEnd` (Nuxt 4 + Vue 3).

O **frontend está limpo**: `pages/` delega para `composables/`, `services/sensorService.ts` isola o Axios, `components/Dashboard.vue` é "burro" (recebe dados via props). Nenhuma correção de arquitetura necessária ali — apenas uma simplificação decorrente da correção #3 abaixo.

O **backend tem 6 problemas reais**, além de itens cosméticos que não justificam desenho próprio (nova conexão AMQP por publicação em `RabbitMqPublisher`; pasta `Controller` vs namespace `Controllers`; `Services/` vs `ApplicationServices/` guardando o mesmo tipo de classe) e serão corrigidos de passagem durante a implementação:

1. **Porta fora de padrão:** `IJobRepository` e `IAnomalyDetector` vivem em `SensorAnalysis.Domain.Interfaces` (correto — Domain é dono da abstração), mas `IMessagePublisher` vive em `SensorAnalysis.Application.Interfaces`, mesmo publicando um Domain Event (`SensorAnomalyDetected`). Isso é a única razão pela qual `SensorAnalysis.Infrastructure.csproj` referencia `SensorAnalysis.Application.csproj`.
2. **Diagrama do README invertido:** o README descreve as camadas como corrente linear `API → Application → Infrastructure → Domain`, que lida como direção de dependência é o inverso do Clean Architecture real e não corresponde aos `.csproj` reais (`Domain` sem deps; `Application → Domain`; `Infrastructure → Domain + Application`; `API → todos`, como composition root).
3. **Processamento "assíncrono" é uma `Task` órfã:** `ProcessSensorFileService.StartAsync` dispara `_ = ProcessInBackgroundAsync(job, samples)` — uma `Task` sem gestão de ciclo de vida pelo host, sem cancelamento, que desaparece num restart do processo. O RabbitMQ hoje só notifica anomalias depois do fato; não orquestra o job em si.
4. **Contrato JSON inconsistente dentro da mesma resposta:** `/status/{jobId}` serializa os campos de topo em camelCase (policy padrão do ASP.NET Core) mas `results[]` (`AnalyzedSampleDto`) tem atributos `[JsonPropertyName]` explícitos forçando snake_case — uma única resposta mistura os dois casings. `/download` reserializa manualmente com `JsonNamingPolicy.SnakeCaseLower`, redundante com os atributos já existentes nos DTOs. Isso obriga `sensorService.ts` a normalizar defensivamente `dew_point`/`dewPoint`/`DewPoint` para o mesmo campo.
5. **Bug funcional no `IqrAnomalyDetector`:** avalia bounds estatísticos só para Temperatura e Umidade — ignora Ponto de Orvalho, que `SensorEvaluator` avalia normalmente. Implementado com dois blocos de código quase idênticos (duplicação).
6. **Configuração morta — CORS:** `docker-compose.yml` e `.env.example` documentam `CORS_ORIGINS`/`CORS__Origins` como se controlassem os domínios permitidos, mas `Program.cs` ignora essa configuração e usa `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` fixo no código.
7. **Zero testes automatizados** em todo o repositório (backend e frontend) — para um projeto que se apresenta como Clean Architecture + DDD, isso desaproveita a principal vantagem prática da arquitetura (Domain isolável e testável sem infraestrutura).

## Correções desenhadas

### 1. Porta do publisher no lugar certo

Mover `IMessagePublisher` de `SensorAnalysis.Application.Interfaces` para `SensorAnalysis.Domain.Interfaces`, ao lado de `IJobRepository` e `IAnomalyDetector`. `SensorAnalysis.Infrastructure.csproj` deixa de referenciar `SensorAnalysis.Application.csproj` — passa a depender só de `SensorAnalysis.Domain.csproj`, como o próprio README já afirma (mas o código não cumpria).

### 2. Processamento gerenciado (Channel + BackgroundService)

Substituir a `Task` solta por uma fila em memória:

- Um `Channel<ProcessingJob>` (registrado como singleton, escrito via `ChannelWriter<ProcessingJob>` e lido via `ChannelReader<ProcessingJob>`) fica na `Infrastructure`.
- `ProcessSensorFileService.StartAsync` passa a **só** criar o `JobStatus`, persistir o estado inicial, escrever na fila e retornar o `jobId` — sem mais nenhuma lógica de execução do processamento. Isso torna a Application "orquestração pura", como o comentário no código já promete.
- Um novo `JobProcessingBackgroundService : BackgroundService` (Infrastructure) lê da fila e executa o fluxo DDD de 3 passos (`job.Process(...)` → despachar Domain Events → persistir) que hoje está dentro de `ProcessSensorFileService.ProcessAsync`. Tratamento de falha vira `job.MarkAsFailed(...)` dentro do próprio `BackgroundService`.
- Sem infraestrutura nova (nenhum banco, nenhum broker adicional) — RabbitMQ continua exclusivamente para notificar anomalias, seu papel atual.

### 3. Contrato JSON consistente (camelCase)

- Remover os atributos `[JsonPropertyName]` snake_case de `AnalyzedSampleDto` e `DownloadResultDto`.
- Remover a serialização manual em `SensorController.DownloadResults`; configurar `WriteIndented = true` globalmente via `AddJsonOptions` para preservar a formatação legível do arquivo de download.
- Resultado: toda a API (`/upload`, `/status`, `/download`) responde consistentemente em camelCase — a policy padrão do ASP.NET Core, e a convenção que o frontend já usa internamente em `types/sensor.ts`.
- Simplificar `sensorService.ts`: remover as verificações defensivas de casing alternativo (`dew_point`/`DewPoint`) que hoje só existem para compensar essa inconsistência.

### 4. CORS configurável

Em `Program.cs`, ler as origens permitidas de configuração (chave `Cors:Origins`, populada por `CORS__Origins` no `docker-compose.yml`, mesmo formato de string separada por vírgula já usado em `CORS_ORIGINS` no README raiz — `Split(',')` na leitura) e usar `.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Content-Disposition")`. Se a configuração vier vazia (execução local sem `.env`), usar `http://localhost:3000` como default seguro em vez de aceitar qualquer origem.

### 5. Bug do IQR

Generalizar `IqrAnomalyDetector.DetectAnomalies` para iterar as três métricas (Temperatura, Umidade, Ponto de Orvalho) em um único loop de cálculo de bounds, eliminando a duplicação `tempBounds`/`humBounds` e corrigindo a omissão do Ponto de Orvalho no mesmo passo.

### 6. Base de testes

Dois novos projetos xUnit:

- **`SensorAnalysis.Domain.Tests`** — `JobStatus` (transições de estado e invariantes: não reprocessar, não voltar de `Completed` para `Failed`), `SensorEvaluator` (limiares por métrica), `MetricThresholds` (validação de min/max), `IqrAnomalyDetector` (incluindo o caso do Ponto de Orvalho após a correção).
- **`SensorAnalysis.Application.Tests`** — `ProcessSensorFileService` (enfileira e retorna `jobId`; falha de parsing propaga `Result.Failure`) com mocks das interfaces do Domain, `DownloadResultsService`/`GetJobStatusService` (cada `Error.Code`: `JOB_NOT_FOUND`, `JOB_NOT_COMPLETED`, `JOB_FAILED`, `NO_RESULTS`, `INVALID_JOB_ID`).

Fora de escopo nesta rodada: testes de integração HTTP (`WebApplicationFactory`) e testes de Infrastructure para RabbitMQ — exigiriam mocks de broker/rede fora do escopo desta correção de arquitetura.

### 7. Atualização de documentação

Corrigir o `README.md` do backend: diagrama de camadas refletindo a direção real de dependência (`Domain ← Application ← Infrastructure`, API como composition root), nomes de classes reais (`...Service`, não `...UseCase`), e um parágrafo substituindo a descrição de "fire-and-forget" pela fila `Channel` + `BackgroundService`.

## Fora de escopo

- Persistência durável de jobs (banco de dados) — mantém `ConcurrentDictionary`, decisão já documentada e justificada no README raiz.
- RabbitMQ orquestrando o processamento do job em si (Opção B avaliada e descartada em favor da fila em memória).
- Testes de integração HTTP e de Infrastructure (RabbitMQ).
- Correções cosméticas de nomenclatura de pastas/namespaces — aplicadas durante a implementação, sem desenho próprio.
