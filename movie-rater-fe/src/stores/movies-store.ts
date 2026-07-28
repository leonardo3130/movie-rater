import { create } from 'zustand'
import type { DiscoverFilters, SortByOption } from '@src/types/movie'

export type MovieMode = 'home' | 'search' | 'discover'

interface MoviesState {
  mode: MovieMode
  searchQuery: string
  pages: {
    topRated: number
    popular: number
    nowPlaying: number
    search: number
    discover: number
  }
  discoverFilters: DiscoverFilters
  setMode: (mode: MovieMode) => void
  setSearchQuery: (query: string) => void
  setPage: (category: keyof MoviesState['pages'], page: number) => void
  setDiscoverFilters: (filters: Partial<DiscoverFilters>) => void
  resetDiscoverFilters: () => void
}

const defaultDiscoverFilters: DiscoverFilters = {
  genreIds: [],
  primaryReleaseYear: '',
  primaryReleaseDateGte: '',
  primaryReleaseDateLte: '',
  voteAverageGte: 0,
  sortBy: 'popularity.desc' as SortByOption,
  includeAdult: false,
}

export const useMoviesStore = create<MoviesState>((set) => ({
  mode: 'home',
  searchQuery: '',
  pages: {
    topRated: 1,
    popular: 1,
    nowPlaying: 1,
    search: 1,
    discover: 1,
  },
  discoverFilters: { ...defaultDiscoverFilters },

  setMode: (mode) => set({ mode }),

  setSearchQuery: (query) => set({ searchQuery: query }),

  setPage: (category, page) =>
    set((state) => ({
      pages: { ...state.pages, [category]: page },
    })),

  setDiscoverFilters: (filters) =>
    set((state) => ({
      discoverFilters: { ...state.discoverFilters, ...filters },
    })),

  resetDiscoverFilters: () => set({ discoverFilters: { ...defaultDiscoverFilters } }),
}))