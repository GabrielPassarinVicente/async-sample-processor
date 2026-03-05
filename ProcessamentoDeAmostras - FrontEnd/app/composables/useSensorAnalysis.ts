import { createSensorService } from '~/services/sensorService'
import type { Progress, SensorReading } from '~/types/sensor'

const POLL_INTERVAL   = 500
const POLL_TIMEOUT_MS = 5 * 60 * 1000

export function useSensorAnalysis() {
  const config  = useRuntimeConfig()
  const service = createSensorService(config.public.apiBaseUrl + '/api/sensor')

  const file           = ref<File | null>(null)
  const isProcessing   = ref(false)
  const isDownloading  = ref(false)
  const statusMessage  = ref('')
  const progress       = ref<Progress>({ processed: 0, total: 0 })
  const results        = ref<SensorReading[] | null>(null)
  const hasError       = ref(false)
  const currentJobId   = ref<string | null>(null)
  let   activeInterval = 0

  const progressPercent = computed<number>(() => {
    if (!progress.value.total) return 0
    return Math.round((progress.value.processed / progress.value.total) * 100)
  })

  const selectedFileName = computed<string | null>(() => file.value?.name ?? null)

  function setError(message: string): void {
    hasError.value      = true
    statusMessage.value = message
  }

  function pollJobStatus(jobId: string): Promise<SensorReading[]> {
    return new Promise((resolve, reject) => {
      const deadline = Date.now() + POLL_TIMEOUT_MS

      activeInterval = window.setInterval(async () => {
        if (Date.now() > deadline) {
          clearInterval(activeInterval)
          reject(new Error('Tempo limite de processamento excedido (5 min)'))
          return
        }

        try {
          const data = await service.getJobStatus(jobId)
          progress.value      = { processed: data.processedSamples, total: data.totalSamples }
          statusMessage.value = `Analisando amostras... ${data.processedSamples} / ${data.totalSamples}`

          if (data.isCompleted) {
            clearInterval(activeInterval)
            resolve(data.results)
          }
        } catch (err) {
          clearInterval(activeInterval)
          reject(err)
        }
      }, POLL_INTERVAL)
    })
  }

  function resetState(): void {
    clearInterval(activeInterval)
    file.value          = null
    statusMessage.value = ''
    results.value       = null
    hasError.value      = false
    isProcessing.value  = false
    isDownloading.value = false
    progress.value      = { processed: 0, total: 0 }
    currentJobId.value  = null
  }

  function handleFileChange(event: Event): void {
    const input         = event.target as HTMLInputElement
    file.value          = input.files?.[0] ?? null
    hasError.value      = false
    statusMessage.value = ''
  }

  async function handleSubmit(): Promise<void> {
    if (!file.value || isProcessing.value) return

    isProcessing.value  = true
    hasError.value      = false
    statusMessage.value = 'Enviando arquivo...'
    progress.value      = { processed: 0, total: 0 }

    try {
      const { jobId } = await service.uploadFile(file.value)
      currentJobId.value  = jobId
      statusMessage.value = 'Processando em background...'

      const readings      = await pollJobStatus(jobId)
      results.value       = readings
      statusMessage.value = `Análise concluída — ${readings.length} amostras processadas.`
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro desconhecido'
      setError(`Falha na análise: ${message}`)
    } finally {
      isProcessing.value = false
    }
  }

  async function handleDownload(): Promise<void> {
    if (!currentJobId.value || isDownloading.value) return

    isDownloading.value = true
    
    try {
      await service.downloadResults(currentJobId.value)
      statusMessage.value = 'Download realizado com sucesso!'
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Erro ao fazer download'
      setError(`Falha no download: ${message}`)
    } finally {
      isDownloading.value = false
    }
  }

  return {
    file,
    isProcessing,
    isDownloading,
    statusMessage,
    progress,
    results,
    hasError,
    progressPercent,
    selectedFileName,
    currentJobId,
    resetState,
    handleFileChange,
    handleSubmit,
    handleDownload,
  }
}
