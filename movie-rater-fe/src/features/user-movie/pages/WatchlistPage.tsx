import { motion } from 'framer-motion'
import { Bookmark, Loader2 } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useWatchlist } from '../hooks/use-watchlist'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { UserMovieToggle } from '../components/UserMovieToggle'
import { Link } from 'react-router'

export function WatchlistPage() {
  const [page, setPage] = useState(1)
  const { data, isLoading, isError, isFetching } = useWatchlist(page, 20)

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Failed to load watchlist</p>
      </div>
    )
  }

  const movies = data?.results ?? []

  if (movies.length === 0) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Bookmark className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">Your watchlist is empty</h2>
        <p className="text-muted-foreground">
          Find something to watch and add it to your list!
        </p>
        <Button asChild>
          <Link to="/movies">Browse movies</Link>
        </Button>
      </div>
    )
  }

  const totalPages = data?.totalPages ?? 1

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Bookmark className="size-6 text-yellow-400" />
        <h1 className="text-2xl font-bold">Watchlist</h1>
        <span className="text-sm text-muted-foreground">
          {data?.totalResults ?? 0} movies
        </span>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {movies.map((movie, index) => (
          <motion.div
            key={movie.id}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: index * 0.03, ease: 'easeOut' }}
            whileHover={{ y: -4 }}
            className="group relative space-y-2"
          >
            <Link to={`/movies/${movie.tmdbId}`} className="block space-y-2">
              <div className="relative overflow-hidden rounded-lg">
                <MoviePoster src={movie.posterUrl} alt={movie.title} />
                <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity">
                  <UserMovieToggle
                    movieId={movie.id}
                    isFavorite={movie.isFavorite}
                    isInWatchlist={true}
                    size="sm"
                  />
                </div>
              </div>
              <div className="space-y-0.5">
                <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
                  {movie.title}
                </p>
                {movie.releaseDate && (
                  <p className="text-xs text-muted-foreground">
                    {movie.releaseDate.slice(0, 4)}
                  </p>
                )}
              </div>
            </Link>
          </motion.div>
        ))}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-4">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1 || isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground px-3">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages || isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {isFetching ? <Loader2 className="size-3 animate-spin mr-1" /> : null}
            Next
          </Button>
        </div>
      )}
    </div>
  )
}