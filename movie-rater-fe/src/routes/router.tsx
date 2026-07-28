import { createBrowserRouter, Navigate } from 'react-router'
import { LoginPage } from '../features/authentication/pages/LoginPage'
import { RegisterPage } from '../features/authentication/pages/RegisterPage'
import { ProtectedRoute } from './ProtectedRoute'
import { MoviesPage } from '../features/movies/pages/MoviesPage'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: '/dashboard',
        element: <div className="flex min-h-dvh items-center justify-center text-muted-foreground">Dashboard coming soon</div>,
      },
      {
        path: '/movies',
        element: <MoviesPage />,
      },
      {
        path: '/movies/:tmdbId',
        element: <MoviesPage />,
      },
    ],
  },
  {
    path: '/',
    element: <Navigate to="/dashboard" replace />,
  },
  {
    path: '*',
    element: <Navigate to="/login" replace />,
  },
])