import { useMutation } from "@tanstack/react-query";
import {
  createGroup,
} from "@src/api/endpoints/group.ts"
import type { CreateGroupRequest } from "@/src/types/groups";
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";
import { t } from "@src/stores/locale-store";
import { getApiErrorMessage } from '@src/api/errors'

export function useCreateGroup() {
  const addGroupToStore = useGroupsStore(s => s.createGroup);

  return useMutation({
    mutationFn: (request: CreateGroupRequest) => createGroup(request),
    onSuccess: (data) => {
      addGroupToStore(data);
      toast.success(t('groups.toast.groupCreated', { name: data.name }));
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('groups.toast.createFailed')))
    }
  });
}
