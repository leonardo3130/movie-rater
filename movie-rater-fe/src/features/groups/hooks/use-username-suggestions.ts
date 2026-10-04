import { useQuery } from '@tanstack/react-query'
import { useDebouncedValue } from '@src/hooks/use-debounced-value'
import { searchUsernames } from '@/src/api/endpoints/users'

export function useUsernameSuggestions(prefix: string) {
  const debouncedPrefix = useDebouncedValue(prefix, 350)
  const trimmed = debouncedPrefix.trim()

  return useQuery({
    queryKey: ['users', 'suggestions', trimmed],
    queryFn: () => searchUsernames({ prefix: trimmed }),
    enabled: trimmed.length > 0,
    staleTime: 60_000,
  })
}