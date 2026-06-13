'use client'

import { type FaiSheetDto } from '@/lib/api-client'
import { VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

type Props = {
  sheet: FaiSheetDto
  onMeasure: (dimId: number, productId: number, rawValue: string) => void
  saving: string | null
}

export function FaiMatrix({ sheet, onMeasure, saving }: Props) {
  const { dimensions: dims, rows } = sheet
  const totalCells  = dims.length * rows.length
  const filledCells = rows.flatMap(r => r.cells).filter(c => c.value != null).length
  const passCells   = rows.flatMap(r => r.cells).filter(c => c.result === 'Pass').length
  const failCells   = rows.flatMap(r => r.cells).filter(c => c.result === 'Fail').length

  if (dims.length === 0) {
    return (
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
        Operation này chưa có dimension. Cần thêm dimensions trước.
      </div>
    )
  }

  return (
    <>
      {/* Stats strip */}
      <div style={{ padding: '10px 22px', background: va.surface, borderBottom: `1px solid ${va.border}`, display: 'flex', alignItems: 'center', gap: 22 }}>
        <div style={{ display: 'flex', gap: 16 }}>
          {[
            ['Tổng ô',    totalCells,  va.text  ],
            ['Đã đo',     filledCells, va.accent ],
            ['Pass',      passCells,   va.ok     ],
            ['Fail',      failCells,   va.err    ],
          ].map(([label, value, color]) => (
            <div key={label as string} style={{ textAlign: 'center' }}>
              <div style={{ fontFamily: va.mono, fontSize: 20, fontWeight: 600, color: color as string, lineHeight: 1 }}>{value}</div>
              <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.5, marginTop: 4 }}>{label}</div>
            </div>
          ))}
        </div>
        <div style={{ marginLeft: 'auto', fontSize: 12, color: va.text3 }}>
          Nhập giá trị → Tab hoặc Enter để lưu
        </div>
      </div>

      {/* Matrix */}
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: '0 0 16px 0' }}>
        <table style={{ borderCollapse: 'separate', borderSpacing: 0, fontSize: 12 }}>
          <thead>
            <tr>
              <th style={{ position: 'sticky', left: 0, top: 0, background: va.surface2, padding: '10px 14px', textAlign: 'left', fontSize: 10, color: va.text2, fontWeight: 700, textTransform: 'uppercase', borderRight: `1px solid ${va.border}`, borderBottom: `1px solid ${va.border}`, zIndex: 3, minWidth: 90 }}>Serial</th>
              {dims.map(d => (
                <th key={d.id} style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'center', fontSize: 11, borderBottom: `1px solid ${va.border}`, borderRight: `1px solid ${va.separator}`, minWidth: 100, zIndex: 2 }}>
                  <div style={{ fontWeight: 700, color: d.isCritical ? va.err : va.text }}>
                    {d.balloonNumber}
                    {d.code && d.code !== d.balloonNumber && (
                      <span style={{ marginLeft: 4, fontWeight: 400, fontSize: 10, color: va.text3 }}>({d.code})</span>
                    )}
                  </div>
                  <div style={{ fontSize: 10, color: va.text3, fontFamily: va.mono, marginTop: 2 }}>
                    {d.isTextType
                      ? d.nominalText
                      : `${d.nominalValue ?? ''} ${(d.tolerancePlus ?? 0) >= 0 ? '+' : ''}${d.tolerancePlus ?? 0} / -${d.toleranceMinus ?? 0}`}
                  </div>
                  <div style={{ fontSize: 9, color: va.text3 }}>{d.unit}</div>
                </th>
              ))}
              <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '10px 12px', textAlign: 'center', fontSize: 10, color: va.text2, fontWeight: 700, textTransform: 'uppercase', borderBottom: `1px solid ${va.border}`, borderLeft: `1px solid ${va.border}`, zIndex: 2, minWidth: 70 }}>Kết quả</th>
            </tr>
          </thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.productId}>
                <td style={{ position: 'sticky', left: 0, background: va.surface, padding: '6px 14px', borderRight: `1px solid ${va.border}`, borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontWeight: 600, zIndex: 1 }}>{row.serialNumber}</td>
                {row.cells.map((cell, i) => {
                  const dim = dims[i]
                  const key = `${dim.id}-${row.productId}`
                  const isSaving = saving === key
                  const bg = cell.result === 'Pass' ? va.okBg : cell.result === 'Fail' ? va.errBg : va.surface
                  return (
                    <td key={dim.id} style={{ padding: '4px 6px', borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, background: bg, textAlign: 'center' }}>
                      <input
                        type="number" step="0.001"
                        defaultValue={cell.value ?? ''}
                        disabled={isSaving}
                        style={{
                          width: 80, textAlign: 'center', padding: '4px 6px',
                          borderRadius: 5, border: `1px solid ${va.border}`,
                          fontSize: 12, fontFamily: va.mono, outline: 'none',
                          background: isSaving ? va.surface2 : 'white',
                          color: cell.result === 'Fail' ? va.err : cell.result === 'Pass' ? va.ok : va.text,
                        }}
                        onBlur={e => onMeasure(dim.id, row.productId, e.target.value)}
                        onKeyDown={e => e.key === 'Enter' && onMeasure(dim.id, row.productId, (e.target as HTMLInputElement).value)}
                      />
                    </td>
                  )
                })}
                <td style={{ padding: '6px 12px', borderBottom: `1px solid ${va.separator}`, borderLeft: `1px solid ${va.border}`, textAlign: 'center' }}>
                  <VABadge kind={row.allPass ? 'ok' : 'err'}>{row.allPass ? 'PASS' : 'FAIL'}</VABadge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
