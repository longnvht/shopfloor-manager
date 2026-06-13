# Web App — Design Language (VA Warm Industrial)

> Hệ thống thiết kế dùng chung cho `clients/web` (Next.js 16 + VA components).
> Rút ra từ 3 view đã hoàn thiện theo mockup mới: `/parts` + `/parts/[id]/operations`, `/dimsheet`, `/documents` (2026-06-12 → 2026-06-13).
> Áp dụng cho mọi view mới — đặc biệt các phase còn ⏳ trong CLAUDE.md (Phase H/I/J/K).
> Tương đương [`16_design_language.md`](16_design_language.md) (Desktop WPF) nhưng cho Web.

---

## 1. Design Tokens (`lib/va-tokens.ts`)

Mọi màu sắc/font lấy từ object `va` — KHÔNG hardcode hex trong component.

```ts
import { va, type VaBadgeKind, type VaBtnKind } from '@/lib/va-tokens'
```

| Token | Hex | Dùng cho |
|---|---|---|
| `va.bg` | `#FFF8F0` | Background toàn page |
| `va.surface` | `#FFFFFF` | Card, table, input background |
| `va.surface2` | `#FBF3E7` | Sticky `<th>` background, hover row, dropdown highlight |
| `va.surface3` | `#F6EADb` | Background phụ (ít dùng) |
| `va.border` | `#E8D5C4` | Border card/input/table |
| `va.borderStr` | `#D9C1A6` | Border đậm hơn (scrollbar thumb) |
| `va.separator` | `#F5E6D3` | Divider trong card (header/footer) |
| `va.primary` | `#6D3B1A` | Brand chính — nút primary, badge "primary", OP badge |
| `va.primaryLt` | `#A0522D` | Hover/focus border, accent phụ |
| `va.accent` | `#F57C00` | Nút accent, highlight |
| `va.accentLt` / `va.accentBg` | `#FFE0B2` / `#FFF3DC` | Badge primary bg, combobox highlight |
| `va.text` | `#3E2723` | Text chính |
| `va.text2` | `#795548` | Text phụ/label |
| `va.text3` | `#9B8473` | Text mờ (placeholder, meta, uppercase label) |
| `va.ok` / `va.okBg` | `#2E7D32` / `#E8F2E8` | Pass, Approved, Max value |
| `va.warn` / `va.warnBg` | `#F57F17` / `#FFF4D6` | Pending, cảnh báo |
| `va.active` / `va.activeBg` | `#E65100` / `#FFE4CC` | Running, in-progress |
| `va.err` / `va.errBg` | `#C62828` / `#FBE9E9` | Fail, Rejected, Min value, critical |
| `va.font` | Inter | Body text |
| `va.serif` | Fraunces | Tiêu đề trang (`VATopbar` title) |
| `va.mono` | JetBrains Mono | Số liệu, mã (part number, balloon, size...) |
| `va.shadow` / `va.shadowLg` | — | Card shadow / dropdown shadow |

---

## 2. Component Library (`components/va/`)

Import qua barrel `@/components/va`.

| Component | Props chính | Dùng khi |
|---|---|---|
| `VATopbar` | `title, breadcrumb?, right?` | Header mỗi page — breadcrumb uppercase nhỏ + title serif |
| `VACard` | `title?, sub?, right?, children, pad=true, style?, className?` | Mọi khối nội dung. `pad={false}` khi bọc table cần full-bleed |
| `VAKpi` | `label, value, sub?, trend?, accent?` | Ô số liệu trong KPI strip |
| `VABadge` | `children, kind?: VaBadgeKind, dot?` | Status/label nhỏ. `kind`: `ok/warn/err/neutral/primary/running` |
| `VABtn` | `children, kind?: VaBtnKind, ...buttonProps` | Nút. `kind`: `primary/accent/ghost` (default `ghost`) |
| `VASeg` | `options: {id,label}[], value, onChange?` | Segmented control — tab switcher trong detail panel |
| `VACombobox` | `value, onChange, options: {value,label}[], placeholder?, style?` | Select gõ-để-tìm cho filter bar khi list lớn (Part, OP...) |
| `VASidebar` | — | Shell, không cần dùng trực tiếp trong page |

**Quy ước bổ sung:**
- `VABadge kind="primary"` dùng cho badge nhấn (vd "Bản vẽ Rev E", OP number)
- `VABtn` không set `kind` → `ghost` (viền nâu nhạt, nền trắng) — dùng cho action phụ (+, Import, Edit)
- `VACard`'s `right` slot là nơi đặt nút hành động của card (+, Import OPs, Quản lý tài liệu →)

---

## 3. Page Shell — bắt buộc cho mọi page root

```tsx
export default function SomePage() {
  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title={t('title')} breadcrumb={t('breadcrumb')} />
      <div style={{ flex: 1, display: 'flex', minHeight: 0, padding: 20, gap: 16 /* hoặc flexDirection: 'column' */ }}>
        {/* content */}
      </div>
    </div>
  )
}
```

- `minHeight: 0` trên **mọi** flex container lồng nhau trên đường đi tới phần tử cần cuộn — thiếu 1 cấp là bị `(main)/layout.tsx`'s `overflow:hidden` clip nội dung (xem CLAUDE.md mục "Scroll trong layout flex").
- `breadcrumb` format: `"{Nhóm sidebar} › {Tên view}"` hoặc sâu hơn `"Sản xuất › Chi tiết kỹ thuật › {part} › Operations"` khi page con (đặt namespace key riêng, có placeholder).

---

## 4. Layout Patterns

### 4.1 Master-detail (list 280px + detail flex-1)

Dùng khi có 1 danh sách chọn (Part, OP...) rồi hiện chi tiết bên phải. Tham chiếu: `parts/page.tsx`, `dimsheet/page.tsx`, `parts/[id]/operations/page.tsx`.

```tsx
<div style={{ flex: 1, display: 'flex', minHeight: 0, gap: 16, padding: 20 }}>
  {/* Panel trái — 280px */}
  <VACard pad={false} style={{ width: 280, flexShrink: 0, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
    <div style={{ padding: 12, borderBottom: `1px solid ${va.separator}` }}>
      {/* search input */}
    </div>
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', minHeight: 0 }}>
      {items.map(item => (
        <div key={item.id} className="va-clickable va-row"
          onClick={() => setSelected(item)}
          style={{
            padding: '10px 14px', borderBottom: `1px solid ${va.separator}`,
            background: selected?.id === item.id ? va.accentBg : 'transparent',
          }}>
          <div style={{ fontFamily: va.mono, fontWeight: 600, fontSize: 13 }}>{item.code}</div>
          <div style={{ fontSize: 11.5, color: va.text2 }}>{item.description}</div>
          {/* meta row: routing/op-count/created date — fontSize 10.5, color va.text3 */}
        </div>
      ))}
    </div>
    {/* pagination footer nếu có — padding 10px, border-top separator */}
  </VACard>

  {/* Panel phải — detail */}
  <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, gap: 12, overflow: 'auto' }} className="va-scroll">
    {!selected ? (
      <VACard style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3 }}>
        {t('selectPart')}
      </VACard>
    ) : (
      <>{/* header, KPI strip, content cards */}</>
    )}
  </div>
</div>
```

- Selected item trong panel trái: `background: va.accentBg`
- Empty state khi chưa chọn gì: `VACard` full height, text căn giữa, màu `va.text3`

### 4.2 Detail header

```tsx
<div style={{ display: 'flex', alignItems: 'baseline', gap: 10 }}>
  <div style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{part.partNumber}</div>
  <VABadge kind="primary">{t('revBadge', { rev: partRevCode })}</VABadge>
</div>
<div style={{ fontSize: 12.5, color: va.text2 }}>{part.description}</div>
```

### 4.3 KPI Strip

4 ô `VAKpi` trong 1 row, `gap: 12`, mỗi ô `flex: 1` (tự co giãn bằng `VAKpi`'s internal `flex:1`):

```tsx
<div style={{ display: 'flex', gap: 12 }}>
  <VAKpi label={t('kpi.totalDims')} value={dims.length} />
  <VAKpi label={t('kpi.uniqueBalloons')} value={uniqueCount} />
  <VAKpi label={t('kpi.faiFinal')} value={finalCount} sub={t('kpi.faiFinalSub')} />
  <VAKpi label={t('kpi.opsWithDims')} value={`${opsWithDims}`} sub={t('kpi.opsWithDimsSub', { total: ops.length })} />
</div>
```

- `sub` dùng cho ghi chú nhỏ ("/ N OP", "chỉ QC nhập")
- `trend`/`accent` chưa dùng ở 3 view này — chỉ dashboard

### 4.4 Filter Bar

1 `VACard` (hoặc `<div>` style card) chứa hàng ngang: combobox filters → separator → chips/checkbox → search input → counter → clear link. Tham chiếu: `documents/page.tsx`, `dimsheet/page.tsx`.

```tsx
<div style={{
  display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap',
  background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11,
  padding: '10px 14px', boxShadow: va.shadow,
}}>
  <FilterField label={t('filter.part')}>
    <VACombobox value={fPart} onChange={setFPart} options={partOptions} placeholder="..." />
  </FilterField>

  {/* separator */}
  <div style={{ width: 1, height: 24, background: va.separator }} />

  {/* category/type chips — xem 4.6 */}

  <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: va.text2 }}>
    <input type="checkbox" checked={finalOnly} onChange={e => setFinalOnly(e.target.checked)} />
    {t('filter.finalOnly')}
  </label>

  <input
    placeholder={t('filter.searchBalloon')}
    value={search} onChange={e => setSearch(e.target.value)}
    style={{ height: 32, padding: '0 10px', border: `1px solid ${va.border}`, borderRadius: 7, fontSize: 12.5, background: va.bg }}
  />

  <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 10 }}>
    <span style={{ fontFamily: va.mono, fontSize: 12, color: va.text3 }}>{filtered.length}/{total}</span>
    {anyFilter && <span className="va-clickable" onClick={reset} style={{ fontSize: 12, color: va.primary }}>{t('filter.clear')}</span>}
  </div>
</div>
```

- **Combobox cho danh sách lớn** (Part, OP, file type, status...) — `VACombobox`, option list xây bằng `useMemo`, luôn có 1 option "Tất cả ..." với `value: 'all'`
- **Cascading filter**: chọn Part → reset Drawing Rev/Routing Rev/OP (`pickPart()` helper) — option list của các combobox con phụ thuộc combobox cha
- **Counter**: `{filtered.length}/{total}` mono font, màu `va.text3`
- **Clear filters**: chỉ hiện khi `anyFilter` true, text màu `va.primary`, click → `reset()`

### 4.5 Type/Category Legend Chips

Chips màu theo loại, click để filter nhanh (toggle), đặt cạnh filter bar hoặc trong nó:

```tsx
const FILE_TYPE_COLORS: Record<string, string> = { DRW: va.primary, GCD: va.accent, /* ... */ }

{Object.entries(FILE_TYPE_COLORS).map(([type, color]) => (
  <div key={type} className="va-clickable"
    onClick={() => setFType(fType === type ? 'all' : type)}
    style={{
      display: 'flex', alignItems: 'center', gap: 5, padding: '4px 9px', borderRadius: 6,
      fontSize: 11, fontWeight: 600, fontFamily: va.mono,
      border: `1px solid ${fType === type ? color : va.border}`,
      background: fType === type ? `${color}1A` : va.surface,
      color: fType === type ? color : va.text2,
    }}>
    <span style={{ width: 7, height: 7, borderRadius: '50%', background: color }} />
    {type} ({countByType[type] ?? 0})
  </div>
))}
```

- Mỗi loại 1 màu cố định trong `Record<string, string>` map ở đầu file
- Active state: border + text màu của loại, background = màu + alpha `1A`
- Inactive: border `va.border`, text `va.text2`, background `va.surface`

### 4.6 Table Pattern

```tsx
<VACard title={t('table.title')} pad={false} style={{ flex: 1, minHeight: 0 }}>
  <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead>
        <tr>
          {TABLE_COLS.map(col => (
            <th key={col} style={{
              position: 'sticky', top: 0, zIndex: 1, background: va.surface2,
              textAlign: 'left', padding: '8px 12px', fontSize: 9.5, fontWeight: 700,
              textTransform: 'uppercase', letterSpacing: 0.5, color: va.text3,
              borderBottom: `1px solid ${va.border}`,
            }}>
              {t(`table.headers.${col}`)}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map(row => (
          <tr key={row.id} className="va-row va-clickable" onClick={() => onRowClick(row)}
            style={{ borderBottom: `1px solid ${va.separator}` }}>
            <td style={{ padding: '8px 12px' }}>{row.value}</td>
            {/* ... */}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
</VACard>
```

- Cột header lấy từ `TABLE_COLS: (keyof ...)[]` array → `t(\`table.headers.${col}\`)` — thêm cột mới chỉ cần thêm vào array + key i18n
- Sticky `<th>`: `position: sticky, top: 0, zIndex: 1, background: va.surface2` — **bắt buộc** nếu không header sẽ cuộn theo nội dung
- Row hover: class `va-row` (định nghĩa sẵn trong `globals.css` → `background: #FBF3E7`)
- Row clickable: thêm class `va-clickable` nếu có `onClick`
- Numeric/mono columns (balloon, nominal, max/min, size...) → `fontFamily: va.mono`
- Max value màu `va.ok`, Min value màu `va.err` (quy ước cho cột dimension)
- Empty/no-match states: xem mục 6

### 4.7 Inline-edit Pattern (✎ / ✓ / ✕)

State: `editingId: number | null` + `editForm: {...}`. Tham chiếu: `dimsheet/page.tsx`.

```tsx
{editingId === row.id ? (
  <>
    <td><input value={editForm.nominal} onChange={...} style={{ width: 70, ...inputStyle }} /></td>
    <td>{previewLimit(editForm).max}</td>  {/* preview realtime, chưa lưu */}
    <td>
      <button onClick={() => handleSave(row.id)} title={t('edit.save')}>✓</button>
      <button onClick={() => setEditingId(null)} title={t('edit.cancel')}>✕</button>
    </td>
  </>
) : (
  <>
    <td>{row.nominal}</td>
    <td>{row.max}</td>
    <td>
      {!row.isTextType && (
        <button onClick={() => startEdit(row)} title={t('edit.tooltip')} className="va-clickable">✎</button>
      )}
    </td>
  </>
)}
```

- `previewLimit(editForm)` tính lại giá trị derived (Max/Min) realtime trong lúc edit — không gọi API cho tới khi `handleSave`
- Dimension `isTextType` → không cho edit, ẩn nút ✎
- Sau `handleSave` thành công → gọi lại API list hoặc cập nhật local state, set `editingId = null`

### 4.8 Tabs trong detail panel (`VASeg`)

```tsx
const [tab, setTab] = useState<'documents' | 'dimension'>('documents')

<VASeg
  value={tab}
  onChange={id => setTab(id as typeof tab)}
  options={[
    { id: 'documents', label: t('opDetail.tabs.documents') },
    { id: 'dimension', label: t('opDetail.tabs.dimension') },
  ]}
/>
{tab === 'documents' ? <DocumentsTable /> : <DimensionsTable />}
```

- Mỗi tab render 1 `VACard pad={false}` table riêng theo pattern 4.6
- Đặt `VASeg` ngay dưới detail header, trên KPI/table

### 4.9 Status Badge Pattern

```tsx
const STATUS_KIND: Record<string, VaBadgeKind> = {
  Pending: 'warn', Approved: 'ok', Rejected: 'err',
}

<VABadge kind={STATUS_KIND[doc.status]}>{t(`status.${doc.status}`)}</VABadge>
```

- **Pattern chuẩn hiện tại**: tách `STATUS_KIND` (chỉ map màu) riêng khỏi label; label luôn lấy qua `t(\`status.${value}\`)` — key động named theo enum value gốc (PascalCase, khớp backend enum)
- **Pattern cũ (không dùng cho view mới)**: `STATUS_META: Record<string, {label, kind}>` với label hardcode tiếng Anh/Việt trực tiếp — còn tồn tại ở `parts/[id]/operations/page.tsx`, sẽ cần dọn khi sửa view đó

---

## 5. i18n Conventions

- 1 namespace riêng theo route (`parts`, `dimsheet`, `documents`...); sub-page dùng nested key (`parts.opDetail.*`)
- `"use client"` + `const t = useTranslations('namespace')`
- Cả `messages/vi.json` và `messages/en.json` phải có **cùng cấu trúc key**, chỉ khác giá trị
- Placeholder: `t('key', { name, date, count })` — key dùng `{name}` trong JSON
- Date format: `useLocale()` rồi `new Date(x).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')` — không hardcode `'vi-VN'`
- Key động (status, category, label theo enum) — dùng pattern `t(\`group.${key}\`)`, KHÔNG hardcode label trong `Record<string, string>`
- Breadcrumb/title luôn qua `t()`, không hardcode tiếng Việt dù page mới viết bằng tiếng Việt trước

---

## 6. Empty / Loading / No-match States

| Trạng thái | Khi nào | Nội dung |
|---|---|---|
| `loading` | Đang fetch lần đầu | `VACard` căn giữa, text `t('loading')`, màu `va.text3` |
| `selectX` | Chưa chọn item ở panel trái | `VACard` flex:1 căn giữa, text `t('selectPart')` |
| `noRouting`/`empty` | Có item nhưng không có data con (vd Part chưa có Routing) | Text căn giữa trong vị trí bảng, màu `va.text3` |
| `noMatch` | Có data nhưng filter không khớp | Text riêng (khác `empty`) + gợi ý xoá filter |

Tất cả các state trên đặt `key` riêng trong i18n (không dùng chung 1 string "Không có dữ liệu" cho mọi trường hợp) — giúp phân biệt "chưa có gì" vs "có nhưng bị filter ẩn".

---

## 7. Known Gaps (chưa xử lý, lưu ý khi gặp)

- `VACombobox`'s `Combobox.Empty` hardcode tiếng Việt `"Không có kết quả"` (`components/va/combobox.tsx:65`) — chưa i18n. Nếu sửa, cần truyền prop `emptyLabel` hoặc đưa `useTranslations('common')` vào component.
- `parts/[id]/operations/page.tsx` còn dùng `STATUS_META` (label hardcode tiếng Anh) — nên đổi sang `STATUS_KIND` + `t('status.*')` khi page này được sửa tiếp (vd Phase G follow-up).

---

## 8. Pages Inventory (theo pattern này)

| Page | Master-detail | KPI strip | Filter bar | Table | Inline-edit | Tabs |
|---|---|---|---|---|---|---|
| `/parts` | ✅ | ✅ | — | ✅ (OP table) | — | — |
| `/parts/[id]/operations` | ✅ | — | — | ✅ x2 | — | ✅ |
| `/dimsheet` | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| `/documents` | — (flat list) | ✅ | ✅ | ✅ | — | — |

Các view ⏳ (Phase H/I/J/K trong CLAUDE.md) nên đối chiếu bảng này để biết pattern nào tái dùng được.

---

## 9. Checklist khi thêm view mới

- [ ] Page root: `flex:1, display:'flex', flexDirection:'column', minWidth:0, minHeight:0, background: va.bg` + `VATopbar`
- [ ] Mọi container lồng trên đường tới phần tử cuộn đều có `minHeight: 0`
- [ ] Namespace i18n mới trong CẢ `messages/vi.json` và `messages/en.json`, cùng cấu trúc key
- [ ] Không hardcode hex màu — dùng `va.*` từ `lib/va-tokens`
- [ ] List dài/table → `VACard pad={false}` + `.va-scroll` wrapper + sticky `<th>` (`va.surface2`, `zIndex:1`)
- [ ] Status/label → `VABadge` + `VaBadgeKind` map riêng khỏi label, label qua `t(\`status.${value}\`)`
- [ ] Filter combobox cho list lớn → `VACombobox`, không dùng `<select>` thô
- [ ] Date hiển thị → `useLocale()` + `toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')`
- [ ] Empty/loading/no-match có key i18n riêng, không tái dùng 1 string chung
- [ ] Action button trong card → `right` prop của `VACard`, `VABtn kind="ghost"` (default)
