interface WatchSessionEpisodeLabelProps {
  seriesTitle?: string | null
  seasonNumber?: number | null
  episodeNumber?: number | null
  className?: string
}

export function WatchSessionEpisodeLabel({
  seriesTitle,
  seasonNumber,
  episodeNumber,
  className = 'text-xs',
}: WatchSessionEpisodeLabelProps) {
  if (!seriesTitle || seasonNumber == null || episodeNumber == null) {
    return null
  }
  return (
    <p className={`text-muted-foreground ${className}`}>
      {seriesTitle} &middot; S{seasonNumber}E{episodeNumber}
    </p>
  )
}