import { useMutation } from "@tanstack/react-query";
import {
  deleteGroup,
} from "@src/api/endpoints/group.ts"
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";

export function useCreateGroup() {
  const removeGroup = useGroupsStore(s => s.deleteGroup);

  return useMutation({
    mutationFn: (gid: string) => deleteGroup(gid),
    onSuccess: (_data, gid) => {
      removeGroup(gid);
      toast.success(`Group deleted successfully`);
    },
    onError: () => {
      toast.error("Error while changing group name")
    }
  });
}
