'use client'

import { useState } from 'react'
import { VATopbar, VACard, VABtn, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

const TABS = ['Máy móc', 'Loại OP', 'Dimension Category', 'Đồ gá', 'Loại tài liệu']

const MACHINES = [
  { code: 'MC-01', name: 'Mazak QT-200',  type: 'CNC Turning', serial: 'QT200-8841', group: 'Khu CNC',   cnc: true,  travel: 'X300 Z550',       mqtt: true  },
  { code: 'MC-02', name: 'DMG NEF-400',   type: 'CNC Turning', serial: 'NEF-22019',  group: 'Khu CNC',   cnc: true,  travel: 'X410 Z650',       mqtt: true  },
  { code: 'MC-03', name: 'Doosan PUMA',   type: 'Grinding',    serial: 'PUMA-5521',  group: 'Khu Mài',   cnc: true,  travel: 'Ø350',             mqtt: true  },
  { code: 'MC-04', name: 'Mori Seiki',    type: 'CNC Milling', serial: 'MS-NV5000',  group: 'Khu CNC',   cnc: true,  travel: 'X800 Y510 Z510',  mqtt: true  },
  { code: 'MC-05', name: 'Mazak VTC',     type: 'CNC Milling', serial: 'VTC-800C',   group: 'Khu CNC',   cnc: true,  travel: 'X1530 Y760',      mqtt: true  },
  { code: 'CMM-01',name: 'Zeiss Contura', type: 'CMM',         serial: 'CONT-9920',  group: 'Phòng CMM', cnc: false, travel: 'X700 Y1000 Z600', mqtt: false },
]
const OP_TYPES = [
  { code: 'CNC_TURNING',    name: 'Tiện CNC',      mesMenu: 'FAI, G-code, Route' },
  { code: 'CNC_MILLING',    name: 'Phay CNC',      mesMenu: 'FAI, G-code, Fixture' },
  { code: 'GRINDING',       name: 'Mài',           mesMenu: 'FAI, Route' },
  { code: 'CMM_INSPECTION', name: 'Đo CMM',        mesMenu: 'FAI, Drawing' },
  { code: 'MANUAL_INSPECTION', name: 'Kiểm tra tay', mesMenu: 'FAI' },
]
const DIM_CATS = [
  { code: 'LIN', name: 'Linear (kích thước thẳng)', gageTypes: 'Micrometer, Caliper'  },
  { code: 'ANG', name: 'Angular (góc)',              gageTypes: 'Protractor'            },
  { code: 'THD', name: 'Thread (ren)',               gageTypes: 'Ring/Plug Gauge'      },
  { code: 'GEO', name: 'Geometric (hình học)',       gageTypes: 'Dial Indicator'        },
  { code: 'SFC', name: 'Surface (bề mặt)',           gageTypes: 'Surface Tester'        },
]

const CodeTag = ({ c }: { c: string }) => (
  <span style={{ fontFamily: va.mono, fontSize: 11, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 7px', borderRadius: 4, border: `1px solid ${va.border}` }}>{c}</span>
)
const td: React.CSSProperties = { padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }

export default function MasterPage() {
  const [tab, setTab] = useState(0)

  const tables = [
    // Machines
    <table key="machines" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead>
        <tr style={{ background: va.surface2 }}>
          {['Mã máy', 'Tên máy', 'Loại', 'Serial', 'Nhóm', 'Hành trình', 'CNC', 'MQTT'].map((h, i) => (
            <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {MACHINES.map(m => (
          <tr key={m.code} className="va-row va-clickable">
            <td style={td}><CodeTag c={m.code} /></td>
            <td style={{ ...td, fontWeight: 500 }}>{m.name}</td>
            <td style={{ ...td, color: va.text2 }}>{m.type}</td>
            <td style={{ ...td, fontFamily: va.mono, color: va.text2 }}>{m.serial}</td>
            <td style={{ ...td, color: va.text2 }}>{m.group}</td>
            <td style={{ ...td, fontFamily: va.mono, fontSize: 11, color: va.text2 }}>{m.travel}</td>
            <td style={td}>{m.cnc ? <VABadge kind="ok">CNC</VABadge> : <span style={{ color: va.text3, fontSize: 11 }}>—</span>}</td>
            <td style={td}>{m.mqtt
              ? <span style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 11, color: va.ok }}><span style={{ width: 6, height: 6, borderRadius: '50%', background: va.ok }} />kết nối</span>
              : <span style={{ color: va.text3, fontSize: 11 }}>—</span>}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // OP Types
    <table key="optypes" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead>
        <tr style={{ background: va.surface2 }}>
          {['Code', 'Tên công đoạn', 'Menu MES hiển thị'].map((h, i) => (
            <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {OP_TYPES.map(o => (
          <tr key={o.code} className="va-row va-clickable">
            <td style={td}><CodeTag c={o.code} /></td>
            <td style={{ ...td, fontWeight: 500 }}>{o.name}</td>
            <td style={{ ...td, color: va.text2 }}>{o.mesMenu}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // Dimension Categories
    <table key="dimcats" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead>
        <tr style={{ background: va.surface2 }}>
          {['Code', 'Tên phương pháp đo', 'Loại gage phù hợp'].map((h, i) => (
            <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {DIM_CATS.map(d => (
          <tr key={d.code} className="va-row va-clickable">
            <td style={td}><CodeTag c={d.code} /></td>
            <td style={{ ...td, fontWeight: 500 }}>{d.name}</td>
            <td style={{ ...td, color: va.text2 }}>{d.gageTypes}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    <div key="fixtures" style={{ padding: 40, textAlign: 'center', color: va.text3 }}>
      <div style={{ fontSize: 28, marginBottom: 10 }}>◫</div>
      <div style={{ fontSize: 13, color: va.text2 }}>Quản lý đồ gá: FixtureType › Location › Slot › Category</div>
    </div>,

    <div key="doctypes" style={{ padding: 40, textAlign: 'center', color: va.text3 }}>
      <div style={{ fontSize: 28, marginBottom: 10 }}>◰</div>
      <div style={{ fontSize: 13, color: va.text2 }}>Loại tài liệu hệ thống (ISO/QMS)</div>
    </div>,
  ]

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Master Data" breadcrumb="Hệ thống › Dữ liệu danh mục nền tảng"
        right={<VABtn kind="primary">+ Thêm mục</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* Tabs */}
        <div style={{ display: 'flex', gap: 4, borderBottom: `1px solid ${va.border}` }}>
          {TABS.map((t, i) => {
            const on = tab === i
            return (
              <div key={t} className="va-clickable" onClick={() => setTab(i)}
                style={{ padding: '10px 16px', fontSize: 13, fontWeight: on ? 600 : 500, color: on ? va.primary : va.text2, borderBottom: on ? `2px solid ${va.accent}` : '2px solid transparent', marginBottom: -1 }}>
                {t}
              </div>
            )
          })}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: va.accentBg, borderRadius: 9, fontSize: 12, color: va.text2 }}>
          <span style={{ color: va.accent, fontSize: 14 }}>ⓘ</span>
          Dữ liệu danh mục đang được tham chiếu không thể xóa — dùng <b style={{ color: va.text }}>is_active = false</b> để ẩn khỏi dropdown.
        </div>

        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {tables[tab]}
          </div>
        </VACard>
      </div>
    </div>
  )
}
