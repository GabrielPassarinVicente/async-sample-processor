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
