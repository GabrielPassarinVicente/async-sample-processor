# 🌡️ Sensores Ambientais — Processamento Assíncrono de Amostras

Sistema **full-stack** de análise de leituras de sensores IoT ambientais. O usuário acessa o **dashboard web**, faz upload de um arquivo `.json` com amostras de temperatura, umidade e ponto de orvalho, e acompanha o processamento em tempo real via barra de progresso. Ao concluir, o painel exibe KPIs, gráficos de linha e distribuição de anomalias por limiar.

Por baixo dos panos, a **API em .NET 10** processa as amostras de forma assíncrona em background, detecta anomalias via **algoritmo IQR** e publica eventos de alerta no **RabbitMQ** — tudo sem bloquear o cliente.

---

## 📁 Estrutura do Repositório

```
Sensores Ambientais/
├── docker-compose.yml                      ← orquestra todos os serviços
├── .env.example                            ← variáveis de ambiente (copie para .env)
├── .gitignore
├── README.md                               ← você está aqui
├── ProcessamentoDeAmostras - FrontEnd/     ← dashboard Nuxt 4 + Vue 3
└── ProcessamentoDeAmostras - BackEnd/      ← API .NET 10 (DDD + Clean Architecture + RabbitMQ )
```

---

## 🚀 Como Rodar

### Pré-requisito único: Docker

```bash
# 1. Clone o repositório
git clone <URL-DO-REPOSITORIO>
cd Sensores Ambientais

# 2. Copie o arquivo de variáveis de ambiente
cp .env.example .env

# 3. Suba tudo
docker-compose up --build
```

Pronto. Acesse o dashboard em **http://localhost**.

| Serviço | URL |
|---|---|
| **Dashboard (front-end)** | `http://localhost` |
| API | `http://localhost:8080` |
| RabbitMQ Management | `http://localhost:15672` (user: `guest` / pass: `guest`) |

> O front-end já está configurado para apontar para a API dentro da rede Docker. Nenhuma configuração adicional é necessária.

---

### Execução Local (sem Docker)

Precisa de: [.NET 10 SDK](https://dotnet.microsoft.com/download) e [Node.js >= 18](https://nodejs.org/).

**RabbitMQ (Docker somente o serviço):**
```bash
docker-compose up rabbitmq -d
```

**Back-end:**
```bash
cd "ProcessamentoDeAmostras - BackEnd/SensorAnalysis.API"
dotnet run
# API disponível em http://localhost:5279
```

**Front-end:**
```bash
cd "ProcessamentoDeAmostras - FrontEnd"
yarn install
yarn dev
# Dashboard disponível em http://localhost:3000
```

---

## ⚙️ Variáveis de Ambiente

O arquivo `.env` fica na raiz do projeto, ao lado do `docker-compose.yml`. O `.env.example` contém todos os valores necessários para rodar com Docker sem modificação:

| Variável | Padrão | Descrição |
|---|---|---|
| `RABBITMQ_USER` | `guest` | Usuário do RabbitMQ |
| `RABBITMQ_PASS` | `guest` | Senha do RabbitMQ |
| `RABBITMQ_PORT` | `5672` | Porta AMQP |
| `RABBITMQ_MGMT_PORT` | `15672` | Porta do painel de gestão |
| `BACKEND_PORT` | `8080` | Porta exposta da API |
| `FRONTEND_PORT` | `80` | Porta exposta do front-end |
| `CORS_ORIGINS` | `http://localhost:3000,http://localhost` | Origens permitidas pelo CORS |


---

## 🛠️ Tecnologias

### Back-end
- **[.NET 10](https://dotnet.microsoft.com/)** + **ASP.NET Core** — Web API com Clean Architecture e DDD
- **[C# 14](https://learn.microsoft.com/dotnet/csharp/)** — language features modernos
- **[RabbitMQ](https://www.rabbitmq.com/)** — mensageria para eventos de anomalia via AMQP
- **Algoritmo IQR** — detecção estatística de outliers no conjunto de amostras
- **Armazenamento in-memory** — `ConcurrentDictionary` para jobs (sem banco de dados externo)

### Front-end
- **[Nuxt 4](https://nuxt.com/)** + **[Vue 3](https://vuejs.org/)** — Composition API com `<script setup>`
- **[TypeScript](https://www.typescriptlang.org/)** — tipagem estática em toda a base de código
- **[@nuxt/ui](https://ui.nuxt.com/)** + **[Tailwind CSS](https://tailwindcss.com/)** — componentes e estilização
- **[Chart.js](https://www.chartjs.org/)** + **[vue-chartjs](https://vue-chartjs.org/)** — gráficos de linha interativos
- **[Axios](https://axios-http.com/)** — cliente HTTP com normalização de resposta da API

### Infraestrutura
- **[Docker](https://www.docker.com/) + Docker Compose** — containerização completa da stack

---

## 🏗️ Arquitetura

### Back-end — Clean Architecture + DDD

```
SensorAnalysis.Domain          ← Núcleo protegido, zero dependências externas
SensorAnalysis.Application     ← Casos de uso e orquestração
SensorAnalysis.Infrastructure  ← RabbitMQ, IQR, repositório in-memory
SensorAnalysis.API             ← Controllers e pipeline HTTP
```

- **Domain** concentra as regras de negócio: `JobStatus` (Aggregate Root), `SensorSample`, `SampleAnalysis` (Value Objects), `SensorEvaluator` (Domain Service) e `Result<T>` para retorno explícito de erros.
- **Application** orquestra os casos de uso (`ProcessSensorFileUseCase`, `DownloadResultsUseCase`) sem conter regras de negócio.
- **Infrastructure** implementa os detalhes técnicos: `IqrAnomalyDetector`, `RabbitMqPublisher` e `InMemoryJobRepository`.

### Front-end — Nuxt 4 (Composition API)

Páginas — toda a lógica vive nos composables.

- **`pages/`** — apenas `index.vue`; delega tudo para `useSensorAnalysis`
- **`components/`** — `Dashboard.vue` recebe dados via props e renderiza KPIs + gráficos
- **`composables/`** — `useSensorAnalysis` (fluxo principal + polling), `useKpis` (métricas derivadas), `useChartConfig` (configuração dos gráficos)
- **`services/`** — `sensorService.ts` isola o Axios e normaliza as respostas da API
- **`layouts/`** — `default.vue` com header fixo e indicador de status

> Para mais detalhes de cada camada, veja os READMEs individuais em cada subpasta.

## 🧠 Decisões Técnicas e Trade-offs

Como engenheiro, acredito que toda decisão de arquitetura envolve *trade-offs*. Abaixo, detalho as justificativas para as principais escolhas técnicas deste desafio:

* **Algoritmo IQR (Interquartile Range) vs. Limiares Fixos:**
  Sensores ambientais geram dados que variam muito dependendo do contexto (estação do ano, localização). Usar *hardcoded thresholds* (ex: "temperatura > 30°C é anomalia") geraria falsos positivos. O IQR foi escolhido porque é um método estatístico robusto que identifica *outliers* com base na própria distribuição do lote de amostras, tornando o sistema adaptável e inteligente, independentemente do ambiente monitorado.

* **Mensageria com RabbitMQ (Desacoplamento):**
  O processamento de arquivos `.json` com milhares de leituras pode ser custoso. Se a API processasse isso de forma síncrona, a requisição HTTP ficaria presa, prejudicando a UX e o uso de recursos do servidor. O RabbitMQ entra para garantir o padrão *Fire and Forget* na ingestão: a API recebe o arquivo, publica o evento na fila e responde instantaneamente ao cliente. O processamento pesado ocorre em *background*, garantindo resiliência sob carga.

* **Armazenamento In-Memory (`ConcurrentDictionary`) vs. Banco de Dados:**
  Para o escopo de um teste técnico, exigir que o avaliador suba um container de banco de dados (ex: SQL Server ou PostgreSQL) e rode *migrations* adicionaria complexidade e tempo de *setup* desnecessários. Optei por usar um `ConcurrentDictionary` no repositório para gerenciar o estado dos *jobs* de forma *thread-safe*. Isso demonstra domínio de concorrência em C# e mantém o foco no que importa: a modelagem do domínio e a arquitetura, sem sacrificar a Experiência do Desenvolvedor (DX) de quem vai avaliar o projeto.