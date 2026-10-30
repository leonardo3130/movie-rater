import { AlertTriangle } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { useI18n } from '@src/i18n/use-i18n'
import type { AdminOverviewResponseDto } from '@src/types/admin'

export function AdminInvitationSummary({
  stats,
}: {
  stats: AdminOverviewResponseDto['invitations']
}) {
  const { t } = useI18n()

  return (
    <Card className="border-border/50 bg-card/50">
      <CardContent className="space-y-1.5">
        <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
          <p className="text-lg font-semibold tabular-nums">
            {t('admin.invitations.acceptanceRate', { value: stats.acceptanceRatePercent })}
          </p>
          <p className="text-sm tabular-nums text-muted-foreground">
            {t('admin.invitations.summary', {
              pending: stats.pending,
              accepted: stats.accepted,
              expired: stats.expired,
              revoked: stats.revoked,
            })}
          </p>
        </div>
        {stats.expiredButStillPending > 0 && (
          <p className="flex items-start gap-1.5 text-xs text-amber-500">
            <AlertTriangle className="mt-px size-3.5 shrink-0" />
            {t('admin.invitations.stalePending', { count: stats.expiredButStillPending })}
          </p>
        )}
      </CardContent>
    </Card>
  )
}
