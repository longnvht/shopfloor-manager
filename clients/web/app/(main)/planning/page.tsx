'use client'

import { VATopbar, VAKpi, VACard, VABtn } from '@/components/va'
import { VASeg } from '@/components/va/seg'
import { va } from '@/lib/va-tokens'

type State = 'done' | 'running' | 'planned' | 'alarm'
const STATE_COLOR: Record<State, { bg: string; bar: string; fg: string }> = {
  done:    { bg: '#E8F2E8', bar: '#2E7D32', fg: '#2E7D32' },
  running: { bg: '#FFE4CC', bar: '#E65100', fg: '#E65100' },
  planned: { bg: '#FFF3DC', bar: '#F57C00', fg: '#6D3B1A' },
  alarm:   { bg: '#FBE9E9', bar: '#C62828', fg: '#C62828' },
}

const DAYS = ['T2 25/05','T3 26/05','T4 27/05','T5 28/05','T6 29/05','T7 30/05','CN 31/05']
const ROWS = [
  { machine: 'MC-01 · Mazak QT-200', items: [
    { day: 0, start: 6, end: 14, job: 'JB-26-031', op: 'OP020', op_label: 'Turn',   operator: 'Hùng', shift: 'Ca 1',   state: 'done'    as State },
    { day: 0, start: 14,end: 22, job: 'JB-26-031', op: 'OP020', op_label: 'Turn',   operator: 'An',   shift: 'Ca 2',   state: 'running' as State },
    { day: 1, start: 6, end: 18, job: 'JB-26-029', op: 'OP010', op_label: 'Rough',  operator: 'Hùng', shift: 'Ca 1',   state: 'planned' as State },
    { day: 4, start: 6, end: 14, job: 'JB-26-031', op: 'OP030', op_label: 'Finish', operator: 'Hùng', shift: 'Ca 1',   state: 'planned' as State },
  ]},
  { machine: 'MC-02 · DMG NEF-400', items: [
    { day: 0, start: 6, end: 14, job: 'JB-26-029', op: 'OP010', op_label: 'Rough',  operator: 'Bình', shift: 'Ca 1',   state: 'running' as State },
    { day: 1, start: 6, end: 22, job: 'JB-26-029', op: 'OP010', op_label: 'Rough',  operator: 'Bình', shift: 'Ca 1+2', state: 'planned' as State },
    { day: 2, start: 6, end: 14, job: 'JB-26-026', op: 'OP010', op_label: 'Turn',   operator: 'An',   shift: 'Ca 1',   state: 'planned' as State },
  ]},
  { machine: 'MC-03 · Doosan PUMA', items: [
    { day: 0, start: 8, end: 14, job: 'JB-26-028', op: 'OP030', op_label: 'Grind',  operator: 'Châu', shift: 'Ca 1',   state: 'done'    as State },
    { day: 1, start: 8, end: 18, job: 'JB-26-031', op: 'OP030', op_label: 'Grind',  operator: 'Châu', shift: 'Ca 1',   state: 'planned' as State },
  ]},
  { machine: 'MC-04 · Mori Seiki', items: [
    { day: 0, start: 6, end: 11, job: 'JB-26-030', op: 'OP020', op_label: 'Mill',   operator: 'Châu', shift: 'Ca 1',   state: 'alarm'   as State },
    { day: 1, start: 14,end: 22, job: 'JB-26-030', op: 'OP020', op_label: 'Mill',   operator: 'Dũng', shift: 'Ca 2',   state: 'planned' as State },
  ]},
  { machine: 'MC-05 · Mazak VTC', items: [
    { day: 0, start: 6, end: 22, job: 'JB-26-027', op: 'OP040', op_label: 'Finish', operator: 'Dũng', shift: 'Ca 1+2', state: 'running' as State },
    { day: 1, start: 6, end: 14, job: 'JB-26-027', op: 'OP040', op_label: 'Finish', operator: 'Dũng', shift: 'Ca 1',   state: 'planned' as State },
  ]},
]

const HOURS = 24
const dayW = 100 / DAYS.length

export default function PlanningPage() {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Lập kế hoạch sản xuất" breadcrumb={`Tuần 22 · Th2 25/05 – CN 31/05/2026`}
        right={<>
          <VASeg value="week" options={[{ id: 'day', label: 'Ngày' }, { id: 'week', label: 'Tuần' }, { id: 'month', label: 'Tháng' }]} />
          <VABtn kind="primary" style={{ marginLeft: 8 }}>+ Lên kế hoạch</VABtn>
        </>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng kế hoạch"    value="38" sub="items / tuần" />
          <VAKpi label="Job đang chạy"     value="6"  sub="máy"         accent={va.active} />
          <VAKpi label="Conflict"          value="2"  sub="cần xử lý"   accent={va.warn}   />
          <VAKpi label="Operator gán ca"   value="14/16" />
          <VAKpi label="Capacity"          value="74%" sub="trung bình" />
        </div>

        <VACard title="Lịch máy theo ngày" sub="Tuần 22/2026 · kéo-thả OP để điều chỉnh" pad={false} style={{ flex: 1, minHeight: 0 }}
          right={<div style={{ display: 'flex', gap: 13, fontSize: 11, color: va.text2 }}>
            {([['Đã xong', 'done'], ['Đang chạy', 'running'], ['Đã lên KH', 'planned'], ['Alarm', 'alarm']] as [string, State][]).map(([l, k]) => (
              <span key={k} style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                <span style={{ width: 11, height: 11, borderRadius: 2, background: STATE_COLOR[k].bg, borderLeft: `3px solid ${STATE_COLOR[k].bar}` }} />{l}
              </span>
            ))}
          </div>}>
          <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            {/* Day header */}
            <div style={{ display: 'flex', borderBottom: `1px solid ${va.border}`, background: va.surface2, flexShrink: 0 }}>
              <div style={{ width: 190, padding: '11px 16px', fontSize: 10.5, fontWeight: 700, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, borderRight: `1px solid ${va.border}`, flexShrink: 0 }}>Máy</div>
              <div style={{ flex: 1, display: 'flex' }}>
                {DAYS.map((d, i) => {
                  const [dn, dd] = d.split(' ')
                  return (
                    <div key={d} style={{ flex: 1, padding: '8px 6px', borderRight: i < DAYS.length - 1 ? `1px solid ${va.border}` : 'none', textAlign: 'center', background: i >= 5 ? va.surface3 : 'transparent' }}>
                      <div style={{ fontSize: 11, fontWeight: 700, color: va.text }}>{dn}</div>
                      <div style={{ fontSize: 9.5, color: va.text3, fontFamily: va.mono }}>{dd}</div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Rows */}
            <div className="va-scroll" style={{ flex: 1, overflow: 'auto' }}>
              {ROWS.map(row => {
                const [code, name] = row.machine.split(' · ')
                return (
                  <div key={row.machine} style={{ display: 'flex', borderBottom: `1px solid ${va.separator}`, minHeight: 70 }}>
                    <div style={{ width: 190, padding: '13px 16px', borderRight: `1px solid ${va.border}`, flexShrink: 0, background: va.surface }}>
                      <div style={{ fontFamily: va.mono, fontSize: 12, fontWeight: 600, color: va.text }}>{code}</div>
                      <div style={{ fontSize: 10, color: va.text3, marginTop: 2 }}>{name}</div>
                    </div>
                    <div style={{ flex: 1, display: 'flex', position: 'relative' }}>
                      {DAYS.map((_, di) => (
                        <div key={di} style={{ flex: 1, borderRight: di < DAYS.length - 1 ? `1px solid ${va.separator}` : 'none', background: di >= 5 ? va.surface3 + '88' : 'transparent' }} />
                      ))}
                      {row.items.map((it, ii) => {
                        const left  = it.day * dayW + (it.start / HOURS) * dayW
                        const width = ((it.end - it.start) / HOURS) * dayW
                        const c     = STATE_COLOR[it.state]
                        return (
                          <div key={ii} className="va-clickable" style={{ position: 'absolute', left: `${left}%`, width: `calc(${width}% - 3px)`, top: 7, bottom: 7, background: c.bg, border: `1px solid ${c.bar}66`, borderLeft: `3px solid ${c.bar}`, borderRadius: 5, padding: '5px 8px', display: 'flex', flexDirection: 'column', justifyContent: 'center', overflow: 'hidden' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                              <span style={{ fontFamily: va.mono, fontWeight: 600, color: va.text, fontSize: 10.5, whiteSpace: 'nowrap' }}>{it.job}</span>
                              <span style={{ fontSize: 9, color: c.fg, fontWeight: 700 }}>{it.op}</span>
                            </div>
                            <div style={{ fontSize: 9.5, color: va.text2, marginTop: 1, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{it.op_label} · {it.operator}</div>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        </VACard>
      </div>
    </div>
  )
}
