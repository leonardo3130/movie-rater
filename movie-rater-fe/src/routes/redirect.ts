const DEFAULT_REDIRECT = '/dashboard'
const AUTH_PATHS = ['/login', '/register', '/reset-password']

/**
 * Builds the login URL that sends the user back to `path` after authenticating.
 * When `path` is itself an auth page, its existing `redirect` is carried over
 * instead of redirecting back to the auth page.
 */
export function buildLoginUrl(path: string): string {
  const url = new URL(path, window.location.origin)
  const target = AUTH_PATHS.includes(url.pathname) ? url.searchParams.get('redirect') : path
  return target ? `/login?redirect=${encodeURIComponent(target)}` : '/login'
}

/**
 * Returns the `redirect` query param if it is a same-origin relative path,
 * otherwise the default post-login route. Guards against open redirects.
 */
export function getSafeRedirect(searchParams: URLSearchParams): string {
  const redirect = searchParams.get('redirect')
  if (!redirect || !redirect.startsWith('/') || redirect.startsWith('//') || redirect.startsWith('/\\')) {
    return DEFAULT_REDIRECT
  }
  return redirect
}
