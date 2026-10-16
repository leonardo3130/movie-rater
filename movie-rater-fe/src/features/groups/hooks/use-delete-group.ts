import { useMutation } from "@tanstack/react-query";
import {
  deleteGroup,
} from "@src/api/endpoints/group.ts"
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";
import { t } from "@src/stores/locale-store";
import { getApiErrorMessage } from '@src/api/errors'

export function useCreateGroup() {
  const removeGroup = useGroupsStore(s => s.deleteGroup);

  return useMutation({
    mutationFn: (gid: string) => deleteGroup(gid),
    onSuccess: (_data, gid) => {
      removeGroup(gid);
      toast.success(t('groups.toast.groupDeleted'));
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('groups.toast.deleteFailed')))
    }
  });
}
