import type { Ref } from 'vue'
import type { SensorReading } from '~/types/sensor'

export interface KpiPercents {
  invalid:           string
  tempMaxAlertOnly:  string
  tempMaxCrit:       string
  humAlert:          string
  humCrit:           string
  dewMaxAlert:       string
  dewMaxCrit:        string
  minAlertTempOrHum: string
  minCritTempOrHum:  string
  anomaly:           string
  normal:            string
}

export interface Kpis {
  total:    number
  invalid:  number
  percents: KpiPercents
}

export function useKpis(data: Ref<SensorReading[]>) {
  return computed<Kpis | null>(() => {
    if (!data.value || !Array.isArray(data.value)) return null
    
    const total = data.value.length
    if (total === 0) return null

    const counts = {
      invalid: 0, tempMaxAlertOnly: 0, tempMaxCrit: 0,
      humAlert: 0, humCrit: 0, dewMaxAlert: 0, dewMaxCrit: 0,
      minAlertTempOrHum: 0, minCritTempOrHum: 0, anomaly: 0, normal: 0,
    }

    for (const r of data.value) {
      const { temperature: temp, humidity: hum, dewPoint: dew, anomaly: anom } = r.analysis

      if (anom.status === 'invalid')                                              counts.invalid++
      if (temp.status === 'alert'    && temp.limitType === 'max')                 counts.tempMaxAlertOnly++
      if (temp.status === 'critical' && temp.limitType === 'max')                 counts.tempMaxCrit++
      if (hum.status  === 'alert')                                                counts.humAlert++
      if (hum.status  === 'critical')                                             counts.humCrit++
      if (dew.status  === 'alert'    && dew.limitType  === 'max')                 counts.dewMaxAlert++
      if (dew.status  === 'critical' && dew.limitType  === 'max')                 counts.dewMaxCrit++
      if ((temp.status === 'alert'    && temp.limitType === 'min') ||
          (hum.status  === 'alert'    && hum.limitType  === 'min'))               counts.minAlertTempOrHum++
      if ((temp.status === 'critical' && temp.limitType === 'min') ||
          (hum.status  === 'critical' && hum.limitType  === 'min'))               counts.minCritTempOrHum++
      if (anom.status === 'anomaly')                                              counts.anomaly++
      if (anom.status === 'normal' && temp.status === 'normal' &&
          hum.status  === 'normal' && dew.status  === 'normal')                   counts.normal++
    }

    const pct = (n: number) => ((n / total) * 100).toFixed(1)

    return {
      total,
      invalid: counts.invalid,
      percents: {
        invalid:           pct(counts.invalid),
        tempMaxAlertOnly:  pct(counts.tempMaxAlertOnly),
        tempMaxCrit:       pct(counts.tempMaxCrit),
        humAlert:          pct(counts.humAlert),
        humCrit:           pct(counts.humCrit),
        dewMaxAlert:       pct(counts.dewMaxAlert),
        dewMaxCrit:        pct(counts.dewMaxCrit),
        minAlertTempOrHum: pct(counts.minAlertTempOrHum),
        minCritTempOrHum:  pct(counts.minCritTempOrHum),
        anomaly:           pct(counts.anomaly),
        normal:            pct(counts.normal),
      },
    }
  })
}
