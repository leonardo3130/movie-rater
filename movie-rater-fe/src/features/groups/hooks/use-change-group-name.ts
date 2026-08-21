
import { useMutation } from "@tanstack/react-query";
import {
  changeGroupName,
} from "@src/api/endpoints/group.ts"
import type { CreateGroupRequest } from "@/src/types/groups";
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";

export function useCreateGroup() {
  const updateGroupName = useGroupsStore(s => s.changeName);

  return useMutation({
    mutationFn: ({ gid, request }: { gid: string, request: CreateGroupRequest }) => changeGroupName(gid, request),
    onSuccess: (data, variables) => {
      updateGroupName(variables.gid, data);
      toast.success(`Group name changed to ${data.name} successfully`);
    },
    onError: () => {
      toast.error("Error while changing group name")
    }
  });
}
