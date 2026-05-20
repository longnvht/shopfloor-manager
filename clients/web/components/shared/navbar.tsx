'use client'

import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

const NAV = [
  { href: '/dashboard', label: 'Dashboard' },
  { href: '/parts', label: 'Parts' },
  { href: '/jobs', label: 'Jobs' },
  { href: '/ncrs', label: 'NCR' },
]

export function Navbar() {
  const router = useRouter()
  const pathname = usePathname()
  const { user, logout } = useAuthStore()

  function handleLogout() {
    logout()
    router.push('/login')
  }

  return (
    <header className="border-b bg-background">
      <div className="container mx-auto flex h-14 items-center gap-6 px-4">
        <span className="font-semibold shrink-0">Shopfloor Manager</span>

        <nav className="flex items-center gap-1">
          {NAV.map(item => (
            <Link
              key={item.href}
              href={item.href}
              className={`rounded-md px-3 py-1.5 text-sm transition-colors
                ${pathname.startsWith(item.href)
                  ? 'bg-muted font-medium text-foreground'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'}`}
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          {user && (
            <span className="text-sm text-muted-foreground hidden md:block">
              {user.name} · {user.role}
            </span>
          )}
          <Button variant="outline" size="sm" onClick={handleLogout}>Đăng xuất</Button>
        </div>
      </div>
    </header>
  )
}
