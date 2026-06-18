'use client'

import { useEffect, useState } from 'react'
import { api, type JobDto } from '@/lib/api-client'
import { VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

type Props = {
  selectedJobId: number | null
  onSelect: (jobId: number, job: JobDto) => void
}

type JobStatus = 'on-track' | 'at-risk' | 'complete' | 'overdue'

function deriveStatus(job: JobDto): JobStatus {
  if (job.isComplete) return 'complete'
  if (!job.shipBy) return 'on-track'
  const today = new Date(); today.setHours(0, 0, 0, 0)
  const shipBy = new Date(job.shipBy); shipBy.setHours(0, 0, 0, 0)
  const daysLeft = Math.round((shipBy.getTime() - today.getTime()) / 86_400_000)
  if (daysLeft < 0) return 'overdue'
  if (daysLeft <= 3) return 'at-risk'
  return 'on-track'
}

const STATUS_BADGE: Record<JobStatus, { kind: 'ok' | 'warn' | 'err' | 'neutral'; label: string }> = {
  'on-track': { kind: 'ok', label: 'Đúng hạn' },
  'at-risk': { kind: 'warn', label: 'Rủi ro' },
  complete: { kind: 'neutral', label: 'Xong' },
  overdue: { kind: 'err', label: 'Trễ' },
}

function completion(job: JobDto): number {
  if (!job.runQty || job.runQty <= 0) return job.completedCount > 0 ? 100 : 0
  return Math.round((job.completedCount / job.runQty) * 100)
}

export function FaiJobList({ selectedJobId, onSelect }: Props) {
  const [search, setSearch] = useState('')
  const [jobs, setJobs] = useState<JobDto[]>([])

  useEffect(() => {
    const t = setTimeout(() => {
      api.jobs.list(1, search || undefined).then(res => {
        if (res.success && res.data) setJobs(res.data)
      })
    }, 250)
    return () => clearTimeout(t)
  }, [search])

  return (
    <div style={{ width: 268, flexShrink: 0, background: va.surface, borderRight: `1px solid ${va.border}`, display: 'flex', flexDirection: 'column', height: '100%' }}>
      <div style={{ padding: '15px 16px 12px', borderBottom: `1px solid ${va.separator}`, flexShrink: 0 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8, marginBottom: 11 }}>
          <span style={{ fontSize: 13, fontWeight: 700, color: va.text }}>Lệnh sản xuất</span>
          <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text3 }}>{jobs.length}</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, height: 34, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 10px' }}>
          <span style={{ color: va.text3, fontSize: 13 }}>⌕</span>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Tìm job, part…"
            style={{ flex: 1, border: 'none', outline: 'none', background: 'transparent', fontSize: 12.5, color: va.text, fontFamily: va.font }} />
        </div>
      </div>
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 8 }}>
        {jobs.length === 0 && (
          <div style={{ padding: 24, textAlign: 'center', color: va.text3, fontSize: 12 }}>Không có job khớp tìm kiếm.</div>
        )}
        {jobs.map(j => {
          const on = j.id === selectedJobId
          const status = STATUS_BADGE[deriveStatus(j)]
          const pct = completion(j)
          return (
            <div key={j.id} className="va-clickable" onClick={() => onSelect(j.id, j)}
              style={{ padding: '10px 11px', borderRadius: 8, marginBottom: 3, background: on ? va.accentBg : 'transparent', boxShadow: on ? `inset 0 0 0 1px ${va.accentLt}` : 'none' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 7, marginBottom: 4 }}>
                <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 700, color: va.primary }}>{j.jobNumber}</span>
                <span style={{ marginLeft: 'auto' }}><VABadge kind={status.kind}>{status.label}</VABadge></span>
              </div>
              <div style={{ fontSize: 12, color: va.text2, marginBottom: 7 }}>
                <span style={{ fontWeight: 600, color: va.text }}>{j.partNumber}</span> · Rev {j.revCode}
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div style={{ flex: 1, height: 4, background: va.surface3, borderRadius: 3, overflow: 'hidden' }}>
                  <div style={{ width: `${pct}%`, height: '100%', background: va.ok }} />
                </div>
                <span style={{ fontFamily: va.mono, fontSize: 10, color: va.text3 }}>{pct}%</span>
                {j.openNcrCount > 0 && <span style={{ fontFamily: va.mono, fontSize: 9.5, fontWeight: 700, color: va.err }}>⚑{j.openNcrCount}</span>}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

export default FaiJobList
