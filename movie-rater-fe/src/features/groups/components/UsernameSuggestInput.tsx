import { Loader2, Search } from 'lucide-react'
import { useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'

import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import type { UserSuggestionDto } from '@/src/types/groups'
import { useUsernameSuggestions } from '../hooks/use-username-suggestions'

interface UsernameSuggestInputProps {
  id?: string
  value?: string
  onChange: (value: string) => void
  placeholder?: string
  ariaInvalid?: boolean
  disabled?: boolean
  onBlur?: () => void
}

export function UsernameSuggestInput({
  id,
  value = '',
  onChange,
  placeholder,
  ariaInvalid,
  disabled,
  onBlur,
}: UsernameSuggestInputProps) {
  const [open, setOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)

  const { data, isPending, isFetching } = useUsernameSuggestions(value)
  const isLoading = isPending || isFetching
  const hasQuery = value.trim().length > 0

  const select = (suggestion: UserSuggestionDto) => {
    onChange(suggestion.username)
    setOpen(false)
    inputRef.current?.blur()
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (!open || !data || data.length === 0) return

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setHighlighted((highlighted + 1) % data.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setHighlighted((highlighted - 1 + data.length) % data.length)
    } else if (event.key === 'Enter' && data[highlighted]) {
      event.preventDefault()
      select(data[highlighted])
    } else if (event.key === 'Escape') {
      setOpen(false)
    }
  }

  return (
    <div className="relative">
      <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        id={id}
        ref={inputRef}
        value={value}
        placeholder={placeholder}
        onChange={(event) => {
          onChange(event.target.value)
          setOpen(true)
          setHighlighted(0)
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => {
          setOpen(false)
          onBlur?.()
        }}
        onKeyDown={handleKeyDown}
        aria-invalid={ariaInvalid}
        disabled={disabled}
        autoComplete="off"
        className="pl-9"
      />

      {open && hasQuery && (
        <div className="absolute z-50 mt-1 w-full overflow-x-hidden rounded-lg bg-popover text-popover-foreground shadow-md ring-1 ring-foreground/10">
          {isLoading ? (
            <div className="flex items-center gap-2 px-2.5 py-2 text-sm text-muted-foreground">
              <Loader2 className="size-3.5 animate-spin" />
              Searching users...
            </div>
          ) : !data || data.length === 0 ? (
            <p className="px-2.5 py-2 text-sm text-muted-foreground">No users found</p>
          ) : (
            <ul className="max-h-56 overflow-y-auto">
              {data.map((suggestion, index) => (
                <li key={suggestion.id}>
                  <button
                    type="button"
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => select(suggestion)}
                    onMouseEnter={() => setHighlighted(index)}
                    className={cn(
                      'flex w-full cursor-pointer items-center gap-2.5 px-2.5 py-1.5 text-left text-sm',
                      index === highlighted && 'bg-accent text-accent-foreground',
                      index !== highlighted && 'hover:bg-accent/50',
                    )}
                  >
                    {suggestion.profilePictureUrl ? (
                      <img
                        src={suggestion.profilePictureUrl}
                        alt=""
                        className="size-6 shrink-0 rounded-full object-cover"
                      />
                    ) : (
                      <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
                        {suggestion.username.charAt(0).toUpperCase()}
                      </span>
                    )}
                    <span className="truncate">{suggestion.username}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
