'use client'

import { useState, useEffect, useRef } from 'react'
import { useParams, useSearchParams, useRouter } from 'next/navigation'
import { api, type FaiSheetDto } from '@/lib/api-client'
import { VATopbar, VABadge, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'

export default function FaiPage() {
  const { id }       = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  const router       = useRouter()
  const opId         = searchParams.get('opId')

  const [sheet,   setSheet]   = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving,  setSaving]  = useState<string | null>(null)

  // Map to hold pending input values per cell (controlled)
  const inputValues = useRef<Record<string, string>>({})

  useEffect(() => {
    if (!opId) { setLoading(false); return }
    api.fai.sheet(Number(opId), Number(id)).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [id, opId])

  async function handleMeasure(dimId: number, productId: number, rawValue: string) {
    const num = parseFloat(rawValue)
    if (isNaN(num)) return
    const key = `${dimId}-${productId}`
    setSaving(key)
    await api.fai.saveMeasure({ dimensionId: dimId, productId, value: num })
    const res = await api.fai.sheet(Number(opId), Number(id))
    if (res.success) {
      setSheet(res.data)
      // Clear stored input value after successful save
      delete inputValues.current[key]
    }
    setSaving(null)
  }

  // ── No opId state ──────────────────────────────────────────────────────────
  if (!opId) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › FAI`}
          right={<VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Quay lại Job</VABtn>} />
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
          <div style={{ fontSize: 28, color: va.text3 }}>◉</div>
          <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chưa chọn Operation</div>
          <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
            Vào Job Detail, chọn Operation cần xem FAI rồi bấm nút <strong>FAI Sheet</strong>.
          </div>
          <VABtn kind="primary" onClick={() => router.push(`/jobs/${id}`)}>← Về Job Detail</VABtn>
        </div>
      </div>
    )
  }

  // ── Loading / Error ────────────────────────────────────────────────────────
  if (loading) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › OP ${opId}`} />
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>Đang tải FAI sheet…</div>
      </div>
    )
  }

  if (!sheet) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › OP ${opId}`}
          right={<VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Quay lại</VABtn>} />
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.err, fontSize: 13 }}>Không tải được FAI sheet.</div>
      </div>
    )
  }

  const { dimensions: dims, rows } = sheet
  const totalCells  = dims.length * rows.length
  const filledCells = rows.flatMap(r => r.cells).filter(c => c.value != null).length
  const passCells   = rows.flatMap(r => r.cells).filter(c => c.result === 'Pass').length
  const failCells   = rows.flatMap(r => r.cells).filter(c => c.result === 'Fail').length

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title={`FAI Sheet — OP ${opId}`}
        breadcrumb={`Chất lượng › Job #${id} › FAI`}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Job</VABtn>
          </div>
        }
      />

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
      {dims.length === 0 ? (
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
          Operation này chưa có dimension. Cần thêm dimensions trước.
        </div>
      ) : (
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
                      {d.nominal} {d.upperTol >= 0 ? '+' : ''}{d.upperTol} / {d.lowerTol}
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
                          onBlur={e => handleMeasure(dim.id, row.productId, e.target.value)}
                          onKeyDown={e => e.key === 'Enter' && handleMeasure(dim.id, row.productId, (e.target as HTMLInputElement).value)}
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
      )}
    </div>
  )
}
