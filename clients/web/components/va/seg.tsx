'use client'

import { va } from '@/lib/va-tokens'

interface SegOption {
  id: string
  label: string
}

interface VASegProps {
  options: SegOption[]
  value: string
  onChange?: (id: string) => void
}

export function VASeg({ options, value, onChange }: VASegProps) {
  return (
    <div style={{
      display: 'flex', gap: 3,
      background: va.bg, border: `1px solid ${va.border}`,
      borderRadius: 8, padding: 3,
    }}>
      {options.map(o => {
        const on = o.id === value
        return (
          <div
            key={o.id}
            className="va-clickable"
            onClick={() => onChange?.(o.id)}
            style={{
              padding: '5px 13px', fontSize: 12, fontWeight: 600, borderRadius: 5,
              background: on ? va.surface : 'transparent',
              color:      on ? va.text : va.text2,
              boxShadow:  on ? va.shadow : 'none',
            }}
          >
            {o.label}
          </div>
        )
      })}
    </div>
  )
}
