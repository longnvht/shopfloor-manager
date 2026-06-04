import type { CSSProperties, ReactNode } from 'react'
import { va } from '@/lib/va-tokens'

interface VACardProps {
  title?: ReactNode
  sub?: string
  right?: ReactNode
  children?: ReactNode
  pad?: boolean
  style?: CSSProperties
  className?: string
}

export function VACard({ title, sub, right, children, pad = true, style, className }: VACardProps) {
  return (
    <div
      className={className}
      style={{
        background: va.surface, border: `1px solid ${va.border}`,
        borderRadius: 11, display: 'flex', flexDirection: 'column',
        overflow: 'hidden', boxShadow: va.shadow,
        ...style,
      }}
    >
      {(title || right) && (
        <div style={{
          padding: '13px 16px', borderBottom: `1px solid ${va.separator}`,
          display: 'flex', alignItems: 'center', gap: 10, flexShrink: 0,
        }}>
          <div style={{ fontSize: 13, fontWeight: 600, color: va.text }}>{title}</div>
          {sub && <div style={{ fontSize: 11, color: va.text2 }}>· {sub}</div>}
          <div style={{ marginLeft: 'auto' }}>{right}</div>
        </div>
      )}
      <div style={{ flex: 1, minHeight: 0, ...(pad ? { padding: 16 } : {}) }}>
        {children}
      </div>
    </div>
  )
}
