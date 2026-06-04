'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type PlanningItemDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn } from '@/components/va'
import { VASeg } from '@/components/va/seg'
import { va } from '@/lib/va-tokens'

type ViewMode = 'day' | 'week' | 'month'

function getWeekRange(offset = 0): { start: Date; end: Date; label: string } {
  const now = new Date()
  const mon = new Date(now)
  mon.setDate(now.getDate() - now.getDay() + 1 + offset * 7)
  mon.setHours(0, 0, 0, 0)
  const sun = new Date(mon); sun.setDate(mon.getDate() + 6); sun.setHours(23, 59, 59, 999)
  const fmt = (d: Date) => d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
  return { start: mon, end: sun, label: `${fmt(mon)} – ${fmt(sun)}` }
}

export default function PlanningPage() {
  const [items, setItems]   = useState<PlanningItemDto[]>([])
  const [loading, setLoading] = useState(true)
  const [view, setView]     = useState<ViewMode>('week')
  const [weekOffset, setWeekOffset] = useState(0)

  const { start, end, label } = getWeekRange(weekOffset)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.planning.items({
      startDate: start.toISOString(),
      endDate:   end.toISOString(),
    })
    if (res.success && res.data) setItems(res.data)
    setLoading(false)
  }, [start.toISOString(), end.toISOString()])

  useEffect(() => { load() }, [load])

  // Group items by machine
  const machines = [...new Map(items.map(i => [i.machineId, { id: i.machineId, code: i.machineCode, name: i.machineName }])).values()]
    .sort((a, b) => a.code.localeCompare(b.code))

  // Days in range
  const days: Date[] = []
  const cur = new Date(start)
  while (cur <= end) { days.push(new Date(cur)); cur.setDate(cur.getDate() + 1) }
  const totalMs = end.getTime() - start.getTime()

  function itemLeft(item: PlanningItemDto) {
    const s = new Date(item.startTime).getTime()
    return ((s - start.getTime()) / totalMs * 100).toFixed(2) + '%'
  }
  function itemWidth(item: PlanningItemDto) {
    const s = new Date(item.startTime).getTime()
    const e = Math.min(new Date(item.endTime).getTime(), end.getTime())
    return (Math.max(0, e - s) / totalMs * 100).toFixed(2) + '%'
  }

  const stateColor = (item: PlanningItemDto) => {
    const now = Date.now()
    const s = new Date(item.startTime).getTime()
    const e = new Date(item.endTime).getTime()
    if (e < now) return { bg: va.okBg,     bar: va.ok,     fg: va.ok     } // done
    if (s < now) return { bg: va.activeBg, bar: va.active, fg: va.active } // running
    return               { bg: va.accentBg,bar: va.accent, fg: va.primary } // planned
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Lập kế hoạch sản xuất"
        breadcrumb={`Sản xuất › ${label}`}
        right={<>
          <VASeg
            value={view}
            onChange={v => setView(v as ViewMode)}
            options={[{ id: 'day', label: 'Ngày' }, { id: 'week', label: 'Tuần' }, { id: 'month', label: 'Tháng' }]}
          />
          <VABtn kind="primary" style={{ marginLeft: 8 }}>+ Lên kế hoạch</VABtn>
        </>}
      />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng kế hoạch"  value={items.length}              sub="items" />
          <VAKpi label="Máy có kế hoạch" value={machines.length}           sub="máy"  accent={va.active} />
          <VAKpi label="Đang thực hiện"  value={items.filter(i => new Date(i.startTime) <= new Date() && new Date(i.endTime) >= new Date()).length} accent={va.active} />
          <VAKpi label="Hoàn thành"      value={items.filter(i => new Date(i.endTime) < new Date()).length} accent={va.ok} />
        </div>

        {/* Week nav */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <VABtn kind="ghost" onClick={() => setWeekOffset(w => w - 1)}>← Tuần trước</VABtn>
          <span style={{ fontSize: 13, fontWeight: 600, color: va.text, minWidth: 140, textAlign: 'center' }}>{label}</span>
          <VABtn kind="ghost" onClick={() => setWeekOffset(w => w + 1)}>Tuần sau →</VABtn>
          {weekOffset !== 0 && <VABtn kind="ghost" onClick={() => setWeekOffset(0)}>Về hôm nay</VABtn>}
        </div>

        {/* Gantt */}
        <VACard title="Lịch máy" sub={machines.length > 0 ? `${machines.length} máy` : ''} pad={false} style={{ flex: 1, minHeight: 300 }}>
          {loading ? (
            <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Đang tải…</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
              {/* Day header */}
              <div style={{ display: 'flex', borderBottom: `1px solid ${va.border}`, background: va.surface2, flexShrink: 0 }}>
                <div style={{ width: 190, padding: '11px 16px', fontSize: 10.5, fontWeight: 700, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, borderRight: `1px solid ${va.border}`, flexShrink: 0 }}>Máy</div>
                <div style={{ flex: 1, display: 'flex' }}>
                  {days.map((d, i) => {
                    const isToday = d.toDateString() === new Date().toDateString()
                    const isWknd  = d.getDay() === 0 || d.getDay() === 6
                    return (
                      <div key={i} style={{ flex: 1, padding: '8px 4px', borderRight: i < days.length - 1 ? `1px solid ${va.border}` : 'none', textAlign: 'center', background: isWknd ? va.surface3 : isToday ? va.accentBg : 'transparent' }}>
                        <div style={{ fontSize: 11, fontWeight: isToday ? 700 : 500, color: isToday ? va.accent : va.text }}>{d.toLocaleDateString('vi-VN', { weekday: 'short' }).toUpperCase()}</div>
                        <div style={{ fontSize: 9.5, color: va.text3, fontFamily: va.mono }}>{d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}</div>
                      </div>
                    )
                  })}
                </div>
              </div>

              {/* Rows */}
              <div className="va-scroll" style={{ flex: 1, overflow: 'auto' }}>
                {machines.length === 0 ? (
                  <div style={{ padding: 32, textAlign: 'center', color: va.text3, fontSize: 13 }}>Không có kế hoạch nào trong tuần này.</div>
                ) : machines.map(machine => {
                  const machineItems = items.filter(i => i.machineId === machine.id)
                  return (
                    <div key={machine.id} style={{ display: 'flex', borderBottom: `1px solid ${va.separator}`, minHeight: 68 }}>
                      <div style={{ width: 190, padding: '13px 16px', borderRight: `1px solid ${va.border}`, flexShrink: 0, background: va.surface }}>
                        <div style={{ fontFamily: va.mono, fontSize: 12, fontWeight: 600, color: va.text }}>{machine.code}</div>
                        <div style={{ fontSize: 10, color: va.text3, marginTop: 2 }}>{machine.name ?? ''}</div>
                      </div>
                      <div style={{ flex: 1, position: 'relative' }}>
                        {/* Day grid lines */}
                        <div style={{ position: 'absolute', inset: 0, display: 'flex' }}>
                          {days.map((d, i) => (
                            <div key={i} style={{ flex: 1, borderRight: i < days.length - 1 ? `1px solid ${va.separator}` : 'none', background: d.getDay() === 0 || d.getDay() === 6 ? va.surface3 + '88' : 'transparent' }} />
                          ))}
                        </div>
                        {/* Planning items */}
                        {machineItems.map(item => {
                          const c = stateColor(item)
                          return (
                            <div key={item.id} className="va-clickable" style={{ position: 'absolute', left: itemLeft(item), width: itemWidth(item), top: 8, bottom: 8, background: c.bg, border: `1px solid ${c.bar}66`, borderLeft: `3px solid ${c.bar}`, borderRadius: 5, padding: '4px 7px', display: 'flex', flexDirection: 'column', justifyContent: 'center', overflow: 'hidden', zIndex: 1 }}>
                              <div style={{ fontFamily: va.mono, fontWeight: 600, color: va.text, fontSize: 10.5, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{item.jobNumber}</div>
                              <div style={{ fontSize: 9.5, color: va.text2, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>OP{item.opNumber} {item.operatorName ? `· ${item.operatorName}` : ''}</div>
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </VACard>
      </div>
    </div>
  )
}
