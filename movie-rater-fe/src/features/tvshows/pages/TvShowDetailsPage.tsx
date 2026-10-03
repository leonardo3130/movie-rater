import { useParams, Link } from 'react-router'
import { motion } from 'framer-motion'
import {
  ArrowLeft,
  Star,
  Calendar,
  MonitorPlay,
  Layers,
  ListVideo,
  Eye,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { useTvShowDetails } from '../hooks/use-tv'
import type { TvSeasonSummaryDto } from '@src/types/tv'

export function TvShowDetailsPage() {
  const { tmdbId } = useParams<{ tmdbId: string }>()
  const tmdbIdNumber = tmdbId ? Number(tmdbId) : null
  const { data: show, isLoading, isError } = useTvShowDetails(tmdbIdNumber)

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-64 w-full rounded-lg" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-3/4" />
      </div>
    )
  }

  if (isError || !show) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Show not found</p>
      </div>
    )
  }

  const year = show.firstAirDate ? show.firstAirDate.slice(0, 4) : null

  return (
    <div className="min-h-dvh bg-background">
      <div className="mx-auto max-w-7xl px-4 py-6">
        <Link
          to="/tv"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-4" />
          Back to TV Shows
        </Link>

        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.3 }}
          className="mt-4 overflow-hidden rounded-xl"
        >
          <div className="relative h-64 md:h-80">
            {show.backdropUrl ? (
              <img
                src={show.backdropUrl}
                alt=""
                className="size-full object-cover"
              />
            ) : (
              <div className="size-full bg-muted" />
            )}
            <div className="absolute inset-0 bg-gradient-to-t from-popover via-popover/60 to-transparent" />
            <div className="absolute bottom-0 left-0 right-0 p-6 flex items-end gap-4">
              <div className="hidden sm:block w-24 shrink-0">
                <MoviePoster src={show.posterUrl} alt={show.name} />
              </div>
              <div className="space-y-2 min-w-0">
                <h1 className="font-heading text-2xl font-bold break-words">{show.name}</h1>
                <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                  {year && (
                    <span className="flex items-center gap-1">
                      <Calendar className="size-3" />
                      {year}
                    </span>
                  )}
                  <span className="flex items-center gap-1">
                    <Layers className="size-3" />
                    {show.numberOfSeasons} season{show.numberOfSeasons !== 1 ? 's' : ''}
                  </span>
                  <span className="flex items-center gap-1">
                    <ListVideo className="size-3" />
                    {show.numberOfEpisodes} episodes
                  </span>
                  {show.status && <Badge variant="outline" className="text-[10px]">{show.status}</Badge>}
                  {show.type && <Badge variant="outline" className="text-[10px]">{show.type}</Badge>}
                </div>
              </div>
            </div>
          </div>

          <div className="p-6 space-y-6">
            <div className="flex flex-wrap items-center gap-4">
              <div className="flex items-center gap-2">
                <Star className="size-5 fill-yellow-500 text-yellow-500" />
                <span className="font-semibold text-lg">{show.voteAverage.toFixed(1)}</span>
                <span className="text-xs text-muted-foreground">
                  ({show.voteCount.toLocaleString()} votes)
                </span>
              </div>
              {show.genres.map((genre) => (
                <Badge key={genre.tmdbId} variant="secondary">{genre.name}</Badge>
              ))}
              {show.watchedCount > 0 && (
                <Badge variant="outline" className="flex items-center gap-1">
                  <Eye className="size-3" />
                  Watched {show.watchedCount} time{show.watchedCount !== 1 ? 's' : ''}
                </Badge>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
              {show.firstAirDate && (
                <span>
                  First air date: {new Date(show.firstAirDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                  })}
                </span>
              )}
              {show.lastAirDate && (
                <span>
                  Last air date: {new Date(show.lastAirDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                  })}
                </span>
              )}
            </div>

            {show.overview && (
              <div>
                <h2 className="font-heading text-sm font-medium text-muted-foreground mb-1">
                  Overview
                </h2>
                <p className="text-sm leading-relaxed break-words">{show.overview}</p>
              </div>
            )}
          </div>
        </motion.div>

        {show.seasons.length > 0 && (
          <>
            <Separator className="my-6" />
            <div className="space-y-4">
              <h2 className="font-heading text-lg font-medium tracking-tight flex items-center gap-2">
                <MonitorPlay className="size-4 text-muted-foreground" />
                Seasons
              </h2>
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
                {show.seasons.map((season, index) => (
                  <SeasonCard
                    key={season.seasonNumber}
                    season={season}
                    seriesTmdbId={show.tmdbId}
                    index={index}
                  />
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

function SeasonCard({
  season,
  seriesTmdbId,
  index,
}: {
  season: TvSeasonSummaryDto
  seriesTmdbId: number
  index: number
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05, ease: 'easeOut' }}
      whileHover={{ y: -4 }}
      className="group shrink-0"
    >
      <Link
        to={`/tv/${seriesTmdbId}/season/${season.seasonNumber}`}
        className="block space-y-2"
      >
        <div className="relative overflow-hidden rounded-lg">
          <MoviePoster src={season.posterUrl} alt={`Season ${season.seasonNumber}`} />
          <div className="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
          <div className="absolute bottom-2 left-2 text-xs font-medium text-white opacity-0 group-hover:opacity-100 transition-opacity">
            {season.episodeCount} episodes
          </div>
        </div>
        <div>
          <p className="text-sm font-medium leading-tight line-clamp-1 group-hover:text-primary transition-colors">
            Season {season.seasonNumber}
          </p>
          {season.airDate && (
            <p className="text-xs text-muted-foreground">
              {new Date(season.airDate).getFullYear()}
            </p>
          )}
        </div>
      </Link>
    </motion.div>
  )
}