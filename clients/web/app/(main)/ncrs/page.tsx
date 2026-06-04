'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type NcrDto, type NcrDetailDto } from '@/lib/api-client'
import { VATopbar, VABadge, VACard, VABtn } from '@/components/va'
import { VASeg } from '@/components/va/seg'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

function ncrBadge(status: string): { label: string; kind: VaBadgeKind } {
  return status === 'Open'
    ? { label: 'Đang mở', kind: 'err'     }
    : { label: 'Đã đóng', kind: 'ok'      }
}

const ACTION_LABEL: Record<string, string> = {
  Open: 'Tạo', Approve: 'Chấp nhận', Rework: 'Sửa lại', Reject: 'Từ chối'
}

// ── NCR detail panel ───────────────────────────────────────────────────────
function NcrDetail({ id, onClose }: { id: number; onClose: () => void }) {
  const [detail, setDetail] = useState<NcrDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [note, setNote]       = useState('')
  const [saving, setSaving]   = useState(false)

  useEffect(() => {
    setLoading(true)
    api.ncrs.get(id).then(res => {
      if (res.success) setDetail(res.data)
      setLoading(false)
    })
  }, [id])

  async function handleAction(action: string) {
    if (!note.trim() && action !== 'Approve') {
      alert('Vui lòng nhập ghi chú')
      return
    }
    setSaving(true)
    const res = await api.ncrs.addAction(id, action, note.trim() || undefined)
    if (res.success) {
      setNote('')
      // reload detail
      const r2 = await api.ncrs.get(id)
      if (r2.success) setDetail(r2.data)
    } else {
      alert(res.error ?? 'Lỗi')
    }
    setSaving(false)
  }

  if (loading) return <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Đang tải…</div>
  if (!detail) return <div style={{ padding: 24, fontSize: 12, color: va.err }}>Không tải được NCR.</div>
  const { ncr, logs } = detail
  const badge = ncrBadge(ncr.status)

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16, minWidth: 0 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{ncr.ncrNumber}</h2>
            <VABadge kind={badge.kind} dot>{badge.label}</VABadge>
          </div>
          <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>
            Job <strong>{ncr.jobNumber}</strong>
            {ncr.serialNumber && <> · Serial <strong>{ncr.serialNumber}</strong></>}
            {ncr.opNumber      && <> · OP {ncr.opNumber}</>}
          </div>
        </div>
        <VABtn kind="ghost" onClick={onClose}>✕ Đóng</VABtn>
      </div>

      {/* Description */}
      <VACard title="Mô tả sự không phù hợp">
        <p style={{ fontSize: 13, color: va.text, lineHeight: 1.7, margin: 0 }}>{ncr.description}</p>
        <div style={{ display: 'flex', gap: 20, marginTop: 12, fontSize: 11.5, color: va.text2 }}>
          <span>Người tạo: <strong>{ncr.raisedBy}</strong></span>
          <span>Ngày: <strong>{new Date(ncr.raisedAt).toLocaleDateString('vi-VN')}</strong></span>
          {ncr.closedBy && <span>Đóng bởi: <strong>{ncr.closedBy}</strong></span>}
        </div>
      </VACard>

      {/* Action logs */}
      {logs.length > 0 && (
        <VACard title="Lịch sử xử lý" pad={false}>
          {logs.map((log, i) => (
            <div key={log.id} style={{ padding: '12px 16px', borderBottom: i < logs.length - 1 ? `1px solid ${va.separator}` : 'none', display: 'flex', gap: 12 }}>
              <div style={{ width: 28, height: 28, borderRadius: '50%', background: va.accentLt, color: va.primary, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 11, flexShrink: 0 }}>
                {log.actionBy[0]}
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 12.5, fontWeight: 600, color: va.text }}>
                  <span style={{ color: va.accent }}>{ACTION_LABEL[log.action] ?? log.action}</span>
                  {' · '}{log.actionBy}
                </div>
                {log.note && <div style={{ fontSize: 12, color: va.text2, marginTop: 2 }}>{log.note}</div>}
                <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2, fontFamily: va.mono }}>
                  {new Date(log.actionAt).toLocaleString('vi-VN')}
                </div>
              </div>
            </div>
          ))}
        </VACard>
      )}

      {/* Action form — chỉ hiện khi NCR còn mở */}
      {ncr.status === 'Open' && (
        <VACard title="Ra quyết định">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            <textarea
              value={note}
              onChange={e => setNote(e.target.value)}
              placeholder="Ghi chú xử lý…"
              rows={3}
              style={{ width: '100%', padding: '10px 12px', borderRadius: 8, border: `1px solid ${va.border}`, fontSize: 13, fontFamily: va.font, resize: 'vertical', outline: 'none' }}
            />
            <div style={{ display: 'flex', gap: 8 }}>
              <VABtn kind="primary" style={{ flex: 1, justifyContent: 'center', background: va.ok,    borderColor: va.ok    }} onClick={() => handleAction('Approve')} disabled={saving}>✓ Chấp nhận</VABtn>
              <VABtn kind="primary" style={{ flex: 1, justifyContent: 'center', background: va.warn,  borderColor: va.warn  }} onClick={() => handleAction('Rework')}  disabled={saving}>↩ Sửa lại</VABtn>
              <VABtn kind="primary" style={{ flex: 1, justifyContent: 'center', background: va.err,   borderColor: va.err   }} onClick={() => handleAction('Reject')}  disabled={saving}>✕ Từ chối</VABtn>
            </div>
          </div>
        </VACard>
      )}
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
type FilterType = '' | 'Open' | 'Closed'

export default function NcrsPage() {
  const [ncrs, setNcrs]     = useState<NcrDto[]>([])
  const [total, setTotal]   = useState(0)
  const [page, setPage]     = useState(1)
  const [filter, setFilter] = useState<FilterType>('')
  const [loading, setLoading] = useState(true)
  const [selId, setSelId]   = useState<number | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.ncrs.list(page, filter || undefined)
    if (res.success && res.data) {
      setNcrs(res.data)
      setTotal(res.pagination?.total ?? 0)
      if (!selId && res.data.length > 0) setSelId(res.data[0].id)
    }
    setLoading(false)
  }, [page, filter]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="NCR · Báo cáo không phù hợp"
        breadcrumb="Chất lượng › Non-Conformance"
        right={
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <VASeg
              value={filter}
              onChange={v => { setFilter(v as FilterType); setPage(1) }}
              options={[
                { id: '',       label: 'Tất cả'  },
                { id: 'Open',   label: 'Đang mở' },
                { id: 'Closed', label: 'Đã đóng' },
              ]}
            />
            <VABtn kind="accent">+ Tạo NCR</VABtn>
          </div>
        }
      />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* NCR list */}
        <div className="va-scroll" style={{ width: 380, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>}

          {!loading && ncrs.length === 0 && (
            <div style={{ padding: 24, textAlign: 'center', fontSize: 12, color: va.text3 }}>Không có NCR nào.</div>
          )}

          {ncrs.map(ncr => {
            const on    = ncr.id === selId
            const badge = ncrBadge(ncr.status)
            return (
              <div key={ncr.id} className="va-clickable" onClick={() => setSelId(ncr.id)}
                style={{ padding: '14px 18px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                  <span style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 700, color: va.text }}>{ncr.ncrNumber}</span>
                  <VABadge kind={badge.kind} dot>{badge.label}</VABadge>
                  <span style={{ marginLeft: 'auto', fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>
                    {new Date(ncr.raisedAt).toLocaleDateString('vi-VN')}
                  </span>
                </div>
                <div style={{ fontSize: 12.5, color: va.text, fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {ncr.description}
                </div>
                <div style={{ fontSize: 11, color: va.text2, marginTop: 3 }}>
                  Job <strong>{ncr.jobNumber}</strong>
                  {ncr.serialNumber && <> · SN {ncr.serialNumber}</>}
                  {' · '}{ncr.raisedBy}
                </div>
              </div>
            )
          })}

          {/* Pagination */}
          {total > 20 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 16px', borderTop: `1px solid ${va.separator}` }}>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}>←</VABtn>
              <span style={{ fontSize: 11, color: va.text3, alignSelf: 'center' }}>{page} / {Math.ceil(total / 20)}</span>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => p + 1)} disabled={ncrs.length < 20}>→</VABtn>
            </div>
          )}
        </div>

        {/* Detail */}
        {selId
          ? <NcrDetail key={selId} id={selId} onClose={() => setSelId(null)} />
          : <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
              Chọn một NCR để xem chi tiết
            </div>
        }
      </div>
    </div>
  )
}
