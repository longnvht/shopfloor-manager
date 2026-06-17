'use client'

import { useState, useEffect, useCallback } from 'react'
import { useTranslations, useLocale } from 'next-intl'
import { api, type PartDto, type PartOpDto, type RoutingRevDimensionDto, type PartRevDto, type RoutingRevDto, type ImportResultDto } from '@/lib/api-client'
import { VATopbar, VACard, VAKpi, VABadge, VACombobox } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { downloadBlob } from '@/lib/doc-format'
import { useAuthStore } from '@/stores/auth.store'

const CENTER_HEADERS = ['nominal', 'tolPlus', 'tolMinus', 'max', 'min', 'unit', 'final', 'status'] as const
const TABLE_COLS = ['balloon', 'op', 'category', 'nominal', 'tolPlus', 'tolMinus', 'max', 'min', 'unit', 'final', 'status'] as const
const ALL_CATS = ['LIN', 'ANG', 'THD', 'GEO', 'SFC']
const CAT_COLORS: Record<string, string> = { LIN: va.primary, ANG: va.accent, THD: va.primaryLt, GEO: '#5D4037', SFC: '#795548' }

const APPROVER_ROLES = ['Administrator', 'Manager', 'Lead Engineer']

type EditForm = { nominalValue: string; tolerancePlus: string; toleranceMinus: string }

function previewLimit(nominal: string, tol: string, sign: 1 | -1): string {
  const n = Number(nominal)
  const t = Number(tol)
  if (nominal === '' || tol === '' || Number.isNaN(n) || Number.isNaN(t)) return '—'
  return String(n + sign * t)
}

// ── Bulk Import Dialog ─────────────────────────────────────────────────────

function ImportDimsheetDialog({
  routingRevId, onClose, onImported,
}: { routingRevId: number; onClose: () => void; onImported: () => void }) {
  const t = useTranslations('dimsheet.importBulk')
  const [file, setFile] = useState<File | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<ImportResultDto | null>(null)
  const [dlError, setDlError] = useState('')

  async function handleDownloadTemplate() {
    setDlError('')
    try {
      const blob = await api.routingRevs.importBulkDimensionsTemplate(routingRevId)
      downloadBlob(blob, 'import-bulk-dimensions-template.xlsx')
    } catch {
      setDlError('Không tải được file mẫu.')
    }
  }

  async function handleSubmit() {
    if (!file) return
    setSubmitting(true)
    const res = await api.routingRevs.importBulkDimensions(routingRevId, file)
    setSubmitting(false)
    if (res.success && res.data) {
      setResult(res.data)
      onImported()
    }
  }

  const overlayStyle: React.CSSProperties = {
    position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', zIndex: 1000,
    display: 'flex', alignItems: 'center', justifyContent: 'center',
  }
  const dialogStyle: React.CSSProperties = {
    background: va.surface, border: `1px solid ${va.border}`, borderRadius: 14, padding: 28,
    width: 500, maxWidth: '95vw', boxShadow: va.shadow,
  }

  return (
    <div style={overlayStyle} onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div style={dialogStyle}>
        <h3 style={{ margin: '0 0 10px', fontSize: 16, fontWeight: 700, color: va.text }}>{t('title')}</h3>
        <p style={{ margin: '0 0 16px', fontSize: 12.5, color: va.text2 }}>{t('hint')}</p>

        <div style={{ marginBottom: 14 }}>
          <button onClick={handleDownloadTemplate}
            style={{ border: `1px solid ${va.border}`, background: va.bg, borderRadius: 7, padding: '6px 14px', fontSize: 12.5, cursor: 'pointer', color: va.text2 }}>
            {t('downloadTemplate')}
          </button>
          {dlError && <span style={{ marginLeft: 8, fontSize: 11.5, color: va.err }}>{dlError}</span>}
        </div>

        {result ? (
          <div style={{ background: va.bg, border: `1px solid ${va.border}`, borderRadius: 9, padding: 16, marginBottom: 18 }}>
            <div style={{ fontWeight: 700, fontSize: 13, color: va.text, marginBottom: 8 }}>{t('result.title')}</div>
            <div style={{ fontSize: 12.5, color: va.ok }}>{t('result.created', { n: result.created })}</div>
            <div style={{ fontSize: 12.5, color: va.text2 }}>{t('result.skipped', { n: result.skipped })}</div>
            {result.errors.length > 0 && (
              <div style={{ marginTop: 10, maxHeight: 150, overflow: 'auto' }}>
                {result.errors.map((e, i) => (
                  <div key={i} style={{ fontSize: 11.5, color: va.err, padding: '2px 0' }}>
                    {t('result.rowError', { row: e.rowNumber, message: e.message })}
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div style={{ marginBottom: 18 }}>
            <input type="file" accept=".xlsx" onChange={e => setFile(e.target.files?.[0] ?? null)}
              style={{ fontSize: 12.5, color: va.text }} />
            {!file && <div style={{ fontSize: 11.5, color: va.text3, marginTop: 6 }}>{t('noFile')}</div>}
          </div>
        )}

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
          {result ? (
            <button onClick={onClose}
              style={{ background: va.primary, color: '#fff', border: 'none', borderRadius: 8, padding: '8px 20px', fontWeight: 600, cursor: 'pointer', fontSize: 13 }}>
              {t('close')}
            </button>
          ) : (
            <>
              <button onClick={onClose} disabled={submitting}
                style={{ border: `1px solid ${va.border}`, background: va.surface, borderRadius: 8, padding: '8px 16px', cursor: 'pointer', fontSize: 13, color: va.text2 }}>
                {t('cancel')}
              </button>
              <button onClick={handleSubmit} disabled={!file || submitting}
                style={{ background: (!file || submitting) ? va.surface2 : va.primary, color: (!file || submitting) ? va.text3 : '#fff', border: 'none', borderRadius: 8, padding: '8px 20px', fontWeight: 600, cursor: (!file || submitting) ? 'default' : 'pointer', fontSize: 13 }}>
                {submitting ? t('submitting') : t('submit')}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

// ── DimSheet Detail Panel ───────────────────────────────────────────────────

function DimSheetDetail({ part }: { part: PartDto }) {
  const t = useTranslations('dimsheet')
  const { user } = useAuthStore()
  const isApprover = user ? APPROVER_ROLES.includes(user.role) : false
  const [mounted, setMounted] = useState(false)

  // Data
  const [dims, setDims] = useState<RoutingRevDimensionDto[]>([])
  const [ops, setOps] = useState<PartOpDto[]>([])
  const [partRevs, setPartRevs] = useState<PartRevDto[]>([])
  const [routingRevs, setRoutingRevs] = useState<RoutingRevDto[]>([])
  const [selRevId, setSelRevId] = useState<number | null>(null)
  const [selRoutingRevId, setSelRoutingRevId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editForm, setEditForm] = useState<EditForm>({ nominalValue: '', tolerancePlus: '', toleranceMinus: '' })
  const [showImport, setShowImport] = useState(false)

  // Filter state
  const [fOp, setFOp] = useState('all')
  const [fCat, setFCat] = useState('all')
  const [finalOnly, setFinalOnly] = useState(false)
  const [showPending, setShowPending] = useState(false)
  const [q, setQ] = useState('')

  useEffect(() => { setMounted(true) }, [])

  // Load part revisions when part changes
  useEffect(() => {
    let cancelled = false
    async function loadRevs() {
      setLoading(true)
      setDims([]); setOps([]); setPartRevs([]); setRoutingRevs([])
      setSelRevId(null); setSelRoutingRevId(null)
      setFOp('all'); setFCat('all'); setFinalOnly(false); setShowPending(false); setQ('')
      setEditingId(null)

      const revsRes = await api.parts.revisions(part.id)
      if (cancelled || !revsRes.success || !revsRes.data) { setLoading(false); return }
      setPartRevs(revsRes.data)
      const activeRev = revsRes.data.find(r => r.isActive) ?? revsRes.data[0]
      if (activeRev) setSelRevId(activeRev.id)
      setLoading(false)
    }
    loadRevs()
    return () => { cancelled = true }
  }, [part.id])

  // Load routing revs when drawing rev changes
  useEffect(() => {
    if (!selRevId) return
    let cancelled = false
    async function loadRoutingRevs() {
      const rrRes = await api.parts.routingRevs(selRevId!)
      if (cancelled || !rrRes.success || !rrRes.data) return
      setRoutingRevs(rrRes.data)
      const activeRR = rrRes.data.find(r => r.isActive) ?? rrRes.data[0]
      if (activeRR) setSelRoutingRevId(activeRR.id)
    }
    loadRoutingRevs()
    return () => { cancelled = true }
  }, [selRevId])

  // Load dimensions + ops when routing rev changes
  const loadDims = useCallback(async () => {
    if (!selRoutingRevId) return
    const [dimsRes, opsRes] = await Promise.all([
      api.routingRevs.dimensions(selRoutingRevId),
      api.operations.listForRoutingRev(selRoutingRevId),
    ])
    if (dimsRes.success && dimsRes.data) setDims(dimsRes.data)
    if (opsRes.success && opsRes.data) setOps(opsRes.data)
  }, [selRoutingRevId])

  useEffect(() => {
    if (selRoutingRevId) {
      setLoading(true)
      loadDims().finally(() => setLoading(false))
    }
  }, [selRoutingRevId, loadDims])

  function startEdit(d: RoutingRevDimensionDto) {
    setEditingId(d.id)
    setEditForm({
      nominalValue: d.nominalValue?.toString() ?? '',
      tolerancePlus: d.tolerancePlus?.toString() ?? '',
      toleranceMinus: d.toleranceMinus?.toString() ?? '',
    })
  }

  async function handleSave(d: RoutingRevDimensionDto) {
    const nominalValue = editForm.nominalValue === '' ? null : Number(editForm.nominalValue)
    const tolerancePlus = editForm.tolerancePlus === '' ? null : Number(editForm.tolerancePlus)
    const toleranceMinus = editForm.toleranceMinus === '' ? null : Number(editForm.toleranceMinus)
    const res = await api.dimensions.update(d.id, { nominalValue, tolerancePlus, toleranceMinus })
    if (res.success && res.data) {
      const updated = res.data
      setDims(prev => prev.map(x => x.id === d.id ? {
        ...x,
        nominalValue: updated.nominalValue,
        tolerancePlus: updated.tolerancePlus,
        toleranceMinus: updated.toleranceMinus,
        maxValue: updated.maxValue,
        minValue: updated.minValue,
      } : x))
      setEditingId(null)
    }
  }

  async function handleApprove(id: number) {
    const res = await api.dimensions.review(id, { approve: true })
    if (res.success && res.data) {
      const updated = res.data
      setDims(prev => prev.map(x => x.id === id ? { ...x, status: updated.status ?? 'Approved' } : x))
    }
  }

  async function handleReject(id: number) {
    const note = window.prompt(t('review.rejectNote')) ?? undefined
    const res = await api.dimensions.review(id, { approve: false, note })
    if (res.success && res.data) {
      const updated = res.data
      setDims(prev => prev.map(x => x.id === id ? { ...x, status: updated.status ?? 'Rejected' } : x))
    }
  }

  async function handleApproveAll() {
    if (!selRoutingRevId) return
    const res = await api.routingRevs.reviewBatchDimensions(selRoutingRevId, { approve: true })
    if (res.success) {
      setDims(prev => prev.map(x => x.status === 'Pending' ? { ...x, status: 'Approved' } : x))
    }
  }

  async function handleRejectAll() {
    if (!selRoutingRevId) return
    const note = window.prompt(t('review.rejectNote')) ?? undefined
    const res = await api.routingRevs.reviewBatchDimensions(selRoutingRevId, { approve: false, note })
    if (res.success) {
      setDims(prev => prev.map(x => x.status === 'Pending' ? { ...x, status: 'Rejected' } : x))
    }
  }

  const inputStyle: React.CSSProperties = {
    height: 32, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7,
    padding: '0 11px', fontSize: 12.5, color: va.text, fontFamily: va.font, outline: 'none',
  }
  const editInputStyle: React.CSSProperties = {
    width: 60, fontFamily: va.mono, fontSize: 12, textAlign: 'center',
    border: `1px solid ${va.border}`, borderRadius: 4, padding: '3px 4px', background: va.bg, color: va.text,
  }

  if (loading) {
    return (
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
        {t('loading')}
      </div>
    )
  }

  const selRev = partRevs.find(r => r.id === selRevId)
  const selRoutingRev = routingRevs.find(r => r.id === selRoutingRevId)

  if (!selRoutingRevId) {
    return (
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16, minWidth: 0 }}>
        <div>
          <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{part.partNumber}</h2>
          <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>{part.description}</div>
        </div>
        <div style={{ fontSize: 12, color: va.text3 }}>{t('noRouting')}</div>
      </div>
    )
  }

  const pendingCount = dims.filter(d => d.status === 'Pending').length
  const filtered = dims.filter(d =>
    (fOp === 'all' || d.opNumber === fOp) &&
    (fCat === 'all' || d.categoryCode === fCat) &&
    (!finalOnly || d.isFinal) &&
    (!showPending || d.status === 'Pending') &&
    (!q || d.balloonNumber.toLowerCase().includes(q.toLowerCase()))
  )

  const catCounts = ALL_CATS.map(c => ({ c, n: dims.filter(d => d.categoryCode === c).length })).filter(x => x.n > 0)
  const uniqBalloons = new Set(dims.map(d => d.balloonNumber)).size
  const finalCount = dims.filter(d => d.isFinal).length
  const opsWithDims = ops.filter(o => o.dimCount > 0).length
  const sortedOps = [...ops].sort((a, b) => (a.opNumberSort ?? 0) - (b.opNumberSort ?? 0))

  // Rev selector options
  const revOptions = partRevs.map(r => ({ value: String(r.id), label: `Rev ${r.revCode}${r.isActive ? ' ★' : ''}` }))
  const routingRevOptions = routingRevs.map(r => ({ value: String(r.id), label: `${r.revCode}${r.isActive ? ' ★' : ''}` }))

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16, minWidth: 0 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 10, flexWrap: 'wrap' }}>
        <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{part.partNumber}</h2>
        {selRev && <VABadge kind="primary">{t('revBadge', { rev: selRev.revCode })}</VABadge>}
        <span style={{ fontSize: 12.5, color: va.text2, alignSelf: 'center' }}>{part.description}</span>
      </div>

      {/* KPIs */}
      <div style={{ display: 'flex', gap: 13, flexWrap: 'wrap' }}>
        <VAKpi label={t('kpi.totalDims')} value={`${dims.length}`} accent={va.accent} />
        <VAKpi label={t('kpi.uniqueBalloons')} value={`${uniqBalloons}`} />
        <VAKpi label={t('kpi.faiFinal')} value={`${finalCount}`} sub={t('kpi.faiFinalSub')} accent={va.primary} />
        <VAKpi label={t('kpi.opsWithDims')} value={`${opsWithDims}`} sub={t('kpi.opsWithDimsSub', { total: ops.length })} />
        {pendingCount > 0 && (
          <VAKpi label={t('kpi.pendingDims')} value={`${pendingCount}`} accent="#E65100" />
        )}
      </div>

      {/* Pending banner */}
      {mounted && isApprover && pendingCount > 0 && (
        <div style={{ display: 'flex', alignItems: 'center', gap: 14, padding: '11px 16px', background: '#FFF3E0', border: `1px solid #FFB74D`, borderRadius: 9 }}>
          <span style={{ fontSize: 13, color: '#E65100', fontWeight: 600 }}>⚠ {t('pending.banner', { count: pendingCount })}</span>
          <button onClick={handleApproveAll}
            style={{ border: 'none', background: va.ok, color: '#fff', borderRadius: 7, padding: '5px 13px', fontSize: 12, fontWeight: 600, cursor: 'pointer' }}>
            {t('pending.approveAll')}
          </button>
          <button onClick={handleRejectAll}
            style={{ border: `1px solid ${va.err}`, background: 'transparent', color: va.err, borderRadius: 7, padding: '5px 13px', fontSize: 12, fontWeight: 600, cursor: 'pointer' }}>
            {t('pending.rejectAll')}
          </button>
        </div>
      )}

      {/* Single unified filter bar: Rev selectors + OP + Category + toggles + search + import */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap', padding: '10px 14px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, boxShadow: va.shadow }}>
        {/* Rev selectors */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontSize: 10.5, color: va.text3, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.5 }}>{t('revSelector.drawingRev')}</span>
          <VACombobox
            value={selRevId ? String(selRevId) : ''}
            onChange={v => { setSelRevId(Number(v)); setSelRoutingRevId(null) }}
            options={revOptions}
            style={{ fontFamily: va.mono, fontWeight: 600, minWidth: 110 }}
          />
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontSize: 10.5, color: va.text3, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.5 }}>{t('revSelector.routingRev')}</span>
          <VACombobox
            value={selRoutingRevId ? String(selRoutingRevId) : ''}
            onChange={v => setSelRoutingRevId(Number(v))}
            options={routingRevOptions}
            style={{ fontFamily: va.mono, fontWeight: 600, minWidth: 90 }}
          />
        </div>

        <div style={{ width: 1, height: 22, background: va.separator }} />

        {/* OP selector */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{ fontSize: 10.5, color: va.text3, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.5 }}>{t('table.headers.op')}</span>
          <VACombobox
            value={fOp}
            onChange={setFOp}
            options={[
              { value: 'all', label: t('filter.allOps') },
              ...sortedOps.map(o => ({ value: o.opNumber, label: `${o.opNumber}${o.description ? ` · ${o.description}` : ''}` })),
            ]}
            style={{ fontFamily: va.mono, fontWeight: 600 }}
          />
        </div>

        <div style={{ width: 1, height: 22, background: va.separator }} />

        {/* Category chips */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 4, flexWrap: 'wrap' }}>
          {[{ c: 'all', label: t('filter.allCategories') }, ...catCounts.map(x => ({ c: x.c, label: `${x.c} ${x.n}` }))].map(x => {
            const on = fCat === x.c
            return (
              <span key={x.c} className="va-clickable" onClick={() => setFCat(x.c)}
                style={{
                  padding: '4px 9px', fontSize: 11, fontWeight: 600, borderRadius: 6, cursor: 'pointer',
                  fontFamily: x.c === 'all' ? va.font : va.mono,
                  background: on ? (x.c === 'all' ? va.primary : (CAT_COLORS[x.c] ?? va.primary)) : va.surface2,
                  color: on ? '#fff' : (x.c === 'all' ? va.text2 : (CAT_COLORS[x.c] ?? va.text2)),
                  border: `1px solid ${on ? 'transparent' : va.border}`,
                }}>{x.label}</span>
            )
          })}
        </div>

        <div style={{ width: 1, height: 22, background: va.separator }} />

        {/* Checkboxes */}
        <label className="va-clickable" style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 11.5, color: va.text2, fontWeight: 500, cursor: 'pointer', whiteSpace: 'nowrap' }}>
          <input type="checkbox" checked={finalOnly} onChange={e => setFinalOnly(e.target.checked)} style={{ accentColor: va.primary, width: 14, height: 14 }} />
          {t('filter.finalOnly')}
        </label>
        {pendingCount > 0 && (
          <label className="va-clickable" style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 11.5, color: '#E65100', fontWeight: 500, cursor: 'pointer', whiteSpace: 'nowrap' }}>
            <input type="checkbox" checked={showPending} onChange={e => setShowPending(e.target.checked)} style={{ accentColor: '#E65100', width: 14, height: 14 }} />
            {t('filter.pendingOnly')}
          </label>
        )}

        {/* Search + counter + Import button */}
        <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 8 }}>
          <input value={q} onChange={e => setQ(e.target.value)} placeholder={t('filter.searchBalloon')} style={{ ...inputStyle, width: 140 }} />
          <span style={{ fontSize: 11.5, color: va.text3, fontFamily: va.mono, whiteSpace: 'nowrap' }}>{filtered.length}/{dims.length}</span>
          {mounted && isApprover && (
            <button onClick={() => setShowImport(true)}
              style={{ border: `1px solid ${va.primary}`, background: va.bg, color: va.primary, borderRadius: 8, padding: '5px 12px', fontSize: 12, fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap' }}>
              {t('importBulk.trigger')}
            </button>
          )}
        </div>
      </div>

      {/* Master table */}
      <VACard pad={false} style={{ flexShrink: 0 }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
          <thead>
            <tr style={{ background: va.surface2 }}>
              {TABLE_COLS.map(key => (
                <th key={key} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: CENTER_HEADERS.includes(key as typeof CENTER_HEADERS[number]) ? 'center' : 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{t(`table.headers.${key}`)}</th>
              ))}
              <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '9px 14px', borderBottom: `1px solid ${va.border}`, zIndex: 1 }} />
            </tr>
          </thead>
          <tbody>
            {dims.length === 0 ? (
              <tr><td colSpan={12} style={{ padding: '34px 0', textAlign: 'center', color: va.text3, fontSize: 12.5 }}>{t('empty')}</td></tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={12} style={{ padding: '34px 0', textAlign: 'center', color: va.text3, fontSize: 12.5 }}>{t('noMatch')}</td></tr>
            ) : filtered.map(d => {
              const editing = editingId === d.id
              const isPending = d.status === 'Pending'
              const isRejected = d.status === 'Rejected'
              const rowBg = isPending ? '#FFFDE7' : isRejected ? '#FFEBEE' : undefined
              return (
                <tr key={d.id} className="va-row" style={rowBg ? { background: rowBg } : undefined}>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 30, height: 24, borderRadius: '50%', border: `1.5px solid ${d.isCritical ? va.err : va.primary}`, color: d.isCritical ? va.err : va.primary, fontFamily: va.mono, fontWeight: 700, fontSize: 11.5, padding: '0 6px' }}>
                        {d.balloonNumber}
                      </span>
                      {d.code && d.code !== d.balloonNumber && <span style={{ fontWeight: 400, fontSize: 11, color: va.text3 }}>({d.code})</span>}
                    </div>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                    <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 44, height: 24, borderRadius: 6, background: va.primary, color: '#fff', fontFamily: va.mono, fontWeight: 600, fontSize: 11 }}>{d.opNumber}</span>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontWeight: 700, fontSize: 11, color: d.categoryCode ? (CAT_COLORS[d.categoryCode] ?? va.text2) : va.text3 }}>{d.categoryCode ?? '—'}</td>
                  {editing ? (
                    <>
                      <td style={{ padding: '6px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>
                        <input type="number" value={editForm.nominalValue} onChange={e => setEditForm(f => ({ ...f, nominalValue: e.target.value }))} style={editInputStyle} />
                      </td>
                      <td style={{ padding: '6px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>
                        <input type="number" value={editForm.tolerancePlus} onChange={e => setEditForm(f => ({ ...f, tolerancePlus: e.target.value }))} style={editInputStyle} />
                      </td>
                      <td style={{ padding: '6px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>
                        <input type="number" value={editForm.toleranceMinus} onChange={e => setEditForm(f => ({ ...f, toleranceMinus: e.target.value }))} style={editInputStyle} />
                      </td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{previewLimit(editForm.nominalValue, editForm.tolerancePlus, 1)}</td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{previewLimit(editForm.nominalValue, editForm.toleranceMinus, -1)}</td>
                    </>
                  ) : d.isTextType ? (
                    <td colSpan={5} style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.nominalText ?? '—'}</td>
                  ) : (
                    <>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text, fontWeight: 600, textAlign: 'center' }}>{d.nominalValue ?? '—'}</td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.tolerancePlus != null ? `+${d.tolerancePlus}` : '—'}</td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.toleranceMinus != null ? `−${d.toleranceMinus}` : '—'}</td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.ok, textAlign: 'center' }}>{d.maxValue ?? '—'}</td>
                      <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.err, textAlign: 'center' }}>{d.minValue ?? '—'}</td>
                    </>
                  )}
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, textAlign: 'center' }}>{d.unit}</td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>{d.isFinal ? <span style={{ color: va.primary, fontWeight: 700 }}>●</span> : <span style={{ color: va.text3 }}>—</span>}</td>
                  <td style={{ padding: '8px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>
                    <VABadge kind={d.status === 'Approved' ? 'ok' : d.status === 'Rejected' ? 'err' : 'warn'}>
                      {t(`review.status.${d.status}`)}
                    </VABadge>
                  </td>
                  <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center', whiteSpace: 'nowrap' }}>
                    {editing ? (
                      <>
                        <button onClick={() => handleSave(d)} title={t('edit.save')} style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: va.ok, fontSize: 14, padding: '2px 4px' }}>✓</button>
                        <button onClick={() => setEditingId(null)} title={t('edit.cancel')} style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: va.err, fontSize: 14, padding: '2px 4px' }}>✕</button>
                      </>
                    ) : (
                      <div style={{ display: 'flex', alignItems: 'center', gap: 4, justifyContent: 'center' }}>
                        {!d.isTextType && (
                          <button onClick={() => startEdit(d)} title={t('edit.tooltip')} style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: va.text3, fontSize: 13, padding: '2px 4px' }}>✎</button>
                        )}
                        {mounted && isApprover && isPending && (
                          <>
                            <button onClick={() => handleApprove(d.id)} title={t('review.approve')}
                              style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: va.ok, fontSize: 13, padding: '2px 4px', fontWeight: 700 }}>✓</button>
                            <button onClick={() => handleReject(d.id)} title={t('review.reject')}
                              style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: va.err, fontSize: 13, padding: '2px 4px', fontWeight: 700 }}>✕</button>
                          </>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </VACard>

      <div style={{ fontSize: 11, color: va.text3, paddingBottom: 8 }}>ⓘ {t('footnote')}</div>

      {showImport && selRoutingRevId && (
        <ImportDimsheetDialog
          routingRevId={selRoutingRevId}
          onClose={() => setShowImport(false)}
          onImported={() => { loadDims(); setShowPending(true) }}
        />
      )}
    </div>
  )
}

// ── Page ────────────────────────────────────────────────────────────────────

export default function DimSheetPage() {
  const t = useTranslations('dimsheet')
  const locale = useLocale()
  const [parts, setParts] = useState<PartDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [selPart, setSelPart] = useState<PartDto | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.parts.list(page, search || undefined)
    if (res.success && res.data) {
      setParts(res.data)
      setTotal(res.pagination?.total ?? 0)
      if (!selPart && res.data.length > 0) setSelPart(res.data[0])
    }
    setLoading(false)
  }, [page, search]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title={t('title')} breadcrumb={t('breadcrumb')} />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* ── LEFT: Part list ─────────────────────────────────────── */}
        <div className="va-scroll" style={{ width: 280, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
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
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1} style={{ border: `1px solid ${va.border}`, background: va.surface, borderRadius: 6, padding: '4px 10px', cursor: page <= 1 ? 'default' : 'pointer', color: va.text2 }}>←</button>
              <span style={{ fontSize: 11, color: va.text3, alignSelf: 'center' }}>{page} / {Math.ceil(total / 20)}</span>
              <button onClick={() => setPage(p => p + 1)} disabled={parts.length < 20} style={{ border: `1px solid ${va.border}`, background: va.surface, borderRadius: 6, padding: '4px 10px', cursor: parts.length < 20 ? 'default' : 'pointer', color: va.text2 }}>→</button>
            </div>
          )}
        </div>

        {/* ── RIGHT: Dimension Sheet ───────────────────────────────── */}
        {selPart
          ? <DimSheetDetail key={selPart.id} part={selPart} />
          : <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
              {t('selectPart')}
            </div>
        }
      </div>
    </div>
  )
}
