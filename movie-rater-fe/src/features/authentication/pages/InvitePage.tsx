import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { Loader2, UserPlus, CheckCircle2, Copy, ExternalLink } from 'lucide-react'
import { toast } from 'sonner'
import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { inviteSchema, type InviteFormValues } from '../schemas/invite.schema'
import { invitePartner } from '../../../api/endpoints/auth'
import type { ApiError } from '@src/types/auth'

interface InviteResult {
  inviteToken: string
  expiresAt: string
  invitationId: string
}

export function InvitePage() {
  const [result, setResult] = useState<InviteResult | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<InviteFormValues>({
    resolver: zodResolver(inviteSchema),
  })

  const mutation = useMutation({
    mutationFn: invitePartner,
    onSuccess: (data) => {
      setResult({
        inviteToken: data.inviteToken,
        expiresAt: data.expiresAt,
        invitationId: data.invitationId,
      })
      toast.success('Invitation sent!')
      reset()
    },
    onError: (error: unknown) => {
      const apiError = error as { response?: { data?: ApiError } }
      const message =
        apiError?.response?.data?.detail ?? apiError?.response?.data?.title ?? 'Failed to send invitation'
      toast.error(message)
    },
  })

  const onSubmit = (values: InviteFormValues) => {
    setResult(null)
    mutation.mutate(values)
  }

  const copyToken = () => {
    if (result) {
      navigator.clipboard.writeText(result.inviteToken)
      toast.success('Token copied to clipboard')
    }
  }

  const copyLink = () => {
    if (result) {
      navigator.clipboard.writeText(`${window.location.origin}/invite/accept?token=${result.inviteToken}`)
      toast.success('Link copied to clipboard')
    }
  }

  return (
    <div className="mx-auto max-w-lg space-y-6 pt-8">
      <div className="space-y-2">
        <h1 className="text-2xl font-bold flex items-center gap-3">
          <UserPlus className="size-6 text-primary" />
          Invite your partner
        </h1>
        <p className="text-muted-foreground">
          Send an invitation to your partner so you can start tracking movies together.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Send invitation</CardTitle>
          <CardDescription>
            Enter your partner&apos;s email address. They need to have an account first.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="inviteeEmail">Partner&apos;s email</Label>
              <Input
                id="inviteeEmail"
                type="email"
                placeholder="partner@example.com"
                autoComplete="email"
                aria-invalid={!!errors.inviteeEmail}
                {...register('inviteeEmail')}
              />
              {errors.inviteeEmail && (
                <p className="text-xs text-destructive">{errors.inviteeEmail.message}</p>
              )}
            </div>

            <Button type="submit" className="w-full" disabled={mutation.isPending}>
              {mutation.isPending && <Loader2 className="size-4 animate-spin" />}
              Send invitation
            </Button>
          </form>
        </CardContent>
      </Card>

      {result && (
        <Card className="border-primary/30">
          <CardHeader>
            <div className="flex items-center gap-2">
              <CheckCircle2 className="size-5 text-green-500" />
              <CardTitle className="text-base">Invitation sent!</CardTitle>
            </div>
            <CardDescription>
              Share this token with your partner so they can accept the invitation.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>Invitation token</Label>
              <div className="flex gap-2">
                <Input
                  readOnly
                  value={result.inviteToken}
                  className="font-mono text-xs"
                />
                <Button variant="outline" size="icon" onClick={copyToken} title="Copy token">
                  <Copy className="size-4" />
                </Button>
              </div>
            </div>

            <div className="text-xs text-muted-foreground space-y-1">
              <p>Expires: {new Date(result.expiresAt).toLocaleDateString()}</p>
            </div>

            <div className="rounded-lg bg-muted p-3 text-sm text-muted-foreground">
              <p className="font-medium mb-1 flex items-center gap-1">
                <ExternalLink className="size-3.5" />
                Your partner can accept by visiting:
              </p>
              <div className="flex gap-2 mt-1">
                <code className="flex-1 text-xs bg-background rounded px-2 py-1 break-all">
                  {window.location.origin}/invite/accept?token={result.inviteToken}
                </code>
                <Button variant="outline" size="icon" onClick={copyLink} title="Copy link" className="shrink-0">
                  <Copy className="size-4" />
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
