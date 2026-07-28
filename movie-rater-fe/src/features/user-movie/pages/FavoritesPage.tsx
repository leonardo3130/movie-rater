import { motion } from 'framer-motion'
import { Heart, Loader2, Star } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useFavorites } from '../hooks/use-favorites'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { UserMovieToggle } from '../components/UserMovieToggle'
import { Link, useNavigate } from 'react-router'

export function FavoritesPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const { data, isLoading, isError, isFetching } = useFavorites(page, 20)

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
        <p className="text-muted-foreground">Failed to load favorites</p>
      </div>
    )
  }

  const movies = data?.results ?? []

  if (movies.length === 0) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Heart className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">No favorites yet</h2>
        <p className="text-muted-foreground">
          Start discovering movies and add your favorites!
        </p>
        <Button onClick={() => navigate('/movies')}>Browse movies</Button>
      </div>
    )
  }

  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 space-y-8">
      <div className="flex items-center gap-3">
        <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-red-500/10">
          <Heart className="size-5 text-red-500" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Favorites</h1>
          <p className="text-sm text-muted-foreground">
            {data?.totalResults ?? 0} movie{data?.totalResults !== 1 ? 's' : ''}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {movies.map((movie, index) => {
          const year = movie.releaseDate ? movie.releaseDate.slice(0, 4) : null
          return (
            <motion.div
              key={movie.id}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.3, delay: index * 0.03, ease: 'easeOut' }}
              whileHover={{ y: -4 }}
              className="group shrink-0"
            >
              <Link to={`/movies/${movie.tmdbId}`} className="block space-y-2">
                <div className="relative overflow-hidden rounded-lg">
                  <MoviePoster src={movie.posterUrl} alt={movie.title} />
                  <div className="absolute top-2 right-2">
                    <Badge variant="secondary" className="flex items-center gap-1 text-xs">
                      <Star className="size-3 fill-yellow-500 text-yellow-500" />
                      {movie.voteAverage.toFixed(1)}
                    </Badge>
                  </div>
                  <div className="absolute bottom-2 left-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <UserMovieToggle
                      movieId={movie.id}
                      isFavorite={true}
                      isInWatchlist={movie.isInWatchlist}
                      size="sm"
                    />
                  </div>
                </div>
                <div className="space-y-0.5">
                  <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
                    {movie.title}
                  </p>
                  {year && <p className="text-xs text-muted-foreground">{year}</p>}
                </div>
              </Link>
            </motion.div>
          )
        })}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1 || isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Previous
          </Button>
          <div className="flex items-center gap-1.5">
            {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
              const pageNum = page <= 3
                ? i + 1
                : page >= totalPages - 2
                  ? totalPages - 4 + i
                  : page - 2 + i
              if (pageNum < 1 || pageNum > totalPages) return null
              return (
                <Button
                  key={pageNum}
                  variant={pageNum === page ? 'default' : 'ghost'}
                  size="xs"
                  onClick={() => setPage(pageNum)}
                  className="min-w-[2rem]"
                >
                  {pageNum}
                </Button>
              )
            })}
          </div>
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