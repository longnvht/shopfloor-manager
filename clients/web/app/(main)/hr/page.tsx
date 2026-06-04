'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type UserListDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const ROLE_COLOR: Record<string, string> = {
  Administrator: va.err,   Manager:  va.primary,
  Engineer:      va.accent, 'QC Inspector': '#5D4037',
  Operator:      va.primaryLt, Planner: va.text2, Leader: va.warn,
}

function initials(name: string) {
  return name.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase()
}

export default function HrPage() {
  const [users,   setUsers]   = useState<UserListDto[]>([])
  const [search,  setSearch]  = useState('')
  const [filter,  setFilter]  = useState<'all' | 'active' | 'inactive'>('all')
  const [loading, setLoading] = useState(true)
  const [selRole, setSelRole] = useState('all')

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.users.list(1, search || undefined)
    if (res.success && res.data) setUsers(res.data)
    setLoading(false)
  }, [search])

  useEffect(() => { load() }, [load])

  // Unique roles from loaded users
  const roles = ['all', ...Array.from(new Set(users.map(u => u.role).filter(Boolean) as string[]))]

  const filtered = users.filter(u => {
    if (filter === 'active'   && !u.isActive) return false
    if (filter === 'inactive' && u.isActive)  return false
    if (selRole !== 'all' && u.role !== selRole) return false
    return true
  })

  const activeCount   = users.filter(u => u.isActive).length
  const inactiveCount = users.filter(u => !u.isActive).length

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Nhân sự & Tài khoản" breadcrumb="Hệ thống › HR & User Management"
        right={<VABtn kind="primary">+ Tạo tài khoản</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPIs */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng tài khoản" value={users.length}                    />
          <VAKpi label="Đang hoạt động" value={activeCount}   accent={va.ok}   />
          <VAKpi label="Đã tắt"         value={inactiveCount} accent={va.text3} />
        </div>

        {/* Toolbar */}
        <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
          {/* Status filter */}
          <div style={{ display: 'flex', gap: 6 }}>
            {(['all', 'active', 'inactive'] as const).map(f => {
              const on = filter === f
              const lbl = f === 'all' ? 'Tất cả' : f === 'active' ? 'Đang hoạt động' : 'Đã tắt'
              return (
                <div key={f} className="va-clickable" onClick={() => setFilter(f)}
                  style={{ padding: '5px 12px', borderRadius: 7, fontSize: 12, fontWeight: 600, background: on ? va.primary : va.surface, color: on ? '#fff' : va.text2, border: `1px solid ${on ? va.primary : va.border}` }}>
                  {lbl}
                </div>
              )
            })}
          </div>

          {/* Role filter */}
          <select value={selRole} onChange={e => setSelRole(e.target.value)}
            style={{ padding: '6px 12px', borderRadius: 7, border: `1px solid ${va.border}`, fontSize: 12, fontFamily: va.font, background: va.surface, color: va.text, outline: 'none' }}>
            {roles.map(r => <option key={r} value={r}>{r === 'all' ? 'Tất cả role' : r}</option>)}
          </select>

          {/* Search */}
          <div style={{ height: 34, flex: 1, maxWidth: 300, background: va.surface, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: va.text3 }}>
            <span>⌕</span>
            <input value={search} onChange={e => setSearch(e.target.value)}
              placeholder="Tìm tên, login…"
              style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }} />
          </div>
        </div>

        {/* User table */}
        <VACard title={`Nhân viên`} sub={`${filtered.length} người`} pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {loading ? (
              <div style={{ padding: 24, fontSize: 12, color: va.text3 }}>Đang tải…</div>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ background: va.surface2 }}>
                    {['Nhân viên', 'Login', 'Role', 'Chức vụ', 'Loại', 'Trạng thái', ''].map((h, i) => (
                      <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(u => (
                    <tr key={u.id} className="va-row va-clickable" style={{ opacity: u.isActive ? 1 : 0.55 }}>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                          <div style={{ width: 30, height: 30, borderRadius: '50%', background: va.accentLt, color: va.primary, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 600, fontSize: 11, flexShrink: 0 }}>
                            {initials(u.name)}
                          </div>
                          <span style={{ fontWeight: 500, color: va.text }}>
                            {u.name}
                            {!u.isActive && <span style={{ color: va.text3, fontWeight: 400 }}> [đã tắt]</span>}
                          </span>
                        </div>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{u.userLogin}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        {u.role && (
                          <span style={{ fontSize: 11.5, fontWeight: 600, color: ROLE_COLOR[u.role] ?? va.text2 }}>{u.role}</span>
                        )}
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{u.position ?? '—'}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{u.userType ?? '—'}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={u.isActive ? 'ok' : 'neutral'} dot={u.isActive}>
                          {u.isActive ? 'Hoạt động' : 'Đã tắt'}
                        </VABadge>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right' }}>
                        <span style={{ color: va.text3, fontSize: 15, cursor: 'pointer' }}>⋯</span>
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr><td colSpan={7} style={{ padding: 24, textAlign: 'center', color: va.text3, fontSize: 12 }}>Không có kết quả.</td></tr>
                  )}
                </tbody>
              </table>
            )}
          </div>
        </VACard>
      </div>
    </div>
  )
}
