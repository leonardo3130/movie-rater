import { useParams, Link } from 'react-router'
import { motion } from 'framer-motion'
import { ArrowLeft, Calendar, Clock, Star } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { useTvEpisode } from '../hooks/use-tv'

export function TvEpisodePage() {
  const { tmdbId, seasonNumber, episodeNumber } = useParams<{
    tmdbId: string
    seasonNumber: string
    episodeNumber: string
  }>()
  const tmdbIdNumber = tmdbId ? Number(tmdbId) : null
  const seasonNumberValue = seasonNumber ? Number(seasonNumber) : null
  const episodeNumberValue = episodeNumber ? Number(episodeNumber) : null
  const { data: episode, isLoading, isError } = useTvEpisode(
    tmdbIdNumber,
    seasonNumberValue,
    episodeNumberValue,
  )

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-56 w-full rounded-lg" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-3/4" />
      </div>
    )
  }

  if (isError || !episode) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Episode not found</p>
      </div>
    )
  }

  return (
    <div className="min-h-dvh bg-background">
      <div className="mx-auto max-w-4xl px-4 py-6 space-y-6">
        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <Link
            to={`/tv/${episode.seriesTmdbId}`}
            className="flex items-center gap-1 hover:text-foreground transition-colors"
          >
            <ArrowLeft className="size-4" />
            {episode.seriesTitle ?? 'Series'}
          </Link>
          <span>/</span>
          <Link
            to={`/tv/${episode.seriesTmdbId}/season/${episode.seasonNumber}`}
            className="hover:text-foreground transition-colors"
          >
            Season {episode.seasonNumber}
          </Link>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="overflow-hidden rounded-xl border border-border/50"
        >
          {episode.stillUrl ? (
            <div className="relative aspect-video">
              <img
                src={episode.stillUrl}
                alt={episode.name}
                className="size-full object-cover"
              />
            </div>
          ) : (
            <div className="aspect-video bg-muted" />
          )}

          <div className="p-6 space-y-4">
            <div>
              <Badge variant="secondary" className="mb-2">
                S{episode.seasonNumber}E{episode.episodeNumber}
              </Badge>
              <h1 className="font-heading text-2xl font-bold break-words">{episode.name}</h1>
              <div className="flex flex-wrap items-center gap-3 mt-2 text-xs text-muted-foreground">
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
                <span className="flex items-center gap-1">
                  <Star className="size-3 fill-yellow-500 text-yellow-500" />
                  {episode.voteAverage.toFixed(1)}
                </span>
              </div>
            </div>

            {episode.overview && (
              <>
                <Separator />
                <div>
                  <h2 className="font-heading text-sm font-medium text-muted-foreground mb-1">
                    Overview
                  </h2>
                  <p className="text-sm leading-relaxed break-words">{episode.overview}</p>
                </div>
              </>
            )}
          </div>
        </motion.div>
      </div>
    </div>
  )
}