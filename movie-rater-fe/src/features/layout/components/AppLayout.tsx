import { motion } from 'framer-motion'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  SidebarInset,
  useSidebar,
} from '@/components/ui/sidebar'
import { Outlet, Link, useLocation, useNavigate } from 'react-router'
import {
  LayoutDashboard,
  Film,
  Heart,
  Bookmark,
  UserPlus,
  LogOut,
  Clapperboard,
} from 'lucide-react'
import { useAuthStore } from '../../../stores/auth-store'
import { useMutation } from '@tanstack/react-query'
import { logout as logoutApi } from '../../../api/endpoints/auth'
import { cn } from '@/lib/utils'

const NAV_ITEMS = [
  { icon: LayoutDashboard, label: 'Dashboard', path: '/dashboard' },
  { icon: Film, label: 'Movies', path: '/movies' },
  { icon: Heart, label: 'Favorites', path: '/favorites' },
  { icon: Bookmark, label: 'Watchlist', path: '/watchlist' },
  { icon: UserPlus, label: 'Invite Partner', path: '/invite' },
]

export function AppLayout() {
  return (
    <SidebarProvider defaultOpen={true}>
      <AppSidebar />
      <SidebarInset>
        <TopBar />
        <main className="flex-1 overflow-auto">
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}

function TopBar() {
  const { isMobile } = useSidebar()
  return (
    <header className="sticky top-0 z-30 flex h-14 items-center gap-3 border-b border-sidebar-border bg-background/80 px-4 backdrop-blur-xl">
      <SidebarTrigger className="text-muted-foreground hover:text-foreground" />
      {isMobile && (
        <Link to="/dashboard" className="flex items-center gap-2">
          <Clapperboard className="size-5 text-primary" />
          <span className="font-heading text-sm font-bold tracking-tight">
            Movie Rater
          </span>
        </Link>
      )}
    </header>
  )
}

function AppSidebar() {
  const location = useLocation()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const clear = useAuthStore((s) => s.clear)

  const logoutMutation = useMutation({
    mutationFn: logoutApi,
    onSettled: () => {
      clear()
      navigate('/login', { replace: true })
    },
  })

  const isActive = (path: string) =>
    location.pathname === path ||
    (path === '/movies' && location.pathname.startsWith('/movies/'))

  return (
    <Sidebar collapsible="icon" variant="sidebar" className="border-r-0">
      {/* Brand header */}
      <SidebarHeader className="px-3 py-4">
        <Link
          to="/dashboard"
          className="group/brand flex items-center gap-2.5 rounded-lg px-2 py-1 transition-colors hover:bg-sidebar-accent"
        >
          <div className="relative flex size-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-primary to-primary/60 shadow-lg shadow-primary/20">
            <Clapperboard className="size-5 text-primary-foreground" />
          </div>
          <div className="flex flex-col group-data-[collapsible=icon]:hidden">
            <span className="font-heading text-base font-bold leading-tight tracking-tight">
              Movie Rater
            </span>
            <span className="text-[0.65rem] font-medium uppercase tracking-wider text-muted-foreground">
              for couples
            </span>
          </div>
        </Link>
      </SidebarHeader>

      <SidebarContent className="px-2">
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="gap-1">
              {NAV_ITEMS.map((item, index) => {
                const active = isActive(item.path)
                return (
                  <SidebarMenuItem key={item.path}>
                    <SidebarMenuButton
                      render={<Link to={item.path} />}
                      isActive={active}
                      tooltip={item.label}
                      size="lg"
                      className={cn(
                        'relative h-10 rounded-lg transition-all duration-200',
                        active
                          ? 'bg-primary/10 text-primary'
                          : 'text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent',
                      )}
                    >
                        <item.icon
                          className={cn(
                            'size-[1.15rem] transition-transform duration-200',
                            active ? 'scale-110' : 'group-hover/menu-button:scale-105',
                          )}
                        />
                        <span className="text-sm font-medium">{item.label}</span>
                    </SidebarMenuButton>
                    {active && (
                      <motion.div
                        layoutId="sidebar-active-indicator"
                        className="absolute left-0 top-1/2 h-5 w-1 -translate-y-1/2 rounded-r-full bg-primary"
                        transition={{ type: 'spring', stiffness: 400, damping: 30 }}
                      />
                    )}
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      {/* Footer with user profile */}
      <SidebarFooter className="p-2">
        <div className="mb-1 h-px bg-sidebar-border group-data-[collapsible=icon]:mx-auto group-data-[collapsible=icon]:w-8" />
        <div className="group/user relative flex items-center gap-2.5 rounded-lg p-2 group-data-[collapsible=icon]:justify-center">
          <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-primary to-primary/60 text-sm font-semibold text-primary-foreground shadow-sm">
            {user?.username?.charAt(0)?.toUpperCase() ?? '?'}
          </div>
          <div className="flex min-w-0 flex-1 flex-col group-data-[collapsible=icon]:hidden">
            <span className="truncate text-sm font-medium leading-tight">
              {user?.username ?? 'User'}
            </span>
            <span className="truncate text-xs text-muted-foreground leading-tight">
              {user?.email ?? ''}
            </span>
          </div>
          <button
            type="button"
            onClick={() => logoutMutation.mutate()}
            className="flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive group-data-[collapsible=icon]:hidden"
            title="Logout"
            aria-label="Logout"
          >
            <LogOut className="size-3.5" />
          </button>
        </div>
        {/* Icon-only logout for collapsed state */}
        <SidebarMenu className="group-data-[collapsible=icon]:block hidden">
          <SidebarMenuItem>
            <SidebarMenuButton
              tooltip="Logout"
              onClick={() => logoutMutation.mutate()}
              className="text-muted-foreground hover:text-destructive hover:bg-destructive/10"
            >
              <LogOut className="size-4" />
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  )
}
