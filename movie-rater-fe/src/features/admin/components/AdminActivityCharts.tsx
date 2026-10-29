import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { useI18n } from '@src/i18n/use-i18n'
import type { MessageKey } from '@src/i18n'
import type { AdminActivityDayDto } from '@src/types/admin'
import { useAdminActivity } from '../hooks/use-admin'
import { AdminBarChart } from './AdminBarChart'

type Metric = keyof Omit<AdminActivityDayDto, 'date'>

const METRICS: { metric: Metric; labelKey: MessageKey }[] = [
  { metric: 'activeUsers', labelKey: 'admin.activity.activeUsers' },
  { metric: 'signups', labelKey: 'admin.activity.signups' },
  { metric: 'watchSessions', labelKey: 'admin.activity.watchSessions' },
  { metric: 'ratings', labelKey: 'admin.activity.ratings' },
]

export function AdminActivityCharts({ days }: { days: number }) {
  const activity = useAdminActivity(days)
  const { t, formatDate } = useI18n()

  if (activity.isPending) {
    return (
      <div className="grid gap-3 md:grid-cols-2">
        {METRICS.map((m) => (
          <Skeleton key={m.metric} className="h-44 rounded-xl" />
        ))}
      </div>
    )
  }

  if (activity.isError) {
    return <p className="text-sm text-destructive">{t('admin.loadFailed')}</p>
  }

  const series = activity.data.series
  // Dates come as "yyyy-mm-dd" (UTC days); format them without shifting to local time.
  const shortDate = (date: string) =>
    formatDate(`${date}T00:00:00Z`, { day: 'numeric', month: 'short', timeZone: 'UTC' })

  return (
    <div className="grid gap-3 md:grid-cols-2">
      {METRICS.map(({ metric, labelKey }) => {
        // Active users are distinct per day, so summing them would double count.
        const total = series.reduce((sum, d) => sum + d[metric], 0)
        return (
          <Card key={metric} className="border-border/50 bg-card/50">
            <CardHeader className="pb-1">
              <CardTitle className="flex items-baseline justify-between gap-2 text-sm">
                <span>{t(labelKey)}</span>
                {metric !== 'activeUsers' && (
                  <span className="text-xs font-normal tabular-nums text-muted-foreground">
                    {t('admin.activity.total', { count: total })}
                  </span>
                )}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <AdminBarChart
                data={series.map((d) => ({
                  key: d.date,
                  value: d[metric],
                  tooltip: t('admin.activity.tooltip', { date: shortDate(d.date), count: d[metric] }),
                }))}
                startLabel={series.length > 0 ? shortDate(series[0].date) : undefined}
                endLabel={series.length > 0 ? shortDate(series[series.length - 1].date) : undefined}
              />
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}
