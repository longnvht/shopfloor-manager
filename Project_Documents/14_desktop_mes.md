# Phase 4 — Desktop MES (Shop Floor Client)

> Nguồn tham khảo: Vinam-MES WinForms (FANUC.sln) — rewrite theo kiến trúc mới (WPF + REST API + MinIO + JWT).
> 
> **Dashboard UI spec**: xem [`15_dashboard_desktop.md`](15_dashboard_desktop.md) — màu sắc, layout thẻ, shortcut grid, WorkContext state management.

---

## Tổng quan

Desktop MES là ứng dụng chạy tại shop floor — trên máy tính công nghiệp hoặc tablet gắn cạnh máy CNC. Operator, QC, Engineer dùng app này để:

- Nhận Job, chọn Operation đang làm
- Nhập kết quả đo FAI (First Article Inspection) tại máy
- Xem tài liệu kỹ thuật (Drawing, G-code, Route Card, Fixture)
- Tạo NCR khi sản phẩm không đạt
- Theo dõi trạng thái sản xuất real-time

**Technology stack:**
- **Framework**: WPF (.NET 8) — desktop only, Windows
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
| Operator         | OPR      | Chọn Job/OP, nhập MeasureValue, xem docs |
| QC Inspector     | QC       | Nhập MeasureValue, approve/reject, tạo NCR |
| Engineer         | ME       | Xem tất cả, không nhập measure |
| Manager          | PLN      | Read-only — monitor |
| Administrator    | —        | Full access |

Menu items bị ẩn/disabled theo role — gọi `GET /api/v1/roles/{id}/menus` sau login.

---

## Workflow chính

### 1. Login flow

```
[Màn hình Login]
  → Nhập Username / Password
  → POST /api/v1/auth/login
  → Nhận JWT token → lưu in-memory (không persist ra disk)
  → Kiểm tra FirstLogin → nếu true → bắt buộc đổi mật khẩu
  → Load role menus → Route đến MainWindow
```

**First login**: Nếu `user.FirstLogin = true` → hiện form đổi mật khẩu, không cho qua cho đến khi đổi xong.

**Session**: JWT 8h. Khi hết hạn → auto redirect về Login (không silent refresh).

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
     → GET /api/v1/operations/{opId}/dimensions
       → Dimension list: BalloonNumber, Nominal, TolerancePlus, ToleranceMinus, CategoryId
     
     → Hiển thị: WrapPanel cards — mỗi Dimension là 1 card
       → Màu: Xám (chưa đo) | Xanh (Pass) | Đỏ (Fail)
       → Hiển thị: BalloonNumber lớn, Nominal ± tol nhỏ bên dưới
     
     → Tap 1 Dimension card → Mở Input Panel:
     
       BƯỚC 1 — Chọn dụng cụ đo (Gage):
         → GET /api/v1/gages?categoryId={dim.categoryId}&status=available
         → Hiển thị danh sách Gage: GageNo, GageName, GageType
         → Chỉ show gages hợp lệ: IsCalibrated=true, Status=Available
         → Operator chọn 1 gage → lưu selectedGage
       
       BƯỚC 2 — Nhập giá trị đo:
         → NumPad xuất hiện tự động
         → Hiển thị: Nominal, Min, Max để tham chiếu
         → Nhập giá trị số
       
       BƯỚC 3 — Xác nhận:
         → POST /api/v1/measure-values
           Body: { dimensionId, productId, partOpId, value, gageId, machineCode }
         → Response: { result: Pass|Fail, value }
         → Cập nhật màu card Dimension
     
     → IsTextType dimensions: input "OK"/"NG" bằng QWERTY keyboard
     → IsFinal dimensions: chỉ QC Inspector nhập được
  
  ③ TIMER STOP + HOÀN THÀNH
     → Khi tất cả Dimensions đã đo → bật nút "■ Kết thúc"
     → Tap "Kết thúc" → PUT /api/v1/production-sessions/{id}/complete
     → Ghi nhận completed_at
     → Hiển thị tổng thời gian gia công
     → Chuyển về ProductListPage → product đó đổi màu xanh
  
  → Nếu có Dimension Fail → hiện dialog NCR
```

**Gage selection rules:**
- Filter theo `dimension.category_id` → chỉ show gage phù hợp loại kích thước
- Gage phải `is_calibrated = true` và `status = available` (chưa được ai mượn)
- Nếu không có gage phù hợp → thông báo, không block (cho phép nhập không chọn gage)

**IsTextType dimensions**: `Dimension.IsTextType=true` → input là text ("OK", "NG", "PASS"...) dùng QWERTY keyboard thay vì NumPad.

**IsFinal dimensions**: `Dimension.IsFinal=true` → chỉ QC Inspector nhập được, Operator thấy nhưng disabled.

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

### 7. Document Viewer

```
[OP đã chọn] → [Documents Tab]
  → GET /api/v1/tech-documents?partOpId={opId}
  → Hiển thị danh sách docs: FileType, FileName, Status, UploadedAt
  → Chọn doc → GET /api/v1/tech-documents/{id}/download-url
    → Nhận presigned URL → download từ MinIO
  → Render:
    → PDF files → WPF PDF viewer (PdfiumViewer hoặc Telerik)
    → G-code files → Text viewer với syntax highlight
    → Images → Image viewer
```

**Chỉ hiển thị docs có `Status=Approved`.** Docs Pending/Rejected không hiện với Operator.

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
| GET /api/v1/gages?categoryId=&status=available | ⏳ Phase 5 (cần cho FAI) |
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

---

## Out of scope (Phase 4)

- Offline mode / local DB sync → Phase 4b
- G-code send to machine via Serial → Phase 5 (MQTT)
- Machine monitoring real-time (cycle time, spindle speed) → Phase 5
- Gage borrow/return tại máy → Phase 5
- Planning/Scheduling display → Phase 5
