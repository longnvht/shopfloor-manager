'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type TechDocListDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { VASeg } from '@/components/va/seg'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const FILE_TYPE_COLORS: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00', CAM: '#8D6E63', CAD: '#6D4C41',
}

const STATUS_META: Record<string, { label: string; kind: VaBadgeKind }> = {
  Pending:  { label: 'Chờ duyệt', kind: 'warn' },
  Approved: { label: 'Đã duyệt',  kind: 'ok'   },
  Rejected: { label: 'Từ chối',   kind: 'err'  },
}

type Filter = 'all' | 'Pending' | 'Approved' | 'Rejected'

export default function DocumentsPage() {
  const [docs,    setDocs]    = useState<TechDocListDto[]>([])
  const [filter,  setFilter]  = useState<Filter>('all')
  const [loading, setLoading] = useState(true)
  const [acting,  setActing]  = useState<number | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.techDocuments.list({ status: filter === 'all' ? undefined : filter })
    if (res.success && res.data) setDocs(res.data)
    setLoading(false)
  }, [filter])

  useEffect(() => { load() }, [load])

  async function handleApprove(id: number) {
    setActing(id)
    const res = await api.techDocuments.inspect(id, 'approve')
    if (res.success) load()
    else alert(res.error ?? 'Lỗi duyệt')
    setActing(null)
  }

  async function handleReject(id: number) {
    const note = prompt('Lý do từ chối:')
    if (note === null) return
    setActing(id)
    const res = await api.techDocuments.inspect(id, 'reject', note)
    if (res.success) load()
    else alert(res.error ?? 'Lỗi từ chối')
    setActing(null)
  }

  async function handleView(id: number) {
    const res = await api.techDocuments.downloadUrl(id)
    if (res.success && res.data) window.open(res.data, '_blank')
    else alert('Không tải được URL tài liệu')
  }

  const pending  = docs.filter(d => d.status === 'Pending').length
  const approved = docs.filter(d => d.status === 'Approved').length
  const rejected = docs.filter(d => d.status === 'Rejected').length

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Tài liệu kỹ thuật" breadcrumb="Hệ thống › Drawing · G-code · Route card"
        right={<VABtn kind="primary">⬆ Upload</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng tài liệu" value={docs.length} />
          <VAKpi label="Chờ duyệt"     value={pending}  accent={va.warn} />
          <VAKpi label="Đã duyệt"      value={approved} accent={va.ok}   />
          <VAKpi label="Từ chối"        value={rejected} accent={va.err}  />
        </div>

        {/* Pending banner */}
        {pending > 0 && (
          <div style={{ background: va.warnBg, border: `1px solid ${va.warn}44`, borderRadius: 11, padding: '14px 18px', display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{ width: 34, height: 34, borderRadius: '50%', background: '#fff', color: va.warn, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, flexShrink: 0 }}>◷</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: va.text }}>{pending} tài liệu chờ Inspector duyệt</div>
              <div style={{ fontSize: 11.5, color: va.text2, marginTop: 1 }}>Chỉ file đã duyệt mới hiển thị trên Desktop MES</div>
            </div>
            <VABtn kind="accent" onClick={() => setFilter('Pending')}>Xem hàng đợi →</VABtn>
          </div>
        )}

        {/* Filter + file type legend */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <VASeg value={filter} onChange={v => setFilter(v as Filter)}
            options={[
              { id: 'all',      label: 'Tất cả'   },
              { id: 'Pending',  label: 'Chờ duyệt' },
              { id: 'Approved', label: 'Đã duyệt'  },
              { id: 'Rejected', label: 'Từ chối'   },
            ]}
          />
          <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
            {Object.entries(FILE_TYPE_COLORS).map(([code, color]) => (
              <span key={code} style={{ display: 'flex', alignItems: 'center', gap: 4, padding: '4px 8px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 6, fontSize: 11 }}>
                <span style={{ width: 7, height: 7, borderRadius: 2, background: color }} />
                <span style={{ fontFamily: va.mono, fontWeight: 700, color, fontSize: 9.5 }}>{code}</span>
              </span>
            ))}
          </div>
        </div>

        {/* Table */}
        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {loading ? (
              <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Đang tải…</div>
            ) : docs.length === 0 ? (
              <div style={{ padding: 24, textAlign: 'center', fontSize: 12, color: va.text3 }}>Không có tài liệu nào.</div>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {['Loại', 'Tên / Code', 'Rev / Segment', 'Trạng thái', 'Người tạo', 'Ngày', ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {docs.map(d => {
                    const color = FILE_TYPE_COLORS[d.fileTypeCode] ?? va.text2
                    const sm    = STATUS_META[d.status] ?? { label: d.status, kind: 'neutral' as VaBadgeKind }
                    return (
                      <tr key={d.id} className="va-row va-clickable">
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                            <span style={{ width: 24, height: 24, borderRadius: 5, background: color + '18', color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12 }}>◰</span>
                            <span style={{ fontFamily: va.mono, fontSize: 10, fontWeight: 700, color, background: va.surface2, padding: '1px 5px', borderRadius: 3 }}>{d.fileTypeCode}</span>
                          </span>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <div style={{ fontFamily: va.mono, fontSize: 12, color: va.text }}>{d.code ?? d.description ?? '—'}</div>
                          {d.description && d.code && <div style={{ fontSize: 11, color: va.text2 }}>{d.description}</div>}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, fontSize: 11.5 }}>
                          {d.revision ?? '—'}{d.segment ? ` · seg ${d.segment}` : ''}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <VABadge kind={sm.kind} dot={d.status === 'Pending'}>{sm.label}</VABadge>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>
                          <div style={{ fontSize: 12 }}>{d.createdByName}</div>
                          <div style={{ fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>{new Date(d.createdAt).toLocaleDateString('vi-VN')}</div>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }} />
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                          {d.status === 'Pending' ? (
                            <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                              <VABtn kind="ghost" style={{ height: 28, fontSize: 11, padding: '0 9px', color: va.err, borderColor: va.err + '55' }}
                                onClick={() => handleReject(d.id)} disabled={acting === d.id}>Từ chối</VABtn>
                              <VABtn kind="primary" style={{ height: 28, fontSize: 11, padding: '0 9px', background: va.ok, borderColor: va.ok }}
                                onClick={() => handleApprove(d.id)} disabled={acting === d.id}>Duyệt</VABtn>
                            </div>
                          ) : (
                            <span className="va-clickable" style={{ fontSize: 11, color: va.primary, fontWeight: 600 }}
                              onClick={() => handleView(d.id)}>Xem →</span>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </div>
        </VACard>
      </div>
    </div>
  )
}
