import client from '../client'
import type {
  AuthResponse,
  RegisterRequest,
  LoginRequest,
  CurrentUserResponse,
  InvitePartnerRequest,
  InviteResponse,
  AcceptInvitationRequest,
  AcceptInvitationResponse,
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
  return client.get<CurrentUserResponse>('/api/auth/me').then((r) => r.data)
}

export function invitePartner(body: InvitePartnerRequest) {
  return client.post<InviteResponse>('/api/auth/invite', body).then((r) => r.data)
}

export function acceptInvitation(body: AcceptInvitationRequest) {
  return client.post<AcceptInvitationResponse>('/api/auth/invite/accept', body).then((r) => r.data)
}
