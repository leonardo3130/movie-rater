import { useMutation } from "@tanstack/react-query";
import {
  createGroup,
} from "@src/api/endpoints/group.ts"
import type { CreateGroupRequest } from "@/src/types/groups";
import { toast } from "sonner";
import { useGroupsStore } from "@/src/stores/groups-store";

export function useCreateGroup() {
  const addGroupToStore = useGroupsStore(s => s.createGroup);

  return useMutation({
    mutationFn: (request: CreateGroupRequest) => createGroup(request),
    onSuccess: (data) => {
      addGroupToStore(data);
      toast.success(`Group ${data.name} created successfully`);
    },
    onError: (error) => {
      toast.error(error.message)
    }
  });
}
