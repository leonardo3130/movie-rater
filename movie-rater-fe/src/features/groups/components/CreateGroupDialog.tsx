import { DialogContent, Dialog, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { useCreateGroup } from "../hooks/use-create-group";
import { createGroupSchema, type CreateGroupFormValues } from "../schemas/create-group.schema";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Mail, Plus } from "lucide-react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { useGroupsStore } from "@/src/stores/groups-store";
import { useQueryClient } from "@tanstack/react-query";
import { useI18n } from '@src/i18n/use-i18n';

interface CreateGroupDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: (sessionId: string) => void
}
export function CreateGroupDialog({ open, onOpenChange, onSuccess }: CreateGroupDialogProps) {
  const createGroup = useCreateGroup();
  const addGroup = useGroupsStore(s => s.createGroup);
  const queryClient = useQueryClient();
  const { t } = useI18n();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateGroupFormValues>({
    resolver: zodResolver(createGroupSchema),
  })

  const onSubmit = (values: CreateGroupFormValues) => {
    createGroup.mutate(
      {
        groupName: values.groupName,
      },
      {
        onSuccess: (data) => {
          addGroup(data);
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['user-groups'] })
          if (onSuccess) onSuccess(data.id)
        },
      },
    )
  }

  const pending = isSubmitting || createGroup.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[30vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('groups.createDialog.title')}</DialogTitle>
          <DialogDescription>
            {t('groups.createDialog.description')}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="watchedAt" className="flex items-center gap-1.5">
              <Mail className="size-3.5 text-muted-foreground" />
              {t('groups.createDialog.groupName')}
            </Label>
            <Input
              id="watchedAt"
              type="text"
              aria-invalid={!!errors.groupName}
              {...register('groupName')}
            />
            {errors.groupName && (
              <p className="text-xs text-destructive">{errors.groupName.message}</p>
            )}
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={pending}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit" size="sm" disabled={pending}>
              {pending && <Loader2 className="size-4 animate-spin" />}
              <Plus className="size-4" />
              {t('groups.createDialog.create')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>);
}
