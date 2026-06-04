'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type GageDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge, VASeg } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const STATUS_META: Record<string, { label: string; kind: VaBadgeKind }> = {
  VALID:    { label: 'Hợp lệ',          kind: 'ok'      },
  EXPIRED:  { label: 'Hết hạn',         kind: 'err'     },
  DAMAGED:  { label: 'Hư hỏng',         kind: 'err'     },
  BORROWED: { label: 'Đang mượn',       kind: 'running' },
  CALIB:    { label: 'Đang hiệu chuẩn', kind: 'warn'    },
}

type Filter = 'all' | 'valid' | 'borrowed' | 'due'

export default function GagesPage() {
  const [gages, setGages]   = useState<GageDto[]>([])
  const [filter, setFilter] = useState<Filter>('all')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    let res
    if (filter === 'due') {
      res = await api.gages.calibDue(60)
    } else {
      res = await api.gages.list({
        search: search || undefined,
        statusCode:  filter === 'valid'    ? 'VALID'    : undefined,
        isBorrowed:  filter === 'borrowed' ? true       : undefined,
      })
    }
    if (res.success && res.data) setGages(res.data)
    setLoading(false)
  }, [filter, search])

  useEffect(() => { load() }, [load])

  const counts = gages.reduce<Record<string, number>>((a, g) => {
    a[g.statusCode] = (a[g.statusCode] ?? 0) + 1; return a
  }, {})

  const dueColor = (d: number | null) => {
    if (d == null) return va.text3
    return d < 0 ? va.err : d <= 30 ? va.warn : va.text2
  }

  async function handleBorrow(gage: GageDto) {
    // Simplified: borrow by current user (manager=1 for now, proper UI later)
    const res = await api.gages.borrow({ gageId: gage.id, borrowerId: 1, managerId: 1 })
    if (res.success) load()
    else alert(res.error ?? 'Lỗi mượn gage')
  }

  async function handleReturn(gage: GageDto) {
    // Get active transaction id from gage — simplified lookup via reload
    // For now just reload; proper return needs transaction id
    const txRes = await fetch(`/api/v1/borrow-transactions?gageId=${gage.id}&status=0`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('auth-token')}` },
    })
    const txData = await txRes.json()
    const tx = txData?.data?.[0]
    if (!tx) { alert('Không tìm thấy giao dịch mượn'); return }
    const res = await api.gages.returnGage(tx.id)
    if (res.success) load()
    else alert(res.error ?? 'Lỗi trả gage')
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Dụng cụ đo (Gage)" breadcrumb="Chất lượng › Quản lý dụng cụ"
        right={<><VABtn kind="ghost" style={{ marginRight: 8 }}>⬆ Import</VABtn><VABtn kind="primary">+ Thêm gage</VABtn></>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng gage"          value={gages.length} />
          <VAKpi label="Hợp lệ"             value={counts.VALID    ?? 0} accent={va.ok}     />
          <VAKpi label="Đang mượn"           value={counts.BORROWED ?? 0} accent={va.active} />
          <VAKpi label="Hết hạn / hỏng"      value={(counts.EXPIRED ?? 0) + (counts.DAMAGED ?? 0)} accent={va.err} />
          <VAKpi label="Đang hiệu chuẩn"     value={counts.CALIB    ?? 0} accent={va.warn}   />
        </div>

        {/* Toolbar */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <VASeg
            value={filter}
            onChange={(v) => setFilter(v as Filter)}
            options={[
              { id: 'all',      label: 'Tất cả'       },
              { id: 'valid',    label: 'Hợp lệ'        },
              { id: 'borrowed', label: 'Đang mượn'     },
              { id: 'due',      label: 'Sắp hết hạn'   },
            ]}
          />
          <div style={{ height: 34, flex: 1, maxWidth: 280, background: va.surface, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: va.text3 }}>
            <span>⌕</span>
            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Tìm Gage No, mô tả…"
              style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }}
            />
          </div>
        </div>

        {/* Table */}
        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {loading ? (
              <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Đang tải…</div>
            ) : gages.length === 0 ? (
              <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Không có dụng cụ đo nào.</div>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {['Gage No', 'Mô tả', 'Loại', 'Range', 'Cat', 'Trạng thái', 'Hạn HC', 'Vị trí', ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {gages.map(g => {
                    const sm = STATUS_META[g.statusCode] ?? { label: g.statusCode, kind: 'neutral' as VaBadgeKind }
                    return (
                      <tr key={g.id} className="va-row va-clickable">
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontWeight: 700, color: va.text }}>{g.gageNo}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontWeight: 500 }}>{g.description}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{g.gageTypeName ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{g.measuringRange ?? '—'}</td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          {g.categoryCode
                            ? <span style={{ fontSize: 9.5, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 6px', borderRadius: 3, fontFamily: va.mono, border: `1px solid ${va.border}` }}>{g.categoryCode}</span>
                            : <span style={{ color: va.text3 }}>—</span>}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                          <VABadge kind={sm.kind} dot>{sm.label}</VABadge>
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono }}>
                          {g.dueDate
                            ? <>
                                <div style={{ color: va.text }}>{g.dueDate}</div>
                                <div style={{ fontSize: 10.5, color: dueColor(g.daysRemaining), fontWeight: 600 }}>
                                  {g.daysRemaining != null ? (g.daysRemaining < 0 ? `quá ${-g.daysRemaining}d` : `còn ${g.daysRemaining}d`) : '—'}
                                </div>
                              </>
                            : <span style={{ color: va.text3 }}>—</span>}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, fontSize: 11.5 }}>
                          {g.currentLocationDesc ?? '—'}
                        </td>
                        <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                          {g.isBorrowed
                            ? <VABtn kind="ghost" style={{ height: 28, fontSize: 11, padding: '0 10px' }} onClick={() => handleReturn(g)}>Trả</VABtn>
                            : g.isValid
                              ? <VABtn kind="accent" style={{ height: 28, fontSize: 11, padding: '0 10px' }} onClick={() => handleBorrow(g)}>Mượn</VABtn>
                              : <span style={{ fontSize: 11, color: va.text3 }}>—</span>}
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
