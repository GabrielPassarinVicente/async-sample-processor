<script setup lang="ts">
import { useSensorAnalysis } from '../composables/useSensorAnalysis'
const {
  isProcessing,
  isDownloading,
  statusMessage,
  results,
  hasError,
  progressPercent,
  selectedFileName,
  resetState,
  handleFileChange,
  handleSubmit,
  handleDownload,
} = useSensorAnalysis()
</script>

<template>

  <Head>
    <Title>Processamento de Amostras</Title>
    <Meta name="description" content="Análise de leituras de sensores IoT" />
  </Head>

  <section v-if="!results" class="flex justify-center items-start pt-16 pb-16">
    <div class="w-full max-w-lg bg-white rounded-2xl border border-slate-200 shadow-md overflow-hidden">

      <div class="bg-gradient-to-br from-primary-600 to-primary-500 px-8 py-8 flex flex-col items-center text-center gap-3">
        <div class="w-14 h-14 rounded-2xl bg-white/20 backdrop-blur flex items-center justify-center shadow">
          <UIcon name="i-heroicons-arrow-up-tray" class="text-white text-3xl" />
        </div>
        <div>
          <h1 class="text-xl font-extrabold text-white tracking-tight">Importar Amostras</h1>
          <p class="text-sm text-primary-100 mt-1 font-medium">
            Arquivo <span class="bg-white/20 text-white font-bold px-1.5 py-0.5 rounded text-xs">.json</span>
            com leituras dos sensores
          </p>
        </div>
      </div>

      <div class="px-8 py-7 flex flex-col gap-5">

        <label
          class="relative flex flex-col items-center justify-center min-h-32 rounded-xl cursor-pointer transition-all duration-200 gap-3 border-2 border-dashed"
          :class="selectedFileName
            ? 'border-green-400 bg-green-50'
            : 'border-slate-200 bg-slate-50 hover:border-primary-400 hover:bg-primary-50'"
        >
          <input
            type="file"
            accept=".json"
            class="absolute inset-0 opacity-0 cursor-pointer w-full h-full"
            @change="handleFileChange"
          />
          <template v-if="selectedFileName">
            <div class="w-10 h-10 rounded-xl bg-green-100 flex items-center justify-center">
              <UIcon name="i-heroicons-document-check" class="text-green-600 text-xl" />
            </div>
            <div class="text-center">
              <p class="text-sm font-bold text-green-700">{{ selectedFileName }}</p>
              <p class="text-xs text-green-500 font-medium mt-0.5">Arquivo selecionado — clique para trocar</p>
            </div>
          </template>
          <template v-else>
            <div class="w-10 h-10 rounded-xl bg-slate-200 flex items-center justify-center">
              <UIcon name="i-heroicons-arrow-up-tray" class="text-slate-500 text-xl" />
            </div>
            <div class="text-center">
              <p class="text-sm font-semibold text-slate-600 select-none">Clique para selecionar o arquivo</p>
              <p class="text-xs text-slate-400 font-medium mt-0.5 select-none">Suporte para arquivos .json</p>
            </div>
          </template>
        </label>

        <div v-if="isProcessing" class="flex flex-col gap-2">
          <div class="flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-500">Processando amostras...</span>
            <span class="text-sm font-extrabold text-primary-600">{{ progressPercent }}%</span>
          </div>
          <UProgress :value="progressPercent" color="primary" size="md" />
        </div>

        <div
          v-if="statusMessage && !isProcessing"
          class="flex items-start gap-3 rounded-xl px-4 py-3"
          :class="hasError ? 'bg-red-50 border border-red-200' : 'bg-blue-50 border border-blue-200'"
        >
          <UIcon
            :name="hasError ? 'i-heroicons-x-circle' : 'i-heroicons-information-circle'"
            class="text-lg mt-0.5 shrink-0"
            :class="hasError ? 'text-red-500' : 'text-blue-500'"
          />
          <p class="text-sm font-medium" :class="hasError ? 'text-red-700' : 'text-blue-700'">
            {{ statusMessage }}
          </p>
        </div>

        <UButton
          class="w-full justify-center text-base font-bold tracking-tight"
          color="primary"
          variant="solid"
          size="lg"
          :disabled="!selectedFileName || isProcessing"
          :loading="isProcessing"
          :icon="isProcessing ? 'i-heroicons-arrow-path' : 'i-heroicons-chart-bar'"
          @click="handleSubmit"
        >
          {{ isProcessing ? 'Processando...' : 'Iniciar Análise' }}
        </UButton>

      </div>
    </div>
  </section>

  <section v-else>

    <div class="flex items-center justify-between bg-white border border-slate-200 rounded-2xl px-6 py-4 mb-6 shadow-sm flex-wrap gap-3">
      <div class="flex items-center gap-3 flex-wrap">
        <div class="flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 text-sm font-bold px-3 py-1.5 rounded-full">
          <UIcon name="i-heroicons-check-circle" class="text-green-500 text-base" />
          Análise concluída
        </div>
        <p class="text-sm text-slate-500 font-medium">{{ statusMessage }}</p>
      </div>
      <div class="flex items-center gap-2 flex-wrap">
        <UButton
          color="primary"
          variant="outline"
          size="md"
          icon="i-heroicons-arrow-down-tray"
          :loading="isDownloading"
          :disabled="isDownloading"
          @click="handleDownload"
        >
          Baixar JSON
        </UButton>
        <UButton
          color="neutral"
          variant="soft"
          size="md"
          icon="i-heroicons-arrow-path"
          @click="resetState"
        >
          Nova análise
        </UButton>
      </div>
    </div>

    <Dashboard :data="results" />

  </section>

</template>
