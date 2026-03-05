export const SENSOR_LIMITS = {
  temperature: { alertMax: 30, critMax: 35, alertMin: 15, critMin: 10 },
  humidity:    { alertMax: 70, critMax: 80, alertMin: 40, critMin: 30 },
  dewPoint:    { alertMax: 22, critMax: 25 },
} as const
