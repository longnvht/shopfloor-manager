'use client'

import { useState, useEffect } from 'react'
import { usePathname, useRouter } from 'next/navigation'
import { useTranslations } from 'next-intl'
import { va } from '@/lib/va-tokens'
import { useAuthStore } from '@/stores/auth.store'
import { VALangSwitcher } from './lang-switcher'

// ── Navigation map ────────────────────────────────────────────────────────
// live: true  → route exists, clickable
// live: false → phase 5/6, shows "SOON"
const VA_NAV = [
  { groupKey: 'overview', items: [
    { id: 'dashboard', labelKey: 'dashboard', ico: '▦', href: '/dashboard',   live: true  },
  ]},
  { groupKey: 'production', items: [
    { id: 'parts',     labelKey: 'parts',     ico: '⊟', href: '/parts',       live: true  },
    { id: 'jobs',      labelKey: 'jobs',      ico: '◫', href: '/jobs',        live: true  },
    { id: 'planning',  labelKey: 'planning',  ico: '▤', href: '/planning',    live: true  },
    { id: 'cnc',       labelKey: 'cnc',       ico: '◈', href: '/cnc',         live: true  },
  ]},
  { groupKey: 'quality', items: [
    { id: 'fai',       labelKey: 'fai',       ico: '◉', href: '/fai',         live: true  },
    { id: 'ncr',       labelKey: 'ncr',       ico: '⊘', href: '/ncrs',        live: true  },
    { id: 'gage',      labelKey: 'gage',      ico: '⊡', href: '/gages',       live: true  },
    { id: 'calib',     labelKey: 'calib',     ico: '⟲', href: '/calibration', live: true  },
  ]},
  { groupKey: 'system', items: [
    { id: 'docs',      labelKey: 'docs',      ico: '◰', href: '/documents',   live: true  },
    { id: 'hr',        labelKey: 'hr',        ico: '◌', href: '/hr',          live: true  },
    { id: 'master',    labelKey: 'master',    ico: '⊞', href: '/master',      live: true  },
  ]},
]

function initials(name: string): string {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

export function VASidebar() {
  const pathname  = usePathname()
  const router    = useRouter()
  const t         = useTranslations('nav')
  const tCommon   = useTranslations('common')
  const { user, logout } = useAuthStore()
  // Avoid Zustand persist hydration mismatch — only render user info after mount
  const [mounted, setMounted] = useState(false)
  useEffect(() => { setMounted(true) }, [])

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
            {t('subtitle')}
          </div>
        </div>
      </div>

      {/* Nav groups */}
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: '10px 10px 14px' }}>
        {VA_NAV.map(sec => (
          <div key={sec.groupKey} style={{ marginBottom: 12 }}>
            <div style={{
              fontSize: 9.5, color: 'rgba(255,255,255,0.42)',
              letterSpacing: 1.4, textTransform: 'uppercase',
              padding: '4px 8px 6px', fontWeight: 600,
            }}>
              {t(`groups.${sec.groupKey}`)}
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
                    <span style={{ flex: 1, fontWeight: on ? 600 : 400 }}>{t(`items.${it.labelKey}`)}</span>
                    {!it.live && (
                      <span style={{ fontSize: 8, color: 'rgba(255,255,255,0.3)', letterSpacing: 0.5 }}>{tCommon('soon')}</span>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </div>

      {/* Language switcher */}
      <div style={{
        padding: '10px 14px', borderTop: '1px solid rgba(255,255,255,0.08)',
        display: 'flex', justifyContent: 'flex-end',
      }}>
        <VALangSwitcher />
      </div>

      {/* User footer — only render after client hydration to avoid Zustand persist mismatch */}
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
          {mounted && user ? initials(user.name) : ''}
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ fontSize: 12, fontWeight: 500, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            {mounted ? (user?.name ?? '—') : ''}
          </div>
          <div style={{ fontSize: 10, color: va.accentLt }}>
            {mounted ? (user?.role ?? '') : ''}
          </div>
        </div>
        <button
          onClick={handleLogout}
          title={tCommon('logout')}
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'rgba(255,255,255,0.5)', fontSize: 14, padding: 2 }}
        >
          ⏻
        </button>
      </div>
    </div>
  )
}
