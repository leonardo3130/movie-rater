import { useRef } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { MovieCard } from './MovieCard'
import type { MovieSummaryDto } from '@src/types/movie'

interface MovieRowProps {
  title: string
  movies: MovieSummaryDto[] | undefined
  isLoading: boolean
  page: number
  totalPages?: number
  onPageChange: (page: number) => void
}

const SCROLL_AMOUNT = 800

export function MovieRow({ title, movies, isLoading, page, totalPages, onPageChange }: MovieRowProps) {
  const scrollRef = useRef<HTMLDivElement>(null)

  const scroll = (direction: 'left' | 'right') => {
    if (!scrollRef.current) return
    const amount = direction === 'left' ? -SCROLL_AMOUNT : SCROLL_AMOUNT
    scrollRef.current.scrollBy({ left: amount, behavior: 'smooth' })
  }

  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h2 className="font-heading text-lg font-medium tracking-tight">{title}</h2>
          {totalPages && totalPages > 1 && (
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="icon-xs"
                disabled={page <= 1}
                onClick={() => onPageChange(page - 1)}
              >
                <ChevronLeft className="size-3" />
              </Button>
              <span className="text-xs text-muted-foreground tabular-nums">
                {page} / {totalPages}
              </span>
              <Button
                variant="ghost"
                size="icon-xs"
                disabled={page >= totalPages}
                onClick={() => onPageChange(page + 1)}
              >
                <ChevronRight className="size-3" />
              </Button>
            </div>
          )}
        </div>
        <div className="flex gap-1">
          <Button variant="ghost" size="icon-sm" onClick={() => scroll('left')}>
            <ChevronLeft className="size-4" />
          </Button>
          <Button variant="ghost" size="icon-sm" onClick={() => scroll('right')}>
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>

      <div
        ref={scrollRef}
        className="flex gap-3 overflow-x-auto pb-2 scrollbar-none scroll-smooth"
      >
        {isLoading
          ? Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="w-[160px] shrink-0 space-y-2">
                <Skeleton className="aspect-[2/3] w-full rounded-lg" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-3 w-16" />
              </div>
            ))
          : movies?.map((movie, i) => (
              <MovieCard key={movie.tmdbId} movie={movie} index={i} />
            ))}
      </div>
    </section>
  )
}