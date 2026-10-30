import { useState } from 'react'
import { motion } from 'framer-motion'
import {
  Activity,
  CalendarRange,
  Clapperboard,
  Film,
  RefreshCw,
  ShieldCheck,
  Star,
  UserRoundX,
  Users,
  UsersRound,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useI18n } from '@src/i18n/use-i18n'
import type { AdminOverviewResponseDto } from '@src/types/admin'
import { useAdminOverview } from '../hooks/use-admin-overview'
import { useRefreshAdmin } from '../hooks/use-refresh-admin'
import { AdminStatTile } from '../components/AdminStatTile'
import { AdminBarChart } from '../components/AdminBarChart'
import { AdminActivityCharts } from '../components/AdminActivityCharts'
import { AdminGroupsTable } from '../components/AdminGroupsTable'
import { AdminInvitationSummary } from '../components/AdminInvitationSummary'
import { AdminInvitationsPanel } from '../components/AdminInvitationsPanel'

const ACTIVITY_RANGES = [7, 30, 90] as const

function Section({
  title,
  action,
  children,
}: {
  title: string
  action?: React.ReactNode
  children: React.ReactNode
}) {
  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
          {title}
        </h2>
        {action}
      </div>
      {children}
    </section>
  )
}

// Renders the overview-backed part of a section: a skeleton while loading, nothing on
// error (the page shows a single error message), and the content once data is in.
function OverviewContent({
  overview,
  skeleton,
  children,
}: {
  overview: ReturnType<typeof useAdminOverview>
  skeleton: React.ReactNode
  children: (data: AdminOverviewResponseDto) => React.ReactNode
}) {
  if (overview.isPending) return skeleton
  if (overview.isError) return null
  return children(overview.data)
}

export function AdminPage() {
  const { t, formatDate } = useI18n()
  const overview = useAdminOverview()
  const [activityDays, setActivityDays] = useState<number>(30)
  const refresh = useRefreshAdmin(activityDays)

  const tileSkeletons = (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {Array.from({ length: 4 }, (_, i) => (
        <Skeleton key={i} className="h-28 rounded-xl" />
      ))}
    </div>
  )

  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, ease: 'easeOut' }}
      className="space-y-8 p-4 md:p-6"
    >
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <h1 className="flex items-center gap-2 text-2xl font-bold tracking-tight">
            <ShieldCheck className="size-6 text-primary" />
            {t('admin.heading')}
          </h1>
          <p className="text-sm text-muted-foreground">{t('admin.subtitle')}</p>
        </div>
        <div className="flex items-center gap-3">
          {overview.data && (
            <span className="text-xs tabular-nums text-muted-foreground">
              {t('admin.updatedAt', {
                time: formatDate(overview.data.generatedAt, { timeStyle: 'short' }),
              })}
            </span>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={() => refresh.mutate()}
            disabled={refresh.isPending}
          >
            <RefreshCw className={cn('size-4', refresh.isPending && 'animate-spin')} />
            {t('admin.refresh')}
          </Button>
        </div>
      </header>

      {overview.isError && <p className="text-sm text-destructive">{t('admin.loadFailed')}</p>}

      <Section title={t('admin.sections.users')}>
        <OverviewContent overview={overview} skeleton={tileSkeletons}>
          {({ users }) => (
            <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
              <AdminStatTile
                icon={Users}
                label={t('admin.stats.totalUsers')}
                value={users.total}
                sublabel={t('admin.stats.newUsers', {
                  week: users.newLast7Days,
                  month: users.newLast30Days,
                })}
              />
              <AdminStatTile
                icon={Activity}
                label={t('admin.stats.activeToday')}
                value={users.activeLast24Hours}
                sublabel={t('admin.stats.activeSessions', { count: users.activeSessions })}
              />
              <AdminStatTile
                icon={CalendarRange}
                label={t('admin.stats.activeWeek')}
                value={users.activeLast7Days}
                sublabel={t('admin.stats.last30Days', { count: users.activeLast30Days })}
              />
              <AdminStatTile
                icon={UserRoundX}
                label={t('admin.stats.withoutGroup')}
                value={users.withoutGroup}
                sublabel={t('admin.stats.withoutGroupHint')}
              />
            </div>
          )}
        </OverviewContent>
      </Section>

      <Section title={t('admin.sections.content')}>
        <OverviewContent overview={overview} skeleton={tileSkeletons}>
          {({ groups, watchSessions, media }) => (
            <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
              <AdminStatTile
                icon={UsersRound}
                label={t('admin.stats.groups')}
                value={groups.total}
                sublabel={t('admin.stats.groupsDetail', {
                  size: groups.averageMembers,
                  dormant: groups.withoutWatchSessions,
                })}
              />
              <AdminStatTile
                icon={Clapperboard}
                label={t('admin.stats.watchSessions')}
                value={watchSessions.total}
                sublabel={t('admin.stats.last30Days', { count: watchSessions.last30Days })}
              />
              <AdminStatTile
                icon={Star}
                label={t('admin.stats.fullyRated')}
                value={`${watchSessions.fullyRatedPercent}%`}
                sublabel={t('admin.stats.fullyRatedHint')}
              />
              <AdminStatTile
                icon={Film}
                label={t('admin.stats.cachedMedia')}
                value={media.movies + media.tvSeries + media.tvSeasons + media.tvEpisodes}
                sublabel={t('admin.stats.mediaBreakdown', {
                  movies: media.movies,
                  series: media.tvSeries,
                  seasons: media.tvSeasons,
                  episodes: media.tvEpisodes,
                })}
              />
            </div>
          )}
        </OverviewContent>
      </Section>

      <Section
        title={t('admin.sections.activity')}
        action={
          <div className="flex rounded-lg border border-border/50 p-0.5">
            {ACTIVITY_RANGES.map((days) => (
              <button
                key={days}
                type="button"
                aria-pressed={activityDays === days}
                onClick={() => setActivityDays(days)}
                className={cn(
                  'cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium transition-colors',
                  activityDays === days
                    ? 'bg-primary/10 text-primary'
                    : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {t('admin.activity.range', { count: days })}
              </button>
            ))}
          </div>
        }
      >
        <AdminActivityCharts days={activityDays} />
      </Section>

      <Section title={t('admin.sections.ratings')}>
        <OverviewContent overview={overview} skeleton={<Skeleton className="h-44 rounded-xl" />}>
          {({ ratings }) => (
            <Card className="border-border/50 bg-card/50">
              <CardHeader>
                <CardTitle className="flex flex-wrap items-baseline justify-between gap-x-2 text-sm">
                  <span>{t('admin.ratings.distribution')}</span>
                  <span className="text-xs font-normal tabular-nums text-muted-foreground">
                    {t('admin.ratings.summary', { count: ratings.total, average: ratings.average })}
                  </span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <AdminBarChart
                  label={t('admin.ratings.distribution')}
                  data={ratings.distribution.map((bucket) => ({
                    key: String(bucket.value),
                    value: bucket.count,
                    tooltip: t('admin.ratings.tooltip', { value: bucket.value, count: bucket.count }),
                  }))}
                  labelEveryBar
                />
              </CardContent>
            </Card>
          )}
        </OverviewContent>
      </Section>

      <Section title={t('admin.sections.groups')}>
        <AdminGroupsTable />
      </Section>

      <Section title={t('admin.sections.invitations')}>
        <OverviewContent overview={overview} skeleton={<Skeleton className="h-16 rounded-xl" />}>
          {({ invitations }) => <AdminInvitationSummary stats={invitations} />}
        </OverviewContent>
        <AdminInvitationsPanel />
      </Section>
    </motion.div>
  )
}
