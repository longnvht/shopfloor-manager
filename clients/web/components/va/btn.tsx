'use client'

import type { ButtonHTMLAttributes, CSSProperties, ReactNode } from 'react'
import { va, type VaBtnKind } from '@/lib/va-tokens'

const BTN_STYLES: Record<VaBtnKind, CSSProperties> = {
  primary: { background: va.primary, color: '#fff', border: `1px solid ${va.primary}` },
  accent:  { background: va.accent,  color: '#fff', border: `1px solid ${va.accent}`  },
  ghost:   { background: va.surface, color: va.text, border: `1px solid ${va.border}` },
}

interface VABtnProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode
  kind?: VaBtnKind
}

export function VABtn({ children, kind = 'ghost', style, ...rest }: VABtnProps) {
  return (
    <button
      className="va-clickable"
      style={{
        height: 34, padding: '0 14px', borderRadius: 7,
        fontSize: 12.5, fontWeight: 600,
        display: 'inline-flex', alignItems: 'center', gap: 7,
        fontFamily: va.font, cursor: 'pointer',
        ...BTN_STYLES[kind],
        ...style,
      }}
      {...rest}
    >
      {children}
    </button>
  )
}
