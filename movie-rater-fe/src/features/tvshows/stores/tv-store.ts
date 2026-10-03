import { create } from 'zustand'

interface TvState {
  searchQuery: string
  page: number
  setSearchQuery: (query: string) => void
  setPage: (page: number) => void
}

export const useTvStore = create<TvState>((set) => ({
  searchQuery: '',
  page: 1,
  setSearchQuery: (searchQuery) => set({ searchQuery, page: 1 }),
  setPage: (page) => set({ page }),
}))