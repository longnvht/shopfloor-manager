# Desktop MES — Dashboard (Màn hình chính)

> Màn hình hiển thị ngay sau khi đăng nhập thành công.
> Thiết kế tối ưu cho màn hình cảm ứng shop floor.
> 
> **Implement status**: ✅ Done (2026-05-22) — `DashboardPage.xaml` + `DashboardViewModel.cs`
> **Target**: 10-inch 16:9, ~1280×720

---

## Tổng quan

Dashboard là **trung tâm điều hướng** của Desktop MES. Không dùng navigation bar/sidebar truyền thống — thay vào đó, toàn bộ thao tác thường xuyên được đưa về 1 màn hình duy nhất thông qua các **thẻ thông tin** và **shortcut grid**.

---

## Màu sắc chủ đạo

| Token | Giá trị | Dùng cho |
|---|---|---|
| `Background` | `#FFF8F0` | Nền toàn app (cam kem nhạt) |
| `Surface` | `#FFFFFF` | Nền thẻ/card |
| `Primary` | `#6D3B1A` | Header, nút chính, viền nổi bật (nâu) |
| `PrimaryLight` | `#A0522D` | Hover state, badge (nâu trung) |
| `Accent` | `#F57C00` | Icon, trạng thái đang chạy, highlight (cam) |
| `AccentLight` | `#FFE0B2` | Card đang active, badge nền (cam nhạt) |
| `TextPrimary` | `#3E2723` | Text chính (nâu rất đậm) |
| `TextSecondary` | `#795548` | Text phụ (nâu trung) |
| `TextHint` | `#A1887F` | Placeholder, hint (nâu nhạt) |
| `Divider` | `#EFEBE9` | Đường kẻ phân cách |
| `Success` | `#2E7D32` | Trạng thái hoàn thành |
| `Warning` | `#F9A825` | Trạng thái đã chọn / pending |
| `Error` | `#C62828` | Fail / NCR |

---

## Layout tổng thể

```
┌──────────────────────────────────────────────────┐  #FFF8F0
│  ┌─────────────────────────────────────────────┐ │
│  │           TOP BAR (nâu Primary)             │ │  56px
│  │  [Logo] Shopfloor Manager    [Logout] [Time]│ │
│  └─────────────────────────────────────────────┘ │
│                                                  │
│  ┌─────────────────┐  ┌──────────────────────┐  │
│  │  EMPLOYEE CARD  │  │    MACHINE CARD       │  │  ~160px
│  │                 │  │                      │  │
│  └─────────────────┘  └──────────────────────┘  │
│                                                  │
│  ┌─────────────────────────────────────────────┐ │
│  │              WORK INFO CARD                 │  │  ~200px
│  │  (Clickable — điều hướng tùy trạng thái)    │  │
│  └─────────────────────────────────────────────┘ │
│                                                  │
│  ┌─────────────────────────────────────────────┐ │
│  │           UTILITY SHORTCUTS                  │  │  flex
│  │  (Grid 2×N — thay đổi theo role & context)  │  │
│  └─────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

---

## Chi tiết từng thành phần

### 1. Top Bar

```
┌────────────────────────────────────────────────────┐
│ 🔶 SHOPFLOOR MANAGER          [Đăng xuất] 14:32:05 │
└────────────────────────────────────────────────────┘
```

- Nền `Primary (#6D3B1A)`, text trắng
- Đồng hồ thời gian thực cập nhật mỗi giây
- Nút Đăng xuất — nhỏ, góc phải

---

### 2. Employee Card

```
┌─────────────────────────────┐
│  👤  Nguyễn Văn A           │
│      Operator               │
│      Dept: Machining        │
└─────────────────────────────┘
```

- Viền trái màu `Primary` (4px)
- Avatar icon hoặc initials
- Hiển thị: Họ tên, Role, Phòng ban
- **Không clickable** — chỉ thông tin

---

### 3. Machine Card

```
┌──────────────────────────────────┐
│  🖥️  3100L                       │
│      LONG LATHE MACHINE          │
│      ● Online    API: Connected  │
└──────────────────────────────────┘
```

- Viền trái màu `Accent (#F57C00)` (4px)
- Hiển thị: MachineCode, MachineName (từ `local.json`)
- Status indicator: ● Online (xanh) / ● Offline (đỏ)
- **Không clickable** — chỉ thông tin

---

### 4. Work Info Card *(Thẻ quan trọng nhất — clickable)*

#### 4a. Khi chưa có công việc

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│  📋  Chưa có công việc                              │
│      Nhấn để chọn Job và bắt đầu sản xuất          │
│                                                     │
│              [ + Chọn Job ]                         │
│                                                     │
└─────────────────────────────────────────────────────┘
```

- Nền `#EFEBE9`, viền nét đứt màu `TextHint`
- Tap vào bất kỳ đâu → JobListPage

#### 4b. Đã chọn Job, chưa chọn OP

```
┌─────────────────────────────────────────────────────┐
│  JOB                          [→ Chọn OP]           │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  00006535                     SHAFT-50H6 Rev A      │
│  Giao: 10/01/2023             RunQty: 81 pcs        │
│                                                     │
│  OP  ——  Chưa chọn                                  │
│  SẢN PHẨM  ——  Chưa chọn                           │
└─────────────────────────────────────────────────────┘
```

- Tap vào card → OperationPage (chọn OP)

#### 4c. Đã chọn Job + OP, chưa chọn sản phẩm

```
┌─────────────────────────────────────────────────────┐
│  JOB  00006535                SHAFT-50H6 Rev A      │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  OP   010 — CNC Turning       Setup: 2h / Prod: 4h  │
│                                                     │
│  SẢN PHẨM  ——  [→ Chọn sản phẩm]                  │
│                                                     │
│  Progress  ████████░░░░ 6/81 hoàn thành             │
└─────────────────────────────────────────────────────┘
```

- Tap vào card → ProductListPage (chọn serial)

#### 4d. Đang gia công (WIP — có session open)

```
┌─────────────────────────────────────────────────────┐
│  JOB  00006535                SHAFT-50H6 Rev A      │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  OP   010 — CNC Turning                             │
│  SN   004  ⚙ ĐANG GIA CÔNG   ⏱  00:12:35           │
│                                                     │
│  FAI  ████████████░░░░  8/15 kích thước đã đo      │
│                                      [ → Vào FAI ] │
└─────────────────────────────────────────────────────┘
```

- Nền `#AccentLight (#FFE0B2)` — card nổi bật
- Viền `Accent (#F57C00)` 2px
- Timer đang chạy (update mỗi giây)
- Progress bar FAI
- Tap vào card → FAIPage (tiếp tục đo)

#### 4e. Sản phẩm vừa hoàn thành

```
┌─────────────────────────────────────────────────────┐
│  JOB  00006535                SHAFT-50H6 Rev A      │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  OP   010 — CNC Turning                             │
│  SN   004  ✅ HOÀN THÀNH     ⏱  00:45:12           │
│                                                     │
│  [ Chọn sản phẩm tiếp theo ]    [ Xem kết quả ]    │
└─────────────────────────────────────────────────────┘
```

- Nền trắng, viền `Success (#2E7D32)`
- 2 action buttons: chọn tiếp hoặc xem kết quả

---

### 5. Utility Shortcuts Grid *(2 cột, icon lớn)*

Mỗi shortcut là 1 nút vuông (~130×130px):

```
┌──────────────┐  ┌──────────────┐
│              │  │              │
│    📐        │  │    📄        │
│  Bắt đầu    │  │  Xem bản    │
│    FAI       │  │   vẽ        │
│              │  │              │
└──────────────┘  └──────────────┘
┌──────────────┐  ┌──────────────┐
│              │  │              │
│    📋        │  │    💾        │
│  Hướng dẫn  │  │  Load       │
│  công việc   │  │  G-code     │
│              │  │              │
└──────────────┘  └──────────────┘
```

**Style nút:**
- Nền `#FFFFFF`, viền `Divider`
- Icon 48px, màu `Primary`
- Text 13px, màu `TextPrimary`
- Pressed: nền `AccentLight`, viền `Accent`
- Disabled: opacity 40%, không tap được

#### Danh sách shortcuts và điều kiện hiển thị

| Shortcut | Icon | Điều kiện hiển thị | Hành động |
|---|---|---|---|
| **Bắt đầu FAI** | ⚙️ | Có session open (WIP) | → FAIPage |
| **Tiếp tục FAI** | 📊 | Có dims chưa đo trong session | → FAIPage |
| **Xem bản vẽ** | 📐 | Có OP được chọn + có file DRW | → DocumentViewer (DRW) |
| **Hướng dẫn gá đặt** | 🔩 | Có OP được chọn + có file FXT | → DocumentViewer (FXT) |
| **Hướng dẫn công việc** | 📋 | Có OP được chọn + có file RTC | → DocumentViewer (RTC) |
| **Load G-code** | 💾 | Có OP được chọn + có file GCD | → DocumentViewer (GCD) / Download |
| **Chọn Job** | 📌 | Chưa có job, hoặc muốn đổi | → JobListPage |
| **Lịch sử đo** | 📈 | Có product được chọn | → MeasureHistory |
| **Tạo NCR** | ⚠️ | QC / Engineer + có product | → NCR Dialog |
| **Bảng đo** | 🔢 | Có dims đã đo | → MeasureValueList |

#### Phân nhóm theo Role

| Role | Shortcuts thấy |
|---|---|
| **Operator** | FAI, Bản vẽ, Hướng dẫn gá, Công việc, Load G-code |
| **QC Inspector** | FAI, Bản vẽ, Lịch sử đo, Tạo NCR, Bảng đo |
| **Engineer** | Tất cả + (future: Edit OP, Upload Doc) |
| **Manager** | Lịch sử đo, Bảng đo, Chọn Job (read-only view) |

---

## Navigation flow từ Dashboard

```
Dashboard
  │
  ├── Work Info Card tap
  │     ├── Không có job         → JobListPage
  │     ├── Có job, không có OP  → OperationPage
  │     ├── Có OP, không có SN   → ProductListPage
  │     └── Có SN đang WIP       → FAIPage
  │
  ├── Shortcut: Bắt đầu FAI     → FAIPage
  ├── Shortcut: Xem bản vẽ      → DocumentViewer (filter DRW)
  ├── Shortcut: Hướng dẫn gá    → DocumentViewer (filter FXT)
  ├── Shortcut: Công việc        → DocumentViewer (filter RTC)
  ├── Shortcut: Load G-code      → DocumentViewer (filter GCD)
  ├── Shortcut: Chọn Job         → JobListPage
  ├── Shortcut: Tạo NCR          → NCR Dialog
  └── Shortcut: Lịch sử đo      → MeasureHistory
```

---

## State management

Dashboard cần duy trì **Work Context** — trạng thái công việc hiện tại của operator:

```csharp
public class WorkContext
{
    public JobSummaryDto?      CurrentJob     { get; set; }
    public PartOpDto?          CurrentOp      { get; set; }
    public ProductWithSessionDto? CurrentProduct { get; set; }
    public ProductionSessionDto?  ActiveSession  { get; set; }
    
    // Timer
    public DateTimeOffset? SessionStartedAt { get; set; }
    
    // Computed
    public bool HasJob     => CurrentJob is not null;
    public bool HasOp      => CurrentOp is not null;
    public bool HasProduct => CurrentProduct is not null;
    public bool IsWip      => ActiveSession?.Status == "open";
}
```

`WorkContext` là **singleton** trong DI — tất cả pages chia sẻ cùng 1 instance. Khi operator chọn job/op/product ở bất kỳ page nào, WorkContext được cập nhật và Dashboard tự refresh.

---

## UI States / Transitions

| Từ trạng thái | Hành động | Sang trạng thái |
|---|---|---|
| No job | Tap Work Card / Shortcut "Chọn Job" | JobListPage |
| Has job | Tap Work Card | OperationPage |
| Has job + OP | Tap Work Card | ProductListPage |
| Has session WIP | Tap Work Card / Shortcut FAI | FAIPage |
| Session complete | Auto-update card | Has OP (chọn product tiếp) |
| Back từ bất kỳ page | → | Dashboard (context giữ nguyên) |

---

## Responsive layout

Tối ưu cho **1920×1080** và **1280×800** (shop floor tablet/PC):

- **1920px**: Employee + Machine card side-by-side, Work card full width, Shortcuts 4 cột
- **1280px**: Employee + Machine side-by-side, Work card full width, Shortcuts 2 cột
- **< 1000px**: Stack vertical, shortcuts 2 cột

---

## Animation & Feedback

| Tương tác | Feedback |
|---|---|
| Tap card/button | Ripple effect màu `AccentLight` |
| Loading data | Skeleton loading (shimmer màu `#F5E6D3`) |
| Session start | Card Work Info chuyển màu `AccentLight`, timer bắt đầu |
| Session complete | Card chuyển màu `Success`, hiệu ứng fade |
| Shortcut disabled | Opacity 40%, icon xám |

---

## Phân biệt vs thiết kế cũ

| Điểm | Thiết kế cũ (sidebar) | Dashboard mới |
|---|---|---|
| Entry point | MainWindow → sidebar → JobList | Dashboard trực tiếp |
| Navigation | Sidebar cố định | Contextual — thay đổi theo work state |
| Work info | Không có | Thẻ trung tâm, always visible |
| Shortcuts | Menu list | Grid icon buttons |
| Màu sắc | BlueGrey / Cyan | Brown / Light Orange |
| Touch UX | Basic | Optimized — card lớn, ripple |

---

## Implement notes (thực tế vs spec)

**Layout thực tế** (khác spec ban đầu):
- Bỏ 2-column layout (left+right) → đổi thành 4-row vertical để phù hợp 10" 16:9
- Machine Card + Operator Card nằm cùng row (50/50), không phải separate section
- Work Info card compact hơn — không có 5 visual states riêng biệt, dùng visibility triggers
- Start/Stop button nằm inline trên Work Info card, không phải separate row

**Stats tracking** (in-memory, reset khi restart app):
- `_appStartTime` = khi Desktop app khởi động
- `_loginTime` = khi operator đăng nhập thành công
- `_totalActiveTime` += thời gian mỗi session khi Complete
- `_productsCreated` / `_productsCompleted` = đếm trong session

**WorkInfo card states** dùng `Visibility` binding:
- `HasWork=false` → Empty state (Chọn Job button)
- `HasWork=true` → hiện các column Job/OP/Serial tùy available
- `HasSession=false` → "Tiếp tục" button (navigate)
- `CanStart=true` → "Bắt đầu" button cam (PUT /start)
- `CanStop=true` → "Kết thúc" button đỏ + timer (PUT /complete)
