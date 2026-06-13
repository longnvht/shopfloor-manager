'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useTranslations, useLocale } from 'next-intl'
import { api, type PartDto, type PartRevDto, type RoutingRevDto, type PartOpDto } from '@/lib/api-client'
import { VATopbar, VABadge, VAKpi, VACard, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { CreatePartDialog } from '@/components/parts/create-part-dialog'
import { AddRevisionDialog } from '@/components/parts/add-revision-dialog'
import { AddRoutingRevDialog } from '@/components/parts/add-routing-rev-dialog'
import { AddOpDialog } from '@/components/parts/add-op-dialog'
import { ImportOpsDialog } from '@/components/parts/import-ops-dialog'

// ── OP type display ────────────────────────────────────────────────────────
const OP_TYPE_COLOR: Record<string, string> = {
  CNC: va.primary, TURN: va.primary, MILL: va.accent,
  GRIND: va.primaryLt, INSP: '#5D4037', WIRE: '#00695C',
}
function opColor(code?: string | null) {
  if (!code) return va.text2
  for (const [k, v] of Object.entries(OP_TYPE_COLOR)) {
    if (code.toUpperCase().includes(k)) return v
  }
  return va.text2
}

const CENTER_HEADERS = ['setup', 'prod', 'documents', 'dim'] as const

// ── Detail panel component ─────────────────────────────────────────────────
function PartDetail({ part }: { part: PartDto }) {
  const t = useTranslations('parts')
  const locale = useLocale()
  const router = useRouter()
  const [revs, setRevs]             = useState<PartRevDto[]>([])
  const [selRev, setSelRev]         = useState<PartRevDto | null>(null)
  const [routingRevs, setRoutingRevs] = useState<RoutingRevDto[]>([])
  const [selRR, setSelRR]           = useState<RoutingRevDto | null>(null)
  const [ops, setOps]               = useState<PartOpDto[]>([])
  const [loadingRevs, setLoadingRevs] = useState(true)
  const [loadingOps, setLoadingOps]   = useState(false)

  const [showAddRevision, setShowAddRevision]     = useState(false)
  const [showAddRoutingRev, setShowAddRoutingRev] = useState(false)
  const [showAddOp, setShowAddOp]                 = useState(false)
  const [showImportOps, setShowImportOps]         = useState(false)

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
    api.parts.routingRevs(selRev.id).then(res => {
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
    return <div style={{ padding: 32, color: va.text3, fontSize: 13 }}>{t('loading')}</div>
  }

  const standardOps = ops.filter(o => !o.forJobOnly)
  const routingId = (selRR ?? routingRevs[0])?.routingId
  const totalDims = ops.reduce((sum, o) => sum + o.dimCount, 0)

  function refreshOps() {
    if (!selRR) return
    api.operations.listForRoutingRev(selRR.id).then(res => {
      if (res.success && res.data) setOps(res.data)
    })
  }

  function goToOp(op: PartOpDto) {
    router.push(`/parts/${part.id}/operations?routingRevId=${selRR?.id ?? ''}&opId=${op.id}`)
  }

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{part.partNumber}</h2>
            {selRev && <VABadge kind="primary">Rev {selRev.revCode}{selRev.isActive ? ` · ${t('drawingRev.active')}` : ''}</VABadge>}
            {selRR  && <VABadge kind="neutral">Routing {selRR.revCode}{selRR.isActive ? ` · ${t('routingRev.active')}` : ''}</VABadge>}
          </div>
          <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>{part.description}</div>
        </div>
        {selRR && (
          <Link href={`/parts/${part.id}/operations?routingRevId=${selRR.id}`}>
            <VABtn kind="ghost">{t('operationsLink')}</VABtn>
          </Link>
        )}
      </div>

      {/* KPI strip */}
      <div style={{ display: 'flex', gap: 14 }}>
        <VAKpi label={t('kpi.ops')} value={part.opCount} />
        <VAKpi label={t('kpi.jobs')} value={part.jobCount} />
        <VAKpi label={t('kpi.dimensions')} value={totalDims} />
        <VAKpi label={t('kpi.currentRouting')} value={part.currentRoutingRevCode ?? '—'} />
      </div>

      {/* Rev + Routing selectors */}
      <div style={{ display: 'flex', gap: 14 }}>
        {/* Drawing Revisions */}
        <VACard
          title={t('drawingRev.title')}
          style={{ flex: 1 }}
          right={<VABtn kind="ghost" style={{ height: 26, fontSize: 12, padding: '0 8px' }} onClick={() => setShowAddRevision(true)}>{t('addRevision.trigger')}</VABtn>}
        >
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {revs.map(r => (
              <div key={r.id} className="va-clickable" onClick={() => setSelRev(r)}
                style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selRev?.id === r.id ? va.primary : va.surface2, color: selRev?.id === r.id ? '#fff' : va.text, border: `1px solid ${selRev?.id === r.id ? va.primary : va.border}` }}>
                Rev {r.revCode}{r.isActive ? ' ★' : ''}
              </div>
            ))}
            {revs.length === 0 && <span style={{ fontSize: 12, color: va.text3 }}>{t('drawingRev.empty')}</span>}
          </div>
          {selRev?.createdByName && (
            <div style={{ fontSize: 11, color: va.text3, marginTop: 8 }}>
              {t('drawingRev.createdBy', { name: selRev.createdByName, date: new Date(selRev.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US') })}
            </div>
          )}
        </VACard>

        {/* Routing Revisions */}
        <VACard
          title={t('routingRev.title')}
          style={{ flex: 1 }}
          right={<VABtn kind="ghost" style={{ height: 26, fontSize: 12, padding: '0 8px' }} onClick={() => setShowAddRoutingRev(true)} disabled={!routingId}>{t('addRoutingRev.trigger')}</VABtn>}
        >
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {routingRevs.map(r => (
              <div key={r.id} className="va-clickable" onClick={() => setSelRR(r)}
                style={{ padding: '6px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, cursor: 'pointer', background: selRR?.id === r.id ? va.accent : va.surface2, color: selRR?.id === r.id ? '#fff' : va.text, border: `1px solid ${selRR?.id === r.id ? va.accent : va.border}` }}>
                {r.revCode} <span style={{ fontWeight: 400, opacity: 0.75 }}>({t('routingRev.opCount', { count: r.opCount })}{r.isActive ? ` · ${t('routingRev.active')}` : ''})</span>
              </div>
            ))}
            {routingRevs.length === 0 && <span style={{ fontSize: 12, color: va.text3 }}>{t('routingRev.empty')}</span>}
          </div>
          {selRR?.changeNote && <div style={{ fontSize: 11, color: va.text3, marginTop: 6 }}>{selRR.changeNote}</div>}
        </VACard>

        {/* Drawing 2D */}
        <VACard title={t('drawing2d.title')} style={{ flex: 1 }}>
          {selRev ? (
            <Link href={`/documents?partRevId=${selRev.id}&partNumber=${encodeURIComponent(part.partNumber)}&revCode=${selRev.revCode}&backHref=${encodeURIComponent('/parts')}`}>
              <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }}>{t('drawing2d.open')}</VABtn>
            </Link>
          ) : (
            <span style={{ fontSize: 12, color: va.text3 }}>{t('drawing2d.empty')}</span>
          )}
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
        title={t('opTable.title')}
        sub={selRR ? `Routing ${selRR.revCode} · ${standardOps.length} OP` : ''}
        pad={false}
        style={{ flex: 1, minHeight: 0 }}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }} onClick={() => setShowAddOp(true)} disabled={!selRR}>{t('addOp.trigger')}</VABtn>
            <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }} onClick={() => setShowImportOps(true)} disabled={!selRR}>{t('importOps.trigger')}</VABtn>
          </div>
        }
      >
        <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
          {loadingOps ? (
            <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('loading')}</div>
          ) : standardOps.length === 0 ? (
            <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('opTable.empty')}</div>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
              <thead>
                <tr style={{ background: va.surface2 }}>
                  {(['op', 'type', 'description', 'setup', 'prod', 'documents', 'dim', 'status'] as const).map(key => (
                    <th key={key} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: CENTER_HEADERS.includes(key as typeof CENTER_HEADERS[number]) ? 'center' : 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{t(`opTable.headers.${key}`)}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {standardOps.map(op => (
                  <tr key={op.id} className="va-row va-clickable" onClick={() => goToOp(op)}>
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
                    <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.docCount}</td>
                    <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.dimCount}</td>
                    <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right' }}>
                      {op.isComplete
                        ? <VABadge kind="ok">{t('opTable.done')}</VABadge>
                        : <span style={{ fontSize: 11, color: va.text3 }}>—</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </VACard>

      {/* Dialogs */}
      <AddRevisionDialog
        open={showAddRevision} partId={part.id}
        onClose={() => setShowAddRevision(false)}
        onCreated={(rev) => { setRevs(prev => [...prev, rev]); setSelRev(rev); setShowAddRevision(false) }}
      />
      <AddRoutingRevDialog
        open={showAddRoutingRev} routingId={routingId ?? 0}
        onClose={() => setShowAddRoutingRev(false)}
        onCreated={(rr) => { setRoutingRevs(prev => [...prev, rr]); setSelRR(rr); setShowAddRoutingRev(false) }}
      />
      <AddOpDialog
        open={showAddOp} routingRevId={selRR?.id ?? 0}
        onClose={() => setShowAddOp(false)}
        onCreated={(op) => { setOps(prev => [...prev, op]); setShowAddOp(false) }}
      />
      <ImportOpsDialog
        open={showImportOps} routingRevId={selRR?.id ?? 0}
        onClose={() => setShowImportOps(false)}
        onImported={() => { setShowImportOps(false); refreshOps() }}
      />
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
export default function PartsPage() {
  const t = useTranslations('parts')
  const locale = useLocale()
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
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={t('title')}
        breadcrumb={t('breadcrumb')}
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>{t('createPart')}</VABtn>}
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
                placeholder={t('searchPlaceholder')}
                style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }}
              />
            </div>
          </div>

          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('loading')}</div>}

          {parts.map(p => {
            const on = selPart?.id === p.id
            return (
              <div key={p.id} className="va-clickable" onClick={() => setSelPart(p)}
                style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 700, color: va.text }}>{p.partNumber}</div>
                <div style={{ fontSize: 11.5, color: va.text2, marginTop: 3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{p.description}</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4, fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>
                  <span>{p.currentRoutingRevCode ?? '—'}</span>
                  <span>· {p.opCount} OP</span>
                  <span>· {p.jobCount} Job</span>
                </div>
                <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2, fontFamily: va.mono }}>
                  {new Date(p.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')}
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
              {t('selectPart')}
            </div>
        }
      </div>

      <CreatePartDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={load} />
    </div>
  )
}
