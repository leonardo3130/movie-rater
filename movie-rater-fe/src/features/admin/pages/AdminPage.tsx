import { useState } from 'react'
import { motion } from 'framer-motion'
import {
  Activity,
  AlertTriangle,
  Clapperboard,
  Film,
  Mail,
  RefreshCw,
  ShieldCheck,
  Star,
  UserPlus,
  UserRoundX,
  Users,
  UsersRound,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useI18n } from '@src/i18n/use-i18n'
import { useAdminOverview, useRefreshAdmin } from '../hooks/use-admin'
import { AdminStatTile } from '../components/AdminStatTile'
import { AdminBarChart } from '../components/AdminBarChart'
import { AdminActivityCharts } from '../components/AdminActivityCharts'
import { AdminGroupsTable } from '../components/AdminGroupsTable'
import { AdminInvitationsPanel } from '../components/AdminInvitationsPanel'

const ACTIVITY_RANGES = [7, 30, 90] as const

function Section({ title, action, children }: { title: string; action?: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">{title}</h2>
        {action}
      </div>
      {children}
    </section>
  )
}

export function AdminPage() {
  const { t, formatDate } = useI18n()
  const overview = useAdminOverview()
  const refreshAdmin = useRefreshAdmin()
  const [activityDays, setActivityDays] = useState<number>(30)
  const [refreshing, setRefreshing] = useState(false)

  const handleRefresh = async () => {
    setRefreshing(true)
    try {
      await refreshAdmin(activityDays)
    } finally {
      setRefreshing(false)
    }
  }

  const data = overview.data

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
          {data && (
            <span className="text-xs tabular-nums text-muted-foreground">
              {t('admin.updatedAt', { time: formatDate(data.generatedAt, { timeStyle: 'short' }) })}
            </span>
          )}
          <Button variant="outline" size="sm" onClick={handleRefresh} disabled={refreshing}>
            <RefreshCw className={cn('size-4', refreshing && 'animate-spin')} />
            {t('admin.refresh')}
          </Button>
        </div>
      </header>

      {overview.isError && <p className="text-sm text-destructive">{t('admin.loadFailed')}</p>}

      <Section title={t('admin.sections.users')}>
        {!data ? (
          <TileSkeletons count={4} />
        ) : (
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <AdminStatTile
              icon={Users}
              label={t('admin.stats.totalUsers')}
              value={data.users.total}
              sublabel={t('admin.stats.newThisWeek', { count: data.users.newLast7Days })}
            />
            <AdminStatTile
              icon={Activity}
              label={t('admin.stats.activeToday')}
              value={data.users.activeLast24Hours}
              sublabel={t('admin.stats.activeSessions', { count: data.users.activeSessions })}
            />
            <AdminStatTile
              icon={Activity}
              label={t('admin.stats.activeWeek')}
              value={data.users.activeLast7Days}
              sublabel={`${t('admin.stats.activeMonth')}: ${data.users.activeLast30Days}`}
            />
            <AdminStatTile
              icon={UserRoundX}
              label={t('admin.stats.withoutGroup')}
              value={data.users.withoutGroup}
              sublabel={t('admin.stats.withoutGroupHint')}
            />
          </div>
        )}
      </Section>

      <Section title={t('admin.sections.content')}>
        {!data ? (
          <TileSkeletons count={4} />
        ) : (
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <AdminStatTile
              icon={UsersRound}
              label={t('admin.stats.groups')}
              value={data.groups.total}
              sublabel={t('admin.stats.averageMembers', { value: data.groups.averageMembers })}
            />
            <AdminStatTile
              icon={Clapperboard}
              label={t('admin.stats.watchSessions')}
              value={data.watchSessions.total}
              sublabel={t('admin.stats.last30Days', { count: data.watchSessions.last30Days })}
            />
            <AdminStatTile
              icon={Star}
              label={t('admin.stats.fullyRated')}
              value={`${data.watchSessions.fullyRatedPercent}%`}
              sublabel={t('admin.stats.fullyRatedHint')}
            />
            <AdminStatTile
              icon={Film}
              label={t('admin.stats.cachedMedia')}
              value={data.media.movies + data.media.tvSeries + data.media.tvSeasons + data.media.tvEpisodes}
              sublabel={t('admin.stats.mediaBreakdown', {
                movies: data.media.movies,
                series: data.media.tvSeries,
                episodes: data.media.tvEpisodes,
              })}
            />
          </div>
        )}
      </Section>

      <Section
        title={t('admin.sections.activity')}
        action={
          <div className="flex rounded-lg border border-border/50 p-0.5">
            {ACTIVITY_RANGES.map((days) => (
              <button
                key={days}
                type="button"
                onClick={() => setActivityDays(days)}
                className={cn(
                  'cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium transition-colors',
                  activityDays === days
                    ? 'bg-primary/10 text-primary'
                    : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {t('admin.activity.days', { count: days })}
              </button>
            ))}
          </div>
        }
      >
        <AdminActivityCharts days={activityDays} />
      </Section>

      <div className="grid gap-8 lg:grid-cols-2">
        <Section title={t('admin.sections.ratings')}>
          {!data ? (
            <Skeleton className="h-44 rounded-xl" />
          ) : (
            <Card className="border-border/50 bg-card/50">
              <CardHeader className="pb-1">
                <CardTitle className="flex items-baseline justify-between gap-2 text-sm">
                  <span>{t('admin.ratingDistribution')}</span>
                  <span className="text-xs font-normal tabular-nums text-muted-foreground">
                    {data.ratings.total} · {t('admin.stats.averageRating', { value: data.ratings.average })}
                  </span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <AdminBarChart
                  data={data.ratings.distribution.map((b) => ({
                    key: String(b.value),
                    value: b.count,
                    tooltip: `${b.value}/10: ${b.count}`,
                  }))}
                  startLabel="1"
                  endLabel="10"
                />
              </CardContent>
            </Card>
          )}
        </Section>

        <Section title={t('admin.sections.invitations')}>
          {!data ? (
            <Skeleton className="h-44 rounded-xl" />
          ) : (
            <Card className="border-border/50 bg-card/50">
              <CardContent className="space-y-4 p-5">
                <div className="flex items-start gap-3">
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                    <Mail className="size-4 text-primary" />
                  </div>
                  <div className="space-y-1">
                    <p className="text-2xl font-bold tabular-nums">
                      {t('admin.invitations.acceptanceRate', { value: data.invitations.acceptanceRatePercent })}
                    </p>
                    <p className="text-sm text-muted-foreground">
                      {t('admin.invitations.summary', {
                        pending: data.invitations.pending,
                        accepted: data.invitations.accepted,
                        expired: data.invitations.expired,
                        revoked: data.invitations.revoked,
                      })}
                    </p>
                  </div>
                </div>
                {data.invitations.expiredButStillPending > 0 && (
                  <p className="flex items-start gap-1.5 text-xs text-amber-500">
                    <AlertTriangle className="mt-px size-3.5 shrink-0" />
                    {t('admin.invitations.stalePending', { count: data.invitations.expiredButStillPending })}
                  </p>
                )}
                <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <UserPlus className="size-3.5" />
                  {t('admin.stats.newThisMonth')}: {data.users.newLast30Days} ·{' '}
                  {t('admin.stats.dormantGroups')}: {data.groups.withoutWatchSessions}
                </p>
              </CardContent>
            </Card>
          )}
        </Section>
      </div>

      <Section title={t('admin.sections.groups')}>
        <AdminGroupsTable />
      </Section>

      <Section title={t('admin.sections.invitations')}>
        <AdminInvitationsPanel />
      </Section>
    </motion.div>
  )
}

function TileSkeletons({ count }: { count: number }) {
  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {Array.from({ length: count }, (_, i) => (
        <Skeleton key={i} className="h-24 rounded-xl" />
      ))}
    </div>
  )
}
