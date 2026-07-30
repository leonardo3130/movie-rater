import { createBrowserRouter, Navigate } from 'react-router'
import { LoginPage } from '../features/authentication/pages/LoginPage'
import { RegisterPage } from '../features/authentication/pages/RegisterPage'
import { ProtectedRoute } from './ProtectedRoute'
import { MoviesPage } from '../features/movies/pages/MoviesPage'
import { AppLayout } from '../features/layout/components/AppLayout'
import { FavoritesPage } from '../features/user-movie/pages/FavoritesPage'
import { WatchlistPage } from '../features/user-movie/pages/WatchlistPage'
import { InvitePage } from '../features/authentication/pages/InvitePage'
import { AcceptInvitePage } from '../features/authentication/pages/AcceptInvitePage'
import { WatchHistoryPage } from '../features/movies/pages/WatchHistoryPage'
import { WatchSessionDetailPage } from '../features/movies/pages/WatchSessionDetailPage'
import { DashboardPage } from '../features/dashboard/pages/DashboardPage'

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
        element: <AppLayout />,
        children: [
          {
            path: '/dashboard',
            element: <DashboardPage />,
          },
          {
            path: '/movies',
            element: <MoviesPage />,
          },
          {
            path: '/movies/:tmdbId',
            element: <MoviesPage />,
          },
          {
            path: '/favorites',
            element: <FavoritesPage />,
          },
          {
            path: '/watchlist',
            element: <WatchlistPage />,
          },
          {
            path: '/invite',
            element: <InvitePage />,
          },
          {
            path: '/invite/accept',
            element: <AcceptInvitePage />,
          },
          {
            path: '/watch-history',
            element: <WatchHistoryPage />,
          },
          {
            path: '/watch-history/:id',
            element: <WatchSessionDetailPage />,
          },
        ],
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