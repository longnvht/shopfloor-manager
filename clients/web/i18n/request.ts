import { getRequestConfig } from 'next-intl/server'
import { cookies } from 'next/headers'

export const locales = ['vi', 'en'] as const
export type AppLocale = (typeof locales)[number]
export const defaultLocale: AppLocale = 'vi'

export default getRequestConfig(async () => {
  const cookieStore = await cookies()
  const cookieLocale = cookieStore.get('NEXT_LOCALE')?.value
  const locale: AppLocale = locales.includes(cookieLocale as AppLocale)
    ? (cookieLocale as AppLocale)
    : defaultLocale

  return {
    locale,
    messages: (await import(`../messages/${locale}.json`)).default,
  }
})
