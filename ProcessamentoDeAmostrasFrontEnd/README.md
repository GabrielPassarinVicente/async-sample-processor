# 🖥️ Sensores Ambientais — Front-end

Dashboard interativo para análise de leituras de sensores IoT ambientais. A interface permite ao usuário fazer upload de um arquivo `.json` com amostras de **temperatura**, **umidade** e **ponto de orvalho**, acompanhar o processamento em tempo real via barra de progresso e, ao concluir, explorar gráficos de linha e KPIs de anomalias detectadas — tudo sem recarregar a página.

A comunicação com a API é totalmente assíncrona: o upload retorna imediatamente um `jobId` e a aplicação faz **polling** em background até o processamento concluir, atualizando o estado reativo em tempo real.

---

## 🛠️ Tecnologias

| Tecnologia | Por que foi escolhida |
|---|---|
| **[Nuxt 4](https://nuxt.com/)** | Auto-imports de composables, componentes e utilitários do Vue sem configuração. Roteamento baseado em arquivos elimina boilerplate de `vue-router`. `runtimeConfig` com suporte nativo a variáveis de ambiente para diferentes ambientes (dev/Docker/prod). |
| **[Vue 3](https://vuejs.org/) + `<script setup>`** | Composition API com inferência de tipos perfeita com TypeScript. `<script setup>` reduz boilerplate e mantém a lógica coesa perto do template. |
| **[TypeScript](https://www.typescriptlang.org/)** | Tipagem estática em toda a base: componentes, composables, services e types. Erros de contrato com a API são capturados em tempo de compilação. |
| **[@nuxt/ui](https://ui.nuxt.com/) + [Tailwind CSS](https://tailwindcss.com/)** | Biblioteca de componentes (botões, cards, progress bar, ícones Heroicons) com design system baseado em Tailwind. Zero CSS customizado necessário para a UI base. |
| **[Chart.js](https://www.chartjs.org/) + [vue-chartjs](https://vue-chartjs.org/)** | Gráficos de linha interativos com suporte a datasets reativos. O wrapper `vue-chartjs` integra nativamente com o sistema de reatividade do Vue 3. |
| **[chartjs-plugin-annotation](https://www.chartjs.org/chartjs-plugin-annotation/)** | Linhas de limiar (alerta/crítico) sobrepostas nos gráficos sem lógica de renderização customizada. |
| **[Axios](https://axios-http.com/)** | Cliente HTTP com suporte a interceptors para normalização de resposta e tipagem da camada de service. |

---

## 🚀 Rodando Localmente

### Pré-requisitos

- **Node.js** >= 18
- Back-end da API rodando em `http://localhost:5279` (ou configure via variável de ambiente)

### Instalação e execução

```bash
# Instale as dependências
yarn install

# Inicie o servidor de desenvolvimento
yarn dev
```

O dashboard estará disponível em **http://localhost:3000**.

### Build de produção

```bash
yarn generate   # SPA estática (padrão — usada no Docker)
yarn build      # SSR (Node.js server)
yarn preview    # Visualiza o build localmente
```

---

## ⚙️ Variáveis de Ambiente

Crie um arquivo `.env` na raiz desta pasta para sobrescrever a URL padrão da API:

```env
# URL base da API REST (sem trailing slash)
NUXT_PUBLIC_API_BASE_URL=http://localhost:5279
```

| Variável | Padrão | Descrição |
|---|---|---|
| `NUXT_PUBLIC_API_BASE_URL` | `http://localhost:5279` | URL base da API. No Docker, use o nome do serviço: `http://localhost:8080` |

> O Nuxt mapeia `NUXT_PUBLIC_*` automaticamente para `useRuntimeConfig().public.*` — sem código extra.

---

## 📁 Estrutura de Diretórios

```
app/
├── pages/          ← Roteamento. Componentes de rota limpos, zero lógica de negócio
├── components/     ← UI isolada e reutilizável (dumb components orientados a props)
├── composables/    ← Lógica complexa: requisições, polling, estado reativo e KPIs
├── services/       ← Camada de acesso à API (instância Axios + normalização de resposta)
├── layouts/        ← Estrutura base das telas (header, slot de conteúdo)
├── types/          ← Interfaces TypeScript que definem o contrato de dados
└── constants/      ← Constantes de domínio (limiares de alerta/crítico)
```

### `pages/` — Roteamento limpo

Contém apenas `index.vue`. A página não gerencia estado, não faz requisições e não contém lógica condicional de negócio. Ela extrai tudo do composable `useSensorAnalysis` e renderiza condicionalmente o formulário de upload ou o `<Dashboard>`.

```vue
<script setup lang="ts">
const { results, handleSubmit, ... } = useSensorAnalysis()
</script>
```

Essa separação garante que a página seja testável de forma isolada e que a lógica de upload/polling possa ser reutilizada sem acoplamento ao componente de rota.

### `components/` — UI isolada

- **`Dashboard.vue`** — recebe `SensorReading[]` via props e delega cálculos para `useKpis` e `useChartConfig`. Não busca dados, não tem side effects. Contém o filtro por tipo de sensor, cards de KPI, barras de progresso de distribuição e três gráficos de linha.

### `composables/` — Lógica e estado

- **`useSensorAnalysis.ts`** — orquestra o fluxo completo de upload e polling. Cria a instância do service, gerencia o estado reativo (`isProcessing`, `progress`, `results`, `hasError`), dispara o polling com `setInterval` (500ms, timeout de 5min) e expõe handlers para a página.

- **`useKpis.ts`** — derivação puramente computada: recebe `Ref<SensorReading[]>` e retorna `computed<Kpis>` com totais e percentuais por categoria (normal, anomalia, inválido, breakdowns por métrica e limiar).

- **`useChartConfig.ts`** — constrói os objetos `data` e `options` do Chart.js de forma reativa, incluindo as linhas de anotação com os valores de `sensorLimits.ts`.

### `services/` — Acesso à API

- **`sensorService.ts`** — factory function que recebe a `baseURL` e retorna um objeto com três métodos: `uploadFile` (multipart), `getJobStatus` (polling) e `downloadResults` (blob download). Centraliza a normalização de resposta para garantir consistência no modelo de dados independente do casing retornado pelo back-end (camelCase ou PascalCase).

### `layouts/` — Estrutura base

- **`default.vue`** — header fixo (sticky) com nome da aplicação e indicador de "Sistema ativo". Envolve todas as páginas via `<slot />`.

### `types/` e `constants/`

- **`sensor.ts`** — interfaces TypeScript do domínio: `SensorReading`, `AnalysisResult`, `SensorMetricResult`, `SensorStatus`, `LimitType`.
- **`sensorLimits.ts`** — objeto `SENSOR_LIMITS` com os limiares de alerta e crítico por métrica. Único ponto de verdade para essas regras no front-end — alterações aqui refletem automaticamente nos gráficos e no cálculo de KPIs.

