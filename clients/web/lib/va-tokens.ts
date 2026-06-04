// VA Design Tokens — Warm Industrial palette
// Converted from D:/Temple/Shopfloor Manage/src/va-kit.jsx
// Source of truth for all VA components (inline styles).
// The same values are also mapped to CSS custom properties in globals.css.

export const va = {
  // ── Brand surfaces ────────────────────────────────────────────
  bg:        '#FFF8F0',
  surface:   '#FFFFFF',
  surface2:  '#FBF3E7',
  surface3:  '#F6EADb',
  border:    '#E8D5C4',
  borderStr: '#D9C1A6',
  separator: '#F5E6D3',

  // ── Brand colors ──────────────────────────────────────────────
  primary:   '#6D3B1A',
  primaryLt: '#A0522D',
  accent:    '#F57C00',
  accentLt:  '#FFE0B2',
  accentBg:  '#FFF3DC',

  // ── Text ──────────────────────────────────────────────────────
  text:      '#3E2723',
  text2:     '#795548',
  text3:     '#9B8473',

  // ── Semantic ─────────────────────────────────────────────────
  ok:        '#2E7D32',  okBg:    '#E8F2E8',
  warn:      '#F57F17',  warnBg:  '#FFF4D6',
  active:    '#E65100',  activeBg:'#FFE4CC',
  err:       '#C62828',  errBg:   '#FBE9E9',

  // ── Typography ───────────────────────────────────────────────
  font:    "'Inter', system-ui, -apple-system, sans-serif",
  serif:   "'Fraunces', Georgia, serif",
  mono:    "'JetBrains Mono', ui-monospace, 'SF Mono', Menlo, monospace",

  // ── Shadows ──────────────────────────────────────────────────
  shadow:   '0 1px 2px rgba(109,59,26,0.04), 0 4px 12px rgba(109,59,26,0.06)',
  shadowLg: '0 4px 16px rgba(109,59,26,0.08), 0 12px 32px rgba(109,59,26,0.12)',
} as const

export type VaBadgeKind = 'ok' | 'warn' | 'err' | 'neutral' | 'primary' | 'running'
export type VaBtnKind   = 'primary' | 'accent' | 'ghost'
