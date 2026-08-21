
import { useQuery } from '@tanstack/react-query'
import { useGroupsStore } from '@/src/stores/groups-store'
import { useEffect } from 'react'
import { getGroups } from '@/src/api/endpoints/group'

export function useGroups() {
  const setGroups = useGroupsStore((s) => s.setGroups)

  const query = useQuery({
    queryKey: ['user-groups'],
    queryFn: () =>
      getGroups().then(res => res)
  })

  useEffect(() => {
    if (query.data) {
      setGroups(
        query.data
      )
    }
  }, [query.data, setGroups])

  return query
}
