export type InvitationStatus = 'Pending' | 'Accepted' | 'Expired' | 'Revoked'

export type InvitationIssue =
  | 'ExpiredButPending'
  | 'GroupMissing'
  | 'InviteeAlreadyMember'
  | 'DuplicatePending'

export interface AdminAccessResponseDto {
  isAdmin: boolean
}

export interface AdminOverviewResponseDto {
  users: {
    total: number
    newLast7Days: number
    newLast30Days: number
    withoutGroup: number
    activeLast24Hours: number
    activeLast7Days: number
    activeLast30Days: number
    activeSessions: number
  }
  groups: {
    total: number
    averageMembers: number
    withoutWatchSessions: number
  }
  media: {
    movies: number
    tvSeries: number
    tvSeasons: number
    tvEpisodes: number
  }
  watchSessions: {
    total: number
    last30Days: number
    fullyRatedPercent: number
  }
  ratings: {
    total: number
    average: number
    distribution: { value: number; count: number }[]
  }
  invitations: {
    pending: number
    accepted: number
    expired: number
    revoked: number
    expiredButStillPending: number
    acceptanceRatePercent: number
  }
  generatedAt: string
}

export interface AdminActivityDayDto {
  date: string
  signups: number
  activeUsers: number
  watchSessions: number
  ratings: number
}

export interface AdminActivityResponseDto {
  days: number
  series: AdminActivityDayDto[]
}

export interface AdminGroupDto {
  id: string
  name: string
  createdAt: string
  members: string[]
  watchSessionCount: number
  ratingCount: number
  lastWatchSessionAt: string | null
  pendingInvitations: number
}

export interface AdminInvitationDto {
  id: string
  groupId: string
  groupName: string | null
  inviterUserId: string
  inviterUsername: string
  inviteeEmail: string
  inviteeUsername: string | null
  status: InvitationStatus
  effectiveStatus: InvitationStatus
  createdAt: string
  expiresAt: string
  acceptedByUsername: string | null
  issues: InvitationIssue[]
}

export interface AdminInvitationListResponseDto {
  totalCount: number
  items: AdminInvitationDto[]
}

export interface AdminInvitationQuery {
  status?: InvitationStatus
  search?: string
  limit?: number
}
