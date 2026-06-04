import type { ReactNode } from 'react'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const PALETTE: Record<VaBadgeKind, { bg: string; fg: string }> = {
  ok:      { bg: va.okBg,     fg: va.ok     },
  running: { bg: va.activeBg, fg: va.active  },
  warn:    { bg: va.warnBg,   fg: va.warn   },
  err:     { bg: va.errBg,    fg: va.err    },
  neutral: { bg: va.surface2, fg: va.text2  },
  primary: { bg: va.accentLt, fg: va.primary},
}

interface VABadgeProps {
  children: ReactNode
  kind?: VaBadgeKind
  dot?: boolean
}

export function VABadge({ children, kind = 'neutral', dot }: VABadgeProps) {
  const p = PALETTE[kind]
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: '2px 8px', fontSize: 11, fontWeight: 600,
      borderRadius: 5, background: p.bg, color: p.fg,
      lineHeight: 1.6, whiteSpace: 'nowrap',
    }}>
      {dot && <span style={{ width: 6, height: 6, borderRadius: '50%', background: p.fg }} />}
      {children}
    </span>
  )
}
