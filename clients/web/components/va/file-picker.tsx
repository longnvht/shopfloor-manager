'use client'

import { useRef, useState } from 'react'
import { va } from '@/lib/va-tokens'

type Props = {
  accept?: string
  multiple?: boolean
  onChange: (files: FileList | null) => void
  label?: string
  hint?: string
}

export function VAFilePicker({ accept, multiple, onChange, label = 'Chọn file', hint }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [names, setNames] = useState<string[]>([])
  const [hover, setHover] = useState(false)

  function trigger() { inputRef.current?.click() }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const files = e.target.files
    setNames(files ? Array.from(files).map(f => f.name) : [])
    onChange(files)
    // reset value so re-selecting same file fires onChange again
    e.target.value = ''
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setHover(false)
    const files = e.dataTransfer.files
    if (!files.length) return
    setNames(Array.from(files).map(f => f.name))
    onChange(files)
  }

  const chosen = names.length > 0
  const borderColor = hover ? va.accent : chosen ? va.primaryLt : va.borderStr

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={trigger}
      onKeyDown={e => e.key === 'Enter' && trigger()}
      onDragOver={e => { e.preventDefault(); setHover(true) }}
      onDragLeave={() => setHover(false)}
      onDrop={handleDrop}
      style={{
        border: `2px dashed ${borderColor}`,
        borderRadius: 10,
        padding: '18px 20px',
        cursor: 'pointer',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 6,
        background: hover ? va.accentBg : chosen ? va.surface2 : va.bg,
        textAlign: 'center',
        transition: 'border-color 0.15s, background 0.15s',
        userSelect: 'none',
      }}
    >
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        multiple={multiple}
        style={{ display: 'none' }}
        onChange={handleChange}
      />

      {/* Icon */}
      <span style={{ fontSize: 26, lineHeight: 1, color: chosen ? va.primaryLt : va.text3 }}>
        {chosen ? '✓' : '⬆'}
      </span>

      {/* Main label */}
      <span style={{ fontWeight: 600, fontSize: 13, color: chosen ? va.primary : va.primaryLt }}>
        {chosen
          ? names.length === 1 ? names[0] : `${names.length} file đã chọn`
          : label}
      </span>

      {/* Multi-file list (up to 3 names) */}
      {chosen && names.length > 1 && (
        <div style={{ fontSize: 11, color: va.text2, maxWidth: '100%' }}>
          {names.slice(0, 3).map((n, i) => (
            <div key={i} style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 340 }}>
              {n}
            </div>
          ))}
          {names.length > 3 && <div>…và {names.length - 3} file khác</div>}
        </div>
      )}

      {/* Hint / secondary text */}
      {!chosen && hint && (
        <span style={{ fontSize: 11.5, color: va.text3 }}>{hint}</span>
      )}

      {/* Change prompt when file chosen */}
      {chosen && (
        <span style={{ fontSize: 11, color: va.text3 }}>Bấm để đổi file</span>
      )}
    </div>
  )
}
