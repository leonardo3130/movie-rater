import { lazy, Suspense } from 'react'

// Code-split so regular users never download the admin bundle.
const AdminPage = lazy(() =>
  import('../features/admin/pages/AdminPage').then((m) => ({ default: m.AdminPage })),
)

export function LazyAdminPage() {
  return (
    <Suspense fallback={null}>
      <AdminPage />
    </Suspense>
  )
}
