import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { getAccessToken, setAccessToken } from './token'
import type { AuthResponse } from '@src/types/auth'

const REFRESH_PATH = '/api/auth/refresh'
const ACCESS_TOKEN_KEY = 'mr_access_token'
const LOGIN_PATH = '/login'

let refreshPromise: Promise<string | null> | null = null

interface QueueItem {
  resolve: (token: string | null) => void
  reject: (error: unknown) => void
}

let failedQueue: QueueItem[] = []

function processQueue(token: string | null, error: unknown) {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error)
    } else {
      resolve(token)
    }
  })
  failedQueue = []
}

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
})

client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken()

  if (token && config.url !== REFRESH_PATH) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

client.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorBody>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (!originalRequest || error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error)
    }

    if (originalRequest.url === REFRESH_PATH) {
      clearAuthAndRedirect()
      return Promise.reject(error)
    }

    if (refreshPromise) {
      try {
        const token = await refreshPromise
        if (token) {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return client(originalRequest)
        }
      } catch {
        return Promise.reject(error)
      }
      return Promise.reject(error)
    }

    originalRequest._retry = true
    refreshPromise = doRefresh()

    try {
      const token = await refreshPromise
      processQueue(token, null)
      if (token) {
        originalRequest.headers.Authorization = `Bearer ${token}`
        return client(originalRequest)
      }
    } catch (refreshError) {
      processQueue(null, refreshError)
      clearAuthAndRedirect()
      return Promise.reject(refreshError)
    } finally {
      refreshPromise = null
    }

    return Promise.reject(error)
  },
)

async function doRefresh(): Promise<string | null> {
  try {
    const response = await axios.post<AuthResponse>(REFRESH_PATH, {}, { withCredentials: true })
    const { accessToken: newToken } = response.data
    localStorage.setItem(ACCESS_TOKEN_KEY, newToken)
    setAccessToken(newToken)
    return newToken
  } catch {
    return null
  }
}

interface ApiErrorBody {
  type?: string
  title?: string
  status?: number
  errors?: Record<string, string[]>
}

function clearAuthAndRedirect() {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  setAccessToken(null)
  window.location.href = LOGIN_PATH
}

export default client