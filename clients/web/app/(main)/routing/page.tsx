'use client'

import { useState } from 'react'
import { VATopbar, VABadge, VACard, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'

const opTypeColor: Record<string, string> = {
  CNC_TURNING: va.primary, CNC_MILLING: va.accent,
  GRINDING: va.primaryLt, CMM_INSPECTION: '#5D4037', MANUAL_INSPECTION: va.text2,
}
const opTypeLabel: Record<string, string> = {
  CNC_TURNING: 'Tiện CNC', CNC_MILLING: 'Phay CNC',
  GRINDING: 'Mài', CMM_INSPECTION: 'Đo CMM', MANUAL_INSPECTION: 'Kiểm tay',
}
const docColor: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00',
}

const PARTS = [
  { part: 'SHAFT-50H6',   rev: 'B', ops: 5, type: 'Trục',      rRev: 12, jobs: 3 },
  { part: 'HOUSING-A12',  rev: 'C', ops: 7, type: 'Vỏ hộp',    rRev: 8,  jobs: 1 },
  { part: 'FLANGE-DN80',  rev: 'A', ops: 4, type: 'Mặt bích',  rRev: 4,  jobs: 2 },
  { part: 'GEAR-Z48',     rev: 'D', ops: 6, type: 'Bánh răng',  rRev: 15, jobs: 1 },
  { part: 'BRACKET-M210', rev: 'A', ops: 3, type: 'Giá đỡ',    rRev: 2,  jobs: 1 },
]
const OPS = [
  { num: '10',   type: 'CNC_TURNING',    name: 'Tiện thô',       setup: 2.0, prod: 4.0, docs: ['DRW','GCD','RTC'],  dims: 4,  complete: true  },
  { num: '20',   type: 'CNC_TURNING',    name: 'Tiện tinh',      setup: 1.5, prod: 6.0, docs: ['GCD','RTC','TLS'],  dims: 7,  complete: false },
  { num: '20.1', type: 'CNC_MILLING',    name: 'Phay rãnh then', setup: 1.0, prod: 2.5, docs: ['GCD','FXT'],         dims: 2,  complete: false, sub: true },
  { num: '30',   type: 'GRINDING',       name: 'Mài cổ trục',    setup: 1.0, prod: 3.0, docs: ['RTC'],               dims: 3,  complete: false },
  { num: '99',   type: 'CMM_INSPECTION', name: 'Kiểm tra cuối',  setup: 0.5, prod: 2.0, docs: ['DRW'],               dims: 14, complete: false },
]

export default function RoutingPage() {
  const [selPart, setSelPart] = useState('SHAFT-50H6')

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Routing & Operations" breadcrumb="Sản xuất › Quy trình gia công"
        right={<VABtn kind="primary">+ Thêm OP</VABtn>} />
      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>

        {/* Part list */}
        <div className="va-scroll" style={{ width: 280, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
          <div style={{ padding: '12px 16px', fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700, borderBottom: `1px solid ${va.separator}` }}>
            Part · {PARTS.length}
          </div>
          {PARTS.map(p => {
            const on = p.part === selPart
            return (
              <div key={p.part} className="va-clickable" onClick={() => setSelPart(p.part)}
                style={{ padding: '13px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface }}>
                <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 700, color: va.text }}>{p.part}</div>
                <div style={{ fontSize: 11.5, color: va.text2, marginTop: 3 }}>{p.type} · Rev {p.rev}</div>
                <div style={{ display: 'flex', gap: 10, marginTop: 7, fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>
                  <span>{p.ops} OP</span><span>·</span><span>{p.jobs} job</span><span>·</span><span>r{p.rRev}</span>
                </div>
              </div>
            )
          })}
        </div>

        {/* Routing detail */}
        <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'flex', alignItems: 'flex-start' }}>
            <div style={{ flex: 1 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 23, fontWeight: 700, color: va.text }}>{selPart}</h2>
                <VABadge kind="primary">Routing rev {PARTS.find(p => p.part === selPart)?.rRev}</VABadge>
              </div>
              <div style={{ fontSize: 12.5, color: va.text2, marginTop: 5 }}>
                {OPS.length} công đoạn · tổng setup {OPS.reduce((a, o) => a + o.setup, 0).toFixed(1)}h · run {OPS.reduce((a, o) => a + o.prod, 0).toFixed(1)}h
              </div>
            </div>
            <VABtn kind="ghost">⬆ Import Excel</VABtn>
          </div>

          {/* Flow strip */}
          <div className="va-scroll" style={{ display: 'flex', alignItems: 'center', padding: '16px 18px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, boxShadow: va.shadow, overflowX: 'auto' }}>
            {OPS.map((op, i) => (
              <div key={op.num} style={{ display: 'flex', alignItems: 'center' }}>
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, minWidth: 76, flexShrink: 0 }}>
                  <div style={{ minWidth: 54, height: 40, borderRadius: 8, background: op.complete ? va.ok : opTypeColor[op.type], color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 14 }}>{op.num}</div>
                  <span style={{ fontSize: 10.5, color: va.text2, fontWeight: 600, textAlign: 'center' }}>{op.name}</span>
                  {op.complete && <span style={{ fontSize: 9, color: va.ok, fontWeight: 700 }}>✓ XONG</span>}
                </div>
                {i < OPS.length - 1 && <div style={{ width: 28, height: 2, background: va.border, flexShrink: 0 }} />}
              </div>
            ))}
          </div>

          {/* OP table */}
          <VACard title="Chi tiết công đoạn" sub="kéo để sắp xếp lại thứ tự" pad={false}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
              <thead>
                <tr style={{ background: va.surface2 }}>
                  {['OP', 'Công đoạn', 'Loại', 'Setup', 'Run', 'Tài liệu', 'Dim', ''].map((h, i) => (
                    <th key={i} style={{ textAlign: i === 3 || i === 4 || i === 6 ? 'center' : 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}` }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {OPS.map(op => (
                  <tr key={op.num} className="va-row va-clickable">
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, paddingLeft: op.sub ? 30 : 14 }}>
                      <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 50, height: 26, borderRadius: 6, background: op.sub ? va.surface2 : va.primary, color: op.sub ? va.text : '#fff', border: op.sub ? `1px solid ${va.border}` : 'none', fontFamily: va.mono, fontWeight: 600, fontSize: 11.5 }}>{op.num}</span>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontWeight: 500 }}>{op.name}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                      <span style={{ fontSize: 10.5, fontWeight: 600, color: opTypeColor[op.type], background: va.surface2, padding: '2px 8px', borderRadius: 4, border: `1px solid ${opTypeColor[op.type]}33` }}>{opTypeLabel[op.type]}</span>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.setup}h</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.prod}h</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {op.docs.map(d => (
                          <span key={d} style={{ fontSize: 9, fontWeight: 700, color: docColor[d] ?? va.text2, background: va.surface2, padding: '2px 5px', borderRadius: 3, fontFamily: va.mono, border: `1px solid ${docColor[d] ?? va.border}33` }}>{d}</span>
                        ))}
                      </div>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{op.dims}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right' }}>
                      <span style={{ color: va.text3, fontSize: 15 }}>⋯</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </VACard>
        </div>
      </div>
    </div>
  )
}
