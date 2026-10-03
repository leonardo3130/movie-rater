import { useParams, Link } from 'react-router'
import { motion } from 'framer-motion'
import { ArrowLeft, Calendar, Clock, Star, ListVideo } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { useTvSeason } from '../hooks/use-tv'
import type { TvEpisodeSummaryDto } from '@src/types/tv'

export function TvSeasonPage() {
  const { tmdbId, seasonNumber } = useParams<{ tmdbId: string; seasonNumber: string }>()
  const tmdbIdNumber = tmdbId ? Number(tmdbId) : null
  const seasonNumberValue = seasonNumber ? Number(seasonNumber) : null
  const { data: season, isLoading, isError } = useTvSeason(tmdbIdNumber, seasonNumberValue)

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-48 w-full rounded-lg" />
        <Skeleton className="h-8 w-64" />
      </div>
    )
  }

  if (isError || !season) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Season not found</p>
      </div>
    )
  }

  return (
    <div className="min-h-dvh bg-background">
      <div className="mx-auto max-w-7xl px-4 py-6 space-y-6">
        <Link
          to={`/tv/${season.seriesTmdbId}`}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-4" />
          Back to series
        </Link>

        <div className="flex flex-col sm:flex-row gap-6">
          <div className="w-28 shrink-0 mx-auto sm:mx-0">
            <MoviePoster src={season.posterUrl} alt={season.name} />
          </div>
          <div className="flex-1 space-y-3">
            <h1 className="font-heading text-2xl font-bold">{season.name}</h1>
            <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
              {season.airDate && (
                <span className="flex items-center gap-1">
                  <Calendar className="size-3" />
                  {new Date(season.airDate).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'short',
                    day: 'numeric',
                  })}
                </span>
              )}
              <span className="flex items-center gap-1">
                <ListVideo className="size-3" />
                {season.episodes.length} episodes
              </span>
            </div>
            <div className="flex items-center gap-2">
              <Star className="size-4 fill-yellow-500 text-yellow-500" />
              <span className="font-semibold">{season.voteAverage.toFixed(1)}</span>
              <span className="text-xs text-muted-foreground">
                ({season.voteCount.toLocaleString()} votes)
              </span>
            </div>
            {season.overview && (
              <p className="text-sm leading-relaxed text-muted-foreground">{season.overview}</p>
            )}
          </div>
        </div>

        <div className="space-y-3">
          <h2 className="font-heading text-lg font-medium tracking-tight">Episodes</h2>
          {season.episodes.map((episode, index) => (
            <EpisodeRow
              key={episode.tmdbId}
              episode={episode}
              seriesTmdbId={season.seriesTmdbId}
              seasonNumber={season.seasonNumber}
              index={index}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

function EpisodeRow({
  episode,
  seriesTmdbId,
  seasonNumber,
  index,
}: {
  episode: TvEpisodeSummaryDto
  seriesTmdbId: number
  seasonNumber: number
  index: number
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2, delay: index * 0.03 }}
    >
      <Link
        to={`/tv/${seriesTmdbId}/season/${seasonNumber}/episode/${episode.episodeNumber}`}
        className="flex items-center gap-4 p-3 rounded-lg border border-border/50 bg-card hover:bg-accent/50 transition-colors group"
      >
        <div className="flex items-center justify-center w-10 h-10 shrink-0 rounded-lg bg-primary/10 text-sm font-semibold text-primary">
          {episode.episodeNumber}
        </div>
        <div className="min-w-0 flex-1 space-y-0.5">
          <p className="text-sm font-medium leading-tight line-clamp-1 group-hover:text-primary transition-colors">
            {episode.name}
          </p>
          <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
            {episode.airDate && (
              <span className="flex items-center gap-1">
                <Calendar className="size-3" />
                {new Date(episode.airDate).toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'short',
                  day: 'numeric',
                })}
              </span>
            )}
            {episode.runtime != null && (
              <span className="flex items-center gap-1">
                <Clock className="size-3" />
                {episode.runtime}m
              </span>
            )}
            <Badge variant="outline" className="text-[10px] px-1.5">
              <Star className="size-3 fill-yellow-500 text-yellow-500 mr-0.5" />
              {episode.voteAverage.toFixed(1)}
            </Badge>
          </div>
        </div>
      </Link>
    </motion.div>
  )
}