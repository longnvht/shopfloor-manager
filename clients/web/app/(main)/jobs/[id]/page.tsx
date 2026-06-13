'use client'

import { useEffect } from 'react'
import { useParams, useRouter } from 'next/navigation'

// Trang chi tiết Job đã hợp nhất vào /jobs (master-detail) — redirect deep-link cũ
export default function JobDetailRedirect() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()

  useEffect(() => { router.replace(`/jobs?jobId=${id}`) }, [id, router])

  return null
}
