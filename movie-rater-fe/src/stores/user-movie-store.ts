import { create } from 'zustand'

interface UserMovieState {
  favoriteIds: Set<string>
  watchlistIds: Set<string>

  toggleFavorite: (movieId: string, value: boolean) => void
  toggleWatchlist: (movieId: string, value: boolean) => void
  setFromResponse: (movieId: string, isFavorite: boolean, isInWatchlist: boolean) => void
  setBatch: (entries: Array<{ id: string; isFavorite: boolean; isInWatchlist: boolean }>) => void
  clear: () => void
}

export const useUserMovieStore = create<UserMovieState>((set) => ({
  favoriteIds: new Set<string>(),
  watchlistIds: new Set<string>(),

  toggleFavorite: (movieId, value) =>
    set((state) => {
      const next = new Set(state.favoriteIds)
      if (value) next.add(movieId)
      else next.delete(movieId)
      return { favoriteIds: next }
    }),

  toggleWatchlist: (movieId, value) =>
    set((state) => {
      const next = new Set(state.watchlistIds)
      if (value) next.add(movieId)
      else next.delete(movieId)
      return { watchlistIds: next }
    }),

  setFromResponse: (movieId, isFavorite, isInWatchlist) =>
    set((state) => {
      const favNext = new Set(state.favoriteIds)
      const wlNext = new Set(state.watchlistIds)
      if (isFavorite) favNext.add(movieId)
      else favNext.delete(movieId)
      if (isInWatchlist) wlNext.add(movieId)
      else wlNext.delete(movieId)
      return { favoriteIds: favNext, watchlistIds: wlNext }
    }),

  setBatch: (entries) =>
    set(() => {
      const favoriteIds = new Set<string>()
      const watchlistIds = new Set<string>()
      for (const e of entries) {
        if (e.isFavorite) favoriteIds.add(e.id)
        if (e.isInWatchlist) watchlistIds.add(e.id)
      }
      return { favoriteIds, watchlistIds }
    }),

  clear: () => set({ favoriteIds: new Set(), watchlistIds: new Set() }),
}))
