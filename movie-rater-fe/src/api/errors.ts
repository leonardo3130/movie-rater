import type { ApiError } from '@src/types/auth'

/**
 * Extracts the most specific user-facing message from an API error response.
 *
 * Priority: FluentValidation messages (`errors`) > domain error `detail` > `fallback`.
 * `title` is deliberately ignored: it is a generic label ("Bad Request",
 * "One or more validation errors occurred.") rather than a useful message.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  const data = (error as { response?: { data?: ApiError } } | null)?.response?.data

  const validationMessages = Object.values(data?.errors ?? {}).flat().filter(Boolean)
  if (validationMessages.length > 0) {
    return [...new Set(validationMessages)].join('. ')
  }

  return data?.detail || fallback
}
