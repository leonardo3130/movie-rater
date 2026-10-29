import { Navigate, Outlet } from 'react-router'
import { Loader2 } from 'lucide-react'
import { useAdminAccess } from '../features/admin/hooks/use-admin'

// Hides the admin area from everyone not configured as admin. The API enforces the same
// rule on every admin endpoint; this guard only keeps the UI from rendering.
export function AdminRoute() {
  const access = useAdminAccess()

  if (access.isPending) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!access.data?.isAdmin) return <Navigate to="/dashboard" replace />

  return <Outlet />
}
