import { create } from 'zustand'

export interface TvGenre {
  tmdbId: number
  name: string
}

interface TvState {
  searchQuery: string
  browseGenre: TvGenre | null
  page: number
  setSearchQuery: (query: string) => void
  setBrowseGenre: (genre: TvGenre | null) => void
  setPage: (page: number) => void
}

export const useTvStore = create<TvState>((set) => ({
  searchQuery: '',
  browseGenre: null,
  page: 1,
  setSearchQuery: (searchQuery) => set({ searchQuery, page: 1, browseGenre: null }),
  setBrowseGenre: (browseGenre) => set({ browseGenre, page: 1, searchQuery: '' }),
  setPage: (page) => set({ page }),
}))