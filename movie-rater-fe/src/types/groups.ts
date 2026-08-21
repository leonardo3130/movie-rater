import type { UserResponse } from "./auth";
import type { WatchSessionResponseDto } from "./watch-session";

export interface CreateGroupRequest {
  groupName: string;
}

export interface GroupDto {
  id: string; // Guid
  name: string;
  createdAt: string; // DateTime
  users: UserResponse[];
  watchSessions: WatchSessionResponseDto[];
}

export interface InvitationResponseDto {
  invitationId: string; // Guid
  inviteToken: string;
  expiresAt: string; // DateTime
}

export interface AcceptInvitationResponseDto {
  groupId: string; // Guid
}

export interface InvitationRequestDto {
  groupId: string; // Guid
  inviteeEmail: string;
}

export interface AcceptInvitationRequestDto {
  inviteToken: string;
}
