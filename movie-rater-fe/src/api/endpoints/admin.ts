import client from '../client'
import type {
  AdminAccessResponseDto,
  AdminActivityResponseDto,
  AdminGroupDto,
  AdminInvitationListResponseDto,
  AdminInvitationQuery,
  AdminOverviewResponseDto,
} from '@src/types/admin'

export function getAdminAccess() {
  return client.get<AdminAccessResponseDto>('/api/admin/access').then((r) => r.data)
}

export function getAdminOverview(refresh = false) {
  return client
    .get<AdminOverviewResponseDto>('/api/admin/overview', { params: { refresh } })
    .then((r) => r.data)
}

export function getAdminActivity(days: number, refresh = false) {
  return client
    .get<AdminActivityResponseDto>('/api/admin/activity', { params: { days, refresh } })
    .then((r) => r.data)
}

export function getAdminGroups() {
  return client.get<AdminGroupDto[]>('/api/admin/groups').then((r) => r.data)
}

export function getAdminInvitations(query: AdminInvitationQuery) {
  return client
    .get<AdminInvitationListResponseDto>('/api/admin/invitations', { params: query })
    .then((r) => r.data)
}
