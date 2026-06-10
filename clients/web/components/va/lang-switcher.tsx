'use client'

import { useTransition } from 'react'
import { useLocale } from 'next-intl'
import { useRouter } from 'next/navigation'
import { setLocale } from '@/app/actions/locale'
import type { AppLocale } from '@/i18n/request'

const OPTIONS: { id: AppLocale; label: string }[] = [
  { id: 'vi', label: 'VI' },
  { id: 'en', label: 'EN' },
]

export function VALangSwitcher() {
  const locale = useLocale()
  const router = useRouter()
  const [isPending, startTransition] = useTransition()

  function switchTo(next: AppLocale) {
    if (next === locale || isPending) return
    startTransition(async () => {
      await setLocale(next)
      router.refresh()
    })
  }

  return (
    <div style={{
      display: 'flex', alignItems: 'center', borderRadius: 6,
      border: '1px solid rgba(255,255,255,0.14)', overflow: 'hidden', flexShrink: 0,
    }}>
      {OPTIONS.map(opt => (
        <button
          key={opt.id}
          onClick={() => switchTo(opt.id)}
          disabled={isPending}
          style={{
            border: 'none', cursor: isPending ? 'default' : 'pointer',
            padding: '4px 8px', fontSize: 10.5, fontWeight: 700, letterSpacing: 0.5,
            background: locale === opt.id ? 'rgba(245,124,0,0.30)' : 'transparent',
            color: locale === opt.id ? '#fff' : 'rgba(255,255,255,0.50)',
          }}
        >
          {opt.label}
        </button>
      ))}
    </div>
  )
}
