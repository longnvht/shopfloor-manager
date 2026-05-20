import type { ReactNode } from 'react'
import { Navbar } from '@/components/shared/navbar'

export default function MainLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-svh bg-background">
      <Navbar />
      <main className="container mx-auto px-4 py-6">{children}</main>
    </div>
  )
}
