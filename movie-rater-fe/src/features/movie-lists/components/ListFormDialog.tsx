import { useForm, type UseFormRegister, type UseFormWatch, type UseFormSetValue, type FieldErrors } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, Lock, Users, Pencil, Plus, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { cn } from '@/lib/utils'
import { useCreateMovieList } from '../hooks/use-create-movie-list'
import { useUpdateMovieList } from '../hooks/use-update-movie-list'
import { movieListSchema, type MovieListFormValues } from '../schemas/movie-list.schema'
import { useGroups } from '../../groups/hooks/use-groups'
import type { GroupDto } from '@src/types/groups'
import type { MovieListBaseDto } from '@src/types/movie-list'

const TEXTAREA_CLASS =
  'flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none'

interface ListFormFieldsProps {
  register: UseFormRegister<MovieListFormValues>
  errors: FieldErrors<MovieListFormValues>
  watch: UseFormWatch<MovieListFormValues>
  setValue: UseFormSetValue<MovieListFormValues>
  groups: GroupDto[]
}

function ListFormFields({ register, errors, watch, setValue, groups }: ListFormFieldsProps) {
  const isPrivate = watch('isPrivate') ?? true
  const canGroupEdit = watch('canGroupEdit') ?? false
  const groupIds = watch('groupIds') ?? []

  const togglePrivate = () => {
    const next = !isPrivate
    setValue('isPrivate', next)
    if (next) {
      setValue('canGroupEdit', false)
      setValue('groupIds', [])
    }
  }

  const toggleGroup = (groupId: string) => {
    const next = groupIds.includes(groupId)
      ? groupIds.filter((id) => id !== groupId)
      : [...groupIds, groupId]
    setValue('groupIds', next, { shouldValidate: true })
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Name</Label>
        <Input
          id="name"
          placeholder="e.g. Date night picks"
          aria-invalid={!!errors.name}
          {...register('name')}
        />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description (optional)</Label>
        <textarea
          id="description"
          rows={3}
          placeholder="What is this list about?"
          className={TEXTAREA_CLASS}
          aria-invalid={!!errors.description}
          {...register('description')}
        />
        {errors.description && (
          <p className="text-xs text-destructive">{errors.description.message}</p>
        )}
      </div>

      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <Label>Privacy</Label>
          <p className="text-xs text-muted-foreground">
            {isPrivate ? 'Only visible to you' : 'Shared with the selected groups'}
          </p>
        </div>
        <Button
          type="button"
          variant={isPrivate ? 'default' : 'outline'}
          size="sm"
          className="shrink-0"
          onClick={togglePrivate}
        >
          <Lock className="size-3.5" fill={isPrivate ? 'currentColor' : 'none'} />
          {isPrivate ? 'Private' : 'Shared'}
        </Button>
      </div>

      <div className={cn('flex items-center justify-between gap-3', isPrivate && 'opacity-50')}>
        <div className="min-w-0">
          <Label>Group members can edit</Label>
          <p className="text-xs text-muted-foreground">
            Allows group members to add and remove movies
          </p>
        </div>
        <Button
          type="button"
          variant={canGroupEdit ? 'default' : 'outline'}
          size="sm"
          className="shrink-0"
          disabled={isPrivate}
          onClick={() => setValue('canGroupEdit', !canGroupEdit)}
        >
          <Users className="size-3.5" fill={canGroupEdit ? 'currentColor' : 'none'} />
          {canGroupEdit ? 'Allowed' : 'Restricted'}
        </Button>
      </div>

      <div className="space-y-2">
        <Label>Share with groups</Label>
        {groups.length === 0 ? (
          <p className="text-xs text-muted-foreground">
            You are not part of any group yet. Create one from the Invite page to share lists.
          </p>
        ) : (
          <div className="flex flex-wrap gap-1.5">
            {groups.map((group) => {
              const selected = groupIds.includes(group.id)
              return (
                <Button
                  key={group.id}
                  type="button"
                  variant={selected ? 'secondary' : 'outline'}
                  size="sm"
                  disabled={isPrivate}
                  onClick={() => toggleGroup(group.id)}
                >
                  {selected && <Check className="size-3.5" />}
                  {group.name}
                </Button>
              )
            })}
          </div>
        )}
        {errors.groupIds && (
          <p className="text-xs text-destructive">{errors.groupIds.message}</p>
        )}
      </div>
    </div>
  )
}

interface ListDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateListDialog({ open, onOpenChange }: ListDialogProps) {
  const createList = useCreateMovieList()
  const groups = useGroups()

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<MovieListFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(movieListSchema) as any,
    defaultValues: {
      name: '',
      description: '',
      isPrivate: true,
      canGroupEdit: false,
      groupIds: [],
    },
  })

  const onSubmit = (values: MovieListFormValues) => {
    createList.mutate(
      {
        name: values.name,
        description: values.description?.trim() || null,
        isPrivate: values.isPrivate,
        canGroupEdit: values.canGroupEdit,
        groupIds: values.groupIds,
      },
      {
        onSuccess: () => onOpenChange(false),
      },
    )
  }

  const pending = isSubmitting || createList.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Create List</DialogTitle>
          <DialogDescription>
            Organize movies into your own custom lists
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <ListFormFields
            register={register}
            errors={errors}
            watch={watch}
            setValue={setValue}
            groups={groups.data ?? []}
          />
          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="ghost" size="sm" onClick={() => onOpenChange(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={pending}>
              {pending && <Loader2 className="size-4 animate-spin" />}
              <Plus className="size-4" />
              Create list
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}

interface EditListDialogProps extends ListDialogProps {
  list: MovieListBaseDto
}

export function EditListDialog({ open, onOpenChange, list }: EditListDialogProps) {
  const updateList = useUpdateMovieList()
  const groups = useGroups()

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<MovieListFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(movieListSchema) as any,
    defaultValues: {
      name: list.name,
      description: list.description ?? '',
      isPrivate: list.isPrivate,
      canGroupEdit: list.canGroupEdit,
      groupIds: list.groupIds,
    },
  })

  const onSubmit = (values: MovieListFormValues) => {
    updateList.mutate(
      {
        listId: list.id,
        request: {
          name: values.name,
          description: values.description?.trim() || null,
          isPrivate: values.isPrivate,
          canGroupEdit: values.canGroupEdit,
          groupIds: values.groupIds,
        },
      },
      {
        onSuccess: () => onOpenChange(false),
      },
    )
  }

  const pending = isSubmitting || updateList.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit List</DialogTitle>
          <DialogDescription>
            Update the details of &quot;{list.name}&quot;
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <ListFormFields
            register={register}
            errors={errors}
            watch={watch}
            setValue={setValue}
            groups={groups.data ?? []}
          />
          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="ghost" size="sm" onClick={() => onOpenChange(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={pending}>
              {pending && <Loader2 className="size-4 animate-spin" />}
              <Pencil className="size-4" />
              Save changes
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}