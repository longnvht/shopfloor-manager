import { va } from '@/lib/va-tokens'

interface VAKpiProps {
  label: string
  value: string | number
  sub?: string
  trend?: { up: boolean; label: string }
  accent?: string
}

export function VAKpi({ label, value, sub, trend, accent }: VAKpiProps) {
  return (
    <div style={{
      background: va.surface, border: `1px solid ${va.border}`,
      borderRadius: 11, padding: '15px 17px',
      flex: 1, minWidth: 0, boxShadow: va.shadow,
    }}>
      <div style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.7, marginBottom: 9, fontWeight: 600 }}>
        {label}
      </div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 6, marginBottom: 5 }}>
        <div style={{ fontFamily: va.mono, fontSize: 27, fontWeight: 600, color: accent ?? va.text, letterSpacing: -0.6, lineHeight: 1 }}>
          {value}
        </div>
        {sub && <div style={{ fontSize: 12, color: va.text3 }}>{sub}</div>}
      </div>
      {trend && (
        <div style={{ fontSize: 11, color: trend.up ? va.ok : va.err, display: 'flex', alignItems: 'center', gap: 4, fontWeight: 500 }}>
          <span>{trend.up ? '▲' : '▼'}</span>
          <span>{trend.label}</span>
        </div>
      )}
    </div>
  )
}
