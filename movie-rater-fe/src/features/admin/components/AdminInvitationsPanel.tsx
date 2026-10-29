import { useState } from 'react'
import { AlertTriangle, Search } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { useI18n } from '@src/i18n/use-i18n'
import type { MessageKey } from '@src/i18n'
import { useDebouncedValue } from '@src/hooks/use-debounced-value'
import type { AdminInvitationDto, InvitationStatus } from '@src/types/admin'
import { useAdminInvitations } from '../hooks/use-admin'

const STATUSES: InvitationStatus[] = ['Pending', 'Accepted', 'Expired', 'Revoked']
const ALL = 'all'

const STATUS_STYLES: Record<InvitationStatus, string> = {
  Pending: 'bg-amber-500/15 text-amber-500',
  Accepted: 'bg-emerald-500/15 text-emerald-500',
  Expired: 'bg-muted text-muted-foreground',
  Revoked: 'bg-destructive/15 text-destructive',
}

function StatusBadge({ status }: { status: InvitationStatus }) {
  const { t } = useI18n()
  return (
    <span className={cn('inline-flex rounded-full px-2 py-0.5 text-xs font-medium', STATUS_STYLES[status])}>
      {t(`admin.invitations.status_${status}` as MessageKey)}
    </span>
  )
}

function InvitationRow({ invitation }: { invitation: AdminInvitationDto }) {
  const { t, formatDate } = useI18n()
  const date = (value: string) => formatDate(value, { dateStyle: 'medium' })

  return (
    <li className="space-y-2 px-4 py-3">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <StatusBadge status={invitation.effectiveStatus} />
        <span className="font-medium">
          {invitation.groupName ?? (
            <span className="italic text-muted-foreground">{t('admin.invitations.deletedGroup')}</span>
          )}
        </span>
        <span className="ml-auto text-xs tabular-nums text-muted-foreground">
          {t('admin.invitations.created')} {date(invitation.createdAt)} · {t('admin.invitations.expires')}{' '}
          {date(invitation.expiresAt)}
        </span>
      </div>

      <div className="grid gap-1 text-sm sm:grid-cols-2">
        <p className="min-w-0 truncate">
          <span className="text-muted-foreground">{t('admin.invitations.from')} </span>
          {invitation.inviterUsername}
        </p>
        <p className="min-w-0 truncate">
          <span className="text-muted-foreground">{t('admin.invitations.to')} </span>
          {invitation.inviteeUsername ?? (
            <span className="text-muted-foreground">({t('admin.invitations.notRegistered')})</span>
          )}{' '}
          <span className="text-muted-foreground">&lt;{invitation.inviteeEmail}&gt;</span>
          {invitation.acceptedByUsername && (
            <span className="text-muted-foreground">
              {' '}
              · {t('admin.invitations.acceptedBy', { username: invitation.acceptedByUsername })}
            </span>
          )}
        </p>
      </div>

      {invitation.issues.length > 0 && (
        <ul className="space-y-1">
          {invitation.issues.map((issue) => (
            <li key={issue} className="flex items-start gap-1.5 text-xs text-amber-500">
              <AlertTriangle className="mt-px size-3.5 shrink-0" />
              <span>{t(`admin.invitations.issues.${issue}` as MessageKey)}</span>
            </li>
          ))}
        </ul>
      )}
    </li>
  )
}

export function AdminInvitationsPanel() {
  const { t } = useI18n()
  const [status, setStatus] = useState<InvitationStatus | typeof ALL>(ALL)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim())

  const invitations = useAdminInvitations({
    status: status === ALL ? undefined : status,
    search: debouncedSearch || undefined,
  })

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('admin.invitations.searchPlaceholder')}
            maxLength={255}
            className="pl-8"
          />
        </div>
        <Select value={status} onValueChange={(value) => setStatus(value as InvitationStatus | typeof ALL)}>
          <SelectTrigger className="w-full sm:w-44">
            <SelectValue>
              {status === ALL
                ? t('admin.invitations.allStatuses')
                : t(`admin.invitations.status_${status}` as MessageKey)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>{t('admin.invitations.allStatuses')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {t(`admin.invitations.status_${s}` as MessageKey)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {invitations.isPending ? (
        <Skeleton className="h-40 rounded-xl" />
      ) : invitations.isError ? (
        <p className="text-sm text-destructive">{t('admin.loadFailed')}</p>
      ) : invitations.data.items.length === 0 ? (
        <p className="py-6 text-center text-sm text-muted-foreground">{t('admin.invitations.empty')}</p>
      ) : (
        <>
          <Card className={cn('border-border/50 bg-card/50 py-0', invitations.isFetching && 'opacity-70')}>
            <CardContent className="p-0">
              <ul className="divide-y divide-border/30">
                {invitations.data.items.map((invitation) => (
                  <InvitationRow key={invitation.id} invitation={invitation} />
                ))}
              </ul>
            </CardContent>
          </Card>
          <p className="text-xs text-muted-foreground">
            {t('admin.invitations.showing', {
              shown: invitations.data.items.length,
              total: invitations.data.totalCount,
            })}
          </p>
        </>
      )}
    </div>
  )
}
