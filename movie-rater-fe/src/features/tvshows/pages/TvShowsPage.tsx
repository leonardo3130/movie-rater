import { Tv } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { TvSearchBar } from '../components/TvSearchBar'
import { TvShowGrid } from '../components/TvShowGrid'
import { useTvStore } from '../stores/tv-store'
import { useSearchTv, useTvGenres } from '../hooks/use-tv'

export function TvShowsPage() {
  const searchQuery = useTvStore((s) => s.searchQuery)
  const setSearchQuery = useTvStore((s) => s.setSearchQuery)
  const page = useTvStore((s) => s.page)
  const { data: results, isLoading: searchLoading } = useSearchTv(searchQuery, page)
  const { data: genres, isLoading: genresLoading } = useTvGenres()

  const searching = searchQuery.trim().length > 0

  return (
    <div className="min-h-dvh bg-background">
      <div className="mx-auto max-w-7xl px-4 py-6 space-y-8">
        <div className="flex items-center gap-3">
          <TvSearchBar />
        </div>

        {searching ? (
          <TvShowGrid
            shows={results?.results}
            isLoading={searchLoading}
            totalPages={results?.totalPages}
          />
        ) : (
          <div className="space-y-8">
            <div className="space-y-3">
              <h2 className="font-heading text-lg font-medium tracking-tight">
                Browse by genre
              </h2>
              {genresLoading ? (
                <div className="flex flex-wrap gap-2">
                  {Array.from({ length: 12 }).map((_, i) => (
                    <Skeleton key={i} className="h-7 w-24 rounded-full" />
                  ))}
                </div>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {genres?.genres.map((genre) => (
                    <button
                      key={genre.tmdbId}
                      type="button"
                      onClick={() => setSearchQuery(genre.name)}
                      className="cursor-pointer rounded-full border border-border/50 bg-card px-3 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
                    >
                      {genre.name}
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
              <Tv className="size-14 text-muted-foreground/30" />
              <h2 className="text-xl font-semibold">Explore TV shows</h2>
              <p className="max-w-sm text-sm text-muted-foreground">
                Search for a series, then dive into seasons and episodes to track what you watch
                together.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}