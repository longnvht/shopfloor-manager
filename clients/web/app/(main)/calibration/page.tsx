'use client'

import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const REQUESTS = [
  { id: 'CR-26-018', gage: 'DIAL-08',  desc: 'Đồng hồ so 0.01',  vendor: 'VinaCAL Lab', status: 'approved',  reqDate: '20/05/2026', proc: 'CP-DIAL-r3' },
  { id: 'CR-26-017', gage: 'GA-0142',  desc: 'Panme 0–25mm #2',   vendor: 'VinaCAL Lab', status: 'pending',   reqDate: '25/05/2026', proc: 'CP-MIC-r5' },
  { id: 'CR-26-016', gage: 'PROT-05',  desc: 'Thước đo góc',       vendor: 'QTech Metro', status: 'pending',   reqDate: '24/05/2026', proc: 'CP-ANG-r2' },
  { id: 'CR-26-015', gage: 'CAL-023',  desc: 'Thước cặp 200mm',    vendor: 'VinaCAL Lab', status: 'completed', reqDate: '15/05/2026', proc: 'CP-CAL-r4', result: 'Pass' },
  { id: 'CR-26-014', gage: 'MIC-001',  desc: 'Panme 0–25mm',       vendor: 'QTech Metro', status: 'completed', reqDate: '08/05/2026', proc: 'CP-MIC-r5', result: 'Pass' },
]
const CAL_STATUS: Record<string, { label: string; kind: VaBadgeKind }> = {
  pending:   { label: 'Chờ duyệt',  kind: 'warn'    },
  approved:  { label: 'Đã gửi',     kind: 'running' },
  completed: { label: 'Hoàn thành', kind: 'ok'      },
  cancelled: { label: 'Đã hủy',     kind: 'neutral' },
}
const VENDORS = [
  { name: 'VinaCAL Lab', contact: 'Mr. Tuấn', phone: '024 3856 xxxx', jobs: 3 },
  { name: 'QTech Metro', contact: 'Ms. Lan',  phone: '028 3925 xxxx', jobs: 2 },
]
const DUE = [
  { gage: 'CAL-023', desc: 'Thước cặp 200mm', days: 17,  due: '20/06/2026' },
  { gage: 'MIC-001', desc: 'Panme 0–25mm',    days: 38,  due: '11/07/2026' },
  { gage: 'MIC-002', desc: 'Panme 25–50mm',   days: 65,  due: '07/08/2026' },
]

export default function CalibrationPage() {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Hiệu chuẩn (Calibration)" breadcrumb="Chất lượng › Chu trình hiệu chuẩn"
        right={<VABtn kind="primary">+ Tạo yêu cầu</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Chờ duyệt"          value={REQUESTS.filter(r => r.status === 'pending').length}   accent={va.warn}   />
          <VAKpi label="Đang hiệu chuẩn"     value={REQUESTS.filter(r => r.status === 'approved').length}  accent={va.active} />
          <VAKpi label="Hoàn thành (tháng)"  value={REQUESTS.filter(r => r.status === 'completed').length} accent={va.ok}     />
          <VAKpi label="Sắp đến hạn (60d)"   value={DUE.length} />
          <VAKpi label="Vendor"              value={VENDORS.length} />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1fr', gap: 14, flex: 1, minHeight: 0 }}>
          {/* Requests */}
          <VACard title="Yêu cầu hiệu chuẩn" sub={`${REQUESTS.length} yêu cầu`} pad={false} style={{ minHeight: 0 }}>
            <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {['Mã YC', 'Gage', 'Vendor', 'Procedure', 'Ngày YC', 'Trạng thái', ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {REQUESTS.map(r => (
                    <tr key={r.id} className="va-row va-clickable">
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontWeight: 700, color: va.text }}>{r.id}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <div style={{ fontFamily: va.mono, fontWeight: 600, color: va.text }}>{r.gage}</div>
                        <div style={{ fontSize: 11, color: va.text2 }}>{r.desc}</div>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{r.vendor}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, color: va.text2 }}>{r.proc}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{r.reqDate}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={CAL_STATUS[r.status].kind} dot={r.status !== 'completed'}>{CAL_STATUS[r.status].label}</VABadge>
                        {r.result && <span style={{ fontSize: 10.5, color: va.ok, marginLeft: 6, fontWeight: 600 }}>{r.result}</span>}
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                        {r.status === 'pending'   && <VABtn kind="primary" style={{ height: 28, fontSize: 11, padding: '0 10px' }}>Duyệt</VABtn>}
                        {r.status === 'approved'  && <VABtn kind="accent"  style={{ height: 28, fontSize: 11, padding: '0 10px' }}>Nhận KQ</VABtn>}
                        {r.status === 'completed' && <span className="va-clickable" style={{ fontSize: 11, color: va.primary, fontWeight: 600 }}>⬇ Chứng chỉ</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </VACard>

          {/* Right: due + vendors */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14, minHeight: 0 }}>
            <VACard title="Sắp đến hạn" sub="60 ngày tới" pad={false} style={{ flex: 1, minHeight: 0 }}>
              <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
                {DUE.map(d => (
                  <div key={d.gage} className="va-row va-clickable" style={{ padding: '12px 16px', borderBottom: `1px solid ${va.separator}`, display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontFamily: va.mono, fontWeight: 600, fontSize: 12.5, color: va.text }}>{d.gage}</div>
                      <div style={{ fontSize: 11, color: va.text2 }}>{d.desc}</div>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 600, color: d.days <= 30 ? va.warn : va.text2 }}>{d.days}d</div>
                      <div style={{ fontSize: 10, color: va.text3, fontFamily: va.mono }}>{d.due}</div>
                    </div>
                  </div>
                ))}
              </div>
            </VACard>

            <VACard title="Vendor hiệu chuẩn" pad={false}>
              {VENDORS.map((v, i) => (
                <div key={v.name} style={{ padding: '12px 16px', borderBottom: i < VENDORS.length - 1 ? `1px solid ${va.separator}` : 'none', display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{ width: 34, height: 34, borderRadius: 8, background: va.accentLt, color: va.primary, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 13, flexShrink: 0 }}>{v.name[0]}</div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 12.5, fontWeight: 600, color: va.text }}>{v.name}</div>
                    <div style={{ fontSize: 11, color: va.text2 }}>{v.contact} · {v.phone}</div>
                  </div>
                  <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text3 }}>{v.jobs} YC</span>
                </div>
              ))}
            </VACard>
          </div>
        </div>
      </div>
    </div>
  )
}
