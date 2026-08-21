import { create } from 'zustand';
import type {
  GroupDto,
} from '@src/types/groups';

interface GroupsState {
  groups: GroupDto[];
  currentGroup: string, // guid
  error: string;
  loading: boolean;

  createGroup: (group: GroupDto) => void;
  getGroup: (gid: string) => GroupDto | undefined;
  getGroups: () => GroupDto[];
  deleteGroup: (gid: string) => void;
  changeName: (gid: string, data: GroupDto) => void;

  setGroups: (groups: GroupDto[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string) => void;

  setCurrentGroup: (gid: string) => void

  clearError: () => void;
}

export const useGroupsStore = create<GroupsState>((set, get) => ({
  groups: [],
  error: '',
  currentGroup: '',
  loading: false,

  createGroup: (group) => {
    set((state) => ({
      groups: [...state.groups, group],
    }));
  },

  getGroup: (gid) => {
    return get().groups.find((group) => group.id === gid);
  },

  getGroups: () => {
    return get().groups;
  },

  deleteGroup: (gid) => {
    set((state) => ({
      groups: state.groups.filter((group) => group.id !== gid),
    }));
  },

  changeName: (gid, data) => {
    set((state) => ({
      groups: state.groups.map((group) =>
        group.id === gid
          ? { ...group, name: data.name }
          : group,
      ),
    }));
  },

  setGroups: (groups) => {
    set({ groups });
  },

  setLoading: (loading) => {
    set({ loading });
  },

  setError: (error) => {
    set({ error });
  },

  clearError: () => {
    set({ error: '' });
  },

  setCurrentGroup: (gid) => set({
    currentGroup: gid
  })
}));
