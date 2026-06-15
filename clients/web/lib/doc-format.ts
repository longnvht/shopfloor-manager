export const FILE_TYPE_COLORS: Record<string, string> = {
  DRW: '#6D3B1A', GCD: '#E65100', RTC: '#5D4037',
  FXT: '#A0522D', TLS: '#795548', THD: '#F57C00', CAM: '#8D6E63', CAD: '#6D4C41',
}

export function formatBytes(bytes: number | null): string {
  if (bytes == null) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}
