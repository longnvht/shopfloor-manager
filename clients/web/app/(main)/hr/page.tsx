'use client'

import { useState } from 'react'
import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const DEPTS = [
  { code: 'MGMT',  name: 'Ban Giám đốc',    count: 3  },
  { code: 'QC',    name: 'Chất lượng',       count: 6  },
  { code: 'PROD',  name: 'Sản xuất',         count: 14 },
  { code: 'ME',    name: 'Kỹ thuật',         count: 5  },
  { code: 'PLAN',  name: 'Kế hoạch',         count: 2  },
  { code: 'MAINT', name: 'Bảo trì',          count: 4  },
]
const USERS = [
  { name: 'Nguyễn V. Quân',  login: 'nvquan',  dept: 'ME',   pos: 'Kỹ sư CNC',       type: 'Engineer',  status: 'Working',  active: true  },
  { name: 'Lê M. Châu',      login: 'lmchau',  dept: 'QC',   pos: 'QC Lead',          type: 'Inspector', status: 'Working',  active: true  },
  { name: 'Phạm V. Hùng',    login: 'pvhung',  dept: 'PROD', pos: 'Operator',         type: 'Operator',  status: 'Working',  active: true  },
  { name: 'Trần Q. Bình',    login: 'tqbinh',  dept: 'QC',   pos: 'Inspector',        type: 'Inspector', status: 'Working',  active: true  },
  { name: 'Nguyễn T. An',    login: 'ntan',    dept: 'PROD', pos: 'Operator',         type: 'Operator',  status: 'On Leave', active: true  },
  { name: 'Hoàng V. Dũng',   login: 'hvdung',  dept: 'PROD', pos: 'Operator',         type: 'Operator',  status: 'Working',  active: true  },
  { name: 'Vũ N. Phước',     login: 'vnphuoc', dept: 'PLAN', pos: 'Planner',          type: 'Planner',   status: 'Working',  active: true  },
  { name: 'Đỗ T. Em',        login: 'dtem',    dept: 'PROD', pos: 'Operator',         type: 'Operator',  status: 'Working',  active: true  },
  { name: 'Lê V. Sơn',       login: 'lvson',   dept: 'ME',   pos: 'Kỹ sư CAM',        type: 'Engineer',  status: 'Working',  active: true  },
  { name: 'Trịnh A. Khoa',   login: 'takhoa',  dept: 'MAINT',pos: 'Kỹ thuật bảo trì', type: 'Operator',  status: 'Resigned', active: false },
]
const WORK_STATUS: Record<string, { label: string; kind: VaBadgeKind }> = {
  'Working':  { label: 'Đang làm',  kind: 'ok'      },
  'On Leave': { label: 'Nghỉ phép', kind: 'warn'    },
  'Resigned': { label: 'Đã nghỉ',   kind: 'neutral' },
}
const TYPE_COLOR: Record<string, string> = {
  Engineer: va.primary, Inspector: va.accent, Operator: va.primaryLt,
  Planner: '#5D4037', Manager: va.text, Admin: va.err,
}

function initials(name: string) { return name.split(' ').map(w => w[0]).slice(-2).join('') }

export default function HrPage() {
  const [selDept, setSelDept] = useState('all')
  const total    = DEPTS.reduce((a, d) => a + d.count, 0)
  const filtered = selDept === 'all' ? USERS : USERS.filter(u => u.dept === selDept)

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Nhân sự & Tài khoản" breadcrumb="Hệ thống › HR & User Management"
        right={<VABtn kind="primary">+ Tạo tài khoản</VABtn>} />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* Org tree */}
        <div className="va-scroll" style={{ width: 250, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0, padding: 12 }}>
          <div style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700, padding: '4px 8px 8px' }}>Phòng ban</div>
          <div className="va-clickable" onClick={() => setSelDept('all')}
            style={{ padding: '10px 12px', borderRadius: 8, display: 'flex', alignItems: 'center', gap: 10, background: selDept === 'all' ? va.accentBg : 'transparent', marginBottom: 2 }}>
            <span style={{ flex: 1, fontSize: 13, fontWeight: selDept === 'all' ? 600 : 500, color: va.text }}>Tất cả nhân viên</span>
            <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text2, background: va.surface2, padding: '1px 7px', borderRadius: 10 }}>{total}</span>
          </div>
          {DEPTS.map(d => {
            const on = selDept === d.code
            return (
              <div key={d.code} className="va-clickable" onClick={() => setSelDept(d.code)}
                style={{ padding: '10px 12px', borderRadius: 8, display: 'flex', alignItems: 'center', gap: 10, background: on ? va.accentBg : 'transparent', borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', marginBottom: 2 }}>
                <span style={{ fontFamily: va.mono, fontSize: 10, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 6px', borderRadius: 4, width: 52, textAlign: 'center' }}>{d.code}</span>
                <span style={{ flex: 1, fontSize: 12.5, color: va.text }}>{d.name}</span>
                <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text2 }}>{d.count}</span>
              </div>
            )
          })}
        </div>

        {/* User table */}
        <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 14 }}>
          <div style={{ display: 'flex', gap: 13 }}>
            <VAKpi label="Tổng nhân viên" value={total} />
            <VAKpi label="Đang làm"       value={USERS.filter(u => u.status === 'Working').length}  accent={va.ok}   />
            <VAKpi label="Nghỉ phép"      value={USERS.filter(u => u.status === 'On Leave').length} accent={va.warn} />
            <VAKpi label="Đã nghỉ"        value={USERS.filter(u => u.status === 'Resigned').length} />
          </div>

          <VACard
            title={selDept === 'all' ? 'Tất cả nhân viên' : DEPTS.find(d => d.code === selDept)?.name}
            sub={`${filtered.length} người`} pad={false} style={{ flex: 1, minHeight: 0 }}>
            <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {['Nhân viên', 'Login', 'Phòng ban', 'Chức vụ', 'User Type', 'Trạng thái', ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(u => (
                    <tr key={u.login} className="va-row va-clickable" style={{ opacity: u.active ? 1 : 0.55 }}>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                          <div style={{ width: 30, height: 30, borderRadius: '50%', background: va.accentLt, color: va.primary, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 600, fontSize: 11, flexShrink: 0 }}>{initials(u.name)}</div>
                          <span style={{ fontWeight: 500, color: va.text }}>{u.name}{!u.active && <span style={{ color: va.text3, fontWeight: 400 }}> [đã nghỉ]</span>}</span>
                        </div>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{u.login}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <span style={{ fontFamily: va.mono, fontSize: 10, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 6px', borderRadius: 4 }}>{u.dept}</span>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{u.pos}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <span style={{ fontSize: 11.5, fontWeight: 600, color: TYPE_COLOR[u.type] ?? va.text2 }}>{u.type}</span>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={WORK_STATUS[u.status].kind} dot={u.status === 'Working'}>{WORK_STATUS[u.status].label}</VABadge>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right' }}>
                        <span style={{ color: va.text3, fontSize: 15 }}>⋯</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </VACard>
        </div>
      </div>
    </div>
  )
}
