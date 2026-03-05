export type SensorStatus = 'normal' | 'alert' | 'critical' | 'invalid' | 'anomaly'
export type LimitType    = 'min' | 'max'

export interface SensorMetricResult {
  status:    SensorStatus
  limitType: LimitType | null
}

export interface AnomalyResult {
  status: SensorStatus
}

export interface AnalysisResult {
  temperature: SensorMetricResult
  humidity:    SensorMetricResult
  dewPoint:    SensorMetricResult
  anomaly:     AnomalyResult
}

export interface SensorReading {
  timestamp:   string
  type:        string
  temperature: number | null
  humidity:    number | null
  dewPoint:    number | null
  analysis:    AnalysisResult
}

export interface Progress {
  processed: number
  total:     number
}

export interface UploadResponse {
  jobId: string
}

export interface JobStatusResponse {
  isCompleted:      boolean
  processedSamples: number
  totalSamples:     number
  results:          SensorReading[]
}
