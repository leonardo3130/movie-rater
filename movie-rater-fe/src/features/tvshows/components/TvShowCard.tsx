import { Link } from 'react-router'
import { motion } from 'framer-motion'
import { Star, Heart, Bookmark } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { useToggleFavorite } from '../../user-movie/hooks/use-toggle-favorite'
import { useToggleWatchlist } from '../../user-movie/hooks/use-toggle-watchlist'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import type { TvShowSummaryDto } from '@src/types/tv'
import { cn } from '@/lib/utils'

interface TvShowCardProps {
  show: TvShowSummaryDto
  index?: number
}

export function TvShowCard({ show, index = 0 }: TvShowCardProps) {
  const toggleFavorite = useToggleFavorite()
  const toggleWatchlist = useToggleWatchlist()
  const favoriteIds = useUserMovieStore((s) => s.favoriteIds)
  const watchlistIds = useUserMovieStore((s) => s.watchlistIds)
  const year = show.firstAirDate ? show.firstAirDate.slice(0, 4) : null
  const isFavorite = show.isFavorite || favoriteIds.has(show.id)
  const isInWatchlist = show.isInWatchlist || watchlistIds.has(show.id)

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05, ease: 'easeOut' }}
      whileHover={{ y: -4 }}
      className="group shrink-0 w-[160px]"
    >
      <Link to={`/tv/${show.tmdbId}`} className="block space-y-2">
        <div className="relative overflow-hidden rounded-lg">
          <MoviePoster src={show.posterUrl} alt={show.title} />
          <div className="absolute top-2 right-2">
            <Badge variant="secondary" className="flex items-center gap-1 text-xs">
              <Star className="size-3 fill-yellow-500 text-yellow-500" />
              {show.voteAverage.toFixed(1)}
            </Badge>
          </div>
          <div className="absolute bottom-2 left-2 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
            <button
              type="button"
              onClick={(e) => {
                e.preventDefault()
                e.stopPropagation()
                toggleFavorite.mutate({ movieId: show.id, value: !isFavorite })
              }}
              className="rounded-full p-1 cursor-pointer transition-colors hover:bg-white/10 text-white/60 hover:text-white/90"
              aria-label={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
            >
              <Heart
                className={cn('size-3.5', isFavorite && 'fill-red-500 text-red-500')}
              />
            </button>
            <button
              type="button"
              onClick={(e) => {
                e.preventDefault()
                e.stopPropagation()
                toggleWatchlist.mutate({ movieId: show.id, value: !isInWatchlist })
              }}
              className="rounded-full p-1 cursor-pointer transition-colors hover:bg-white/10 text-white/60 hover:text-white/90"
              aria-label={isInWatchlist ? 'Remove from watchlist' : 'Add to watchlist'}
            >
              <Bookmark
                className={cn('size-3.5', isInWatchlist && 'fill-yellow-400 text-yellow-400')}
              />
            </button>
          </div>
        </div>
        <div className="space-y-0.5">
          <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
            {show.title}
          </p>
          {year && <p className="text-xs text-muted-foreground">{year}</p>}
        </div>
      </Link>
    </motion.div>
  )
}