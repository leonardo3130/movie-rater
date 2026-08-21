import client from '../client'
import type {
  AuthResponse,
  RegisterRequest,
  LoginRequest,
  UserResponse,
} from '@src/types/auth'

export function register(body: RegisterRequest) {
  return client.post<AuthResponse>('/api/auth/register', body).then((r) => r.data)
}

export function login(body: LoginRequest) {
  return client.post<AuthResponse>('/api/auth/login', body).then((r) => r.data)
}

export function refresh() {
  return client.post<AuthResponse>('/api/auth/refresh').then((r) => r.data)
}

export function logout() {
  return client.post('/api/auth/logout').then((r) => r.data)
}

export function getCurrentUser() {
  return client.get<UserResponse>('/api/auth/me').then((r) => r.data)
}
