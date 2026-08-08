import { useEffect, useRef, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router'
import { useMutation } from '@tanstack/react-query'
import { Loader2, CheckCircle2, XCircle } from 'lucide-react'
import { toast } from 'sonner'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { acceptInvitation } from '../../../api/endpoints/auth'
import type { ApiError } from '@src/types/auth'
import { useAuthStore } from '@/src/stores/auth-store'

type AcceptState = 'loading' | 'success' | 'error'

export function AcceptInvitePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [state, setState] = useState<AcceptState>('loading')
  const [errorMessage, setErrorMessage] = useState('')
  const setAccessToken = useAuthStore(s => s.setAccessToken);
  const setCoupleId = useAuthStore(s => s.setCoupleId);

  const token = searchParams.get('token')

  const mutation = useMutation({
    mutationFn: acceptInvitation,
    onSuccess: (response) => {
      setState('success')
      setAccessToken(response.newAccessToken)
      setCoupleId(response.coupleId)
      toast.success('You are now connected!')
      setTimeout(() => navigate('/dashboard', { replace: true }), 2000)
    },
    onError: (error: unknown) => {
      setState('error')
      const apiError = error as { response?: { data?: ApiError } }
      const message =
        apiError?.response?.data?.detail ??
        apiError?.response?.data?.title ??
        'Failed to accept invitation'
      setErrorMessage(message)
      toast.error(message)
    },
  })

  const hasMutated = useRef(false)

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage('No invitation token provided.')
      return
    }

    if (hasMutated.current) return
    hasMutated.current = true

    mutation.mutate({ inviteToken: token })
  }, [token, mutation])

  return (
    <div className="mx-auto max-w-md px-4 pt-8 sm:pt-16">
      <Card>
        <CardHeader className="text-center">
          {state === 'loading' && (
            <div className="flex justify-center mb-4">
              <Loader2 className="size-10 animate-spin text-primary" />
            </div>
          )}
          {state === 'success' && (
            <div className="flex justify-center mb-4">
              <CheckCircle2 className="size-10 text-green-500" />
            </div>
          )}
          {state === 'error' && (
            <div className="flex justify-center mb-4">
              <XCircle className="size-10 text-destructive" />
            </div>
          )}

          <CardTitle>
            {state === 'loading' && 'Accepting invitation...'}
            {state === 'success' && 'Connected!'}
            {state === 'error' && 'Invitation failed'}
          </CardTitle>
          <CardDescription>
            {state === 'loading' && 'Please wait while we process your invitation.'}
            {state === 'success' && 'You and your partner are now connected. Redirecting...'}
            {state === 'error' && errorMessage}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex justify-center">
          {state === 'error' && (
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => navigate('/dashboard')}>
                Go to Dashboard
              </Button>
              <Button onClick={() => navigate('/invite')}>
                Create new invitation
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
