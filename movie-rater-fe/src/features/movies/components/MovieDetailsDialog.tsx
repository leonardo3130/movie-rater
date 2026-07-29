import { useNavigate, useParams } from 'react-router'
import { motion } from 'framer-motion'
import { Star, Clock, Calendar, Users, Play, ExternalLink, Heart, Bookmark } from 'lucide-react'
import { Dialog, DialogContent } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { MoviePoster } from './MoviePoster'
import { MovieCard } from './MovieCard'
import { useMovieDetails } from '../hooks/use-movie-details'
import { useMovieRecommendations } from '../hooks/use-movie-recommendations'
import { useToggleFavorite } from '../../user-movie/hooks/use-toggle-favorite'
import { useToggleWatchlist } from '../../user-movie/hooks/use-toggle-watchlist'
import { useUserMovieStore } from '../../../stores/user-movie-store'

export function MovieDetailsDialog() {
  const { tmdbId } = useParams<{ tmdbId: string }>()
  const navigate = useNavigate()
  const movieId = tmdbId ? Number(tmdbId) : null
  const movieIdStr = String(movieId ?? '')

  const { data: movie, isLoading } = useMovieDetails(movieId)
  const { data: recs } = useMovieRecommendations(movieId)

  const toggleFavorite = useToggleFavorite()
  const toggleWatchlist = useToggleWatchlist()
  const favoriteIds = useUserMovieStore((s) => s.favoriteIds)
  const watchlistIds = useUserMovieStore((s) => s.watchlistIds)
  const isFavorite = movie
    ? movie.isFavorite || favoriteIds.has(movieIdStr)
    : false
  const isInWatchlist = movie
    ? movie.isInWatchlist || watchlistIds.has(movieIdStr)
    : false

  const open = movieId !== null && !isNaN(Number(tmdbId))

  const handleClose = () => navigate('/movies')

  const year = movie?.releaseDate ? movie.releaseDate.slice(0, 4) : null
  const runtime = movie?.runtime ? `${Math.floor(movie.runtime / 60)}h ${movie.runtime % 60}m` : null

  const trailer = movie?.videos?.find(
    (v) => v.site === 'YouTube' && (v.type === 'Trailer' || v.type === 'Teaser') && v.official
  )

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) handleClose() }}>
      <DialogContent className="max-w-5xl w-[calc(100%-2rem)] max-h-[85vh] p-0 gap-0 overflow-hidden sm:max-w-none sm:w-full" showCloseButton={false}>
        <div className="max-h-[80vh] min-h-0 overflow-y-auto">
          {isLoading || !movie ? (
            <div className="p-6 space-y-4">
              <Skeleton className="h-48 w-full rounded-lg" />
              <Skeleton className="h-8 w-64" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-32 w-full" />
            </div>
          ) : (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.3 }}
              className="w-full"
            >
              <div className="relative h-64 md:h-80 overflow-hidden">
                {movie.backdropUrl ? (
                  <img
                    src={movie.backdropUrl}
                    alt=""
                    className="size-full object-cover"
                  />
                ) : (
                  <div className="size-full bg-muted" />
                )}
                <div className="absolute inset-0 bg-gradient-to-t from-popover via-popover/60 to-transparent" />
                <div className="absolute bottom-0 left-0 right-0 p-6 flex items-end gap-4">
                  <div className="hidden sm:block w-24 shrink-0">
                    <MoviePoster src={movie.posterUrl} alt={movie.title} />
                  </div>
                  <div className="space-y-2 min-w-0">
                    <h2 className="font-heading text-2xl font-bold break-words">{movie.title}</h2>
                    {movie.tagline && (
                      <p className="text-sm text-muted-foreground italic break-words">{movie.tagline}</p>
                    )}
                    <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                      {year && <span className="flex items-center gap-1"><Calendar className="size-3" />{year}</span>}
                      {runtime && <span className="flex items-center gap-1"><Clock className="size-3" />{runtime}</span>}
                      {movie.status && <Badge variant="outline" className="text-[10px]">{movie.status}</Badge>}
                    </div>
                  </div>
                </div>
              </div>

              <div className="p-6 space-y-6 min-w-0">
                <div className="flex flex-wrap items-center gap-4">
                  <div className="flex items-center gap-2">
                    <Star className="size-5 fill-yellow-500 text-yellow-500" />
                    <span className="font-semibold text-lg">{movie.voteAverage.toFixed(1)}</span>
                    <span className="text-xs text-muted-foreground">({movie.voteCount.toLocaleString()} votes)</span>
                  </div>
                  {movie.genres.map((g) => (
                    <Badge key={g.tmdbId} variant="secondary">{g.name}</Badge>
                  ))}
                </div>

                {movie.overview && (
                  <div>
                    <h3 className="font-heading text-sm font-medium text-muted-foreground mb-1">Overview</h3>
                    <p className="text-sm leading-relaxed break-words">{movie.overview}</p>
                  </div>
                )}

                <div className="flex flex-wrap gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={(e) => {
                      e.preventDefault()
                      toggleFavorite.mutate({ movieId: movieIdStr, value: !isFavorite })
                    }}
                  >
                    <Heart className="size-4" fill={isFavorite ? 'currentColor' : 'none'} />
                    {isFavorite ? 'Remove from Favorites' : 'Add to Favorites'}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={(e) => {
                      e.preventDefault()
                      toggleWatchlist.mutate({ movieId: movieIdStr, value: !isInWatchlist })
                    }}
                  >
                    <Bookmark className="size-4" fill={isInWatchlist ? 'currentColor' : 'none'} />
                    {isInWatchlist ? 'Remove from Watchlist' : 'Add to Watchlist'}
                  </Button>
                  {trailer && (
                    <Button variant="outline" size="sm">
                      <a
                        href={`https://www.youtube.com/watch?v=${trailer.key}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center gap-1"
                      >
                        <Play className="size-4" />
                        Trailer
                      </a>
                    </Button>
                  )}
                  {movie.homepage && (
                    <Button variant="ghost" size="sm">
                      <a
                        href={movie.homepage}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center gap-1"
                      >
                        <ExternalLink className="size-4" />
                        Homepage
                      </a>
                    </Button>
                  )}
                </div>

                {(movie.budget > 0 || movie.revenue > 0) && (
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    {movie.budget > 0 && (
                      <div>
                        <span className="text-muted-foreground">Budget</span>
                        <p className="font-medium">${movie.budget.toLocaleString()}</p>
                      </div>
                    )}
                    {movie.revenue > 0 && (
                      <div>
                        <span className="text-muted-foreground">Revenue</span>
                        <p className="font-medium">${movie.revenue.toLocaleString()}</p>
                      </div>
                    )}
                  </div>
                )}

                {movie.cast.length > 0 && (
                  <>
                    <Separator />
                    <div>
                      <h3 className="font-heading text-sm font-medium text-muted-foreground mb-3 flex items-center gap-2">
                        <Users className="size-4" />
                        Cast
                      </h3>
                      <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-none max-w-full">
                        {movie.cast.slice(0, 10).map((person) => (
                          <div key={person.id} className="w-20 shrink-0 text-center space-y-1">
                            <div className="size-20 rounded-full overflow-hidden bg-muted mx-auto">
                              {person.profileUrl ? (
                                <img src={person.profileUrl} alt={person.name} className="size-full object-cover" />
                              ) : (
                                <div className="size-full flex items-center justify-center text-muted-foreground text-xs">
                                  N/A
                                </div>
                              )}
                            </div>
                            <p className="text-xs font-medium leading-tight line-clamp-2">{person.name}</p>
                            {person.character && (
                              <p className="text-[10px] text-muted-foreground line-clamp-1">{person.character}</p>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  </>
                )}

                {recs && recs.results.length > 0 && (
                  <>
                    <Separator />
                    <div>
                      <h3 className="font-heading text-sm font-medium text-muted-foreground mb-3">Recommendations</h3>
                      <div className="flex gap-3 overflow-x-auto pb-2 scrollbar-none max-w-full">
                        {recs.results.slice(0, 10).map((recMovie, i) => (
                          <div key={recMovie.tmdbId} className="w-[120px] shrink-0" onClick={handleClose}>
                            <MovieCard movie={recMovie} index={i} />
                          </div>
                        ))}
                      </div>
                    </div>
                  </>
                )}
              </div>
            </motion.div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
