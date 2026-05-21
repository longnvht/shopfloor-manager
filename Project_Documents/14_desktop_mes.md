# Phase 4 — Desktop MES (Shop Floor Client)

> Nguồn tham khảo: Vinam-MES WinForms (FANUC.sln) — rewrite theo kiến trúc mới (WPF + REST API + MinIO + JWT).

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

### 4. Product Serial Selection

```
[OP đã chọn] → [Serial List]
  → GET /api/v1/jobs/{jobId}/products
  → Hiển thị grid: Serial (001, 002...), FAI status (Not started / In progress / Done)
  → Chọn serial → lưu productSelected
  → Chuyển sang FAI Entry
```

---

### 5. FAI Entry flow (Core feature)

```
[FAI Entry Page]
  → GET /api/v1/jobs/{jobId}/operations/{opId}/dimensions
    → Trả về: Dimension list với BalloonNumber, Nominal, UpperTol, LowerTol, CategoryId
  
  → Hiển thị: FlowLayoutPanel style — mỗi Dimension là 1 button/card
    → Card hiển thị: BalloonNumber, Nominal ± Tol, Result (nếu đã đo)
    → Color: Gray (chưa đo), Green (Pass), Red (Fail)
  
  → Chọn Dimension card → Hiện input panel:
    → Chọn Gage (GET /api/v1/gages?categoryCode=...)
    → Nhập giá trị đo (numeric keyboard)
    → Submit → POST /api/v1/measure-values
      Body: { dimensionId, productId, partOpId, value, gageId, machineId }
    → Auto-calculate Pass/Fail từ response
    → Update card color
  
  → Nếu Fail → hiện dialog tạo NCR
  → Khi tất cả Dimensions đã đo → mark serial complete
```

**Numeric keyboard**: Custom on-screen keyboard (touch-friendly) — dùng cho nhập số đo.

**Gage selection**: Chỉ show gages có `IsCalibrated=true` và `Status=Available`. CategoryCode filter theo dimension.

**IsTextType dimensions**: Nếu `Dimension.IsTextType=true` → input là text (e.g. "OK", "NG") thay vì số.

**IsFinal dimensions**: Nhóm riêng — chỉ QC Inspector mới nhập được.

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

### SerialSelectionPage
- Grid: Serial | FAI Status | Last Updated | Operator
- Color: Gray (not started), Yellow (in progress), Green (done), Red (has fail)

### FAIPage
- Split layout:
  - Left: Dimension cards (FlowLayoutPanel style)
  - Right: Input panel + Gage selection + History
- Bottom: Previous serial / Next serial navigation
- "Xem Drawing" button → mở Drawing trong panel phụ hoặc new window

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
| GET /api/v1/jobs/{id}/products | ✅ Done |
| GET /api/v1/jobs/{jobId}/operations/{opId}/dimensions | ✅ Done (Phase 3) |
| POST /api/v1/measure-values | ✅ Done |
| GET /api/v1/tech-documents/{id}/download-url | ✅ Done |
| GET /api/v1/gages | ⏳ Phase 5 |
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
