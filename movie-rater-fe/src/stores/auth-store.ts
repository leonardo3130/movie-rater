import { create } from 'zustand'
import { setAccessToken } from '../api/token'
import type { UserResponse } from '@src/types/auth'

const ACCESS_TOKEN_KEY = 'mr_access_token'

interface AuthState {
  user: UserResponse | null
  accessToken: string | null
  status: 'idle' | 'authenticated' | 'unauthenticated'
  setAuth: (user: UserResponse, accessToken: string) => void
  setUser: (user: UserResponse | null) => void
  setAccessToken: (token: string) => void
  clear: () => void
}

function hydrateToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY)
}

const storedToken = hydrateToken()
if (storedToken) {
  setAccessToken(storedToken)
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: storedToken,
  status: storedToken ? 'idle' : 'unauthenticated',

  setAuth: (user, accessToken) => {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
    setAccessToken(accessToken)
    set({ user, accessToken, status: 'authenticated' })
  },

  setUser: (user) => {
    set({ user, status: user ? 'authenticated' : 'unauthenticated' })
  },

  setAccessToken: (token) => {
    localStorage.setItem(ACCESS_TOKEN_KEY, token)
    setAccessToken(token)
    set({ accessToken: token })
  },

  clear: () => {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    setAccessToken(null)
    set({ user: null, accessToken: null, status: 'unauthenticated' })
  },
}))