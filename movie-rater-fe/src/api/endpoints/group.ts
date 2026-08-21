import client from '../client';
import type {
  CreateGroupRequest,
  GroupDto,
  InvitationRequestDto,
  InvitationResponseDto,
  AcceptInvitationRequestDto,
  AcceptInvitationResponseDto,
} from '@src/types/groups';

export function createGroup(body: CreateGroupRequest) {
  return client.post<GroupDto>('/api/groups', body).then((r) => r.data);
}

export function getGroups() {
  return client.get<GroupDto[]>('/api/groups').then((r) => r.data);
}

export function getGroup(id: string) {
  return client.get<GroupDto>(`/api/groups/${id}`).then((r) => r.data);
}

export function deleteGroup(id: string) {
  return client.delete(`/api/groups/${id}`).then((r) => r.data);
}

export function changeGroupName(id: string, body: CreateGroupRequest) {
  return client
    .patch<GroupDto>(`/api/groups/${id}/change-name`, body)
    .then((r) => r.data);
}

export function inviteInGroup(body: InvitationRequestDto) {
  return client
    .post<InvitationResponseDto>('/api/groups/invite', body)
    .then((r) => r.data);
}

export function acceptInvitation(body: AcceptInvitationRequestDto) {
  return client
    .post<AcceptInvitationResponseDto>('/api/groups/invite/accept', body)
    .then((r) => r.data);
}
