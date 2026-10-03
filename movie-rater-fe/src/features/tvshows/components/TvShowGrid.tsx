import { motion } from 'framer-motion'
import { ChevronLeft, ChevronRight, SearchX } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { TvShowCard } from './TvShowCard'
import type { TvShowSummaryDto } from '@src/types/tv'
import { useTvStore } from '../stores/tv-store'

interface TvShowGridProps {
  shows: TvShowSummaryDto[] | undefined
  isLoading: boolean
  totalPages?: number
  emptyMessage?: string
}

export function TvShowGrid({ shows, isLoading, totalPages, emptyMessage }: TvShowGridProps) {
  const page = useTvStore((s) => s.page)
  const setPage = useTvStore((s) => s.setPage)

  if (!isLoading && (!shows || shows.length === 0)) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
        <SearchX className="size-12 mb-3" />
        <p className="text-sm">{emptyMessage ?? 'No TV shows match your search'}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <motion.div
        layout
        className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
      >
        {isLoading
          ? Array.from({ length: 12 }).map((_, i) => (
              <div key={i} className="space-y-2">
                <Skeleton className="aspect-[2/3] w-full rounded-lg" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-3 w-16" />
              </div>
            ))
          : shows?.map((show, i) => (
              <TvShowCard key={show.tmdbId} show={show} index={i} />
            ))}
      </motion.div>

      {totalPages && totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage(page - 1)}
          >
            <ChevronLeft className="size-4" />
            Previous
          </Button>
          <span className="text-sm text-muted-foreground tabular-nums">
            {page} / {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(page + 1)}
          >
            Next
            <ChevronRight className="size-4" />
          </Button>
        </div>
      )}
    </div>
  )
}