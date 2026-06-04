import type { ReactNode } from 'react'
import { va } from '@/lib/va-tokens'

interface VATopbarProps {
  title: string
  breadcrumb?: string
  right?: ReactNode
}

export function VATopbar({ title, breadcrumb, right }: VATopbarProps) {
  return (
    <div style={{
      height: 62, background: va.surface,
      borderBottom: `1px solid ${va.border}`,
      padding: '0 24px', display: 'flex', alignItems: 'center', gap: 16, flexShrink: 0,
    }}>
      <div style={{ flex: 1, minWidth: 0 }}>
        {breadcrumb && (
          <div style={{ fontSize: 10.5, color: va.text3, marginBottom: 3, letterSpacing: 0.5, textTransform: 'uppercase', fontWeight: 600 }}>
            {breadcrumb}
          </div>
        )}
        <div style={{ fontFamily: va.serif, fontSize: 21, fontWeight: 600, color: va.text, letterSpacing: -0.2, lineHeight: 1 }}>
          {title}
        </div>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div className="va-clickable" style={{
          width: 230, height: 35, background: va.bg,
          border: `1px solid ${va.border}`, borderRadius: 7,
          padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8,
          fontSize: 12.5, color: va.text3,
        }}>
          <span>⌕</span>
          <span>Tìm job, part, NCR…</span>
          <span style={{ marginLeft: 'auto', fontFamily: va.mono, fontSize: 10, padding: '1px 5px', border: `1px solid ${va.border}`, borderRadius: 3, background: va.surface }}>
            ⌘K
          </span>
        </div>
        <div className="va-clickable" style={{
          width: 35, height: 35, borderRadius: 7,
          border: `1px solid ${va.border}`, background: va.surface,
          display: 'flex', alignItems: 'center', justifyContent: 'center', position: 'relative',
        }}>
          <span style={{ fontSize: 15, color: va.text2 }}>◔</span>
          <span style={{ position: 'absolute', top: 6, right: 6, width: 7, height: 7, borderRadius: '50%', background: va.err, border: `1.5px solid ${va.surface}` }} />
        </div>
        {right}
      </div>
    </div>
  )
}
