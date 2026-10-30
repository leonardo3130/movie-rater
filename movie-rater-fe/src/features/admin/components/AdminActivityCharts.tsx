import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useI18n } from '@src/i18n/use-i18n'
import type { MessageKey } from '@src/i18n'
import type { AdminActivityDayDto } from '@src/types/admin'
import { useAdminActivity } from '../hooks/use-admin-activity'
import { AdminBarChart } from './AdminBarChart'

type Metric = Exclude<keyof AdminActivityDayDto, 'date'>

// Active users are distinct per day, so a total over the range would double count them.
const METRICS: { metric: Metric; labelKey: MessageKey; showTotal: boolean }[] = [
  { metric: 'activeUsers', labelKey: 'admin.activity.activeUsers', showTotal: false },
  { metric: 'signups', labelKey: 'admin.activity.signups', showTotal: true },
  { metric: 'watchSessions', labelKey: 'admin.activity.watchSessions', showTotal: true },
  { metric: 'ratings', labelKey: 'admin.activity.ratings', showTotal: true },
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
  // Dates are UTC days ("yyyy-mm-dd"); format them in UTC so they don't shift a day.
  const shortDate = (date: string) =>
    formatDate(`${date}T00:00:00Z`, { day: 'numeric', month: 'short', timeZone: 'UTC' })
  const startLabel = series.length > 0 ? shortDate(series[0].date) : undefined
  const endLabel = series.length > 0 ? shortDate(series[series.length - 1].date) : undefined

  return (
    <div
      className={cn(
        'grid gap-3 transition-opacity md:grid-cols-2',
        activity.isPlaceholderData && 'opacity-60',
      )}
    >
      {METRICS.map(({ metric, labelKey, showTotal }) => (
        <Card key={metric} className="border-border/50 bg-card/50">
          <CardHeader>
            <CardTitle className="flex items-baseline justify-between gap-2 text-sm">
              <span>{t(labelKey)}</span>
              {showTotal && (
                <span className="text-xs font-normal tabular-nums text-muted-foreground">
                  {t('admin.activity.total', {
                    count: series.reduce((sum, d) => sum + d[metric], 0),
                  })}
                </span>
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <AdminBarChart
              label={t(labelKey)}
              data={series.map((d) => ({
                key: d.date,
                value: d[metric],
                tooltip: t('admin.activity.tooltip', { date: shortDate(d.date), count: d[metric] }),
              }))}
              startLabel={startLabel}
              endLabel={endLabel}
            />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
