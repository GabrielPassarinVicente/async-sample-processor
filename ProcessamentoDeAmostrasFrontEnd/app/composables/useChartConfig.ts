import type { Ref } from 'vue'
import type { SensorReading } from '~/types/sensor'
import { SENSOR_LIMITS } from '~/constants/sensorLimits'

function makeBaseChartOptions() {
  return {
    responsive:          true,
    maintainAspectRatio: false,
    interaction: { mode: 'index' as const, intersect: false },
    scales: {
      x: {
        grid:  { color: '#f1f5f9', drawBorder: false },
        ticks: { font: { family: 'Inter, sans-serif', size: 12 }, color: '#94a3b8', maxTicksLimit: 12 },
      },
      y: {
        grid:  { color: '#f1f5f9', drawBorder: false },
        ticks: { font: { family: 'Inter, sans-serif', size: 12 }, color: '#94a3b8' },
      },
    },
    plugins: {
      legend: {
        display:  true,
        position: 'top'  as const,
        align:    'end'  as const,
        labels: { font: { family: 'Inter, sans-serif', size: 13 }, color: '#475569', boxWidth: 14, padding: 20, usePointStyle: true },
      },
      tooltip: {
        backgroundColor: '#0f172a',
        titleFont: { family: 'Inter, sans-serif', size: 13, weight: 'bold' as const },
        bodyFont:  { family: 'Inter, sans-serif', size: 12 },
        padding:       12,
        cornerRadius:   8,
        displayColors: true,
      },
    },
  }
}

function annotationLine(yVal: number, color: string, dashed: boolean, labelText?: string) {
  return {
    type:        'line' as const,
    yMin:        yVal,
    yMax:        yVal,
    borderColor: color,
    borderWidth: 1.5,
    ...(dashed    ? { borderDash: [6, 4] } : {}),
    ...(labelText ? {
      label: {
        content:         labelText,
        display:         true,
        font:            { size: 10 },
        backgroundColor: color === '#f59e0b' ? '#fffbeb' : '#fef2f2',
        color:           color === '#f59e0b' ? '#b45309' : '#991b1b',
        padding:         4,
      },
    } : {}),
  }
}

const EMPTY_CHART = { labels: [] as string[], datasets: [] as never[] }

interface SplitData {
  labels:      string[]
  temperature: (number | null)[]
  humidity:    (number | null)[]
  dewPoint:    (number | null)[]
}

export function useChartConfig(filteredData: Ref<SensorReading[]>) {
  const splitData = computed<SplitData>(() => {
    const data = filteredData.value
    if (!data || !Array.isArray(data) || data.length === 0) {
      return { labels: [], temperature: [], humidity: [], dewPoint: [] }
    }
    const labels:      string[]         = []
    const temperature: (number | null)[] = []
    const humidity:    (number | null)[] = []
    const dewPoint:    (number | null)[] = []

    for (const d of data) {
      labels.push(new Date(d.timestamp).toLocaleTimeString())
      temperature.push(d.temperature)
      humidity.push(d.humidity)
      dewPoint.push(d.dewPoint)
    }
    return { labels, temperature, humidity, dewPoint }
  })

  const chartDataTemp = computed(() => {
    if (!splitData.value.labels.length) return EMPTY_CHART
    return {
      labels:   splitData.value.labels,
      datasets: [{
        label:           'Temperatura (°C)',
        data:            splitData.value.temperature,
        borderColor:     '#2563eb',
        backgroundColor: 'rgba(37, 99, 235, 0.06)',
        borderWidth: 2, pointRadius: 2, pointHoverRadius: 5, fill: true, tension: 0.3,
      }],
    }
  })

  const chartOptionsTemp = {
    ...makeBaseChartOptions(),
    plugins: {
      ...makeBaseChartOptions().plugins,
      annotation: {
        annotations: {
          alertMax: annotationLine(SENSOR_LIMITS.temperature.alertMax, '#f59e0b', true,  `Alerta (${SENSOR_LIMITS.temperature.alertMax}°C)`),
          critMax:  annotationLine(SENSOR_LIMITS.temperature.critMax,  '#dc2626', false, `Crítico (${SENSOR_LIMITS.temperature.critMax}°C)`),
          alertMin: annotationLine(SENSOR_LIMITS.temperature.alertMin, '#f59e0b', true),
          critMin:  annotationLine(SENSOR_LIMITS.temperature.critMin,  '#dc2626', false),
        },
      },
    },
  }

  const chartDataHum = computed(() => {
    if (!splitData.value.labels.length) return EMPTY_CHART
    return {
      labels:   splitData.value.labels,
      datasets: [{
        label:           'Umidade (%)',
        data:            splitData.value.humidity,
        borderColor:     '#16a34a',
        backgroundColor: 'rgba(22, 163, 74, 0.06)',
        borderWidth: 2, pointRadius: 2, pointHoverRadius: 5, fill: true, tension: 0.3,
      }],
    }
  })

  const chartOptionsHum = {
    ...makeBaseChartOptions(),
    plugins: {
      ...makeBaseChartOptions().plugins,
      annotation: {
        annotations: {
          alertMax: annotationLine(SENSOR_LIMITS.humidity.alertMax, '#f59e0b', true,  `Alerta (${SENSOR_LIMITS.humidity.alertMax}%)`),
          critMax:  annotationLine(SENSOR_LIMITS.humidity.critMax,  '#dc2626', false, `Crítico (${SENSOR_LIMITS.humidity.critMax}%)`),
          alertMin: annotationLine(SENSOR_LIMITS.humidity.alertMin, '#f59e0b', true),
          critMin:  annotationLine(SENSOR_LIMITS.humidity.critMin,  '#dc2626', false),
        },
      },
    },
  }

  const chartDataDew = computed(() => {
    if (!splitData.value.labels.length) return EMPTY_CHART
    return {
      labels:   splitData.value.labels,
      datasets: [{
        label:           'Ponto de Orvalho',
        data:            splitData.value.dewPoint,
        borderColor:     '#7c3aed',
        backgroundColor: 'rgba(124, 58, 237, 0.06)',
        borderWidth: 2, pointRadius: 2, pointHoverRadius: 5, fill: true, tension: 0.3,
      }],
    }
  })

  const chartOptionsDew = {
    ...makeBaseChartOptions(),
    plugins: {
      ...makeBaseChartOptions().plugins,
      annotation: {
        annotations: {
          alertMax: annotationLine(SENSOR_LIMITS.dewPoint.alertMax, '#f59e0b', true,  `Alerta (${SENSOR_LIMITS.dewPoint.alertMax})`),
          critMax:  annotationLine(SENSOR_LIMITS.dewPoint.critMax,  '#dc2626', false, `Crítico (${SENSOR_LIMITS.dewPoint.critMax})`),
        },
      },
    },
  }

  return {
    chartDataTemp, chartOptionsTemp,
    chartDataHum,  chartOptionsHum,
    chartDataDew,  chartOptionsDew,
  }
}
