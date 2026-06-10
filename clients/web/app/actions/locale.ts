'use server'

import { cookies } from 'next/headers'
import { locales, type AppLocale } from '@/i18n/request'

export async function setLocale(locale: AppLocale) {
  if (!locales.includes(locale)) return
  ;(await cookies()).set('NEXT_LOCALE', locale, {
    maxAge: 60 * 60 * 24 * 365,
    path: '/',
  })
}
