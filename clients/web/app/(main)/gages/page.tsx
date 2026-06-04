'use client'

import { VATopbar, VAKpi, VACard, VABtn, VABadge, VASeg } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const GAGES = [
  { no: 'MIC-001', desc: 'Panme 0–25mm',       type: 'Micrometer',   range: '0–25mm',   cat: 'LIN', status: 'VALID',    due: '11/07/2026', days: 38,  loc: 'Tủ A · A1',       borrowed: false },
  { no: 'MIC-002', desc: 'Panme 25–50mm',      type: 'Micrometer',   range: '25–50mm',  cat: 'LIN', status: 'BORROWED', due: '07/08/2026', days: 65,  loc: 'MC-01 · Hùng',    borrowed: true  },
  { no: 'CAL-023', desc: 'Thước cặp 200mm',    type: 'Caliper',      range: '0–200mm',  cat: 'LIN', status: 'VALID',    due: '20/06/2026', days: 17,  loc: 'Tủ A · A3',       borrowed: false },
  { no: 'GA-0142', desc: 'Panme 0–25mm #2',    type: 'Micrometer',   range: '0–25mm',   cat: 'LIN', status: 'EXPIRED',  due: '31/05/2026', days: -3,  loc: 'Tủ A · A2',       borrowed: false },
  { no: 'DIAL-08', desc: 'Đồng hồ so 0.01',    type: 'Dial Ind.',    range: '0–10mm',   cat: 'GEO', status: 'CALIB',    due: '15/05/2026', days: -19, loc: 'Vendor VinaCAL',   borrowed: false },
  { no: 'RING-M10',desc: 'Ring gauge M10×1.5', type: 'Ring Gauge',   range: 'M10×1.5',  cat: 'THD', status: 'VALID',    due: '03/03/2027', days: 273, loc: 'Tủ B · B1',       borrowed: false },
  { no: 'SFC-01',  desc: 'Máy đo độ nhám',      type: 'Surf. Tester', range: 'Ra 0.05–10',cat:'SFC', status: 'VALID',   due: '10/10/2026', days: 129, loc: 'Phòng QC',         borrowed: false },
  { no: 'PROT-05', desc: 'Thước đo góc',        type: 'Protractor',   range: '0–360°',   cat: 'ANG', status: 'DAMAGED',  due: '22/03/2026', days: -73, loc: 'Sửa chữa',         borrowed: false },
]

const GAGE_STATUS: Record<string, { label: string; kind: VaBadgeKind }> = {
  VALID:    { label: 'Hợp lệ',          kind: 'ok'      },
  EXPIRED:  { label: 'Hết hạn',         kind: 'err'     },
  DAMAGED:  { label: 'Hư hỏng',         kind: 'err'     },
  BORROWED: { label: 'Đang mượn',       kind: 'running' },
  CALIB:    { label: 'Đang hiệu chuẩn', kind: 'warn'    },
}

export default function GagesPage() {
  const counts = GAGES.reduce<Record<string, number>>((a, g) => { a[g.status] = (a[g.status] ?? 0) + 1; return a }, {})
  const dueColor = (d: number) => d < 0 ? va.err : d <= 30 ? va.warn : va.text2

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Dụng cụ đo (Gage)" breadcrumb="Chất lượng › Quản lý dụng cụ"
        right={<><VABtn kind="ghost" style={{ marginRight: 8 }}>⬆ Import</VABtn><VABtn kind="primary">+ Thêm gage</VABtn></>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng gage"          value={GAGES.length} />
          <VAKpi label="Hợp lệ"             value={counts.VALID    ?? 0} accent={va.ok}     />
          <VAKpi label="Đang mượn"           value={counts.BORROWED ?? 0} accent={va.active}  />
          <VAKpi label="Hết hạn / hỏng"      value={(counts.EXPIRED ?? 0) + (counts.DAMAGED ?? 0)} accent={va.err} />
          <VAKpi label="Đang hiệu chuẩn"     value={counts.CALIB    ?? 0} accent={va.warn}   />
        </div>

        {/* Toolbar */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <VASeg value="all" options={[{ id: 'all', label: 'Tất cả' }, { id: 'valid', label: 'Hợp lệ' }, { id: 'borrowed', label: 'Đang mượn' }, { id: 'due', label: 'Sắp hết hạn' }]} />
        </div>

        {/* Table */}
        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
              <thead>
                <tr style={{ background: va.surface2 }}>
                  {['Gage No', 'Mô tả', 'Loại', 'Range', 'Cat', 'Trạng thái', 'Hạn HC', 'Vị trí', ''].map((h, i) => (
                    <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {GAGES.map(g => (
                  <tr key={g.no} className="va-row va-clickable">
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontWeight: 700, color: va.text }}>{g.no}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontWeight: 500 }}>{g.desc}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{g.type}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{g.range}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                      <span style={{ fontSize: 9.5, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 6px', borderRadius: 3, fontFamily: va.mono, border: `1px solid ${va.border}` }}>{g.cat}</span>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                      <VABadge kind={GAGE_STATUS[g.status].kind} dot>{GAGE_STATUS[g.status].label}</VABadge>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono }}>
                      <div style={{ color: va.text }}>{g.due}</div>
                      <div style={{ fontSize: 10.5, color: dueColor(g.days), fontWeight: 600 }}>{g.days < 0 ? `quá ${-g.days}d` : `còn ${g.days}d`}</div>
                    </td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, fontSize: 11.5 }}>{g.loc}</td>
                    <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                      {g.borrowed
                        ? <VABtn kind="ghost" style={{ height: 28, fontSize: 11, padding: '0 10px' }}>Trả</VABtn>
                        : g.status === 'VALID'
                          ? <VABtn kind="accent" style={{ height: 28, fontSize: 11, padding: '0 10px' }}>Mượn</VABtn>
                          : <span style={{ fontSize: 11, color: va.text3 }}>—</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </VACard>
      </div>
    </div>
  )
}
