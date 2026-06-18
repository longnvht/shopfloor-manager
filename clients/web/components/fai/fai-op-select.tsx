'use client'

import { useState } from 'react'
import type { PartOpDto } from '@/lib/api-client'
import { va } from '@/lib/va-tokens'

type Props = {
  ops: PartOpDto[]
  value: number | null
  onChange: (opId: number) => void
}

function isInspectionOp(op: PartOpDto): boolean {
  return (op.opTypeCode ?? '').toUpperCase() === 'INS'
}

const ALL_OPS_ID = 0

export function FaiOpSelect({ ops, value, onChange }: Props) {
  const [open, setOpen] = useState(false)
  const cur = ops.find(o => o.id === value) ?? null
  const isAll = value === ALL_OPS_ID

  return (
    <div style={{ position: 'relative' }}>
      <button type="button" className="va-clickable" onClick={() => setOpen(o => !o)}
        style={{ height: 34, minWidth: 220, padding: '0 12px', borderRadius: 7, border: `1px solid ${va.border}`, background: va.surface, color: va.text, fontSize: 12.5, fontWeight: 600, fontFamily: va.font, display: 'inline-flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
        <span style={{ flex: 1, textAlign: 'left' }}>
          {isAll ? '— Tất cả OP —' : cur ? `OP${cur.opNumber}${cur.description ? ` · ${cur.description}` : ''}` : '— Chọn Operation —'}
        </span>
        {cur && isInspectionOp(cur) && <span title="OP kiểm tra">🔍</span>}
        <span style={{ color: va.text3, fontSize: 11 }}>▾</span>
      </button>
      {open && (
        <>
          <div onClick={() => setOpen(false)} style={{ position: 'fixed', inset: 0, zIndex: 50 }} />
          <div style={{ position: 'absolute', top: 38, left: 0, zIndex: 51, minWidth: 260, background: va.surface, border: `1px solid ${va.border}`, borderRadius: 9, boxShadow: va.shadowLg, padding: 5, display: 'flex', flexDirection: 'column', gap: 1, maxHeight: 320, overflow: 'auto' }}>
            <div className="va-clickable" onClick={() => { onChange(ALL_OPS_ID); setOpen(false) }}
              style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 10px', borderRadius: 6, background: isAll ? va.accentBg : 'transparent', fontSize: 12.5, fontWeight: isAll ? 600 : 400, color: va.text, borderBottom: `1px solid ${va.separator}`, marginBottom: 2 }}>
              <span style={{ flex: 1 }}>— Tất cả OP —</span>
            </div>
            {ops.map(o => {
              const on = o.id === value
              const hasSheet = o.dimCount > 0
              return (
                <div key={o.id} className="va-clickable" onClick={() => { onChange(o.id); setOpen(false) }}
                  style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 10px', borderRadius: 6, background: on ? va.accentBg : 'transparent', fontSize: 12.5, fontWeight: on ? 600 : 400, color: va.text }}>
                  <span style={{ width: 7, height: 7, borderRadius: '50%', background: hasSheet ? va.ok : va.borderStr, flexShrink: 0 }} />
                  <span style={{ flex: 1 }}>OP{o.opNumber}{o.description ? ` · ${o.description}` : ''}</span>
                  {isInspectionOp(o) && <span title="OP kiểm tra" style={{ fontSize: 12 }}>🔍</span>}
                  <span style={{ fontSize: 9.5, color: hasSheet ? va.ok : va.text3, fontWeight: 600 }}>
                    {hasSheet ? '● sheet' : 'chưa đo'}
                  </span>
                </div>
              )
            })}
          </div>
        </>
      )}
    </div>
  )
}
