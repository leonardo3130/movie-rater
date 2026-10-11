
import { useMutation } from "@tanstack/react-query";
import {
  changeGroupName,
} from "@src/api/endpoints/group.ts"
import type { CreateGroupRequest } from "@/src/types/groups";
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";
import { t } from "@src/stores/locale-store";

export function useCreateGroup() {
  const updateGroupName = useGroupsStore(s => s.changeName);

  return useMutation({
    mutationFn: ({ gid, request }: { gid: string, request: CreateGroupRequest }) => changeGroupName(gid, request),
    onSuccess: (data, variables) => {
      updateGroupName(variables.gid, data);
      toast.success(t('groups.toast.groupRenamed', { name: data.name }));
    },
    onError: () => {
      toast.error(t('groups.toast.renameFailed'))
    }
  });
}
