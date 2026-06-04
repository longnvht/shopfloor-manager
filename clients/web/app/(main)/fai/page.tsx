'use client'

import { useState } from 'react'
import { VATopbar, VACard, VABtn, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

const CAT_COLOR: Record<string, string> = {
  LIN: va.primary, ANG: va.primaryLt, THD: va.accent, GEO: '#5D4037', SFC: va.text2,
}

const FAI = {
  job: 'JB-26-031', part: 'SHAFT-50H6', rev: 'B', op: 'OP020 · CNC Turning',
  inspector: 'Lê M. Châu', started: '25/05/2026 08:14',
  totalDims: 7, passCount: 11, failCount: 2, pendingCount: 1,
  serials: ['01','02','03','04','05','06','07','08'],
  dimensions: [
    { balloon: '1',  category: 'LIN', nominal: 50.0000, tolPlus: 0.0200, tolMinus: 0.0000, gage: 'Panme 0–25',
      values: { '01': { v: 50.012, r: 'pass' }, '02': { v: 50.018, r: 'pass' }, '03': { v: 50.009, r: 'pass' }, '04': { v: 50.014, r: 'pass' }, '05': { v: 50.011, r: 'pass' }, '06': { v: 50.016, r: 'pass' }, '07': { v: 50.013, r: 'pass' }, '08': { v: 50.010, r: 'pass' } } },
    { balloon: '2',  category: 'LIN', nominal: 80.0000, tolPlus: 0.1000, tolMinus: 0.1000, gage: 'Thước cặp 200',
      values: { '01': { v: 80.04, r: 'pass' }, '02': { v: 80.06, r: 'pass' }, '03': { v: 80.02, r: 'pass' }, '04': { v: 80.08, r: 'pass' }, '05': { v: 80.05, r: 'pass' }, '06': { v: 80.03, r: 'pass' }, '07': { v: 80.07, r: 'pass' }, '08': { v: 80.04, r: 'pass' } } },
    { balloon: '3',  category: 'LIN', nominal: 25.0000, tolPlus: 0.0200, tolMinus: 0.0200, gage: 'Panme 0–25',
      values: { '01': { v: 25.012, r: 'pass' }, '02': { v: 25.034, r: 'fail', ncr: 'NCR-26-0042' }, '03': { v: 25.008, r: 'pass' }, '04': { v: 25.015, r: 'pass' }, '05': { v: 25.011, r: 'pass' }, '06': { v: 25.019, r: 'pass' }, '07': { v: 25.013, r: 'pass' }, '08': null } },
    { balloon: '5A', category: 'ANG', nominal: 30.0000, tolPlus: 0.5000, tolMinus: 0.5000, gage: 'Thước góc',
      values: { '01': { v: 29.8, r: 'pass' }, '02': { v: 30.2, r: 'pass' }, '03': { v: 30.6, r: 'fail', ncr: 'NCR-26-0043' }, '04': { v: 30.1, r: 'pass' }, '05': { v: 30.0, r: 'pass' }, '06': { v: 29.9, r: 'pass' }, '07': { v: 30.3, r: 'pass' }, '08': null } },
    { balloon: '5B', category: 'THD', text: 'M10x1.5-6H', gage: 'Ring gauge',
      values: { '01': { v: '—', r: 'pass' }, '02': { v: '—', r: 'pass' }, '03': { v: '—', r: 'pass' }, '04': { v: '—', r: 'pass' }, '05': { v: '—', r: 'pass' }, '06': { v: '—', r: 'pass' }, '07': { v: '—', r: 'pass' }, '08': null } },
  ] as any[],
}

export default function FaiPage() {
  const f = FAI
  const [sel, setSel] = useState({ b: '5A', s: '03' })

  const selDim = f.dimensions.find((d: any) => d.balloon === sel.b)
  const selVal = selDim?.values?.[sel.s]

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="FAI · Dimension Sheet"
        breadcrumb={`Job ${f.job} › ${f.part} Rev ${f.rev} › ${f.op}`}
        right={<><VABtn kind="ghost" style={{ marginRight: 8 }}>⬇ Excel</VABtn><VABtn kind="primary">⬇ Xuất FAI PDF</VABtn></>}
      />
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>

        {/* Stats strip */}
        <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '15px 18px', display: 'flex', alignItems: 'center', gap: 22, boxShadow: va.shadow }}>
          <div style={{ minWidth: 150 }}>
            <div style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600, marginBottom: 4 }}>Inspector</div>
            <div style={{ fontSize: 14, fontWeight: 600 }}>{f.inspector}</div>
            <div style={{ fontSize: 11, color: va.text3, marginTop: 1 }}>Bắt đầu {f.started}</div>
          </div>
          <div style={{ height: 42, width: 1, background: va.border }} />
          {[
            { v: f.passCount,    t: 'Pass',      c: va.ok,     sub: `/ ${f.totalDims * f.serials.length} ô` },
            { v: f.failCount,    t: 'Fail · NCR', c: va.err },
            { v: f.pendingCount, t: 'Chưa đo',   c: va.text2 },
            { v: '94.6%',        t: 'Pass rate',  c: va.accent },
          ].map(x => (
            <div key={x.t}>
              <div style={{ fontFamily: va.mono, fontSize: 23, fontWeight: 600, color: x.c, lineHeight: 1 }}>
                {x.v}{x.sub && <span style={{ fontSize: 12, color: va.text3, fontWeight: 400 }}> {x.sub}</span>}
              </div>
              <div style={{ fontSize: 10.5, color: x.c, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600, marginTop: 5 }}>{x.t}</div>
            </div>
          ))}
          <div style={{ marginLeft: 'auto', display: 'flex', gap: 8 }}>
            <VABtn kind="ghost">Import Excel</VABtn>
            <VABtn kind="accent">+ Thêm Balloon</VABtn>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 14, flex: 1, minHeight: 0 }}>
          {/* Matrix */}
          <VACard title="Ma trận đo kiểm" sub={`${f.totalDims} balloon × ${f.serials.length} serial`} pad={false} style={{ minHeight: 0 }}>
            <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
              <table style={{ borderCollapse: 'separate', borderSpacing: 0, fontSize: 12, width: '100%' }}>
                <thead>
                  <tr>
                    {[['Ball', 56], ['Nominal', 96], ['Tol', 92], ['Cat · Gage', 168]] .map(([h, w]: any, i) => (
                      <th key={h} style={{ position: 'sticky', top: 0, left: i === 0 ? 0 : undefined, background: va.surface2, padding: '9px 12px', textAlign: 'left', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, borderRight: i === 3 ? `2px solid ${va.borderStr}` : `1px solid ${va.separator}`, minWidth: w, zIndex: i === 0 ? 3 : 2 }}>{h}</th>
                    ))}
                    {f.serials.map((s: string) => (
                      <th key={s} style={{ position: 'sticky', top: 0, background: va.surface2, padding: '9px 10px', textAlign: 'center', fontSize: 10, color: va.text2, fontWeight: 700, borderBottom: `1px solid ${va.border}`, fontFamily: va.mono, minWidth: 84 }}>SN {s}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {f.dimensions.map((d: any) => (
                    <tr key={d.balloon}>
                      <td style={{ position: 'sticky', left: 0, background: va.surface, padding: '6px 12px', borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, zIndex: 1 }}>
                        <div style={{ width: 32, height: 32, borderRadius: '50%', border: `2px solid ${CAT_COLOR[d.category]}`, color: CAT_COLOR[d.category], display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 12 }}>{d.balloon}</div>
                      </td>
                      <td style={{ padding: '6px 12px', fontFamily: va.mono, fontSize: 13, fontWeight: 500, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}` }}>
                        {d.text ? <span style={{ color: va.primary }}>{d.text}</span> : d.nominal.toFixed(4)}
                      </td>
                      <td style={{ padding: '6px 12px', fontFamily: va.mono, fontSize: 10.5, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, lineHeight: 1.5 }}>
                        {d.text ? <span style={{ color: va.text3 }}>—</span> : <><span style={{ color: va.ok }}>+{d.tolPlus.toFixed(4)}</span><br /><span style={{ color: va.err }}>−{d.tolMinus.toFixed(4)}</span></>}
                      </td>
                      <td style={{ padding: '6px 12px', fontSize: 11, color: va.text2, borderBottom: `1px solid ${va.separator}`, borderRight: `2px solid ${va.borderStr}` }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                          <span style={{ padding: '1px 5px', fontSize: 9, fontWeight: 700, color: CAT_COLOR[d.category], background: va.surface2, border: `1px solid ${CAT_COLOR[d.category]}44`, borderRadius: 3, fontFamily: va.mono }}>{d.category}</span>
                          <span style={{ whiteSpace: 'nowrap' }}>{d.gage}</span>
                        </div>
                      </td>
                      {f.serials.map((s: string) => {
                        const v = d.values[s]
                        if (!v) return (
                          <td key={s} style={{ padding: 0, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, background: va.bg }}>
                            <div style={{ height: 44, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>—</div>
                          </td>
                        )
                        const pass  = v.r === 'pass'
                        const isSel = sel.b === d.balloon && sel.s === s
                        return (
                          <td key={s} className="va-clickable" onClick={() => setSel({ b: d.balloon, s })}
                            style={{ padding: 0, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, background: !pass ? va.errBg : isSel ? va.accentBg : va.surface, boxShadow: isSel ? `inset 0 0 0 2px ${va.accent}` : 'none' }}>
                            <div style={{ height: 44, padding: '0 10px', display: 'flex', flexDirection: 'column', justifyContent: 'center', borderLeft: pass ? '2px solid transparent' : `2px solid ${va.err}` }}>
                              <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: pass ? 500 : 700, color: pass ? va.text : va.err }}>{v.v}</span>
                              {v.ncr && <span style={{ fontFamily: va.mono, fontSize: 9, color: va.err }}>{v.ncr}</span>}
                            </div>
                          </td>
                        )
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </VACard>

          {/* Detail panel */}
          <VACard title="Chi tiết Balloon" pad={false} style={{ minHeight: 0 }}>
            <div className="va-scroll" style={{ overflow: 'auto', height: '100%', padding: 16, display: 'flex', flexDirection: 'column', gap: 14 }}>
              {selDim && (
                <>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 13 }}>
                    <div style={{ width: 46, height: 46, borderRadius: '50%', border: `3px solid ${selVal && selVal.r === 'fail' ? va.err : CAT_COLOR[selDim.category]}`, color: selVal && selVal.r === 'fail' ? va.err : CAT_COLOR[selDim.category], display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 17, flexShrink: 0 }}>{sel.b}</div>
                    <div>
                      <div style={{ fontFamily: va.mono, fontSize: 14, fontWeight: 600 }}>{selDim.text ?? selDim.nominal?.toFixed(4)}</div>
                      <div style={{ fontSize: 11, color: va.text2, marginTop: 2 }}>{selDim.category} · {selDim.gage}</div>
                    </div>
                  </div>

                  {selVal ? (
                    <div style={{ padding: 13, borderRadius: 9, background: selVal.r === 'fail' ? va.errBg : va.okBg, border: `1px solid ${selVal.r === 'fail' ? va.err : va.ok}33` }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <VABadge kind={selVal.r === 'fail' ? 'err' : 'ok'} dot>{selVal.r === 'fail' ? 'FAIL' : 'PASS'}</VABadge>
                        <span style={{ fontSize: 12, color: va.text2 }}>Serial {sel.s}</span>
                      </div>
                      <div style={{ fontFamily: va.mono, fontSize: 24, fontWeight: 600, color: selVal.r === 'fail' ? va.err : va.ok, marginTop: 8 }}>{selVal.v}</div>
                      {selVal.ncr && <div style={{ fontFamily: va.mono, fontSize: 11, color: va.err, marginTop: 4 }}>Vượt dung sai → {selVal.ncr}</div>}
                    </div>
                  ) : (
                    <div style={{ padding: 13, borderRadius: 9, background: va.surface2, color: va.text2, fontSize: 12 }}>Serial {sel.s} chưa được đo.</div>
                  )}

                  <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 'auto' }}>
                    {selVal?.r === 'fail' && <VABtn kind="accent" style={{ justifyContent: 'center' }}>⊘ Mở NCR cho ô này</VABtn>}
                    <VABtn kind="ghost" style={{ justifyContent: 'center' }}>Xem lịch sử đo</VABtn>
                    <VABtn kind="primary" style={{ justifyContent: 'center' }}>+ Nhập giá trị đo</VABtn>
                  </div>
                </>
              )}
            </div>
          </VACard>
        </div>
      </div>
    </div>
  )
}
