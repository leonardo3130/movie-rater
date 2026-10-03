import { motion } from 'framer-motion'
import { Star } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { UserMovieToggle } from './UserMovieToggle'
import { WatchSessionEpisodeLabel } from '../../movies/components/WatchSessionEpisodeLabel'
import { Link } from 'react-router'
import type { UserMovieWithMovie } from '@src/types/user-movie'

interface UserMovieCardProps {
  item: UserMovieWithMovie
  isFavorite: boolean
  isInWatchlist: boolean
  index?: number
}

function mediaHref(item: UserMovieWithMovie): string {
  switch (item.mediaType) {
    case 'TvSeries':
      return `/tv/${item.tmdbId}`
    case 'TvSeason':
      return item.seriesTmdbId != null && item.seasonNumber != null
        ? `/tv/${item.seriesTmdbId}/season/${item.seasonNumber}`
        : '/tv'
    case 'TvEpisode':
      return item.seriesTmdbId != null && item.seasonNumber != null && item.episodeNumber != null
        ? `/tv/${item.seriesTmdbId}/season/${item.seasonNumber}/episode/${item.episodeNumber}`
        : '/tv'
    default:
      return `/movies/${item.tmdbId}`
  }
}

export function UserMovieCard({
  item,
  isFavorite,
  isInWatchlist,
  index = 0,
}: UserMovieCardProps) {
  const year = item.releaseDate ? item.releaseDate.slice(0, 4) : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.03, ease: 'easeOut' }}
      whileHover={{ y: -4 }}
      className="group shrink-0"
    >
      <Link to={mediaHref(item)} className="block space-y-2">
        <div className="relative overflow-hidden rounded-lg">
          <MoviePoster src={item.posterUrl} alt={item.title} />
          <div className="absolute top-2 right-2">
            <Badge variant="secondary" className="flex items-center gap-1 text-xs">
              <Star className="size-3 fill-yellow-500 text-yellow-500" />
              {item.voteAverage.toFixed(1)}
            </Badge>
          </div>
          <div className="absolute bottom-2 left-2 opacity-0 group-hover:opacity-100 transition-opacity">
            <UserMovieToggle
              movieId={item.id}
              isFavorite={isFavorite}
              isInWatchlist={isInWatchlist}
              size="sm"
            />
          </div>
        </div>
        <div className="space-y-0.5">
          <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
            {item.title}
          </p>
          {item.mediaType === 'TvEpisode' ? (
            <WatchSessionEpisodeLabel
              seriesTitle={item.seriesTitle}
              seasonNumber={item.seasonNumber}
              episodeNumber={item.episodeNumber}
              className="text-xs"
            />
          ) : year ? (
            <p className="text-xs text-muted-foreground">{year}</p>
          ) : null}
        </div>
      </Link>
    </motion.div>
  )
}