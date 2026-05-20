'use client'

import { useRouter } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth.store'

export function Navbar() {
  const router = useRouter()
  const { user, logout } = useAuthStore()

  function handleLogout() {
    logout()
    router.push('/login')
  }

  return (
    <header className="border-b bg-background">
      <div className="container mx-auto flex h-14 items-center justify-between px-4">
        <span className="font-semibold">Shopfloor Manager</span>
        <div className="flex items-center gap-3">
          {user && (
            <span className="text-sm text-muted-foreground">
              {user.name} · {user.role}
            </span>
          )}
          <Button variant="outline" size="sm" onClick={handleLogout}>
            Đăng xuất
          </Button>
        </div>
      </div>
    </header>
  )
}
