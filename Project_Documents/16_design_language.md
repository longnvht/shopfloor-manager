# Desktop MES — Design Language

> Hệ thống thiết kế thống nhất cho toàn bộ Desktop MES (WPF).
> Tất cả màn hình đều tuân theo tài liệu này.

---

## 1. Màu sắc (Color Palette)

### Brand Colors

| Token | Hex | Dùng cho |
|---|---|---|
| `BrandBg` | `#FFF8F0` | Nền toàn app (cam kem nhạt) |
| `BrandPrimary` | `#6D3B1A` | TitleBar, nút chính, selected border |
| `BrandPrimaryLight` | `#A0522D` | Hover state, badge nâu |
| `BrandAccent` | `#F57C00` | Icon, timer, trạng thái active (cam) |
| `BrandAccentLight` | `#FFE0B2` | Card selected bg, pressed state (cam nhạt) |
| `BrandText` | `#3E2723` | Text chính (nâu rất đậm) |
| `BrandTextSecondary` | `#795548` | Text phụ (nâu trung) |

### Semantic Colors

| Mục đích | Màu | Hex |
|---|---|---|
| Success / Complete | Xanh lá | `#2E7D32` |
| Warning / Claimed | Vàng hổ phách | `#F57F17` |
| In Progress / Active | Cam đậm | `#E65100` |
| Error / Fail / Kết thúc | Đỏ | `#C62828` |
| Disabled border | Xám nhạt | `#E8D5C4` |
| Separator | Kem nhạt | `#F5E6D3` |

### App.xaml Keys
```xml
<SolidColorBrush x:Key="BrandBg"            Color="#FFF8F0"/>
<SolidColorBrush x:Key="BrandPrimary"        Color="#6D3B1A"/>
<SolidColorBrush x:Key="BrandAccent"         Color="#F57C00"/>
<SolidColorBrush x:Key="BrandAccentLight"    Color="#FFE0B2"/>
<SolidColorBrush x:Key="BrandText"           Color="#3E2723"/>
<SolidColorBrush x:Key="BrandTextSecondary"  Color="#795548"/>
```

MaterialDesign theme: `PrimaryColor="Brown" SecondaryColor="DeepOrange"` (BundledTheme)

---

## 2. Typography

| Element | FontSize | FontWeight | Color |
|---|---|---|---|
| TitleBar title | 15 | Bold | White |
| TitleBar subtitle | 11 | Normal | BrandAccentLight |
| Card heading | 15-17 | Bold/SemiBold | BrandText |
| Card body | 12-13 | Normal | BrandTextSecondary |
| Large number (serial, job#) | 20-32 | Bold | #212121 |
| Badge/label | 10-11 | Medium | varies |
| Stat value | 13 | SemiBold | BrandText |
| Clock | 17 | Normal | BrandAccentLight (Consolas) |

---

## 3. Layout Patterns

### 3.1 Dashboard (màn hình chính)

```
┌────────────────────────── TitleBar (52px) ──────────────────────────┐
│  [Logo] SHOPFLOOR MANAGER              [Clock HH:mm:ss]  [Logout]   │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──── Machine Card ────┐  ┌──── Operator Card ────┐               │
│  │ MachineCode/Name     │  │ Name / Role           │  (Auto height) │
│  │ Uptime / Active /    │  │ Check-in / Work time  │               │
│  │ Idle / SP hoàn thành │  │ Idle / SP tạo ra      │               │
│  └──────────────────────┘  └───────────────────────┘               │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────── Work Info Card (clickable) ─────────────────────┐   │
│  │  Job | OP | Serial | Status/Timer    [Bắt đầu] / [Kết thúc]│   │
│  └──────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────── Tiện ích (Utility Shortcuts) ───────────────────┐   │
│  │  [icon] [icon] [icon] [icon] ...  (WrapPanel, 120×80px)     │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Sub-pages (Job / OP / Product selection)

Pattern chung cho **mọi màn hình chọn**:

```
┌────────────────────── TitleBar (52px) ─────────────────────────┐
│  [Logo]  PAGE TITLE                              [Loading]      │
│          context breadcrumb (job#, part#...)                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────── Search Bar (60px) ──────────────────────┐     │
│  │  [TextBox hint="Tìm theo..."]           [🔍 Tìm]     │     │
│  └─────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Card Grid (Job/Product: UniformGrid 5 cols)                   │ (*)
│   OR Card List (OP: StackPanel full-width)                      │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  ┌─── Bottom Bar (64px) ───────────────────────────────────┐   │
│  │  [← Quay lại]                          [Lựa chọn →]   │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Grid vs List:**
- **UniformGrid Columns="5"**: Job cards, Product cards
- **StackPanel vertical**: OP cards (full width)

---

## 4. Components

### 4.1 TitleBar

```xml
<Border Background="{StaticResource BrandPrimary}">
    <!-- Chiều cao: 52px -->
    <!-- Content: Logo (trái) + Title (trái) + Loading (phải) -->
    <!-- Sub-pages: thêm context text (breadcrumb) nhỏ bên dưới title -->
</Border>
```

### 4.2 Card — Base Style

```xml
<Style x:Key="Card" TargetType="Border">
    Background="White"
    CornerRadius="10"
    Padding="16,12"
    Effect: DropShadow (BlurRadius=6, Opacity=0.08, Color=#6D3B1A)
</Style>
```

### 4.3 Selectable Card (ListBoxItem)

```xml
<!-- Normal state -->
Background="White" | BorderBrush="#E8D5C4" | BorderThickness=2

<!-- Selected state (IsSelected=True trigger) -->
Background="#FFE0B2" (BrandAccentLight)
BorderBrush="#6D3B1A" (BrandPrimary)
BorderThickness=3

<!-- Disabled -->
Opacity=0.5
```

### 4.4 Job Card (UniformGrid, Height=160)

```
┌──────────────────────┐
│ J2023-001            │  FontSize=17, Bold, BrandText
│ SHAFT-50H6           │  FontSize=12, BrandTextSecondary
│ Rev A  ·  81 pcs     │  FontSize=11, BrandTextSecondary
│                      │
│ [10/01/2023]         │  Badge: xám (đúng hạn) / đỏ (trễ)
└──────────────────────┘
```

### 4.5 OP Card (StackPanel, full-width)

```
┌─────────────────────────────────────────────────────────────────┐
│  [010]   CNC Turning            Setup: 2h  Prod: 4h    [badges] │
│  (badge  (OpType, bold 15)      (icons+text, 12)       [→]     │
│   nâu)   Mô tả nếu có (12, secondary)                          │
└─────────────────────────────────────────────────────────────────┘
```

OP Number Badge: `Background=BrandPrimary`, Width=60, Height=60, CornerRadius=8, White text.

### 4.6 Product Card (UniformGrid, Height=155)

```
┌──────────────────────┐
│        001           │  Serial, FontSize=28, Bold, center
│         ○            │  Icon theo status (26px)
│    SẴN SÀNG         │  Status text (11, SemiBold)
│    3100L  12:35     │  Machine + time (10px, gray)
└──────────────────────┘
```

**4 màu card product** (theo `StatusCode`):
| Status | Bg card | Icon | Text color |
|---|---|---|---|
| `available` | White | ○ gray | #757575 |
| `claimed` | White | ◷ amber | #F57F17 |
| `inprogress` | White | ⚙ orange | #E65100 |
| `complete` | White | ✓ green | #2E7D32 |

> Note: màu bg = white, màu phân biệt qua icon + text color. Selected state áp dụng chung (BrandAccentLight bg + BrandPrimary border).

### 4.7 Search Bar

```xml
<Border Background="White" CornerRadius="8" 
        BorderBrush="#E8D5C4" BorderThickness="1.5" Margin="12,6,12,0">
    <!-- Height: 60px (Row definition) -->
    <!-- Content: TextBox (*)  +  Button "🔍 Tìm" -->
    <!-- TextBox: kb:KeyboardBehavior.Mode="Qwerty" -->
    <!-- Enter key → SearchCommand -->
</Border>
```

### 4.8 Bottom Bar (Sub-pages)

```xml
<Border Background="White" BorderBrush="#E8D5C4" BorderThickness="0,1,0,0">
    <!-- Height: 64px -->
    <!-- Left: "← Quay lại" (MaterialDesignOutlinedButton, BrandPrimary color) -->
    <!-- Right: "Lựa chọn →" (MaterialDesignRaisedButton, BrandPrimary bg) -->
    <!-- "Lựa chọn" disabled khi chưa chọn item -->
</Border>
```

### 4.9 Utility Shortcut Button (Dashboard)

```xml
Width="120" Height="80" CornerRadius="10"
Background="White" BorderBrush="#E8D5C4"
<!-- Pressed: Background=BrandAccentLight, BorderBrush=BrandAccent -->
<!-- Icon: 28px, BrandPrimary -->
<!-- Label: 11px, Medium, max 2 dòng -->
```

### 4.10 Virtual Keyboard

Cả NumPad và QWERTY dùng **light theme**:
- Container: `Background="#FFF8F0"`, `BorderBrush="#A0522D"`, CornerRadius 14-16
- Key thường: `Background=White`, `BorderBrush="#A0522D"`, `Foreground="#3E2723"`
- Key Fn/Action: `Background="#FFE0B2"` (cam nhạt)
- Key Confirm: `Background="#2E7D32"` (xanh)
- Caps ON: `Background="#E65100"`, `Foreground=White` (cam đậm)
- NumPad display: `Background="#3E2723"`, `Foreground="#FF8C00"` (cam sáng)

---

## 5. Touch Guidelines

| Element | Minimum size |
|---|---|
| Button | Height ≥ 44px (BottomBar), Height ≥ 36px (Search) |
| Global button style | MinHeight = 56px |
| TextBox | MinHeight = 52px |
| DataGrid Row | MinHeight = 52px |
| Job/Product card | Width ~240px, Height = 155-170px |
| OP card | Width = full, Height ~90px |
| Utility shortcut | 120×80px |
| Keyboard key | 62-82px × 54-72px |

---

## 6. Navigation Patterns

### 6.1 Dashboard-centric

Dashboard là **màn hình gốc** — operator luôn quay về sau mỗi thao tác.

```
Login → Dashboard
  ↕ (Work Info card tap / shortcuts)
  ├── Job List ─→ OP List ─→ Product List ─→ Dashboard (session claimed)
  ├── FAI Page (from dashboard Start button)
  └── Document Viewer
```

### 6.2 WorkContext — State Management

`WorkContext` singleton giữ trạng thái công việc:

```csharp
CurrentJob     → set khi chọn Job
CurrentOp      → set khi chọn OP
CurrentProduct → set khi claim Product
ActiveSession  → set sau claim (ProductionSessionDto từ API)
```

**WorkState** (computed):
- `empty` → chưa có job
- `has-job` → có job, chưa có OP
- `has-op` → có job+OP, chưa có product
- `wip` → có session open (claimed/in-progress)
- `complete` → session vừa complete

### 6.3 Session Flow

```
ProductListPage: chọn card → "Lựa chọn →"
  → POST /api/v1/production-sessions
  → _work.SetProduct(product, session)
  → NavigateToDashboard()

Dashboard Work Info card:
  - CanStart = IsWip && !StartedAt → hiện nút "▶ Bắt đầu" (cam)
  - CanStop  = IsWip && StartedAt  → hiện nút "■ Kết thúc" (đỏ)
  - "Bắt đầu" → PUT /start → timer chạy
  - "Kết thúc" → PUT /complete → về Dashboard, Work Info reset product
```

---

## 7. Keyboard Behavior

`KeyboardBehavior.Mode` attached property:

| Mode | Khi nào dùng | Window hiện |
|---|---|---|
| `Qwerty` | TextBox nhập text (search, tên...) | QwertyWindow |
| `NumPad` | TextBox nhập số đo FAI | NumPadWindow |
| `None` | TextBox không cần keyboard | Không hiện |

Keyboard tự hiện khi TextBox gets focus, ẩn khi tap ngoài input area.
Cả 2 keyboard dùng `WS_EX_NOACTIVATE` — không steal focus từ TextBox.

---

## 8. Chuẩn mực XAML

| Rule | Chi tiết |
|---|---|
| Read-only binding | Luôn dùng `Mode=OneWay` cho computed properties |
| Border children | `Border` chỉ nhận 1 child — wrap nhiều panels trong `<Grid>` |
| Run.Text binding | `Run.Text="{Binding Prop, Mode=OneWay}"` — TwoWay sẽ crash |
| ListBox selection | Dùng `ListBox + ItemContainerStyle` thay vì code-behind tap |
| Load trigger | Gọi `InitializeAsync()` từ ViewModel navigation, không từ `View.Loaded` |
| Timer | `DispatcherTimer` trong ViewModel, `Stop()` khi cleanup/navigate away |

---

## 9. Screens Inventory

| Màn hình | File | Status |
|---|---|---|
| LoginWindow | `Views/LoginWindow.xaml` | ✅ |
| DashboardPage | `Views/Pages/DashboardPage.xaml` | ✅ |
| JobListPage | `Views/Pages/JobListPage.xaml` | ✅ |
| OperationPage | `Views/Pages/OperationPage.xaml` | ✅ |
| ProductListPage | `Views/Pages/ProductListPage.xaml` | ✅ |
| FAIPage | — | ⏳ Next |
| DocumentViewerPage | — | ⏳ |
| NCR Dialog | — | ⏳ |

---

## 10. Checklist khi thêm màn hình mới

- [ ] Dùng `BrandBg` làm background UserControl
- [ ] TitleBar: Brown `BrandPrimary`, 52px, có title + context breadcrumb
- [ ] Search bar nếu có danh sách dài (60px, white, CornerRadius 8)
- [ ] Card grid: `ListBox + ItemContainerStyle CardItem + UniformGrid/StackPanel`
- [ ] Selection: trigger `IsSelected` → BrandPrimary border 3px + BrandAccentLight bg
- [ ] Bottom bar: 64px, trắng, "← Quay lại" trái + action button phải
- [ ] Tất cả `Text="{Binding..."` trên computed property → `Mode=OneWay`
- [ ] TextBox cần keyboard → tag `kb:KeyboardBehavior.Mode`
- [ ] Register ViewModel trong `App.xaml.cs` và DataTemplate trong `App.xaml`
