import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { useI18n } from '@src/i18n/use-i18n'
import { useAdminGroups } from '../hooks/use-admin'

export function AdminGroupsTable() {
  const groups = useAdminGroups()
  const { t, formatDate } = useI18n()

  if (groups.isPending) return <Skeleton className="h-40 rounded-xl" />
  if (groups.isError) return <p className="text-sm text-destructive">{t('admin.loadFailed')}</p>

  if (groups.data.length === 0) {
    return <p className="py-6 text-center text-sm text-muted-foreground">{t('admin.groupsTable.empty')}</p>
  }

  return (
    <Card className="border-border/50 bg-card/50 py-0">
      <CardContent className="overflow-x-auto p-0">
        <table className="w-full min-w-[640px] text-sm">
          <thead>
            <tr className="border-b border-border/50 text-left text-xs uppercase tracking-wider text-muted-foreground">
              <th className="px-4 py-3 font-medium">{t('admin.groupsTable.name')}</th>
              <th className="px-4 py-3 font-medium">{t('admin.groupsTable.members')}</th>
              <th className="px-4 py-3 text-right font-medium">{t('admin.groupsTable.sessions')}</th>
              <th className="px-4 py-3 text-right font-medium">{t('admin.groupsTable.ratings')}</th>
              <th className="px-4 py-3 font-medium">{t('admin.groupsTable.lastActivity')}</th>
              <th className="px-4 py-3 text-right font-medium">{t('admin.groupsTable.pendingInvites')}</th>
            </tr>
          </thead>
          <tbody>
            {groups.data.map((group) => (
              <tr key={group.id} className="border-b border-border/30 last:border-0 hover:bg-muted/30">
                <td className="px-4 py-3 font-medium">{group.name}</td>
                <td className="px-4 py-3 text-muted-foreground">{group.members.join(', ')}</td>
                <td className="px-4 py-3 text-right tabular-nums">{group.watchSessionCount}</td>
                <td className="px-4 py-3 text-right tabular-nums">{group.ratingCount}</td>
                <td className="px-4 py-3 tabular-nums text-muted-foreground">
                  {group.lastWatchSessionAt
                    ? formatDate(group.lastWatchSessionAt, { dateStyle: 'medium' })
                    : t('admin.groupsTable.never')}
                </td>
                <td className="px-4 py-3 text-right">
                  {group.pendingInvitations > 0 ? (
                    <Badge variant="secondary" className="tabular-nums">
                      {group.pendingInvitations}
                    </Badge>
                  ) : (
                    <span className="text-muted-foreground">0</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </CardContent>
    </Card>
  )
}
