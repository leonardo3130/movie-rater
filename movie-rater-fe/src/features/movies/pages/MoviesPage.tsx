import { useParams } from 'react-router'
import { MovieSearchBar } from '../components/MovieSearchBar'
import { MovieRow } from '../components/MovieRow'
import { MovieGrid } from '../components/MovieGrid'
import { MovieDetailsDialog } from '../components/MovieDetailsDialog'
import { useMoviesStore } from '@src/stores/movies-store'
import { useTopRated } from '../hooks/use-top-rated'
import { usePopular } from '../hooks/use-popular'
import { useNowPlaying } from '../hooks/use-now-playing'
import { useSearchMovies } from '../hooks/use-search-movies'
import { useDiscoverMovies } from '../hooks/use-discover-movies'

export function MoviesPage() {
  const { tmdbId } = useParams<{ tmdbId: string }>()
  const mode = useMoviesStore((s) => s.mode)
  const searchQuery = useMoviesStore((s) => s.searchQuery)
  const pages = useMoviesStore((s) => s.pages)
  const setPage = useMoviesStore((s) => s.setPage)
  const discoverFilters = useMoviesStore((s) => s.discoverFilters)

  const { data: topRated, isLoading: topRatedLoading } = useTopRated(pages.topRated)
  const { data: popular, isLoading: popularLoading } = usePopular(pages.popular)
  const { data: nowPlaying, isLoading: nowPlayingLoading } = useNowPlaying(pages.nowPlaying)
  const { data: searchResults, isLoading: searchLoading } = useSearchMovies(searchQuery, pages.search)
  const { data: discoverResults, isLoading: discoverLoading } = useDiscoverMovies(discoverFilters, pages.discover)

  return (
    <div className="min-h-dvh bg-background">
      <div className="mx-auto max-w-7xl px-4 py-6 space-y-8">
        <MovieSearchBar />

        {mode === 'home' && (
          <>
            <MovieRow
              title="Top Rated"
              movies={topRated?.results}
              isLoading={topRatedLoading}
              page={pages.topRated}
              totalPages={topRated?.totalPages}
              onPageChange={(p) => setPage('topRated', p)}
            />
            <MovieRow
              title="Popular"
              movies={popular?.results}
              isLoading={popularLoading}
              page={pages.popular}
              totalPages={popular?.totalPages}
              onPageChange={(p) => setPage('popular', p)}
            />
            <MovieRow
              title="Now Playing"
              movies={nowPlaying?.results}
              isLoading={nowPlayingLoading}
              page={pages.nowPlaying}
              totalPages={nowPlaying?.totalPages}
              onPageChange={(p) => setPage('nowPlaying', p)}
            />
          </>
        )}

        {mode === 'search' && (
          <MovieGrid
            movies={searchResults?.results}
            isLoading={searchLoading}
            totalPages={searchResults?.totalPages}
            category="search"
          />
        )}

        {mode === 'discover' && (
          <MovieGrid
            movies={discoverResults?.results}
            isLoading={discoverLoading}
            totalPages={discoverResults?.totalPages}
            category="discover"
          />
        )}
      </div>

      {tmdbId && <MovieDetailsDialog />}
    </div>
  )
}