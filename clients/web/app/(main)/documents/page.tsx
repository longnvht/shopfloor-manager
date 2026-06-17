'use client'

import { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { useTranslations, useLocale } from 'next-intl'
import { api, type TechDocListDto, type FileTypeDto, type PartDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VACombobox, type VAComboboxOption } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { FILE_TYPE_COLORS, formatBytes } from '@/lib/doc-format'
import { BulkUploadDialog } from '@/components/documents/bulk-upload-dialog'
import { useAuthStore } from '@/stores/auth.store'

const APPROVER_ROLES = ['Administrator', 'Manager', 'Lead Engineer']

const STATUS_KIND: Record<string, VaBadgeKind> = {
  Pending: 'warn', Approved: 'ok', Rejected: 'err',
}

const inputStyle = { width: '100%', marginTop: 4, padding: '7px 10px', borderRadius: 7, border: `1px solid ${va.border}`, fontSize: 12.5, fontFamily: va.font, background: va.surface, color: va.text, outline: 'none', boxSizing: 'border-box' as const }
const labelStyle = { fontSize: 11.5, fontWeight: 600, color: va.text2 }
const selStyle = { height: 32, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 9px', fontSize: 12, color: va.text, fontFamily: va.font, cursor: 'pointer', outline: 'none', maxWidth: 168 }

function Lbl({ children }: { children: React.ReactNode }) {
  return <span style={{ fontSize: 9.5, color: va.text3, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 700, marginBottom: 4, display: 'block' }}>{children}</span>
}

const uniq = <T,>(arr: T[]) => [...new Set(arr)]

export default function DocumentsPage() {
  const t = useTranslations('documents')
  const locale = useLocale()
  const { user } = useAuthStore()
  const [mounted, setMounted] = useState(false)
  const searchParams = useSearchParams()
  useEffect(() => { setMounted(true) }, [])
  const partRevId = searchParams.get('partRevId')
  const partOpId  = searchParams.get('partOpId')
  const jobId     = searchParams.get('jobId')
  const backHref  = searchParams.get('backHref')
  const hasContext = !!(partRevId || partOpId || jobId)

  const [docs, setDocs] = useState<TechDocListDto[]>([])
  const [loading, setLoading] = useState(true)
  const [acting, setActing] = useState<number | null>(null)
  const [fileTypes, setFileTypes] = useState<FileTypeDto[]>([])

  // Part list panel (API-driven, like dimsheet)
  const [allParts, setAllParts] = useState<PartDto[]>([])
  const [partSearch, setPartSearch] = useState('')
  const [partPage, setPartPage] = useState(1)
  const [partTotal, setPartTotal] = useState(0)

  const loadParts = useCallback(async () => {
    const res = await api.parts.list(partPage, partSearch || undefined)
    if (res.success && res.data) { setAllParts(res.data); setPartTotal(res.pagination?.total ?? 0) }
  }, [partPage, partSearch])

  useEffect(() => { loadParts() }, [loadParts])

  // Filters: part · drawing rev · routing rev · op (+ type / status / search) — cascading theo part
  const [fPart, setFPart]     = useState(searchParams.get('partNumber') ?? 'all')
  const [fRev, setFRev]       = useState(searchParams.get('revCode') ?? 'all')
  const [fRout, setFRout]     = useState('all')
  const [fOp, setFOp]         = useState(searchParams.get('opNumber') ?? 'all')
  const [fType, setFType]     = useState('all')
  const [fStatus, setFStatus] = useState('all')
  const [q, setQ] = useState('')

  const [uploadForm, setUploadForm] = useState<{ fileTypeId: string; file: File | null; revision: string; description: string; machineType: string } | null>(null)
  const [uploading, setUploading] = useState(false)
  const [bulkOpen, setBulkOpen] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.techDocuments.list()
    if (res.success && res.data) setDocs(res.data)
    setLoading(false)
  }, [])

  useEffect(() => { load() }, [load])

  useEffect(() => {
    api.techDocuments.fileTypes().then(res => {
      if (res.success && res.data) setFileTypes(res.data)
    })
  }, [])

  async function handleApprove(id: number) {
    setActing(id)
    const res = await api.techDocuments.inspect(id, 'approve')
    if (res.success) load()
    else alert(res.error ?? t('actions.errApprove'))
    setActing(null)
  }

  async function handleReject(id: number) {
    const note = prompt(t('actions.rejectPrompt'))
    if (note === null) return
    setActing(id)
    const res = await api.techDocuments.inspect(id, 'reject', note)
    if (res.success) load()
    else alert(res.error ?? t('actions.errReject'))
    setActing(null)
  }

  async function handleView(id: number) {
    const res = await api.techDocuments.downloadUrl(id)
    if (res.success && res.data) window.open(res.data, '_blank')
    else alert(t('actions.errViewUrl'))
  }

  async function handleUpload() {
    if (!uploadForm?.file || !uploadForm.fileTypeId) return
    setUploading(true)
    try {
      const res = await api.techDocuments.create({
        fileTypeId:    parseInt(uploadForm.fileTypeId),
        fileName:      uploadForm.file.name,
        partRevId:     partOpId ? null : (partRevId ? Number(partRevId) : null),
        partOpId:      partOpId ? Number(partOpId) : null,
        jobId:         jobId ? Number(jobId) : null,
        description:   uploadForm.description || null,
        revision:      uploadForm.revision || null,
        machineType:   uploadForm.machineType || null,
        fileSizeBytes: uploadForm.file.size,
      })
      if (!res.success || !res.data) { alert(res.error ?? t('upload.errorGeneric')); return }
      await fetch(res.data.uploadUrl, { method: 'PUT', body: uploadForm.file })
      setUploadForm(null)
      load()
    } finally {
      setUploading(false)
    }
  }

  // Type legend (T) — derive từ fileTypes API, màu theo FILE_TYPE_COLORS
  const T = useMemo(() => {
    const m: Record<string, { color: string; label: string }> = {}
    for (const ft of fileTypes) m[ft.code] = { color: FILE_TYPE_COLORS[ft.code] ?? va.text2, label: ft.name }
    return m
  }, [fileTypes])

  const parts = useMemo(() => uniq(docs.map(d => d.partNumber).filter((p): p is string => !!p)).sort(), [docs])
  const scoped = useMemo(() => docs.filter(d => fPart === 'all' || d.partNumber === fPart), [docs, fPart])
  const revs = useMemo(() => uniq(scoped.map(d => d.drawingRevCode ?? '—')).sort(), [scoped])
  const routs = useMemo(() => uniq(scoped.map(d => d.routingRevCode ?? '—')).sort(), [scoped])
  const ops = useMemo(() => uniq(scoped.map(d => d.opNumber ?? '—')).sort(), [scoped])

  // Option lists cho VACombobox (gõ để tìm) — luôn có option "all" đầu tiên
  const revOptions: VAComboboxOption[] = useMemo(() =>
    [{ value: 'all', label: t('filter.allRevs') }, ...revs.map(r => ({ value: r, label: r === '—' ? t('filter.noRev') : t('filter.revPrefix', { rev: r }) }))], [revs, t])
  const routOptions: VAComboboxOption[] = useMemo(() =>
    [{ value: 'all', label: t('filter.allRouting') }, ...routs.map(r => ({ value: r, label: r === '—' ? t('filter.noRouting') : r }))], [routs, t])
  const opOptions: VAComboboxOption[] = useMemo(() =>
    [{ value: 'all', label: t('filter.allOps') }, ...ops.map(o => ({ value: o, label: o === '—' ? t('filter.noOp') : t('filter.opPrefix', { op: o }) }))], [ops, t])
  const typeOptions: VAComboboxOption[] = useMemo(() =>
    [{ value: 'all', label: t('filter.allTypes') }, ...Object.entries(T).map(([c, ft]) => ({ value: c, label: `${c} · ${ft.label}` }))], [T, t])
  const statusOptions: VAComboboxOption[] = useMemo(() =>
    [{ value: 'all', label: t('filter.allStatuses') }, ...Object.keys(STATUS_KIND).map(c => ({ value: c, label: t(`status.${c}`) }))], [t])

  const filtered = useMemo(() => docs.filter(d =>
    (fPart === 'all'   || d.partNumber === fPart) &&
    (fRev === 'all'    || (d.drawingRevCode ?? '—') === fRev) &&
    (fRout === 'all'   || (d.routingRevCode ?? '—') === fRout) &&
    (fOp === 'all'     || (d.opNumber ?? '—') === fOp) &&
    (fType === 'all'   || d.fileTypeCode === fType) &&
    (fStatus === 'all' || d.status === fStatus) &&
    (!q || d.fileName.toLowerCase().includes(q.toLowerCase()))
  ), [docs, fPart, fRev, fRout, fOp, fType, fStatus, q])

  const anyFilter = [fPart, fRev, fRout, fOp, fType, fStatus].some(v => v !== 'all') || !!q
  const reset = () => { setFPart('all'); setFRev('all'); setFRout('all'); setFOp('all'); setFType('all'); setFStatus('all'); setQ('') }
  const pickPart = (v: string) => { setFPart(v); setFRev('all'); setFRout('all'); setFOp('all') }

  const pending  = filtered.filter(d => d.status === 'Pending').length
  const approved = filtered.filter(d => d.status === 'Approved').length
  const rejected = filtered.filter(d => d.status === 'Rejected').length

  const isApprover = mounted && user ? APPROVER_ROLES.includes(user.role) : false

  // Part list with doc counts (for left panel)
  const partList = useMemo(() => {
    const map = new Map<string, number>()
    for (const d of docs) {
      if (d.partNumber) map.set(d.partNumber, (map.get(d.partNumber) ?? 0) + 1)
    }
    return [...map.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(([p, n]) => ({ partNumber: p, count: n }))
  }, [docs])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title={t('title')} breadcrumb={t('breadcrumb')}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            {backHref && (
              <Link href={backHref}>
                <VABtn kind="ghost">{t('backLink')}</VABtn>
              </Link>
            )}
            <VABtn kind="ghost" onClick={() => setFStatus('Pending')}>{t('queueButton', { count: pending })}</VABtn>
            <VABtn kind="ghost" onClick={() => setBulkOpen(true)}>{t('bulkUpload.trigger')}</VABtn>
            {hasContext && (
              <VABtn kind="primary" onClick={() => setUploadForm({ fileTypeId: '', file: null, revision: '', description: '', machineType: '' })}>
                {t('uploadButton')}
              </VABtn>
            )}
          </div>
        } />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* ── LEFT: Part panel (like dimsheet) ─────────────────── */}
        <div className="va-scroll" style={{ width: 280, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
          {/* Search box — sticky */}
          <div style={{ padding: '12px 14px', borderBottom: `1px solid ${va.separator}`, position: 'sticky', top: 0, background: va.surface, zIndex: 1 }}>
            <div style={{ height: 34, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: va.text3 }}>
              <span>⌕</span>
              <input
                value={partSearch}
                onChange={e => { setPartSearch(e.target.value); setPartPage(1) }}
                placeholder={t('searchPlaceholder')}
                style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }}
              />
            </div>
          </div>

          {/* "All parts" row */}
          <div className="va-clickable" onClick={() => pickPart('all')}
            style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: fPart === 'all' ? `3px solid ${va.accent}` : '3px solid transparent', background: fPart === 'all' ? va.accentBg : va.surface }}>
            <div style={{ fontSize: 12.5, fontWeight: 600, color: fPart === 'all' ? va.accent : va.text2 }}>{t('filter.allParts')}</div>
            <div style={{ fontSize: 10.5, color: va.text3, fontFamily: va.mono, marginTop: 2 }}>{docs.length} docs</div>
          </div>

          {/* Part rows */}
          {allParts.map(p => {
            const on = fPart === p.partNumber
            const docCount = docs.filter(d => d.partNumber === p.partNumber).length
            return (
              <div key={p.id} className="va-clickable" onClick={() => pickPart(p.partNumber)}
                style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 700, color: va.text }}>{p.partNumber}</div>
                <div style={{ fontSize: 11.5, color: va.text2, marginTop: 3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{p.description}</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4, fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>
                  <span>{p.currentRoutingRevCode ?? '—'}</span>
                  <span>· {p.opCount} OP</span>
                  {docCount > 0 && <span style={{ color: va.primary }}>· {docCount} docs</span>}
                </div>
                <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2, fontFamily: va.mono }}>
                  {new Date(p.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')}
                </div>
              </div>
            )
          })}

          {/* Pagination */}
          {partTotal > 20 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 14px', borderTop: `1px solid ${va.separator}` }}>
              <button onClick={() => setPartPage(p => Math.max(1, p - 1))} disabled={partPage <= 1}
                style={{ border: `1px solid ${va.border}`, background: va.surface, borderRadius: 6, padding: '4px 10px', cursor: partPage <= 1 ? 'default' : 'pointer', color: va.text2 }}>←</button>
              <span style={{ fontSize: 11, color: va.text3, alignSelf: 'center' }}>{partPage} / {Math.ceil(partTotal / 20)}</span>
              <button onClick={() => setPartPage(p => p + 1)} disabled={allParts.length < 20}
                style={{ border: `1px solid ${va.border}`, background: va.surface, borderRadius: 6, padding: '4px 10px', cursor: allParts.length < 20 ? 'default' : 'pointer', color: va.text2 }}>→</button>
            </div>
          )}
        </div>

        {/* ── RIGHT: content ────────────────────────────────────── */}
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label={t('kpi.total')}    value={filtered.length} />
          <VAKpi label={t('kpi.pending')}  value={pending}  accent={va.warn} />
          <VAKpi label={t('kpi.approved')} value={approved} accent={va.ok}   />
          <VAKpi label={t('kpi.rejected')} value={rejected} accent={va.err}  />
        </div>

        {/* Pending banner */}
        {pending > 0 && (
          <div style={{ background: va.warnBg, border: `1px solid ${va.warn}44`, borderRadius: 11, padding: '14px 18px', display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{ width: 34, height: 34, borderRadius: '50%', background: '#fff', color: va.warn, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, flexShrink: 0 }}>◷</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: va.text }}>{t('pendingBanner.title', { count: pending })}</div>
              <div style={{ fontSize: 11.5, color: va.text2, marginTop: 1 }}>{t('pendingBanner.sub')}</div>
            </div>
            <VABtn kind="accent" onClick={() => setFStatus('Pending')}>{t('pendingBanner.action')}</VABtn>
          </div>
        )}

        {/* Upload form */}
        {uploadForm && (
          <VACard title={t('upload.title')} pad>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div>
                <label style={labelStyle}>{t('upload.fileType')}</label>
                <select style={inputStyle} value={uploadForm.fileTypeId}
                  onChange={e => setUploadForm(f => f && ({ ...f, fileTypeId: e.target.value }))}>
                  <option value="">{t('upload.fileTypePlaceholder')}</option>
                  {fileTypes.map(ft => (
                    <option key={ft.id} value={ft.id}>{ft.code} — {ft.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label style={labelStyle}>{t('upload.revision')}</label>
                <input style={inputStyle} placeholder={t('upload.revisionPlaceholder')} value={uploadForm.revision}
                  onChange={e => setUploadForm(f => f && ({ ...f, revision: e.target.value }))} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>{t('upload.file')}</label>
                <input type="file" style={{ ...inputStyle, padding: '5px 10px' }}
                  onChange={e => setUploadForm(f => f && ({ ...f, file: e.target.files?.[0] ?? null }))} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>{t('upload.description')}</label>
                <input style={inputStyle} value={uploadForm.description}
                  onChange={e => setUploadForm(f => f && ({ ...f, description: e.target.value }))} />
              </div>
            </div>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 14 }}>
              <VABtn kind="ghost" onClick={() => setUploadForm(null)}>{t('upload.cancel')}</VABtn>
              <VABtn kind="primary" disabled={uploading || !uploadForm.file || !uploadForm.fileTypeId} onClick={handleUpload}>
                {uploading ? t('upload.submitting') : t('upload.submit')}
              </VABtn>
            </div>
          </VACard>
        )}

        {/* Filter bar: Drawing Rev · Routing Rev · OP · Loại · Trạng thái · tìm tên file */}
        <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '13px 16px', boxShadow: va.shadow, display: 'flex', alignItems: 'flex-end', gap: 14, flexWrap: 'wrap' }}>
          <div>
            <Lbl>{t('filter.drawingRev')}</Lbl>
            <VACombobox value={fRev} onChange={setFRev} options={revOptions} placeholder={t('filter.allRevs')}
              style={{ fontFamily: va.mono, fontWeight: 600 }} />
          </div>
          <div>
            <Lbl>{t('filter.routingRev')}</Lbl>
            <VACombobox value={fRout} onChange={setFRout} options={routOptions} placeholder={t('filter.allRouting')}
              style={{ fontFamily: va.mono, fontWeight: 600 }} />
          </div>
          <div>
            <Lbl>{t('filter.op')}</Lbl>
            <VACombobox value={fOp} onChange={setFOp} options={opOptions} placeholder={t('filter.allOps')}
              style={{ fontFamily: va.mono, fontWeight: 600 }} />
          </div>
          <div style={{ width: 1, alignSelf: 'stretch', background: va.separator, margin: '0 2px' }} />
          <div>
            <Lbl>{t('filter.type')}</Lbl>
            <VACombobox value={fType} onChange={setFType} options={typeOptions} placeholder={t('filter.allTypes')} />
          </div>
          <div>
            <Lbl>{t('filter.status')}</Lbl>
            <VACombobox value={fStatus} onChange={setFStatus} options={statusOptions} placeholder={t('filter.allStatuses')} />
          </div>
          <div style={{ flex: 1, minWidth: 150 }}>
            <Lbl>{t('filter.search')}</Lbl>
            <input value={q} onChange={e => setQ(e.target.value)} placeholder={t('filter.searchPlaceholder')} style={{ ...selStyle, maxWidth: 'none', width: '100%' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, paddingBottom: 1 }}>
            <span style={{ fontFamily: va.mono, fontSize: 12, color: va.text2, fontWeight: 600 }}>{filtered.length}/{docs.length}</span>
            {anyFilter && <span className="va-clickable" onClick={reset} style={{ fontSize: 11.5, color: va.primary, fontWeight: 600 }}>{t('filter.clear')}</span>}
          </div>
        </div>

        {/* Type legend — click để lọc nhanh */}
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {Object.entries(T).map(([code, t]) => {
            const on = fType === code
            return (
              <span key={code} className="va-clickable" onClick={() => setFType(on ? 'all' : code)}
                style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '5px 10px', background: on ? t.color + '18' : va.surface, border: `1px solid ${on ? t.color : va.border}`, borderRadius: 7, fontSize: 11.5 }}>
                <span style={{ width: 8, height: 8, borderRadius: 2, background: t.color }} />
                <span style={{ fontFamily: va.mono, fontWeight: 700, color: t.color, fontSize: 10 }}>{code}</span>
                <span style={{ color: va.text2 }}>{t.label}</span>
              </span>
            )
          })}
        </div>

        {/* Doc list */}
        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {loading ? (
              <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>{t('loading')}</div>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {[t('table.headers.fileName'), t('table.headers.type'), t('table.headers.part'), t('table.headers.routing'), t('table.headers.op'), t('table.headers.rev'), t('table.headers.status'), t('table.headers.createdBy'), t('table.headers.size'), ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.length === 0 && (
                    <tr><td colSpan={10} style={{ padding: '36px 0', textAlign: 'center', color: va.text3, fontSize: 12.5 }}>
                      {docs.length === 0 ? t('table.empty') : (
                        <>{t('table.noMatch')} · <span className="va-clickable" onClick={reset} style={{ color: va.primary, fontWeight: 600 }}>{t('table.clearFilter')}</span></>
                      )}
                    </td></tr>
                  )}
                  {filtered.map(d => {
                    const color = T[d.fileTypeCode]?.color ?? FILE_TYPE_COLORS[d.fileTypeCode] ?? va.text2
                    const statusKind = STATUS_KIND[d.status] ?? ('neutral' as VaBadgeKind)
                    const statusLabel = STATUS_KIND[d.status] ? t(`status.${d.status}`) : d.status
                    return (
                      <tr key={d.id} className="va-row va-clickable">
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                            <span style={{ width: 26, height: 26, borderRadius: 6, background: color + '18', color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, flexShrink: 0 }}>◰</span>
                            <span style={{ fontFamily: va.mono, fontSize: 12, color: va.text }}>{d.fileName}</span>
                          </div>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <span style={{ fontSize: 10, fontWeight: 700, color, background: va.surface2, padding: '2px 7px', borderRadius: 4, fontFamily: va.mono, border: `1px solid ${color}33` }}>{d.fileTypeCode}</span>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, color: va.text2 }}>{d.partNumber ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, color: va.text2 }}>{d.routingRevCode ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, color: d.opNumber ? va.text2 : va.text3 }}>{d.opNumber ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{d.drawingRevCode ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <VABadge kind={statusKind} dot={d.status === 'Pending'}>{statusLabel}</VABadge>
                          {d.status === 'Rejected' && d.description && <div style={{ fontSize: 10.5, color: va.err, marginTop: 3 }}>⚠ {d.description}</div>}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>
                          <div style={{ fontSize: 12 }}>{d.createdByName}</div>
                          <div style={{ fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>{new Date(d.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')}</div>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, fontSize: 11.5 }}>{formatBytes(d.fileSizeBytes)}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                          {d.status === 'Pending' && isApprover ? (
                            <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                              <VABtn kind="ghost" style={{ height: 28, fontSize: 11, padding: '0 9px', color: va.err, borderColor: va.err + '55' }}
                                onClick={() => handleReject(d.id)} disabled={acting === d.id}>{t('actions.reject')}</VABtn>
                              <VABtn kind="primary" style={{ height: 28, fontSize: 11, padding: '0 9px', background: va.ok, borderColor: va.ok }}
                                onClick={() => handleApprove(d.id)} disabled={acting === d.id}>{t('actions.approve')}</VABtn>
                            </div>
                          ) : (
                            <span className="va-clickable" style={{ fontSize: 11, color: va.primary, fontWeight: 600 }}
                              onClick={() => handleView(d.id)}>{t('actions.view')}</span>
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
      </div>  {/* END right content */}
      </div>  {/* END flex row (left panel + right) */}

      <BulkUploadDialog open={bulkOpen} onClose={() => setBulkOpen(false)} onDone={load} />
    </div>
  )
}
