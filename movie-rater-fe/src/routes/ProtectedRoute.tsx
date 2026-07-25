import { Navigate, Outlet } from 'react-router'
import { useAuthStore } from '../stores/auth-store'

export function ProtectedRoute() {
  const status = useAuthStore((s) => s.status)

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}