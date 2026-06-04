'use client'

import { usePathname, useRouter } from 'next/navigation'
import { va } from '@/lib/va-tokens'
import { useAuthStore } from '@/stores/auth.store'

// ── Navigation map ────────────────────────────────────────────────────────
// live: true  → route exists, clickable
// live: false → phase 5/6, shows "SOON"
const VA_NAV = [
  { group: 'Tổng quan', items: [
    { id: 'dashboard', label: 'Dashboard',          ico: '▦', href: '/dashboard',   live: true  },
  ]},
  { group: 'Sản xuất', items: [
    { id: 'parts',     label: 'Chi tiết kỹ thuật',  ico: '⊟', href: '/parts',       live: true  },
    { id: 'jobs',      label: 'Lệnh SX & Sản phẩm', ico: '◫', href: '/jobs',        live: true  },
    { id: 'planning',  label: 'Lập kế hoạch',        ico: '▤', href: '/planning',    live: true  },
    { id: 'cnc',       label: 'CNC Live',            ico: '◈', href: '/cnc',         live: true  },
  ]},
  { group: 'Chất lượng', items: [
    { id: 'fai',       label: 'FAI & Đo kiểm',ico: '◉', href: '/fai',         live: true  },
    { id: 'ncr',       label: 'NCR',          ico: '⊘', href: '/ncrs',        live: true  },
    { id: 'gage',      label: 'Dụng cụ đo',   ico: '⊡', href: '/gages',       live: true  },
    { id: 'calib',     label: 'Hiệu chuẩn',   ico: '⟲', href: '/calibration', live: true  },
  ]},
  { group: 'Hệ thống', items: [
    { id: 'docs',      label: 'Tài liệu KT',  ico: '◰', href: '/documents',   live: true  },
    { id: 'hr',        label: 'Nhân sự',      ico: '◌', href: '/hr',          live: true  },
    { id: 'master',    label: 'Master data',  ico: '⊞', href: '/master',      live: true  },
  ]},
]

function initials(name: string): string {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

export function VASidebar() {
  const pathname  = usePathname()
  const router    = useRouter()
  const { user, logout } = useAuthStore()

  function handleNav(href: string) {
    router.push(href)
  }

  function handleLogout() {
    logout()
    router.push('/login')
  }

  return (
    <div style={{
      width: 224, background: va.primary, color: '#fff',
      display: 'flex', flexDirection: 'column', flexShrink: 0, height: '100%',
    }}>
      {/* Logo */}
      <div style={{
        padding: '18px 18px 16px',
        borderBottom: '1px solid rgba(255,255,255,0.08)',
        display: 'flex', alignItems: 'center', gap: 11,
      }}>
        <div style={{
          width: 30, height: 30, borderRadius: 7, background: va.accent,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontWeight: 700, fontSize: 15, color: '#fff', flexShrink: 0,
        }}>S</div>
        <div>
          <div style={{ fontSize: 13.5, fontWeight: 700, letterSpacing: 0.4 }}>SHOPFLOOR</div>
          <div style={{ fontSize: 9.5, color: va.accentLt, letterSpacing: 1.6, textTransform: 'uppercase' }}>
            Manager · Office
          </div>
        </div>
      </div>

      {/* Nav groups */}
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: '10px 10px 14px' }}>
        {VA_NAV.map(sec => (
          <div key={sec.group} style={{ marginBottom: 12 }}>
            <div style={{
              fontSize: 9.5, color: 'rgba(255,255,255,0.42)',
              letterSpacing: 1.4, textTransform: 'uppercase',
              padding: '4px 8px 6px', fontWeight: 600,
            }}>
              {sec.group}
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {sec.items.map(it => {
                const on = pathname.startsWith(it.href) && it.live
                return (
                  <div
                    key={it.id}
                    className={'va-nav-item' + (it.live ? ' va-clickable' : '')}
                    onClick={() => it.live && handleNav(it.href)}
                    style={{
                      display: 'flex', alignItems: 'center', gap: 11,
                      padding: '8px 10px', borderRadius: 7, fontSize: 13,
                      background:  on ? 'rgba(245,124,0,0.20)' : 'transparent',
                      color:       on ? '#fff' : it.live ? 'rgba(255,255,255,0.80)' : 'rgba(255,255,255,0.35)',
                      boxShadow:   on ? `inset 2px 0 0 ${va.accent}` : 'none',
                      cursor:      it.live ? 'pointer' : 'default',
                    }}
                  >
                    <span style={{ width: 15, textAlign: 'center', color: on ? va.accent : 'inherit', fontSize: 13, opacity: it.live ? 1 : 0.5 }}>
                      {it.ico}
                    </span>
                    <span style={{ flex: 1, fontWeight: on ? 600 : 400 }}>{it.label}</span>
                    {!it.live && (
                      <span style={{ fontSize: 8, color: 'rgba(255,255,255,0.3)', letterSpacing: 0.5 }}>SOON</span>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </div>

      {/* User footer */}
      <div style={{
        padding: 14, borderTop: '1px solid rgba(255,255,255,0.08)',
        display: 'flex', alignItems: 'center', gap: 10,
      }}>
        <div style={{
          width: 32, height: 32, borderRadius: '50%',
          background: va.accentLt, color: va.primary,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontWeight: 600, fontSize: 12, flexShrink: 0,
        }}>
          {user ? initials(user.name) : '?'}
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 12, fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {user?.name ?? '—'}
          </div>
          <div style={{ fontSize: 10, color: va.accentLt }}>
            {user?.role ?? ''}
          </div>
        </div>
        <button
          onClick={handleLogout}
          title="Đăng xuất"
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'rgba(255,255,255,0.5)', fontSize: 14, padding: 2 }}
        >
          ⏻
        </button>
      </div>
    </div>
  )
}
