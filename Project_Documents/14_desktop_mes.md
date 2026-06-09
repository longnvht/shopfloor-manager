# Phase 4 — Desktop MES (Shop Floor Client)

> Nguồn tham khảo: Vinam-MES WinForms (FANUC.sln) — rewrite theo kiến trúc mới (WPF + REST API + MinIO + JWT).
> 
> **Dashboard UI spec**: xem [`15_dashboard_desktop.md`](15_dashboard_desktop.md) — màu sắc, layout thẻ, shortcut grid, WorkContext state management.
> 
> **Design Language**: xem [`16_design_language.md`](16_design_language.md) — hệ thống thiết kế thống nhất toàn bộ Desktop app (màu sắc, component, navigation pattern, checklist màn hình mới).

---

## Tổng quan

Desktop MES là ứng dụng chạy tại shop floor — trên máy tính công nghiệp hoặc tablet gắn cạnh máy CNC. Operator, QC, Engineer dùng app này để:

- Nhận Job, chọn Operation đang làm
- Nhập kết quả đo FAI (First Article Inspection) tại máy
- Xem tài liệu kỹ thuật (Drawing, G-code, Route Card, Fixture)
- Tạo NCR khi sản phẩm không đạt
- Theo dõi trạng thái sản xuất real-time

## Trạng thái implement *(cập nhật 2026-06-09, phase 4b hoàn tất)*

| Tính năng | Trạng thái |
|---|---|
| LoginWindow, MainWindow shell | ✅ Done |
| JobListPage (TitleBar + Search + UniformGrid 5 cols + BottomBar) | ✅ Done |
| OperationPage (TitleBar + Search + full-width cards + BottomBar) | ✅ Done |
| ProductListPage (TitleBar + Search + UniformGrid 5 cols + BottomBar) | ✅ Done |
| Virtual Keyboard (NumPad + QWERTY, light theme, no-focus, drag) | ✅ Done |
| ProductionSession backend + API (begin/complete/cancel/force-complete) | ✅ Done |
| WorkContext singleton — dual context (Operation + View slots) | ✅ Done |
| Dashboard (4-row layout, Machine/Operator stats, Start/Stop/ForceFinish) | ✅ Done |
| Design Language (16_design_language.md) | ✅ Done |
| FAIPage — dimension card grid, NumPad/PASS·FAIL, POST measure, auto-advance | ✅ Done |
| FAI one-time entry (IsInputLocked — dimension đã đo lock re-entry) | ✅ Done |
| NCR dialog (khi dimension Fail) | ✅ Done |
| Operation_Mode / View_Mode (dual context, toggle chip, independent nav) | ✅ Done |
| Force-finish session (Leader/Admin kết thúc session của người khác) | ✅ Done |
| Session resume on login (khôi phục WorkContext từ active inprogress session) | ✅ Done |
| View Mode product selection (xem product không tạo session) | ✅ Done |
| DragScrollBehavior (drag-to-scroll trên tất cả list pages) | ✅ Done |
| DocumentViewer — G-code text viewer | ✅ Done |
| Session constraint redesign — claim client-side, BeginSession atomically | ✅ Done |
| Shortcut lock khi inprogress (Chọn Job/OP/SP disabled, opacity 0.4) | ✅ Done |
| DocumentViewer — PDF viewer (WebView2 — Drawing, Route Card) | ✅ Done |
| Settings Page (Admin — ApiBaseUrl, MachineCode, MachineName) | ✅ Done |
| FAI Final (re-inspect sau rework, is_final=true, chỉ QC Inspector) | ✅ Done |
| SignalR real-time notifications (NCR banner trên Dashboard) | ✅ Done |
| Gage Selection trong FAI (chọn dụng cụ đo trước khi nhập) | ⏳ Phase 5 |

---

**Technology stack:**
- **Framework**: WPF (.NET 9) — desktop only, Windows
- **Auth**: JWT từ API (giống web — POST `/api/v1/auth/login`)
- **Data**: REST API (shopfloor-manager API) — không kết nối DB trực tiếp
- **Documents**: MinIO presigned URL để download PDF/G-code
- **Notifications**: SignalR hub `/hub/shopfloor` để nhận real-time updates
- **Machine**: MQTT subscribe `factory/cnc/{machineCode}/status` (xem Phase 5)

---

## Kiến trúc app

```
WPF App
├── Services/
│   ├── ApiClient          # HttpClient wrapper cho REST API
│   ├── AuthService        # JWT login, refresh, logout
│   ├── DocumentService    # Download from MinIO presigned URL
│   └── SignalRService     # Kết nối /hub/shopfloor
│
├── ViewModels/            # MVVM pattern
│   ├── LoginViewModel
│   ├── JobSelectionViewModel
│   ├── OperationViewModel
│   ├── FAIViewModel
│   └── ...
│
├── Views/                 # WPF Windows/Pages
│   ├── LoginWindow
│   ├── MainWindow         # Shell với navigation
│   ├── JobSelectionPage
│   ├── OperationPage
│   ├── FAIPage
│   ├── DocumentViewerPage
│   └── ...
│
├── Config/
│   ├── appsettings.json   # API URL, machine code
│   └── local.json         # Override per-machine (machineCode, machineIP)
│
└── AutoUpdate/            # Squirrel.Windows hoặc MSIX
```

**Offline mode**: Phase 4 KHÔNG có offline — app yêu cầu kết nối API. Offline sync dành cho Phase 4b (tương lai) nếu cần.

---

## Cấu hình per-machine

Mỗi máy tính tại shop floor có file `local.json` riêng:

```json
{
  "MachineCode": "3100L",
  "MachineName": "3100L LONG LATHE MACHINE",
  "ApiBaseUrl": "http://192.168.0.100:5066"
}
```

`MachineCode` được dùng để:
- Tag AuditLog ("đăng nhập từ máy nào")
- Subscribe MQTT topic đúng machine
- MeasureValue.MachineId (biết giá trị đo từ máy nào)

---

## Roles và quyền truy cập

| Role (API)       | Vinam cũ | Quyền trong Desktop MES |
|---|---|---|
| Operator         | OPR      | Chọn Job/OP, nhập MeasureValue, xem docs; forced View_Mode khi máy đang có session của người khác |
| Leader           | —        | Như Operator + có thể force-finish session của người khác trên cùng máy |
| QC Inspector     | QC       | Nhập MeasureValue, approve/reject, tạo NCR; View_Mode khi máy bận |
| Engineer         | ME       | Xem tất cả, không nhập measure; View_Mode khi máy bận |
| Manager          | PLN      | Read-only — monitor; View_Mode khi máy bận |
| Administrator    | —        | Full access + force-finish + Settings page |

Menu items bị ẩn/disabled theo role — gọi `GET /api/v1/roles/{id}/menus` sau login.

---

## Workflow chính

### 1. Login flow

```
[Màn hình Login]
  → Nhập Username / Password
  → POST /api/v1/auth/login
  → Nhận JWT token → lưu in-memory (không persist ra disk)
  → GET /api/v1/machines/{machineCode}/active-session
      ├── Không có session active   → Operation_Mode
      ├── Session của chính mình    → Operation_Mode + resume WorkContext
      ├── Session của người khác + Role Leader/Admin → Operation_Mode (có ForceFinish button)
      └── Session của người khác + Role Operator/khác → View_Mode (forced)
  → Navigate MainWindow → Dashboard
```

**Desktop app KHÔNG xử lý FirstLogin**: Desktop bỏ qua `firstLogin` flag — navigate thẳng Dashboard sau login thành công. FirstLogin change-password chỉ dành cho Web app.

**Session**: JWT 8h. Khi hết hạn → auto redirect về Login (không silent refresh).

**Session resume**: Nếu active session là của chính mình → reconstruct minimal DTOs từ `ActiveSessionDto` → `_work.SetJob/SetOp/SetProduct` → Dashboard với WorkContext đã restore.

---

### 2. Job Selection flow

```
[MainWindow] → [Job List]
  → GET /api/v1/jobs?status=InProgress&pageSize=50
  → Hiển thị danh sách Jobs: JobNumber, PartNumber, PartRev, ShipBy, Progress
  → Operator chọn 1 Job → lưu vào session state (jobSelected)
  → Chuyển sang OP Selection
```

**Filter**: Operator chỉ thấy Jobs đang sản xuất (status InProgress). QC thấy thêm Jobs cần inspect.

**Search**: Theo JobNumber, PartNumber, ShipBy range.

---

### 3. Operation Selection flow

```
[Job đã chọn] → [OP List]
  → GET /api/v1/jobs/{jobId}/operations
    → Trả về: PartOps từ RoutingRev + ForJobOnly OPs
  → Hiển thị: OpNumber, OpType, SetupTime, ProdTime, Documents available
  → Chọn OP → lưu opSelected
  → Chuyển sang: FAI hoặc Document Viewer
```

**Mỗi OP card hiển thị:**
- OpNumber (e.g. "010"), OpType (CNC, GRIND...)
- Icon badges: có Drawing?, có G-code?, có RouteCard?
- Progress: X/Y serials đã đo xong

---

### 4. Product Serial Selection — ProductListPage

```
[OP đã chọn] → [ProductListPage — Card Grid]
  → GET /api/v1/jobs/{jobId}/operations/{opId}/products
    → Trả về: Product list + session status per product per OP
  
  → Hiển thị dạng card (WrapPanel):
    ┌──────────────┐
    │     001      │  ← Serial lớn
    │      ○       │  ← Icon trạng thái
    │  SẴN SÀNG   │  ← Text trạng thái
    │  3100L  12:35│  ← Machine + elapsed time
    └──────────────┘
    
    Màu sắc theo trạng thái:
    - ⚪ Sẵn sàng    (xám nhạt)  — có thể chọn
    - 🟡 Đã chọn    (vàng)      — locked by machine
    - 🟠 Đang gia công (cam)    — WIP, in progress
    - 🟢 Hoàn thành  (xanh)     — FAI complete
  
  → Tap card Available → chọn sản phẩm
  → POST /api/v1/production-sessions (Claim)
    Body: { productId, partOpId, machineCode }
  → Chuyển sang FAI Entry
```

**Ràng buộc `production_sessions`:**
1. **Per-product**: Mỗi product chỉ có 1 session `status=open` tại bất kỳ thời điểm → không thể chọn product đang WIP ở máy khác hoặc OP khác.
2. **Per-machine**: Mỗi máy chỉ có 1 session `status=open` tại một thời điểm → đang gia công product A thì không thể chọn thêm product B trên cùng máy.

**Supervisor unlock**: Có thể cancel session đang `open` — giải phóng product + máy.

---

### 5. FAI Entry flow (Core feature)

```
[FAI Entry Page]
  Sau khi claim session thành công:
  
  ① TIMER START
     → Nút "▶ Bắt đầu" → PUT /api/v1/production-sessions/{id}/start
     → Ghi nhận started_at (thời gian bắt đầu gia công thực tế)
     → Timer đếm thời gian hiển thị trên màn hình
  
  ② NHẬP FAI
     → GET /api/v1/fai?jobId=&partOpId=
       → Dimension list: BalloonNumber, Nominal, TolerancePlus, ToleranceMinus, CategoryId
       → Kèm MeasuredValue hiện tại (nếu đã đo) cho product đó
     
     → Hiển thị: WrapPanel cards — mỗi Dimension là 1 card
       → Màu: Xám (chưa đo) | Xanh (Pass) | Đỏ (Fail)
       → Dimension đã đo (IsInputLocked=true): hiển thị giá trị cũ, input bị disabled hoàn toàn
     
     → Tap 1 Dimension card → Mở Input Panel:
     
       [Phase 5 — chưa implement] BƯỚC 1 — Chọn dụng cụ đo (Gage):
         → GET /api/v1/gages?categoryId={dim.categoryId}&status=available
         → Filter: is_calibrated=true + status=available
         → Optional: nếu không chọn → gageId=null
       
       BƯỚC 2 — Nhập giá trị đo:
         → Kích thước số: NumPad, nhập giá trị số
         → Kích thước text (is_text_type=true): nút PASS / FAIL (auto-save ngay, không qua confirm)
         → Hiển thị: Nominal, Min, Max để tham chiếu
       
       BƯỚC 3 — Xác nhận (chỉ kích thước số):
         → POST /api/v1/fai/measure
           Body: { dimensionId, productId, value, manualResult, note }
         → Response: { result: Pass|Fail }
         → Cập nhật màu card → tự động chuyển sang dimension tiếp theo chưa đo
     
     → IsFinal dimensions: chỉ QC Inspector nhập được, Operator thấy nhưng disabled
  
  ③ TIMER STOP + HOÀN THÀNH
     → Khi tất cả Dimensions đã đo → bật nút "■ Kết thúc"
     → Tap "Kết thúc" → PUT /api/v1/production-sessions/{id}/complete
     → Ghi nhận completed_at
     → Chuyển về ProductListPage → product đó đổi màu xanh
  
  → Nếu có Dimension Fail → hiện dialog NCR (bắt buộc)
```

**IsInputLocked:** Dimension đã có kết quả đo → input panel disabled hoàn toàn (TextBox `IsEnabled=false`, NumPad không mở), hiển thị giá trị cũ, amber notice banner. Mỗi lần đo là record mới — không overwrite.

**IsTextType dimensions**: `is_text_type=true` → nút PASS / FAIL (không có NumPad), gửi `manualResult=true/false`, `value=null`. Auto-save ngay khi bấm.

**IsFinal dimensions**: `is_final=true` → chỉ QC Inspector nhập được, Operator thấy nhưng disabled.

---

### 5b. FAI Final flow (sau rework — ⏳ Phase 4b)

```
NCR đã xử lý xong (rework/repair hoàn tất)
  → QC Inspector mở FAI Page cho Serial bị fail
  → Chỉ hiển thị dimensions có trạng thái Fail
  → Đo lại từng dimension:
      → POST /api/v1/fai/measure với is_final=true + final_op_id
      → Pass → balloon xanh, NCR liên quan tự đóng
      → Fail lại → NCR mới, tiếp tục chu kỳ rework
```

> Schema đã sẵn sàng (`is_final`, `final_op_id` trong `measure_values`). Cần thêm: filter UI "chỉ hiện dims Fail", API param `isFinal=true`, quyền chỉ QC Inspector.

---

### 5c. Gage Selection (⏳ Phase 5)

Ghi nhận dụng cụ đo cho mỗi kết quả — phục vụ truy vết khi gage bị thu hồi hoặc hết hạn calibration.

Cần implement:
- `GET /api/v1/gages?categoryId=&status=available` endpoint
- UI: danh sách gage trước input panel, filter theo `dimension.category_id`
- `measure_values.gage_id` hiện lưu null → populate sau khi có endpoint

> Legacy ghi `GageID` trong 100% 904,699 records — dùng để invalidate hàng loạt kết quả đo khi gage hỏng.

---

### 6. NCR Creation (khi Fail)

```
[Dimension Fail] → Dialog NCR
  → Nhập: Reason (dropdown từ NcrReason), Description (text)
  → POST /api/v1/ncrs
    Body: { measureValueId, reasonId, description, machineCode }
  → NCR tạo với format NCR-{YY}-{NNNN}
  → SignalR broadcast → QC nhận thông báo real-time
  → (Optional future) Teams webhook notification
```

---

### 7. Operation_Mode / View_Mode *(✅ implemented)*

Hai mode giải quyết: operator browse hồ sơ mà không ảnh hưởng session đang chạy; user B login khi máy đang được dùng bởi user A.

```
Operation_Mode                         View_Mode
────────────────────────────────────   ────────────────────────────────────
WorkContext.CurrentJob/Op/Product ACTIVE   WorkContext.ViewJob/Op/Product ACTIVE
Dashboard hiện Work Info + timer       Dashboard hiện View context
Navigation GHI CurrentJob/Op/Product   Navigation GHI ViewJob/Op/Product
Claim/Start/Stop/FAI khả dụng         Chỉ xem hồ sơ (không tạo session)
```

**Toggle chip (TitleBar Dashboard):**
- Luôn visible (kể cả khi forced View_Mode)
- Operation: transparent/BrandPrimary background, text "VIEW"
- View Mode: orange (`#FF8F00`) background, text "VIEW MODE"
- `CanSwitchMode = IncomingSession is null || ClaimedBy == userId || role in Leader/Manager/Admin`
- Khi `CanSwitchMode=false`: chip mờ (Opacity=0.4), cursor Arrow — nhìn thấy đang ở View Mode nhưng không toggle được
- Khi `CanSwitchMode=true`: cursor Hand, click gọi `ToggleModeCommand`

**Context independence:**
- `WorkContext.ViewJob/ViewOp/ViewProduct` là fields riêng biệt — toggle mode không copy/xóa context
- `OnModeChanged` KHÔNG gọi `ClearViewContext()` — context giữ nguyên khi toggle
- View context chỉ clear khi logout (`Clear()`)
- Dashboard dùng `CtxJob/CtxOp/CtxProduct` helpers: `IsViewMode ? ViewJob : CurrentJob`

**View Mode navigation:**
- `MainViewModel` gọi `_work.SetViewJob/SetViewOp/SetViewProduct` (không phải SetJob/Op/Product)
- `ProductListPage` với `IsViewMode=true`: button "Xem sản phẩm →", không tạo session khi chọn
- Shortcuts trong View Mode cập nhật theo `HasViewJob/Op/Product`

**FAI constraint:**
- FAI chỉ khả dụng trong Operation Mode VÀ session đã được start (`StartedAt != null`)
- Shortcut "Bảng đo": `canFai = !_work.IsViewMode && hasProd && _work.ActiveSession?.StartedAt.HasValue == true`
- `NavigateToFai()` guard: `_work.ActiveSession?.StartedAt.HasValue != true` → NavigateToDashboard

---

### 8. Document Viewer

```
[OP đã chọn] → Shortcut "Xem G-code" / "Xem tài liệu"
  → GET /api/v1/tech-documents?partOpId={opId}&status=Approved
  → Hiển thị danh sách docs: FileType, FileName, UploadedAt
  → Chọn doc → GET /api/v1/tech-documents/{id}/download-url
    → Nhận presigned URL → download từ MinIO vào temp folder
  → Render theo loại file:
    → G-code (.nc, .tap, .cnc) → Text viewer với syntax highlight (N/G/X/Y/Z keywords)
    → PDF → PDF viewer embedded (PdfiumViewer hoặc WebView2)
    → Images (.png, .jpg) → Image viewer với zoom/pan
```

**Chỉ hiển thị docs có `Status=Approved`.** Docs Pending/Rejected không hiện với Operator.

**Ưu tiên implement** (dựa trên phân bố tài liệu thực tế từ legacy data):

| Loại | Tỷ lệ | Ưu tiên |
|---|---|---|
| Route Card (RC, RTC) | ~34% | 1 — PDF viewer |
| G-code (WC, GCode, FGCode) | ~34% | 2 — Text viewer (đơn giản hơn PDF) |
| Drawing (DRW) | varies | 3 — PDF viewer (dùng chung với RC) |

→ **G-code text viewer nên làm trước** vì: (1) chiếm 34% tổng docs, (2) chỉ cần `TextBox` + syntax highlight, không cần thư viện nặng.

**G-code viewer chi tiết:**
- Load file text từ MinIO temp path
- Highlight: màu xanh cho G/M codes, màu cam cho X/Y/Z/F values, xám cho comments `;...`
- Scroll theo line number
- Search theo keyword (N block number, G code)
- Nút "Tải về" để copy ra USB/folder

---

### 8. Real-time updates (SignalR)

Sau login, app kết nối `/hub/shopfloor` và join group theo role:

| Event | Nhận bởi | Action |
|---|---|---|
| `ncr-created` | QC Inspector | Toast notification + badge |
| `job-status-changed` | All | Refresh job list |
| `measure-submitted` | Engineer, QC | Update FAI progress counter |
| `document-approved` | All | Refresh doc availability |

---

## Màn hình / Pages

### LoginWindow
- Username, Password fields
- "Quên mật khẩu?" → gửi email reset (POST /api/v1/auth/forgot-password)
- Machine info hiển thị phía dưới (machineCode, machineName từ local config)

### MainWindow (Shell)
- Navigation sidebar: Job, Documents, NCR, Settings
- Header: User info, role, logout
- Badge counters: số NCR open, số Jobs cần inspect

### JobSelectionPage
- Card grid với Jobs
- Filter: Status, ShipBy range, search by JobNumber/PartNumber
- Phân trang

### OperationPage
- OP cards với badges (docs available, progress)
- Nút "Xem tài liệu" → DocumentViewerPage
- Nút "Bắt đầu FAI" → SerialSelectionPage

### ProductListPage *(Serial Selection)*
- **Card grid** (WrapPanel, 4 màu trạng thái)
- Card hiển thị: Serial number lớn, icon, trạng thái, machine code, elapsed time
- Tap card Available → footer confirm xuất hiện → Claim session
- Ràng buộc: per-product + per-machine (1 session open tại một thời điểm)

### FAIPage
- **Header**: Job, OP, Serial, Timer (đang chạy hoặc đã dừng)
- **Nút Bắt đầu / Kết thúc**: ghi nhận thời gian gia công
- **Dimension card grid** (WrapPanel): Xám/Xanh/Đỏ
- **Input panel** (khi tap dimension):
  1. Gage selection list (filter theo category + availability)
  2. NumPad nhập giá trị
  3. Confirm button
- **FAI complete**: khi tất cả dims đo xong → Enable "Kết thúc"

### DocumentViewerPage
- Tabs: Drawing | G-code | Route Card | Fixture | Thread | Tools
- PDF viewer embedded
- G-code viewer với line highlight
- Download button (lưu local)

### SettingsPage (Admin/Engineer only)
- API URL override
- Machine code/name
- Test connection button
- Version info + "Check for updates"

---

## Auto-update

Dùng **Squirrel.Windows** hoặc **MSIX/AppInstaller**:
- Check update mỗi lần khởi động
- Download từ file server nội bộ (hoặc MinIO bucket `app-updates`)
- Silent install khi user confirm
- Hiển thị version hiện tại ở footer

---

## API endpoints cần có (checklist)

Các endpoint này phải có sẵn ở API trước khi Desktop MES dùng được:

| Endpoint | Status |
|---|---|
| POST /api/v1/auth/login | ✅ Done (Phase 1) |
| POST /api/v1/auth/forgot-password | ✅ Done |
| GET /api/v1/jobs | ✅ Done (Phase 2) |
| GET /api/v1/jobs/{id}/operations | ✅ Done |
| GET /api/v1/jobs/{jobId}/operations/{opId}/products | ✅ Done (Phase 4) |
| GET /api/v1/operations/{opId}/dimensions | ✅ Done (Phase 3) |
| POST /api/v1/measure-values | ✅ Done |
| GET /api/v1/tech-documents/{id}/download-url | ✅ Done |
| POST /api/v1/production-sessions | ✅ Done (Phase 4) |
| PUT /api/v1/production-sessions/{id}/start | ✅ Done (Phase 4) |
| PUT /api/v1/production-sessions/{id}/complete | ✅ Done (Phase 4) |
| PUT /api/v1/production-sessions/{id}/cancel | ✅ Done (Phase 4) |
| PUT /api/v1/production-sessions/{id}/force-complete | ✅ Done (Phase 4) |
| GET /api/v1/machines/{machineCode}/active-session | ✅ Done (Phase 4) |
| GET /api/v1/gages?categoryId=&status=available | ⏳ Phase 5 (Gage Selection trong FAI) |
| POST /api/v1/ncrs | ✅ Done |
| GET /api/v1/roles/{id}/menus | ✅ Done |

---

## Phân biệt vs old Vinam-MES

| Điểm | Vinam-MES (cũ) | Phase 4 (mới) |
|---|---|---|
| Data access | Direct MySQL | REST API |
| Documents | FTP download | MinIO presigned URL |
| Auth | MD5 + MySQL query | JWT token |
| Notifications | Teams webhook | SignalR + (optional Teams) |
| Machine control | Serial RS-343 | MQTT (Phase 5) |
| UI framework | WinForms + Guna2 | WPF + Material Design |
| Update | FTP AutoUpdate.xml | Squirrel.Windows |
| Offline | Không | Không (Phase 4b tương lai) |
| Measure precision | FLOAT (mất chính xác) | DECIMAL(14,4) |
| Measure history | Upsert (chỉ giữ lần đo cuối) | Tạo record mới mỗi lần (giữ lịch sử đầy đủ) |
| Gage traceability | GageID bắt buộc cho mọi kết quả | Planned Phase 5 (hiện null) |

---

## Dữ liệu test dev

Dev database (PostgreSQL) sau khi chuẩn bị:
- **OPs**: 76 PartOps, 100% có OpType
- **Dimensions**: 162 kích thước, phân bổ LIN=154 / GEO=6 / SFC=2
- **TechDocuments**: 5 files (2 DRW PDF, 2 GCD G-code, 1 RTC, 1 FXT) — status=Approved
- **MeasureValues**: 0 records ← cần seed để test locked state và FAI Final

**Seed test measure values (chạy khi cần test):**
```sql
-- Seed ~10 measure values cho serial "001" của 1 job để test FAI locked state
-- Chạy từ PostgreSQL dev: docker exec shopfloor-manager-dev-postgres-1 psql -U shopfloor -d shopfloor_dev
INSERT INTO measure_values (dimension_id, product_id, part_op_id, value, result, measured_at, created_at, updated_at)
SELECT d.id, p.id, d.part_op_id,
       (d.min_value + d.max_value) / 2,   -- giá trị nominal → Pass
       1,                                  -- 1=Pass
       NOW(), NOW(), NOW()
FROM dimensions d
JOIN products p ON p.job_id = (SELECT id FROM jobs LIMIT 1)
WHERE p.serial_number = '001'
  AND d.is_text_type = false
LIMIT 10;
```

---

## Out of scope (Phase 4)

- Offline mode / local DB sync → Phase 4b
- G-code send to machine via Serial → Phase 5 (MQTT)
- Machine monitoring real-time (cycle time, spindle speed) → Phase 5
- Gage borrow/return tại máy → Phase 5
- Planning/Scheduling display → Phase 5
