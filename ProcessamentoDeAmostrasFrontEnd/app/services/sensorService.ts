import axios from 'axios'
import type {
  SensorStatus, LimitType,
  SensorMetricResult, AnomalyResult, SensorReading,
  UploadResponse, JobStatusResponse,
} from '~/types/sensor'

function normalizeMetric(raw: Record<string, unknown>): SensorMetricResult {
  return {
    status:    ((raw.status    ?? raw.Status    ?? 'normal') as SensorStatus),
    limitType: ((raw.limitType ?? raw.LimitType ?? raw.limit_type ?? null) as LimitType | null),
  }
}

function normalizeAnomaly(raw: Record<string, unknown>): AnomalyResult {
  return { status: ((raw.status ?? raw.Status ?? 'normal') as SensorStatus) }
}

function normalizeReading(raw: Record<string, unknown>): SensorReading {
  const rawAnalysis = (raw.analysis ?? raw.Analysis ?? {}) as Record<string, unknown>
  return {
    timestamp:   (raw.timestamp   ?? raw.Timestamp   ?? '') as string,
    type:        (raw.type        ?? raw.Type         ?? 'unknown') as string,
    temperature: (raw.temperature ?? raw.Temperature ?? null) as number | null,
    humidity:    (raw.humidity    ?? raw.Humidity    ?? null) as number | null,
    dewPoint:    (raw.dew_point   ?? raw.dewPoint    ?? raw.DewPoint ?? null) as number | null,
    analysis: {
      temperature: normalizeMetric((rawAnalysis.temperature ?? rawAnalysis.Temperature ?? {}) as Record<string, unknown>),
      humidity:    normalizeMetric((rawAnalysis.humidity    ?? rawAnalysis.Humidity    ?? {}) as Record<string, unknown>),
      dewPoint:    normalizeMetric((rawAnalysis.dew_point   ?? rawAnalysis.dewPoint ?? rawAnalysis.DewPoint ?? {}) as Record<string, unknown>),
      anomaly:     normalizeAnomaly((rawAnalysis.anomaly   ?? rawAnalysis.Anomaly     ?? {}) as Record<string, unknown>),
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
      results:          Record<string, unknown>[] | null
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
