'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { api, type PartDto, type PartRevDto, type RoutingRevDto, type PartOpDto } from '@/lib/api-client'
import { VATopbar, VABadge, VACard, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { CreatePartDialog } from '@/components/parts/create-part-dialog'

// ── OP type display ────────────────────────────────────────────────────────
const OP_TYPE_COLOR: Record<string, string> = {
  CNC: va.primary, TURN: va.primary, MILL: va.accent,
  GRIND: va.primaryLt, INSP: '#5D4037', WIRE: '#00695C',
}
const DOC_COLOR: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00',
}
function opColor(code?: string | null) {
  if (!code) return va.text2
  for (const [k, v] of Object.entries(OP_TYPE_COLOR)) {
    if (code.toUpperCase().includes(k)) return v
  }
  return va.text2
}

// ── Detail panel component ─────────────────────────────────────────────────
function PartDetail({ part }: { part: PartDto }) {
  const [revs, setRevs]             = useState<PartRevDto[]>([])
  const [selRev, setSelRev]         = useState<PartRevDto | null>(null)
  const [routingRevs, setRoutingRevs] = useState<RoutingRevDto[]>([])
  const [selRR, setSelRR]           = useState<RoutingRevDto | null>(null)
  const [ops, setOps]               = useState<PartOpDto[]>([])
  const [loadingRevs, setLoadingRevs] = useState(true)
  const [loadingOps, setLoadingOps]   = useState(false)

  // Load revisions when part changes
  useEffect(() => {
    setLoadingRevs(true)
    setRevs([]); setSelRev(null); setRoutingRevs([]); setSelRR(null); setOps([])
    api.parts.revisions(part.id).then(res => {
      if (res.success && res.data) {
        setRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelRev(active)
      }
      setLoadingRevs(false)
    })
  }, [part.id])

  // Load routing revisions when rev changes
  useEffect(() => {
    if (!selRev) return
    setRoutingRevs([]); setSelRR(null); setOps([])
    api.parts.routingRevs(selRev.id, 1).then(res => {
      if (res.success && res.data) {
        setRoutingRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelRR(active)
      }
    })
  }, [selRev])

  // Load operations when routing rev changes
  useEffect(() => {
    if (!selRR) { setOps([]); return }
    setLoadingOps(true)
    api.operations.listForRoutingRev(selRR.id).then(res => {
      if (res.success && res.data) setOps(res.data)
      setLoadingOps(false)
    })
  }, [selRR])

  if (loadingRevs) {
    return <div style={{ padding: 32, color: va.text3, fontSize: 13 }}>Đang tải…</div>
  }

  const standardOps = ops.filter(o => !o.forJobOnly)

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{part.partNumber}</h2>
            {selRev && <VABadge kind="primary">Rev {selRev.revCode}{selRev.isActive ? ' · Active' : ''}</VABadge>}
            {selRR  && <VABadge kind="neutral">Routing {selRR.revCode}{selRR.isActive ? ' · Active' : ''}</VABadge>}
          </div>
          <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>{part.description}</div>
        </div>
        <Link href={`/parts/${part.id}`}>
          <VABtn kind="ghost">Chi tiết →</VABtn>
        </Link>
      </div>

      {/* Rev + Routing selectors */}
      <div style={{ display: 'flex', gap: 14 }}>
        {/* Drawing Revisions */}
        <VACard title="Drawing Rev" style={{ flex: 1 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {revs.map(r => (
              <div key={r.id} className="va-clickable" onClick={() => setSelRev(r)}
                style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selRev?.id === r.id ? va.primary : va.surface2, color: selRev?.id === r.id ? '#fff' : va.text, border: `1px solid ${selRev?.id === r.id ? va.primary : va.border}` }}>
                Rev {r.revCode}{r.isActive ? ' ★' : ''}
              </div>
            ))}
            {revs.length === 0 && <span style={{ fontSize: 12, color: va.text3 }}>Chưa có revision.</span>}
          </div>
        </VACard>

        {/* Routing Revisions */}
        <VACard title="Routing Rev" style={{ flex: 1 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {routingRevs.map(r => (
              <div key={r.id} className="va-clickable" onClick={() => setSelRR(r)}
                style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selRR?.id === r.id ? va.accent : va.surface2, color: selRR?.id === r.id ? '#fff' : va.text, border: `1px solid ${selRR?.id === r.id ? va.accent : va.border}` }}>
                {r.revCode} <span style={{ fontWeight: 400, opacity: 0.75 }}>({r.opCount} OP{r.isActive ? ' · Active' : ''})</span>
              </div>
            ))}
            {routingRevs.length === 0 && <span style={{ fontSize: 12, color: va.text3 }}>Chưa có routing.</span>}
          </div>
          {selRR?.changeNote && <div style={{ fontSize: 11, color: va.text3, marginTop: 6 }}>{selRR.changeNote}</div>}
        </VACard>
      </div>

      {/* OP Flow strip */}
      {standardOps.length > 0 && (
        <div className="va-scroll" style={{ display: 'flex', alignItems: 'center', padding: '14px 18px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, boxShadow: va.shadow, overflowX: 'auto', gap: 0 }}>
          {standardOps.map((op, i) => (
            <div key={op.id} style={{ display: 'flex', alignItems: 'center' }}>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 5, minWidth: 72, flexShrink: 0 }}>
                <div style={{ minWidth: 50, height: 36, borderRadius: 7, background: op.isComplete ? va.ok : opColor(op.opTypeName), color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 13 }}>{op.opNumber}</div>
                <span style={{ fontSize: 10, color: va.text2, fontWeight: 600, textAlign: 'center', maxWidth: 70 }}>{op.description?.split(' ').slice(0, 2).join(' ') ?? op.opTypeName ?? ''}</span>
                {op.isComplete && <span style={{ fontSize: 9, color: va.ok, fontWeight: 700 }}>✓</span>}
              </div>
              {i < standardOps.length - 1 && <div style={{ width: 24, height: 2, background: va.border, flexShrink: 0 }} />}
            </div>
          ))}
        </div>
      )}

      {/* OP Table */}
      <VACard
        title="Chi tiết công đoạn"
        sub={selRR ? `Routing ${selRR.revCode} · ${standardOps.length} OP` : ''}
        pad={false}
        style={{ flex: 1, minHeight: 240 }}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            {selRev && (
              <Link href={`/parts/${part.id}/documents?partRevId=${selRev.id}&partNumber=${encodeURIComponent(part.partNumber)}&revCode=${selRev.revCode}`}>
                <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }}>Bản vẽ / CAD</VABtn>
              </Link>
            )}
          </div>
        }
      >
        {loadingOps ? (
          <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>
        ) : standardOps.length === 0 ? (
          <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Chưa có operation. Chọn Routing Rev để xem.</div>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
            <thead>
              <tr style={{ background: va.surface2 }}>
                {['OP', 'Loại', 'Mô tả', 'Setup (h)', 'Run (h)', 'Tài liệu', ''].map((h, i) => (
                  <th key={i} style={{ textAlign: ['Setup (h)', 'Run (h)'].includes(h) ? 'center' : 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}` }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {standardOps.map(op => (
                <tr key={op.id} className="va-row va-clickable">
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                    <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 48, height: 26, borderRadius: 6, background: va.primary, color: '#fff', fontFamily: va.mono, fontWeight: 600, fontSize: 11.5 }}>{op.opNumber}</span>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                    {op.opTypeName
                      ? <span style={{ fontSize: 10.5, fontWeight: 600, color: opColor(op.opTypeName), background: va.surface2, padding: '2px 7px', borderRadius: 4, border: `1px solid ${opColor(op.opTypeName)}33` }}>{op.opTypeName}</span>
                      : <span style={{ color: va.text3, fontSize: 11 }}>—</span>}
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, maxWidth: 200 }}>
                    <span style={{ display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{op.description ?? '—'}</span>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.setupTime ?? '—'}</td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.prodTime ?? '—'}</td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                    <Link href={`/parts/${part.id}/documents?opId=${op.id}&opNumber=${op.opNumber}&partNumber=${encodeURIComponent(part.partNumber)}&revCode=${selRev?.revCode ?? ''}`}>
                      <VABtn kind="ghost" style={{ height: 26, fontSize: 11, padding: '0 8px' }}>Tài liệu →</VABtn>
                    </Link>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right' }}>
                    {op.isComplete
                      ? <VABadge kind="ok">Done</VABadge>
                      : <span style={{ fontSize: 11, color: va.text3 }}>—</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </VACard>
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
export default function PartsPage() {
  const [parts, setParts]       = useState<PartDto[]>([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(true)
  const [selPart, setSelPart]   = useState<PartDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.parts.list(page, search || undefined)
    if (res.success && res.data) {
      setParts(res.data)
      setTotal(res.pagination?.total ?? 0)
      // Auto-select first if nothing selected
      if (!selPart && res.data.length > 0) setSelPart(res.data[0])
    }
    setLoading(false)
  }, [page, search]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Chi tiết kỹ thuật"
        breadcrumb="Sản xuất › Part · Revision · Routing · Operations"
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>+ Tạo Part</VABtn>}
      />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* ── LEFT: Part list ─────────────────────────────────────── */}
        <div className="va-scroll" style={{ width: 280, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
          {/* Search */}
          <div style={{ padding: '12px 14px', borderBottom: `1px solid ${va.separator}`, position: 'sticky', top: 0, background: va.surface, zIndex: 1 }}>
            <div style={{ height: 34, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: va.text3 }}>
              <span>⌕</span>
              <input
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1) }}
                placeholder="Tìm part number…"
                style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }}
              />
            </div>
          </div>

          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>}

          {parts.map(p => {
            const on = selPart?.id === p.id
            return (
              <div key={p.id} className="va-clickable" onClick={() => setSelPart(p)}
                style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 700, color: va.text }}>{p.partNumber}</div>
                <div style={{ fontSize: 11.5, color: va.text2, marginTop: 3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{p.description}</div>
                <div style={{ fontSize: 10.5, color: va.text3, marginTop: 4, fontFamily: va.mono }}>
                  {new Date(p.createdAt).toLocaleDateString('vi-VN')}
                </div>
              </div>
            )
          })}

          {/* Pagination */}
          {total > 20 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 14px', borderTop: `1px solid ${va.separator}` }}>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}>←</VABtn>
              <span style={{ fontSize: 11, color: va.text3, alignSelf: 'center' }}>{page} / {Math.ceil(total / 20)}</span>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => p + 1)} disabled={parts.length < 20}>→</VABtn>
            </div>
          )}
        </div>

        {/* ── RIGHT: Detail ───────────────────────────────────────── */}
        {selPart
          ? <PartDetail key={selPart.id} part={selPart} />
          : <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
              Chọn một Part để xem chi tiết
            </div>
        }
      </div>

      <CreatePartDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={load} />
    </div>
  )
}
