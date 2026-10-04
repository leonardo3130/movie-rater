import client from '../client';
import type { UserSuggestionDto } from '@src/types/groups';

export function searchUsernames(params: { prefix: string; limit?: number }) {
  const qp = new URLSearchParams()
  qp.set('prefix', params.prefix)
  if (params.limit) qp.set('limit', String(params.limit))
  return client
    .get<UserSuggestionDto[]>(`/api/users/suggest?${qp.toString()}`)
    .then((r) => r.data);
}