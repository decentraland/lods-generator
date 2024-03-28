import { validateMetricsDeclaration } from '@well-known-components/metrics'
import { metricDeclarations as logsMetricsDeclarations } from '@well-known-components/logger'
import { IMetricsComponent } from '@well-known-components/interfaces'
import { getDefaultHttpMetrics } from '@well-known-components/http-server'

export const metricDeclarations = {
  ...getDefaultHttpMetrics(),
  ...logsMetricsDeclarations,
  lod_generation_duration_minutes: {
    help: 'Histogram of lods generation duration in minutes',
    type: IMetricsComponent.HistogramType,
    buckets: [1, 2, 5, 7, 10, 13, 15, 18, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80].map((minutes) => minutes * 60)
  },
  lod_generation_count: {
    help: 'Count of lods generation',
    type: IMetricsComponent.CounterType,
    labelNames: ['status']
  },
  license_server_health: {
    help: 'Tracks the health of the license server',
    type: IMetricsComponent.GaugeType
  }
}

// type assertions
validateMetricsDeclaration(metricDeclarations)