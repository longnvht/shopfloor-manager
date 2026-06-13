'use client'

import { useState, useEffect, useCallback, type CSSProperties } from 'react'
import Link from 'next/link'
import { useRouter, useSearchParams } from 'next/navigation'
import { useTranslations, useLocale } from 'next-intl'
import { api, type JobDto, type JobDetailDto, type PartOpDto, type ProductDto, type JobProgressDto } from '@/lib/api-client'
import { VATopbar, VABadge, VACard, VABtn, VAKpi } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { CreateJobDialog } from '@/components/jobs/create-job-dialog'
import { AddOpDialog } from '@/components/parts/add-op-dialog'

// ── Status helpers ─────────────────────────────────────────────────────────
function jobStatus(job: JobDto): { statusKey: 'complete' | 'overdue' | 'atRisk' | 'running'; kind: 'ok' | 'warn' | 'err' | 'neutral' | 'running' } {
  if (job.isComplete) return { statusKey: 'complete', kind: 'ok' }
  if (job.shipBy) {
    const days = Math.ceil((new Date(job.shipBy).getTime() - Date.now()) / 86400000)
    if (days < 0) return { statusKey: 'overdue', kind: 'err'  }
    if (days < 3) return { statusKey: 'atRisk',  kind: 'warn' }
  }
  return { statusKey: 'running', kind: 'neutral' }
}

function progressPct(job: JobDto) {
  if (!job.runQty || job.runQty === 0) return 0
  return Math.min(100, Math.round((job.completedCount ?? 0) / job.runQty * 100))
}

function progressColor(job: JobDto) {
  if (job.isComplete) return va.ok
  const s = jobStatus(job)
  if (s.kind === 'err') return va.err
  if (s.kind === 'warn') return va.warn
  return va.accent
}

// ── FAI OP selector modal ──────────────────────────────────────────────────
function FaiOpModal({ job, ops, onClose }: { job: JobDto; ops: PartOpDto[]; onClose: () => void }) {
  const router = useRouter()
  const t = useTranslations('jobs')
  const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50 }
  const box: React.CSSProperties = { background: va.surface, borderRadius: 12, padding: 24, width: 400, maxHeight: '80vh', overflow: 'auto', boxShadow: va.shadowLg }
  return (
    <div style={overlay} onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={box}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
          <div style={{ fontSize: 15, fontWeight: 700, color: va.text }}>{t('fai.modalTitle')}</div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 18, color: va.text3 }}>✕</button>
        </div>
        {ops.filter(o => !o.forJobOnly).map(op => (
          <div key={op.id} className="va-clickable va-row" onClick={() => { router.push(`/jobs/${job.id}/fai?opId=${op.id}`); onClose() }}
            style={{ padding: '12px 14px', borderBottom: `1px solid ${va.separator}`, display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ fontFamily: va.mono, fontWeight: 700, color: va.text, minWidth: 40 }}>{op.opNumber}</span>
            <span style={{ fontSize: 12.5, color: va.text2, flex: 1 }}>{op.opTypeName ?? ''} {op.description ? `· ${op.description}` : ''}</span>
            <span style={{ fontSize: 11, color: va.accent, fontWeight: 600 }}>{t('fai.open')}</span>
          </div>
        ))}
        {ops.filter(o => !o.forJobOnly).length === 0 && (
          <div style={{ fontSize: 12, color: va.text3 }}>{t('fai.empty')}</div>
        )}
      </div>
    </div>
  )
}

// ── Serial/Product state helpers ───────────────────────────────────────────
const SERIAL_META: Record<string, { fg: string; bg: string }> = {
  available:  { fg: va.text3,  bg: va.surface2 },
  claimed:    { fg: va.warn,   bg: va.warnBg },
  inprogress: { fg: va.active, bg: va.activeBg },
  complete:   { fg: va.ok,     bg: va.okBg },
}

function productState(p: ProductDto): keyof typeof SERIAL_META {
  if (p.isComplete) return 'complete'
  if (p.sessionStatus === 'inprogress') return 'inprogress'
  if (p.sessionStatus === 'claimed') return 'claimed'
  return 'available'
}

// ── Job detail panel ───────────────────────────────────────────────────────
function JobDetail({ job: jobSummary }: { job: JobDto }) {
  const t = useTranslations('jobs')
  const locale = useLocale()
  const [detail, setDetail] = useState<JobDetailDto | null>(null)
  const [progress, setProgress] = useState<JobProgressDto | null>(null)
  const [ncrCount, setNcrCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [showFaiPicker, setShowFaiPicker] = useState(false)
  const [showAddOp, setShowAddOp] = useState(false)
  const [generating, setGenerating] = useState(false)

  const loadDetail = useCallback(() => {
    setLoading(true); setDetail(null); setProgress(null)
    Promise.all([
      api.jobs.get(jobSummary.id),
      api.jobs.progress(jobSummary.id),
      api.ncrs.list(1, undefined, jobSummary.id),
    ]).then(([jobRes, progRes, ncrRes]) => {
      if (jobRes.success) setDetail(jobRes.data)
      if (progRes.success) setProgress(progRes.data)
      setNcrCount(ncrRes.pagination?.total ?? 0)
      setLoading(false)
    })
  }, [jobSummary.id])

  useEffect(() => { loadDetail() }, [loadDetail])

  async function handleGenerateProducts() {
    if (!jobSummary.runQty) { alert(t('detail.noRunQty')); return }
    if (!confirm(t('detail.generateConfirm', { qty: jobSummary.runQty, job: jobSummary.jobNumber }))) return
    setGenerating(true)
    const res = await api.jobs.generateProducts(jobSummary.id, jobSummary.runQty)
    if (res.success) loadDetail()
    else alert(res.error ?? t('detail.errorGenerate'))
    setGenerating(false)
  }

  const s = jobStatus(jobSummary)

  const totalDim    = progress?.totalDim ?? 0
  const completeDim = progress?.completeDim ?? 0
  const passDim     = progress?.passDim ?? 0
  const failDim     = progress?.failDim ?? 0
  const pendDim     = Math.max(0, totalDim - completeDim)
  const dimPct      = totalDim > 0 ? Math.round(completeDim / totalDim * 100) : 0
  const dimPer      = jobSummary.runQty ? Math.round(totalDim / jobSummary.runQty) : 0

  const templateOps = detail?.operations.filter(o => !o.forJobOnly) ?? []
  const customOps   = detail?.operations.filter(o => o.forJobOnly) ?? []
  const products    = detail?.products ?? []
  const shown       = Math.min(products.length, 48)

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 14 }}>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 24, fontWeight: 700, color: va.text }}>{jobSummary.jobNumber}</h2>
            <VABadge kind={s.kind} dot>{t(`status.${s.statusKey}`)}</VABadge>
          </div>
          <div style={{ fontSize: 13, color: va.text2, marginTop: 6 }}>
            <Link href={`/parts/${jobSummary.partId}/operations`} style={{ color: va.primary, fontWeight: 600, textDecoration: 'none' }}>
              {jobSummary.partNumber} · Rev {jobSummary.revCode}
            </Link>
            <span> · {jobSummary.runQty ?? '—'} {t('detail.pcsSuffix')}</span>
            {jobSummary.shipBy && <span> · {t('detail.shipBy', { date: new Date(jobSummary.shipBy).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US') })}</span>}
          </div>
          <div style={{ fontSize: 11.5, color: va.text3, marginTop: 4, fontFamily: va.mono }}>
            {t('detail.createdLine', { date: new Date(jobSummary.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US'), routing: jobSummary.routingRevCode })}
          </div>
        </div>
        <Link href={`/parts/${jobSummary.partId}/operations`}>
          <VABtn kind="primary">{t('detail.partRoutingLink')}</VABtn>
        </Link>
      </div>

      {loading ? (
        <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('loading')}</div>
      ) : !detail ? (
        <div style={{ padding: 16, fontSize: 12, color: va.err }}>{t('detail.errorLoad')}</div>
      ) : (
        <>
          {/* KPI strip */}
          <div style={{ display: 'flex', gap: 13, flexWrap: 'wrap' }}>
            <VAKpi label={t('detail.kpi.progress')} value={`${dimPct}%`} accent={s.kind === 'err' ? va.err : va.ok} />
            <VAKpi label={t('detail.kpi.produced')} value={`${jobSummary.completedCount ?? 0}`} sub={`/ ${jobSummary.runQty ?? '—'} ${t('detail.pcsSuffix')}`} />
            <VAKpi label={t('detail.kpi.passFai')} value={`${passDim}`} sub={`/ ${totalDim} dim`} accent={va.ok} />
            <VAKpi label={t('detail.kpi.fail')} value={`${failDim}`} accent={failDim > 0 ? va.err : va.text} />
            <VAKpi label={t('detail.kpi.ncr')} value={`${ncrCount}`} accent={ncrCount > 0 ? va.err : va.text} />
          </div>

          {/* Tiến độ đo kiểm */}
          <VACard title={t('detail.progress.title')} sub={t('detail.progress.sub', { total: totalDim, qty: jobSummary.runQty ?? 0, perSerial: dimPer })}>
            <div style={{ display: 'flex', height: 12, borderRadius: 6, overflow: 'hidden', background: va.surface2 }}>
              {totalDim > 0 && (
                <>
                  <div style={{ width: `${passDim / totalDim * 100}%`, background: va.ok }} />
                  <div style={{ width: `${failDim / totalDim * 100}%`, background: va.err }} />
                </>
              )}
            </div>
            <div style={{ display: 'flex', gap: 22, marginTop: 12, fontSize: 12, flexWrap: 'wrap' }}>
              {[
                { c: va.ok, label: t('detail.progress.pass'), v: passDim },
                { c: va.err, label: t('detail.progress.fail'), v: failDim },
                { c: va.border, label: t('detail.progress.pending'), v: pendDim },
                { c: va.text2, label: t('detail.progress.completeDim'), v: completeDim },
              ].map(x => (
                <div key={x.label} style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
                  <span style={{ width: 10, height: 10, borderRadius: 3, background: x.c, flexShrink: 0 }} />
                  <span style={{ color: va.text2 }}>{x.label}</span>
                  <span style={{ fontFamily: va.mono, fontWeight: 700, color: va.text }}>{x.v}</span>
                </div>
              ))}
            </div>
          </VACard>

          {/* Routing (tham chiếu) + Custom OPs */}
          {(templateOps.length > 0 || customOps.length > 0) && (
            <VACard
              title={t('detail.routing.title')}
              sub={customOps.length > 0
                ? t('detail.routing.subWithCustom', { count: templateOps.length, customCount: customOps.length })
                : t('detail.routing.subInherited', { count: templateOps.length })}
              pad={false}
              right={
                <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                  <Link href={`/parts/${jobSummary.partId}/operations`} style={{ fontSize: 11, color: va.primary, fontWeight: 600, textDecoration: 'none' }}>
                    {t('detail.routing.openInEngineering')}
                  </Link>
                  <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }} onClick={() => setShowAddOp(true)}>{t('detail.routing.addCustomOp')}</VABtn>
                </div>
              }
            >
              <div className="va-scroll" style={{ display: 'flex', alignItems: 'center', padding: '16px 18px', overflowX: 'auto', gap: 0 }}>
                {templateOps.map((op, i, arr) => (
                  <div key={op.id} style={{ display: 'flex', alignItems: 'center' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, minWidth: 76, flexShrink: 0 }}>
                      <div style={{ minWidth: 52, height: 38, borderRadius: 8, background: op.isComplete ? va.ok : va.primary, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 13.5 }}>{op.opNumber}</div>
                      <span style={{ fontSize: 10.5, color: va.text2, fontWeight: 600, textAlign: 'center', maxWidth: 74, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{op.opTypeName ?? ''}</span>
                      {op.isComplete && <span style={{ fontSize: 9, color: va.ok, fontWeight: 700 }}>{t('detail.routing.done')}</span>}
                    </div>
                    {i < arr.length - 1 && <div style={{ width: 26, height: 2, background: va.border, flexShrink: 0 }} />}
                  </div>
                ))}

                {customOps.length > 0 && (
                  <>
                    {templateOps.length > 0 && <div style={{ width: 1, alignSelf: 'stretch', background: va.borderStr, margin: '0 16px', flexShrink: 0 }} />}
                    {customOps.map((op, i, arr) => (
                      <div key={op.id} style={{ display: 'flex', alignItems: 'center' }}>
                        <Link href={`/jobs/${jobSummary.id}/operations?opId=${op.id}`} style={{ textDecoration: 'none' }}>
                          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, minWidth: 76, flexShrink: 0, cursor: 'pointer' }}>
                            <div style={{ minWidth: 52, height: 38, borderRadius: 8, border: `2px dashed ${va.accent}`, background: va.accentBg, color: va.accent, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 13.5 }}>{op.opNumber}</div>
                            <span style={{ fontSize: 10.5, color: va.accent, fontWeight: 600, textAlign: 'center', maxWidth: 74, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{op.description ?? op.opTypeName ?? '—'}</span>
                            <span style={{ fontSize: 9, color: va.accent, fontWeight: 700 }}>{t('detail.routing.customLabel')}</span>
                          </div>
                        </Link>
                        {i < arr.length - 1 && <div style={{ width: 26, height: 2, background: va.accentBg, flexShrink: 0 }} />}
                      </div>
                    ))}
                  </>
                )}
              </div>
              <div style={{ padding: '10px 18px', borderTop: `1px solid ${va.separator}`, fontSize: 11, color: va.text3, background: va.surface2 }}>
                {t('detail.routing.footerInfo')}<strong style={{ color: va.text2 }}>{t('detail.routing.footerInfoStrong')}</strong>{t('detail.routing.footerInfoAfter')}
                {customOps.length > 0 && <>{t('detail.routing.footerCustomInfo')}<strong style={{ color: va.accent }}>{t('detail.routing.footerCustomInfoStrong')}</strong>{t('detail.routing.footerCustomInfoAfter')}</>}
              </div>
            </VACard>
          )}

          {/* Serial / Product */}
          <VACard
            title={t('detail.serial.title')}
            sub={products.length > shown ? t('detail.serial.shown', { shown, total: products.length }) : t('detail.serial.total', { count: products.length })}
            right={
              <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                <div style={{ display: 'flex', gap: 12, fontSize: 11, color: va.text2, flexWrap: 'wrap' }}>
                  {Object.entries(SERIAL_META).map(([k, m]) => (
                    <span key={k} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                      <span style={{ width: 9, height: 9, borderRadius: 2, background: m.fg }} />{t(`serialStatus.${k}`)}
                    </span>
                  ))}
                </div>
                {products.length === 0 && jobSummary.runQty && (
                  <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }} onClick={handleGenerateProducts} disabled={generating}>
                    {generating ? t('detail.serial.generating') : t('detail.serial.generate', { qty: jobSummary.runQty })}
                  </VABtn>
                )}
                <VABtn kind="accent" style={{ height: 28, fontSize: 11 }} onClick={() => setShowFaiPicker(true)}>{t('detail.serial.faiSheet')}</VABtn>
              </div>
            }
          >
            {products.length === 0 ? (
              <div style={{ padding: 4, fontSize: 12, color: va.text3 }}>
                {jobSummary.runQty ? t('detail.serial.emptyWithRunQty') : t('detail.serial.emptyNoRunQty')}
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(92px, 1fr))', gap: 10 }}>
                {products.slice(0, shown).map(p => {
                  const st = productState(p)
                  const m = SERIAL_META[st]
                  return (
                    <div key={p.id} style={{ border: `1px solid ${va.border}`, borderTop: `3px solid ${m.fg}`, borderRadius: 8, padding: '12px 8px', textAlign: 'center', background: va.surface }}>
                      <div style={{ fontFamily: va.mono, fontSize: 18, fontWeight: 700, color: va.text }}>{p.serialNumber}</div>
                      <div style={{ fontSize: 9.5, fontWeight: 600, color: m.fg, marginTop: 4, textTransform: 'uppercase', letterSpacing: 0.3 }}>{t(`serialStatus.${st}`)}</div>
                    </div>
                  )
                })}
                {products.length > shown && (
                  <div style={{ border: `1px dashed ${va.borderStr}`, borderRadius: 8, padding: '12px 8px', textAlign: 'center', background: va.surface2, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
                    <div style={{ fontFamily: va.mono, fontSize: 14, fontWeight: 700, color: va.text2 }}>+{products.length - shown}</div>
                    <div style={{ fontSize: 9.5, color: va.text3, marginTop: 3 }}>{t('detail.serial.more')}</div>
                  </div>
                )}
              </div>
            )}
          </VACard>

        </>
      )}

      {/* FAI OP picker modal */}
      {showFaiPicker && detail && (
        <FaiOpModal
          job={jobSummary}
          ops={detail.operations}
          onClose={() => setShowFaiPicker(false)}
        />
      )}

      {/* Add custom OP (ForJobOnly) */}
      {showAddOp && (
        <AddOpDialog
          open={showAddOp}
          jobId={jobSummary.id}
          onClose={() => setShowAddOp(false)}
          onCreated={() => loadDetail()}
        />
      )}
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
export default function JobsPage() {
  const t = useTranslations('jobs')
  const locale = useLocale()
  const searchParams = useSearchParams()
  const jobIdParam = searchParams.get('jobId')

  const [jobs, setJobs]         = useState<JobDto[]>([])
  const [total, setTotal]       = useState(0)
  const [page, setPage]         = useState(1)
  const [search, setSearch]     = useState('')
  const [loading, setLoading]   = useState(true)
  const [selJob, setSelJob]     = useState<JobDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.jobs.list(page, search || undefined)
    if (res.success && res.data) {
      setJobs(res.data)
      setTotal(res.pagination?.total ?? 0)
      if (!selJob && res.data.length > 0) setSelJob(res.data[0])
    }
    setLoading(false)
  }, [page, search]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  // Deep-link: /jobs?jobId=123 (vd. quay lại từ FAI/Documents) → chọn job đó
  useEffect(() => {
    if (!jobIdParam) return
    api.jobs.get(Number(jobIdParam)).then(res => {
      if (res.success) setSelJob(res.data)
    })
  }, [jobIdParam])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={t('title')}
        breadcrumb={t('breadcrumb')}
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>{t('createButton')}</VABtn>}
      />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* ── LEFT: Job list ──────────────────────────────────────── */}
        <div className="va-scroll" style={{ width: 320, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
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

          {jobs.map(job => {
            const on  = selJob?.id === job.id
            const s   = jobStatus(job)
            const pct = progressPct(job)

            return (
              <div key={job.id} className="va-clickable" onClick={() => setSelJob(job)}
                style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                {/* Job number + status */}
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, marginBottom: 5 }}>
                  <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 700, color: va.text }}>{job.jobNumber}</span>
                  <span style={{ fontSize: 12, color: va.text2, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{job.partNumber}</span>
                  <VABadge kind={s.kind}>{t(`status.${s.statusKey}`)}</VABadge>
                </div>
                {/* Progress bar */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <div style={{ flex: 1, height: 5, background: va.surface3, borderRadius: 3, overflow: 'hidden' }}>
                    <div style={{ height: '100%', width: `${pct}%`, background: progressColor(job) }} />
                  </div>
                  <span style={{ fontFamily: va.mono, fontSize: 10.5, color: va.text2, minWidth: 72, textAlign: 'right' }}>
                    {job.completedCount ?? 0}/{job.runQty ?? '?'} · {job.shipBy ? new Date(job.shipBy).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US', { day: '2-digit', month: '2-digit' }) : '—'}
                  </span>
                </div>
              </div>
            )
          })}

          {/* Pagination */}
          {total > 20 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 14px', borderTop: `1px solid ${va.separator}` }}>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}>←</VABtn>
              <span style={{ fontSize: 11, color: va.text3, alignSelf: 'center' }}>{page} / {Math.ceil(total / 20)}</span>
              <VABtn kind="ghost" style={{ height: 28 }} onClick={() => setPage(p => p + 1)} disabled={jobs.length < 20}>→</VABtn>
            </div>
          )}
        </div>

        {/* ── RIGHT: Detail ───────────────────────────────────────── */}
        {selJob
          ? <JobDetail key={selJob.id} job={selJob} />
          : <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
              {t('selectPrompt')}
            </div>
        }
      </div>

      <CreateJobDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={load} />
    </div>
  )
}
