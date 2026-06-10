'use client'

import { useState, useEffect } from 'react'
import { useParams } from 'next/navigation'
import Link from 'next/link'
import { api, type PartRevDto, type RoutingRevDto, type PartOpDto } from '@/lib/api-client'
import { VATopbar, VABadge, VACard, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'

export default function PartDetailPage() {
  const { id } = useParams<{ id: string }>()
  const partId = Number(id)

  const [revs,        setRevs]       = useState<PartRevDto[]>([])
  const [selectedRev, setSelectedRev] = useState<PartRevDto | null>(null)
  const [routingRevs, setRoutingRevs] = useState<RoutingRevDto[]>([])
  const [selectedRR,  setSelectedRR]  = useState<RoutingRevDto | null>(null)
  const [ops,         setOps]         = useState<PartOpDto[]>([])
  const [loading,     setLoading]     = useState(true)

  useEffect(() => {
    api.parts.revisions(partId).then(res => {
      if (res.success && res.data) {
        setRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelectedRev(active)
      }
      setLoading(false)
    })
  }, [partId])

  useEffect(() => {
    if (!selectedRev) return
    setRoutingRevs([]); setSelectedRR(null); setOps([])
    api.parts.routingRevs(selectedRev.id).then(res => {
      if (res.success && res.data) {
        setRoutingRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelectedRR(active)
      }
    })
  }, [selectedRev])

  useEffect(() => {
    if (!selectedRR) { setOps([]); return }
    api.operations.listForRoutingRev(selectedRR.id).then(res => {
      if (res.success && res.data) setOps(res.data)
    })
  }, [selectedRR])

  const partNumber = revs[0]?.partNumber ?? `Part #${partId}`

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={loading ? '…' : partNumber}
        breadcrumb={`Sản xuất › Chi tiết kỹ thuật › ${partNumber}`}
        right={
          <Link href="/parts">
            <VABtn kind="ghost">← Quay lại</VABtn>
          </Link>
        }
      />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
        {loading ? (
          <div style={{ fontSize: 12, color: va.text3 }}>Đang tải…</div>
        ) : (
          <>
            {/* Drawing Revisions */}
            <VACard title="Drawing Revisions" sub={`${revs.length} revision`}>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginBottom: selectedRev ? 12 : 0 }}>
                {revs.map(rev => (
                  <div key={rev.id} className="va-clickable" onClick={() => setSelectedRev(rev)}
                    style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selectedRev?.id === rev.id ? va.primary : va.surface2, color: selectedRev?.id === rev.id ? '#fff' : va.text, border: `1px solid ${selectedRev?.id === rev.id ? va.primary : va.border}` }}>
                    Rev {rev.revCode}{rev.isActive ? ' ★' : ''}
                  </div>
                ))}
                {revs.length === 0 && <span style={{ fontSize: 12, color: va.text3 }}>Chưa có revision.</span>}
              </div>
              {selectedRev?.description && (
                <div style={{ fontSize: 12, color: va.text2, marginBottom: 10 }}>{selectedRev.description}</div>
              )}
              {selectedRev && (
                <Link href={`/documents?partRevId=${selectedRev.id}&partNumber=${encodeURIComponent(partNumber)}&revCode=${selectedRev.revCode}&backHref=${encodeURIComponent(`/parts/${id}`)}`}>
                  <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }}>Bản vẽ / CAD (Rev {selectedRev.revCode})</VABtn>
                </Link>
              )}
            </VACard>

            {/* Routing Revisions */}
            {routingRevs.length > 0 && (
              <VACard title="Routing Revisions" sub={`${routingRevs.length} revision`}>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                  {routingRevs.map(rr => (
                    <div key={rr.id} className="va-clickable" onClick={() => setSelectedRR(rr)}
                      style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selectedRR?.id === rr.id ? va.accent : va.surface2, color: selectedRR?.id === rr.id ? '#fff' : va.text, border: `1px solid ${selectedRR?.id === rr.id ? va.accent : va.border}` }}>
                      {rr.revCode}
                      <span style={{ fontWeight: 400, opacity: 0.75, marginLeft: 4 }}>({rr.opCount} OP{rr.isActive ? ' · Active' : ''})</span>
                    </div>
                  ))}
                </div>
                {selectedRR?.changeNote && (
                  <div style={{ fontSize: 11, color: va.text3, marginTop: 6 }}>{selectedRR.changeNote}</div>
                )}
              </VACard>
            )}

            {/* Operations */}
            <VACard
              title="Operations"
              sub={selectedRR ? `Routing ${selectedRR.revCode} · ${ops.length} OP` : ''}
              pad={false}
              style={{ flex: 1, minHeight: 0 }}
            >
              <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
                {ops.length === 0 ? (
                  <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Chưa có operation nào.</div>
                ) : (
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                    <thead>
                      <tr style={{ background: va.surface2 }}>
                        {['OP', 'Loại', 'Mô tả', 'Setup (h)', 'Prod (h)', 'Trạng thái', 'Tài liệu'].map((h, i) => (
                          <th key={i} style={{ position: 'sticky', top: 0, textAlign: 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, background: va.surface2, zIndex: 1 }}>{h}</th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {ops.map(op => (
                        <tr key={op.id} className="va-row va-clickable">
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                            <span style={{ fontFamily: va.mono, fontWeight: 700, color: va.text }}>{op.opNumber}</span>
                          </td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{op.opTypeName ?? '—'}</td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, maxWidth: 200 }}>
                            <span style={{ display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{op.description ?? '—'}</span>
                          </td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{op.setupTime ?? '—'}</td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{op.prodTime ?? '—'}</td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                            <VABadge kind={op.isComplete ? 'ok' : 'neutral'}>{op.isComplete ? 'Done' : 'Active'}</VABadge>
                          </td>
                          <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                            <Link href={`/documents?partOpId=${op.id}&opNumber=${op.opNumber}&partNumber=${encodeURIComponent(partNumber)}&revCode=${selectedRev?.revCode ?? ''}&backHref=${encodeURIComponent(`/parts/${id}`)}`}>
                              <VABtn kind="ghost" style={{ height: 26, fontSize: 11, padding: '0 8px' }}>Tài liệu →</VABtn>
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </VACard>
          </>
        )}
      </div>
    </div>
  )
}
