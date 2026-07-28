import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { router } from './routes/router'
import { Toaster } from '@/components/ui/sonner'
import { ThemeProvider } from 'next-themes'
import { TooltipProvider } from '@/components/ui/tooltip'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: 1000 * 60,
    },
  },
})

function App() {
  return (
    <ThemeProvider attribute="class" defaultTheme="dark" enableSystem={false}>
      <QueryClientProvider client={queryClient}>
        <TooltipProvider>
          <RouterProvider router={router} />
        </TooltipProvider>
        <Toaster position="top-center" richColors closeButton />
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App