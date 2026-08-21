import { useEffect, useState } from 'react'
import { Navigate, Outlet } from 'react-router'
import { Loader2 } from 'lucide-react'
import { useAuthStore } from '../stores/auth-store'
import { getCurrentUser } from '../api/endpoints/auth'

export function ProtectedRoute() {
  const status = useAuthStore((s) => s.status)
  const accessToken = useAuthStore((s) => s.accessToken)
  const setUser = useAuthStore((s) => s.setUser)
  const clear = useAuthStore((s) => s.clear)
  const [booting, setBooting] = useState(status === 'idle')

  useEffect(() => {
    if (status !== 'idle' || !accessToken) {
      setBooting(false)
      return
    }

    getCurrentUser()
      .then((user) => {
        setUser(user)
      })
      .catch(() => {
        clear()
      })
      .finally(() => {
        setBooting(false)
      })
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  if (status === 'unauthenticated' && !booting) {
    return <Navigate to="/login" replace />
  }

  if (booting) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return <Outlet />
}
