import { useState } from 'react'
import { cn } from '@/lib/utils'

export interface AdminBarDatum {
  key: string
  value: number
  // Shown in the hover tooltip, e.g. "Oct 12: 3".
  tooltip: string
}

// Single-series bar chart: rounded data-ends on a shared baseline and a full-height hit
// target per bar, so days with small values are still easy to hover or tap.
export function AdminBarChart({
  data,
  label,
  startLabel,
  endLabel,
  labelEveryBar = false,
}: {
  data: AdminBarDatum[]
  label: string
  startLabel?: string
  endLabel?: string
  // Prints each datum's key under its bar; only readable for short series.
  labelEveryBar?: boolean
}) {
  const [hovered, setHovered] = useState<number | null>(null)
  const max = Math.max(1, ...data.map((d) => d.value))
  const active = hovered !== null ? data[hovered] : null
  // Center of the hovered bar, clamped so the tooltip never overflows the card.
  const tooltipLeft =
    hovered !== null ? Math.min(85, Math.max(15, ((hovered + 0.5) / data.length) * 100)) : 0

  return (
    <div className="space-y-1.5">
      <div className="relative" role="img" aria-label={label}>
        {active && (
          <span
            className="pointer-events-none absolute top-0 z-10 -translate-x-1/2 whitespace-nowrap rounded-md border border-border/50 bg-popover px-2 py-0.5 text-xs tabular-nums text-popover-foreground shadow-sm"
            style={{ left: `${tooltipLeft}%` }}
          >
            {active.tooltip}
          </span>
        )}
        <div
          className={cn(
            'flex h-28 items-end border-b border-border/60 pt-7',
            data.length > 45 ? 'gap-px' : 'gap-0.5',
          )}
          onMouseLeave={() => setHovered(null)}
        >
          {data.map((d, index) => (
            <div
              key={d.key}
              className="flex h-full min-w-0 flex-1 items-end justify-center"
              onMouseEnter={() => setHovered(index)}
              onTouchStart={() => setHovered(index)}
            >
              <div
                className={cn(
                  'w-full max-w-10 rounded-t-sm transition-colors',
                  hovered === index ? 'bg-primary' : 'bg-primary/70',
                )}
                style={{ height: d.value > 0 ? `${Math.max(4, (d.value / max) * 100)}%` : 0 }}
              />
            </div>
          ))}
        </div>
      </div>
      {labelEveryBar && (
        <div className="flex gap-0.5 text-[0.65rem] tabular-nums text-muted-foreground">
          {data.map((d) => (
            <span key={d.key} className="min-w-0 flex-1 text-center">
              {d.key}
            </span>
          ))}
        </div>
      )}
      {!labelEveryBar && (startLabel || endLabel) && (
        <div className="flex justify-between text-[0.65rem] tabular-nums text-muted-foreground">
          <span>{startLabel}</span>
          <span>{endLabel}</span>
        </div>
      )}
    </div>
  )
}
