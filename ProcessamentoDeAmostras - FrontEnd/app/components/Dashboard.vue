<script setup lang="ts">
import { Line } from 'vue-chartjs'
import type { SensorReading } from '../types/sensor'
import { useKpis } from '../composables/useKpis'
import { useChartConfig } from '../composables/useChartConfig'
import { computed, ref } from 'vue'

const props = defineProps<{ data: SensorReading[] }>()

const selectedType   = ref('Todos')
const availableTypes = computed(() => {
  if (!props.data || !Array.isArray(props.data)) return ['Todos']
  const types = new Set(props.data.map(item => item.type))
  return ['Todos', ...Array.from(types)]
})

const filteredData = computed(() => {
  if (!props.data || !Array.isArray(props.data)) return []
  return selectedType.value === 'Todos'
    ? props.data
    : props.data.filter(item => item.type === selectedType.value)
})

const kpis = useKpis(filteredData)
const { chartDataTemp, chartOptionsTemp, chartDataHum, chartOptionsHum, chartDataDew, chartOptionsDew } = useChartConfig(filteredData)
</script>

<template>
  <div class="flex flex-col gap-6 lg:gap-8">
    <UCard class="bg-white"> 
      <div class="flex flex-col sm:flex-row items-start sm:items-center gap-4 sm:gap-5">
        <div class="flex items-center gap-2.5">
          <div class="w-9 h-9 rounded-xl bg-primary-100 flex items-center justify-center">
            <UIcon name="i-heroicons-funnel" class="text-primary-600 text-lg" />
          </div>
          <span class="text-sm font-bold uppercase tracking-widest text-slate-500">Filtrar por sensor</span>
        </div>
        <div class="flex flex-wrap gap-2">
          <UButton
            v-for="type in availableTypes"
            :key="type"
            size="md"
            :color="selectedType === type ? 'primary' : 'neutral'"
            :variant="selectedType === type ? 'solid' : 'soft'"
            :aria-pressed="selectedType === type"
            class="rounded-full font-semibold"
            @click="selectedType = type"
          >{{ type }}</UButton>
        </div>
      </div>
    </UCard>

    <template v-if="kpis">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 lg:gap-5">

        <UCard class="hover:-translate-y-1 transition-all duration-200">
          <div class="flex items-center justify-between mb-4">
            <span class="text-xs font-bold uppercase tracking-widest text-slate-500">Total</span>
            <div class="w-11 h-11 rounded-xl bg-blue-100 flex items-center justify-center">
              <UIcon name="i-heroicons-chart-bar" class="text-blue-600 text-2xl" />
            </div>
          </div>
          <p class="text-5xl lg:text-6xl font-black text-blue-700 leading-none tracking-tight mb-2">{{ kpis.total }}</p>
          <p class="text-sm text-slate-500 font-semibold">amostras analisadas</p>
        </UCard>

        <UCard class="hover:-translate-y-1 transition-all duration-200">
          <div class="flex items-center justify-between mb-4">
            <span class="text-xs font-bold uppercase tracking-widest text-slate-500">Normais</span>
            <div class="w-11 h-11 rounded-xl bg-green-100 flex items-center justify-center">
              <UIcon name="i-heroicons-check-circle" class="text-green-600 text-2xl" />
            </div>
          </div>
          <p class="text-5xl lg:text-6xl font-black text-green-700 leading-none tracking-tight mb-2">
            {{ kpis.percents.normal }}<span class="text-3xl font-bold text-green-500">%</span>
          </p>
          <p class="text-sm text-slate-500 font-semibold">dentro dos limites</p>
        </UCard>

        <UCard class="hover:-translate-y-1 transition-all duration-200">
          <div class="flex items-center justify-between mb-4">
            <span class="text-xs font-bold uppercase tracking-widest text-slate-500">Anomalias</span>
            <div class="w-11 h-11 rounded-xl bg-amber-100 flex items-center justify-center">
              <UIcon name="i-heroicons-exclamation-triangle" class="text-amber-600 text-2xl" />
            </div>
          </div>
          <p class="text-5xl lg:text-6xl font-black text-amber-700 leading-none tracking-tight mb-2">
            {{ kpis.percents.anomaly }}<span class="text-3xl font-bold text-amber-500">%</span>
          </p>
          <p class="text-sm text-slate-500 font-semibold">desvios detectados</p>
        </UCard>

        <UCard class="hover:-translate-y-1 transition-all duration-200">
          <div class="flex items-center justify-between mb-4">
            <span class="text-xs font-bold uppercase tracking-widest text-slate-500">Inválidas</span>
            <div class="w-11 h-11 rounded-xl bg-red-100 flex items-center justify-center">
              <UIcon name="i-heroicons-x-circle" class="text-red-600 text-2xl" />
            </div>
          </div>
          <p class="text-5xl lg:text-6xl font-black text-red-700 leading-none tracking-tight mb-2">{{ kpis.invalid }}</p>
          <p class="text-sm text-slate-500 font-semibold">leituras nulas</p>
        </UCard>

      </div>

      <UCard class="bg-white">
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-slate-100 flex items-center justify-center">
              <UIcon name="i-heroicons-chart-pie" class="text-slate-600 text-xl" />
            </div>
            <div>
              <h2 class="text-base font-bold text-slate-900">Distribuição por Status</h2>
              <p class="text-xs text-slate-400 font-medium mt-0.5">Percentuais calculados sobre o total filtrado</p>
            </div>
          </div>
        </template>

          <p class="text-[0.7rem] font-extrabold tracking-widest uppercase text-slate-500 flex items-center gap-2 mt-1.5 mb-0.5">Geral</p>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-green-100"><UIcon name="i-heroicons-check-circle" class="text-green-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Normais</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.normal)" aria-valuemin="0" aria-valuemax="100" aria-label="Normais" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-green-500 rounded-full" :style="{ width: kpis.percents.normal + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-green-700">{{ kpis.percents.normal }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-amber-100"><UIcon name="i-heroicons-exclamation-triangle" class="text-amber-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Anomalias</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.anomaly)" aria-valuemin="0" aria-valuemax="100" aria-label="Anomalias" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-amber-500 rounded-full" :style="{ width: kpis.percents.anomaly + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-amber-700">{{ kpis.percents.anomaly }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-red-100"><UIcon name="i-heroicons-x-circle" class="text-red-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Inválidas (nulos)</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.invalid)" aria-valuemin="0" aria-valuemax="100" aria-label="Inválidas" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-red-500 rounded-full" :style="{ width: kpis.percents.invalid + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-red-700">{{ kpis.percents.invalid }}%</span>
          </div>

          <div class="flex items-center gap-2 pt-2.5 mt-1 border-t border-slate-100">
            <span class="w-3 h-3 rounded bg-blue-500 inline-block"></span>
            <span class="text-[0.7rem] font-extrabold tracking-widest uppercase text-slate-500">Temperatura</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-amber-100"><UIcon name="i-heroicons-arrow-trending-up" class="text-amber-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Alerta máx. temperatura</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.tempMaxAlertOnly)" aria-valuemin="0" aria-valuemax="100" aria-label="Alerta máx. temperatura" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-amber-500 rounded-full" :style="{ width: kpis.percents.tempMaxAlertOnly + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-amber-700">{{ kpis.percents.tempMaxAlertOnly }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-red-100"><UIcon name="i-heroicons-fire" class="text-red-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Crítico máx. temperatura</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.tempMaxCrit)" aria-valuemin="0" aria-valuemax="100" aria-label="Crítico máx. temperatura" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-red-500 rounded-full" :style="{ width: kpis.percents.tempMaxCrit + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-red-700">{{ kpis.percents.tempMaxCrit }}%</span>
          </div>

          <div class="flex items-center gap-2 pt-2.5 mt-1 border-t border-slate-100">
            <span class="w-3 h-3 rounded bg-green-500 inline-block"></span>
            <span class="text-[0.7rem] font-extrabold tracking-widest uppercase text-slate-500">Umidade</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-amber-100"><UIcon name="i-heroicons-arrow-trending-up" class="text-amber-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Alerta umidade</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.humAlert)" aria-valuemin="0" aria-valuemax="100" aria-label="Alerta umidade" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-amber-500 rounded-full" :style="{ width: kpis.percents.humAlert + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-amber-700">{{ kpis.percents.humAlert }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-red-100"><UIcon name="i-heroicons-fire" class="text-red-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Crítico umidade</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.humCrit)" aria-valuemin="0" aria-valuemax="100" aria-label="Crítico umidade" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-red-500 rounded-full" :style="{ width: kpis.percents.humCrit + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-red-700">{{ kpis.percents.humCrit }}%</span>
          </div>

          <div class="flex items-center gap-2 pt-2.5 mt-1 border-t border-slate-100">
            <span class="w-3 h-3 rounded bg-violet-500 inline-block"></span>
            <span class="text-[0.7rem] font-extrabold tracking-widest uppercase text-slate-500">Ponto de Orvalho</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-amber-100"><UIcon name="i-heroicons-arrow-trending-up" class="text-amber-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Alerta máx. orvalho</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.dewMaxAlert)" aria-valuemin="0" aria-valuemax="100" aria-label="Alerta máx. orvalho" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-amber-500 rounded-full" :style="{ width: kpis.percents.dewMaxAlert + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-amber-700">{{ kpis.percents.dewMaxAlert }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-red-100"><UIcon name="i-heroicons-fire" class="text-red-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Crítico máx. orvalho</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.dewMaxCrit)" aria-valuemin="0" aria-valuemax="100" aria-label="Crítico máx. orvalho" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-red-500 rounded-full" :style="{ width: kpis.percents.dewMaxCrit + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-red-700">{{ kpis.percents.dewMaxCrit }}%</span>
          </div>

          <div class="flex items-center gap-2 pt-2.5 mt-1 border-t border-slate-100">
            <span class="w-3 h-3 rounded bg-slate-400 inline-block"></span>
            <span class="text-[0.7rem] font-extrabold tracking-widest uppercase text-slate-500">Mínimos</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-amber-100"><UIcon name="i-heroicons-arrow-trending-down" class="text-amber-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Abaixo alerta mín. (Temp/Umid)</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.minAlertTempOrHum)" aria-valuemin="0" aria-valuemax="100" aria-label="Abaixo alerta mínimo temperatura ou umidade" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-amber-500 rounded-full" :style="{ width: kpis.percents.minAlertTempOrHum + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-amber-700">{{ kpis.percents.minAlertTempOrHum }}%</span>
          </div>

          <div class="flex items-center gap-3 py-1">
            <div class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 text-base bg-red-100"><UIcon name="i-heroicons-arrow-trending-down" class="text-red-600" /></div>
            <span class="text-sm text-slate-700 font-semibold min-w-[220px] shrink-0">Abaixo crítico mín. (Temp/Umid)</span>
            <div role="progressbar" :aria-valuenow="parseFloat(kpis.percents.minCritTempOrHum)" aria-valuemin="0" aria-valuemax="100" aria-label="Abaixo crítico mínimo temperatura ou umidade" class="flex-1 h-2.5 bg-gray-200 rounded-full overflow-hidden">
              <div class="h-full bg-red-500 rounded-full" :style="{ width: kpis.percents.minCritTempOrHum + '%' }"></div>
            </div>
            <span class="text-sm font-extrabold min-w-[52px] text-right shrink-0 tracking-tight text-red-700">{{ kpis.percents.minCritTempOrHum }}%</span>
          </div>

      </UCard>

    </template>

    <div class="grid grid-cols-1 gap-6 lg:gap-8">

      <UCard class="shadow-sm bg-white">
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-11 h-11 rounded-xl bg-blue-100 flex items-center justify-center">
              <UIcon name="i-heroicons-fire" class="text-blue-600 text-2xl" />
            </div>
            <div>
              <h2 class="text-xl font-bold text-slate-900">Temperatura (°C)</h2>
              <p class="text-sm text-slate-500 font-medium mt-1">Monitoramento em tempo real</p>
            </div>
          </div>
        </template>
        <div class="h-[450px] lg:h-[500px] w-full">
          <Line :data="chartDataTemp" :options="chartOptionsTemp" />
        </div>
      </UCard>

      <UCard class="shadow-sm bg-white">
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-11 h-11 rounded-xl bg-green-100 flex items-center justify-center">
              <UIcon name="i-heroicons-beaker" class="text-green-600 text-2xl" />
            </div>
            <div>
              <h2 class="text-xl font-bold text-slate-900">Umidade (%)</h2>
              <p class="text-sm text-slate-500 font-medium mt-1">Monitoramento em tempo real</p>
            </div>
          </div>
        </template>
        <div class="h-[450px] lg:h-[500px] w-full">
          <Line :data="chartDataHum" :options="chartOptionsHum" />
        </div>
      </UCard>

      <UCard class="shadow-sm bg-white">
        <template #header>
          <div class="flex items-center gap-3">
            <div class="w-11 h-11 rounded-xl bg-violet-100 flex items-center justify-center">
              <UIcon name="i-heroicons-cloud" class="text-violet-600 text-2xl" />
            </div>
            <div>
              <h2 class="text-xl font-bold text-slate-900">Ponto de Orvalho</h2>
              <p class="text-sm text-slate-500 font-medium mt-1">Temperatura de condensação</p>
            </div>
          </div>
        </template>
        <div class="h-[450px] lg:h-[500px] w-full">
          <Line :data="chartDataDew" :options="chartOptionsDew" />
        </div>
      </UCard>

    </div>

  </div>
</template>
