import type { ReactNode } from 'react'
import { VASidebar } from '@/components/va/sidebar'

export default function MainLayout({ children }: { children: ReactNode }) {
  return (
    <div className="flex h-full overflow-hidden">
      <VASidebar />
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {children}
      </div>
    </div>
  )
}
