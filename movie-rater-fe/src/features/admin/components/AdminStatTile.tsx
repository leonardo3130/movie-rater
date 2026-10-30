import { Card, CardContent } from '@/components/ui/card'
import type { LucideIcon } from 'lucide-react'

export function AdminStatTile({
  icon: Icon,
  label,
  value,
  sublabel,
}: {
  icon: LucideIcon
  label: string
  value: string | number
  sublabel?: string
}) {
  return (
    <Card className="border-border/50 bg-card/50">
      <CardContent className="flex items-start justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            {label}
          </p>
          <p className="text-2xl font-bold tabular-nums tracking-tight">{value}</p>
          {sublabel && <p className="text-xs text-muted-foreground">{sublabel}</p>}
        </div>
        <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10">
          <Icon className="size-4 text-primary" />
        </div>
      </CardContent>
    </Card>
  )
}
