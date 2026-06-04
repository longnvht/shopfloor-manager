'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { api, type JobDto, type JobDetailDto } from '@/lib/api-client'
import { VATopbar, VABadge, VACard, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { CreateJobDialog } from '@/components/jobs/create-job-dialog'

// ── Status helpers ─────────────────────────────────────────────────────────
function jobStatus(job: JobDto): { label: string; kind: 'ok' | 'warn' | 'err' | 'neutral' | 'running' } {
  if (job.isComplete) return { label: 'Hoàn thành', kind: 'ok' }
  if (job.shipBy) {
    const days = Math.ceil((new Date(job.shipBy).getTime() - Date.now()) / 86400000)
    if (days < 0) return { label: 'Quá hạn',    kind: 'err'  }
    if (days < 3) return { label: 'Có nguy cơ', kind: 'warn' }
  }
  return { label: 'Đang chạy', kind: 'neutral' }
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

// ── Job detail panel ───────────────────────────────────────────────────────
function JobDetail({ job: jobSummary }: { job: JobDto }) {
  const [detail, setDetail] = useState<JobDetailDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setLoading(true); setDetail(null)
    api.jobs.get(jobSummary.id).then(res => {
      if (res.success) setDetail(res.data)
      setLoading(false)
    })
  }, [jobSummary.id])

  const s = jobStatus(jobSummary)
  const pct = progressPct(jobSummary)

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{jobSummary.jobNumber}</h2>
            <VABadge kind={s.kind} dot>{s.label}</VABadge>
          </div>
          <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4, display: 'flex', gap: 16 }}>
            <span style={{ fontFamily: va.mono }}>{jobSummary.partNumber}</span>
            <span>Rev <strong>{jobSummary.revCode}</strong></span>
            <span>Routing <strong>{jobSummary.routingRevCode}</strong></span>
            {jobSummary.shipBy && <span>Giao <strong>{new Date(jobSummary.shipBy).toLocaleDateString('vi-VN')}</strong></span>}
          </div>
        </div>
        <Link href={`/jobs/${jobSummary.id}`}>
          <VABtn kind="ghost">Chi tiết →</VABtn>
        </Link>
      </div>

      {/* Progress */}
      <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '14px 18px', boxShadow: va.shadow }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, fontSize: 12, color: va.text2 }}>
          <span style={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5, fontSize: 10.5 }}>Tiến độ sản xuất</span>
          <span style={{ fontFamily: va.mono, fontWeight: 600, color: va.text }}>{jobSummary.completedCount ?? 0} / {jobSummary.runQty ?? '—'} sp hoàn thành ({pct}%)</span>
        </div>
        <div style={{ height: 10, background: va.surface2, borderRadius: 5, overflow: 'hidden' }}>
          <div style={{ height: '100%', width: `${pct}%`, background: progressColor(jobSummary), transition: 'width 0.3s' }} />
        </div>
      </div>

      {loading ? (
        <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>
      ) : !detail ? (
        <div style={{ padding: 16, fontSize: 12, color: va.err }}>Không tải được chi tiết.</div>
      ) : (
        <>
          {/* OP Flow */}
          {detail.operations.filter(o => !o.forJobOnly).length > 0 && (
            <div className="va-scroll" style={{ display: 'flex', alignItems: 'center', padding: '13px 16px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, boxShadow: va.shadow, overflowX: 'auto', gap: 0 }}>
              {detail.operations.filter(o => !o.forJobOnly).map((op, i, arr) => (
                <div key={op.id} style={{ display: 'flex', alignItems: 'center' }}>
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4, minWidth: 68, flexShrink: 0 }}>
                    <div style={{ minWidth: 48, height: 34, borderRadius: 7, background: op.isComplete ? va.ok : va.primary, color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 12.5 }}>{op.opNumber}</div>
                    <span style={{ fontSize: 10, color: va.text2, textAlign: 'center', maxWidth: 66 }}>{op.opTypeName ?? ''}</span>
                    {op.isComplete && <span style={{ fontSize: 9, color: va.ok, fontWeight: 700 }}>✓</span>}
                  </div>
                  {i < arr.length - 1 && <div style={{ width: 20, height: 2, background: va.border, flexShrink: 0 }} />}
                </div>
              ))}
            </div>
          )}

          {/* Product / Serial grid */}
          <VACard
            title={`Sản phẩm — ${detail.products.length} serial`}
            pad={false}
            right={
              <Link href={`/jobs/${jobSummary.id}/fai`}>
                <VABtn kind="accent" style={{ height: 28, fontSize: 11 }}>FAI Sheet</VABtn>
              </Link>
            }
          >
            {detail.products.length === 0 ? (
              <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>
                Chưa có serial. <span className="va-clickable" style={{ color: va.primary, fontWeight: 600 }}>Tạo products →</span>
              </div>
            ) : (
              <div style={{ padding: 14, display: 'flex', flexWrap: 'wrap', gap: 7 }}>
                {detail.products.map(p => (
                  <div key={p.id} style={{
                    padding: '6px 11px', borderRadius: 7, fontSize: 12, fontFamily: va.mono, fontWeight: 600,
                    background: p.isComplete ? va.okBg    : va.surface2,
                    color:      p.isComplete ? va.ok      : va.text2,
                    border:     `1px solid ${p.isComplete ? va.ok + '44' : va.border}`,
                    cursor: 'default',
                  }}>
                    {p.serialNumber}
                    {p.isComplete && <span style={{ marginLeft: 4, fontSize: 10 }}>✓</span>}
                  </div>
                ))}
              </div>
            )}
          </VACard>

          {/* ForJobOnly OPs (nếu có) */}
          {detail.operations.filter(o => o.forJobOnly).length > 0 && (
            <VACard title={`Custom OPs — chỉ job này (${detail.operations.filter(o => o.forJobOnly).length})`} pad={false}>
              <div style={{ padding: '0 4px' }}>
                {detail.operations.filter(o => o.forJobOnly).map(op => (
                  <div key={op.id} style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, display: 'flex', alignItems: 'center', gap: 12 }}>
                    <span style={{ fontFamily: va.mono, fontSize: 12, fontWeight: 700, color: va.text, minWidth: 40 }}>{op.opNumber}</span>
                    <span style={{ fontSize: 12, color: va.text2, flex: 1 }}>{op.description ?? op.opTypeName ?? '—'}</span>
                    <Link href={`/jobs/${jobSummary.id}/documents?opId=${op.id}&opNumber=${op.opNumber}`}>
                      <VABtn kind="ghost" style={{ height: 26, fontSize: 11 }}>Tài liệu</VABtn>
                    </Link>
                  </div>
                ))}
              </div>
            </VACard>
          )}
        </>
      )}
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
export default function JobsPage() {
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

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Lệnh SX & Sản phẩm"
        breadcrumb="Sản xuất › Job · Serial · Tiến độ"
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>+ Tạo Job</VABtn>}
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
                placeholder="Tìm job, part…"
                style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }}
              />
            </div>
          </div>

          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>}

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
                  <VABadge kind={s.kind}>{s.label}</VABadge>
                </div>
                {/* Progress bar */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <div style={{ flex: 1, height: 5, background: va.surface3, borderRadius: 3, overflow: 'hidden' }}>
                    <div style={{ height: '100%', width: `${pct}%`, background: progressColor(job) }} />
                  </div>
                  <span style={{ fontFamily: va.mono, fontSize: 10.5, color: va.text2, minWidth: 72, textAlign: 'right' }}>
                    {job.completedCount ?? 0}/{job.runQty ?? '?'} · {job.shipBy ? new Date(job.shipBy).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }) : '—'}
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
              Chọn một Job để xem chi tiết
            </div>
        }
      </div>

      <CreateJobDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={load} />
    </div>
  )
}
