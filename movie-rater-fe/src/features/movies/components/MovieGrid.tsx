import { motion } from 'framer-motion'
import { ChevronLeft, ChevronRight, SearchX } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { MovieCard } from './MovieCard'
import type { MovieSummaryDto } from '@src/types/movie'
import { useMoviesStore } from '@src/stores/movies-store'

interface MovieGridProps {
  movies: MovieSummaryDto[] | undefined
  isLoading: boolean
  totalPages?: number
  category: 'search' | 'discover'
}

export function MovieGrid({ movies, isLoading, totalPages, category }: MovieGridProps) {
  const page = useMoviesStore((s) => s.pages[category])
  const setPage = useMoviesStore((s) => s.setPage)

  if (!isLoading && (!movies || movies.length === 0)) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
        <SearchX className="size-12 mb-3" />
        <p className="text-sm">
          {category === 'search' ? 'No movies match your search' : 'No movies match your filters'}
        </p>
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
          : movies?.map((movie, i) => (
              <MovieCard key={movie.tmdbId} movie={movie} index={i} />
            ))}
      </motion.div>

      {totalPages && totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage(category, page - 1)}
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
            onClick={() => setPage(category, page + 1)}
          >
            Next
            <ChevronRight className="size-4" />
          </Button>
        </div>
      )}
    </div>
  )
}