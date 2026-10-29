import { useState } from 'react'
import { cn } from '@/lib/utils'

export interface AdminBarDatum {
  key: string
  value: number
  // Shown in the hover tooltip, e.g. "Oct 12: 3".
  tooltip: string
}

// Single-series bar chart: thin bars with a 2px gap, rounded data-ends on the baseline,
// and a full-height hit target per bar so small values stay easy to hover.
export function AdminBarChart({
  data,
  startLabel,
  endLabel,
  height = 'h-28',
}: {
  data: AdminBarDatum[]
  startLabel?: string
  endLabel?: string
  height?: string
}) {
  const [hovered, setHovered] = useState<number | null>(null)
  const max = Math.max(1, ...data.map((d) => d.value))
  const active = hovered !== null ? data[hovered] : null

  return (
    <div className="space-y-1.5">
      <div className="relative">
        <div className="pointer-events-none absolute -top-1 left-0 right-0 flex h-5 justify-center">
          {active && (
            <span className="rounded-md border border-border/50 bg-popover px-2 py-0.5 text-xs tabular-nums text-popover-foreground shadow-sm">
              {active.tooltip}
            </span>
          )}
        </div>
        <div
          className={cn('flex items-end gap-[2px] border-b border-border/60 pt-6', height)}
          onMouseLeave={() => setHovered(null)}
        >
          {data.map((d, index) => (
            <div
              key={d.key}
              className="flex h-full min-w-0 flex-1 items-end"
              onMouseEnter={() => setHovered(index)}
              onTouchStart={() => setHovered(index)}
              aria-label={d.tooltip}
              role="img"
            >
              <div
                className={cn(
                  'w-full rounded-t-[4px] transition-colors',
                  hovered === index ? 'bg-primary' : 'bg-primary/70',
                )}
                style={{ height: d.value > 0 ? `${Math.max(4, (d.value / max) * 100)}%` : 0 }}
              />
            </div>
          ))}
        </div>
      </div>
      {(startLabel || endLabel) && (
        <div className="flex justify-between text-[0.65rem] tabular-nums text-muted-foreground">
          <span>{startLabel}</span>
          <span>{endLabel}</span>
        </div>
      )}
    </div>
  )
}
