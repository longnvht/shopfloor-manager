# 20 — Progress Log & Implementation History

Log triển khai chi tiết — lịch sử từng tính năng, lessons learned, quyết định kỹ thuật. Đọc file này khi cần context lịch sử cho một tính năng cụ thể.

---

## Phase 1 — Auth & HR ✅ (2026-05-20)

- EF Core `ShopfloorDbContext` + 9 entities (User, Role, Department, UserType, Position, WorkStatus, Menu, RoleMenu, AuditLog)
- Migration `InitialSchema` — seed 6 roles, 4 departments, 3 work statuses
- `DbSeeder` tạo `admin/Admin@123` khi DB trống
- `POST /api/v1/auth/login` → JWT token (8h)
- `POST /api/v1/auth/forgot-password` + `POST /api/v1/auth/reset-password` (MailKit)
- `GET|POST|PUT /api/v1/users` — phân trang, role-based, update, change password
- `GET|POST|PUT /api/v1/roles`, `/api/v1/departments`
- `GET|POST /api/v1/positions`, `/api/v1/user-types`; `GET /api/v1/work-statuses`
- SignalR hub tại `/hub/shopfloor` (auto-join group theo role)
- `ValidationBehavior` MediatR pipeline, `ExceptionMiddleware`, Swagger + JWT

---

## Phase 2 — Production Core ✅ (2026-05-20)

- Entities: Part, PartRev, Routing, RoutingRev, PartOp, Job (snapshot PartRevId+RoutingRevId), Product
- `CreateJob` tự động tạo Products theo RunQty
- API: `/api/v1/parts`, `/api/v1/jobs`, `/api/v1/operations`
- MinIO: TechDocument upload với pre-signed URL + 3 upload rules
- FileTypes: DRW, GCD, RTC, FXT, THD, TLS, CAM, CAD

---

## Phase 3 — Quality ✅ (2026-05-20)

- Dimension: BalloonNumber + BalloonSort, TolerancePlus/Minus (cả 2 dương), MaxValue/MinValue, IsTextType, CategoryId, IsFinal
- DimensionCategory: LIN, ANG, THD, GEO, SFC (seed)
- MeasureValue: KHÔNG upsert — tạo record mới mỗi lần đo (giữ lịch sử)
- NCR: format `NCR-{YY}-{NNNN}`, thêm ReasonId, DepartmentId, MachineCode
- NcrReason: seed 7 lý do (Tool wear, Setup error, Drawing error...)
- SPC: ISpcService + MathNet dùng MaxValue/MinValue

---

## Phase 4 — Desktop MES ✅ (2026-05-21 → 2026-06-09)

- Project: `ShopfloorManager.Desktop` (WPF .NET 9, trong cùng solution)
- Spec: `Project_Documents/14_desktop_mes.md`
- Stack: WPF + CommunityToolkit.Mvvm + MaterialDesignThemes + SignalR.Client

### Tính năng đã hoàn thành

- ✅ JobListPage: search, ShowCompleted toggle, pagination 20/trang, overdue highlight, status badge
- ✅ OperationPage: danh sách OP dạng card, badge ForJobOnly/Complete, SetupTime/ProdTime, back về JobList
- ✅ Virtual Keyboard: NumPadWindow (số, floating no-focus), QwertyWindow (QWERTY + 123 panel, CapsLock toggle)
- ✅ Touch-optimized: Button 56px, TextBox 52px, DataGridRow 52px, KeyboardBehavior attached property
- ✅ Virtual Keyboard light theme: nền cam kem #FFF8F0, viền bo nâu #A0522D, Caps ON cam #E65100
- ✅ ProductListPage: card grid 4 màu trạng thái (available/claimed/inprogress/complete), claim session
- ✅ ProductionSession backend: entity + migration + API (claim/start/complete/cancel)
- ✅ WorkContext singleton: chia sẻ Job/OP/Product/Session state giữa tất cả pages
- ✅ Dashboard: layout 4 rows (TitleBar / Machine+Operator / WorkInfo / Utilities) cho 10" 16:9
- ✅ Design Language thống nhất: `16_design_language.md`
- ✅ FAIPage: split layout 55/45, dimension card grid (xám/xanh/đỏ), NumPad, PASS/FAIL text, auto-advance
- ✅ Virtual keyboard drag: drag handle strip (⠿ icon, 22px) — kéo được không mất focus TextBox
- ✅ NCR dialog: department chip + reason ComboBox + "Khác" option
- ✅ NcrReasons seed data: 15 lý do gắn DepartmentId
- ✅ DragScrollBehavior: attached property — drag-to-scroll trên list pages
- ✅ Operation_Mode/View_Mode: AppMode enum trong WorkContext; dual context (ViewJob/ViewOp/ViewProduct)
- ✅ session resume on login: GET /api/v1/machines/{code}/active-session → reconstruct WorkContext
- ✅ force-finish session: PUT /api/v1/production-sessions/{id}/force-complete (role Leader/Admin)
- ✅ Leader role: thêm vào AppConstants.Roles + DB seed (Id=7)
- ✅ VIEW MODE toggle chip: luôn visible trên TitleBar
- ✅ FAI one-time entry (IsInputLocked)
- ✅ Settings page (Admin): ApiBaseUrl, MachineCode, MachineName
- ✅ FAI Final mode: chỉ load dims có State=Fail; title bar đỏ thẫm `#B71C1C`
- ✅ SignalR real-time: NcrCreated event → DashboardViewModel banner đỏ auto-dismiss 8s
- ✅ SetPage() cleanup pattern: `CurrentPage?.Cleanup()` trước khi switch
- ✅ DocumentViewer: G-code syntax highlight + WebView2 PDF viewer

### Session constraint (thiết kế 2026-05-27)

- **Claim = client-side only**: không ghi DB; `_work.SetProduct(product, null)`
- **Per-product inprogress**: block nếu product đã có session `open + started_at IS NOT NULL` ở máy khác
- **Per-machine inprogress**: block nếu máy đã có session `open + started_at IS NOT NULL`
- **BeginSession**: POST `/api/v1/production-sessions` — tạo + set `started_at` atomically, check 2 constraints

### FAI workflow

1. Chọn product → `_work.SetProduct(product, null)` → Dashboard hiện "Bắt đầu"
2. POST `/api/v1/production-sessions` → tạo + start atomically → timer chạy
3. FAIPage: dimension card → NumPad/PASS·FAIL → confirm → auto-advance
4. Tất cả dims đo xong → "Kết thúc" → PUT complete
5. Nếu Fail → NCR dialog

### Operation_Mode / View_Mode

```
Operation_Mode                         View_Mode
────────────────────────────────────   ────────────────────────────────────
WorkContext operation slot ACTIVE       WorkContext operation slot FROZEN
Dashboard hiện Work Info + timer        Dashboard ẩn Work Info
Navigation GHI WorkContext              Navigation KHÔNG ghi WorkContext
```

Login flow: GET /api/v1/machines/{code}/active-session → không có session → Operation_Mode; session của mình → Operation_Mode + resume; session người khác → Leader/Admin → Operation_Mode, Operator → View_Mode (forced).

### WPF Lessons Learned (Desktop)

- **`HttpClient` + `IApiClient` phải là singleton** — nếu transient, mỗi ViewModel nhận instance riêng và không có token
- **Trigger data load từ ViewModel** (NavigateTo command), KHÔNG dùng `Loaded` event — tránh race condition DataContext timing
- **`Run.Text` binding trong WPF mặc định TwoWay** — computed/read-only properties phải dùng `Mode=OneWay`
- **`Border` chỉ nhận 1 child** — khi có nhiều state panels, wrap trong `<Grid>`
- **DispatcherTimer** cho clock/elapsed time — `Stop()` khi Cleanup()
- **WorkContext** là singleton ObservableObject — inject vào mọi ViewModel
- **`FlowDocument.PageWidth/ColumnWidth`**: `double.PositiveInfinity` KHÔNG hợp lệ .NET 9 WPF → dùng `100000.0`
- **MinIO presigned URL download**: KHÔNG dùng shared HttpClient (đã có Bearer header) — MinIO trả 400/403. Luôn dùng `new System.Net.Http.HttpClient()` riêng
- **WebView2 airspace problem**: WPF loading spinner bị che bởi Win32 control. Fix: chỉ set `IsPdfViewerVisible=true` sau khi loading xong
- **Keyboard drag (no-focus window)**: dùng `ReleaseCapture()` + `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0)`. KHÔNG dùng `DragMove()` (yêu cầu window activate)
- **Floating window dynamic content**: dùng `SizeToContent="Height"`, gọi `PositionBottomRight()` trong `Loaded` event
- **DragScrollBehavior**: state lưu per-instance qua DependencyProperty (không dùng static field)
- **ProductionSessionConfiguration**: `HasForeignKey(s => s.ClaimedBy)` + `HasForeignKey(s => s.CancelledBy)` — map explicit int FK props thay shadow properties. Thiếu → Include navigation luôn null
- **BeginSessionRequest**: Desktop POST body chỉ cần `ProductId, PartOpId, MachineCode` — server inject `UserId` từ JWT
- **Desktop FirstLogin**: Desktop KHÔNG redirect đổi mật khẩu khi `FirstLogin=true` — chỉ xử lý trên Web app
- **WorkContext dual context**: `OnModeChanged` KHÔNG gọi `ClearViewContext()` — view context giữ nguyên khi toggle; chỉ clear khi `Clear()` (logout)
- **FAI IsInputLocked**: `SelectedDimension?.IsMeasured == true` → lock mọi input. `TextBox IsEnabled="{Binding IsInputEnabled}"` (disabled hoàn toàn)
- **Shortcut disabled khi inprogress**: `canChangeContext = IsViewMode || !IsWip` → truyền `isEnabled: canChangeContext`
- **SetPage() cleanup pattern**: `MainViewModel.SetPage(vm)` → `CurrentPage?.Cleanup()`. Tất cả ViewModel phải override `Cleanup()` để unsubscribe events
- **IRealtimeNotifier pattern**: Interface ở Application layer; implementation `SignalRNotifier` ở API layer; đăng ký `Scoped`
- **SignalR Desktop singleton**: connection và event subscriptions sống suốt vòng đời app

---

## Phase 5 — Gage & Calibration ✅ (2026-06-04 → 2026-06-10)

- Entities: `GageType`, `GageLocation`, `GageSlot`, `Gage`, `BorrowTransaction`, `CalibVendor`, `CalibProcedure`, `CalibRequest`, `CalibRecord` — migration `AddGageAndCalibration`
- `Gage` computed: `IsValid`, `DueDate`, `DaysRemaining` (`due_date = last_calibration + calib_frequency_days`)
- API: GET/POST gages, GET calib-due, POST borrow-transactions, PUT return, GET/POST calib-vendors, GET/POST calib-requests, PUT approve, POST calib-records
- Web: `/gages` (KPIs, filter, mượn/trả) + `/calibration` (calib-due list, CreateRequestModal, CompleteModal)
- Migration `SeedGageReferenceData`: seed calib_procedures (3), calib_vendors (2), gage_slots (5)
- **Lưu ý dữ liệu dev DB**: `gage_types` (36 dòng) và `gage_locations` (89 dòng) đã có dữ liệu thực import — KHÔNG seed thêm

---

## i18n ✅ (2026-06-10)

### Web — next-intl

- Cookie `NEXT_LOCALE`; `i18n/request.ts`; `next.config.ts` wrap `createNextIntlPlugin`; `app/layout.tsx` async; `VALangSwitcher` trong sidebar footer
- **Đã dịch:** `nav`+`common` (sidebar), `dashboard`, `parts`, `dimsheet`, `documents`, `jobs`, `erp`
- **Chưa dịch:** `/fai`, `/ncrs`, `/gages`, `/calibration`, `/hr`, `/master`, `/planning`, `/cnc`, `/(auth)/login`

### Desktop — RESX + MarkupExtension

- `Resources/Strings.resx` (VI, default) + `Resources/Strings.en-US.resx` (EN satellite) + `Strings.Designer.cs` hand-written
- `LocalizationManager.cs` singleton + `SetLanguage()` live switch; `LocExtension.cs` → `{loc:Loc Key=...}`
- **Đã dịch:** `LoginWindow.xaml`, `SettingsPage.xaml` (gồm section "NGÔN NGỮ" — 2 nút Tiếng Việt/English)
- **Chưa dịch:** DashboardPage, JobListPage, OperationPage, ProductListPage, FaiPage, DocumentViewerPage, NcrDialogWindow, virtual keyboards

---

## Web UI — VA Design System ✅ (2026-06-04)

- VA warm industrial design system: tokens, sidebar, topbar, badge, kpi, card, btn, seg
- 18 routes — tất cả có VA shell, sidebar navigation
- `/parts` redesign → "Chi tiết kỹ thuật": master-detail Part list + Revision + Routing + OP flow
- `/jobs` redesign → "Lệnh SX & Sản phẩm": master-detail Job list + progress bar + serial grid
- Fonts: Inter + Fraunces + JetBrains Mono (next/font/google)

---

## Web UI — HR + FAI + Phase 5 gaps ✅ (2026-06-10)

- `/hr`: `UserDialog` (create/edit), menu "⋯" → Sửa/Vô hiệu hoá-Kích hoạt, `ManageLookupsDialog`
- `UserDto` thêm `Sex, RoleId, UserTypeId, PositionId, WorkStatusId`
- `/fai` (top-level): viết lại từ mock → chọn Job + Operation → `FaiSheetDto` thật qua `FaiMatrix`
- `components/fai/fai-matrix.tsx` dùng chung giữa `/fai` và `/jobs/[id]/fai`

---

## Web UI — `/documents` hợp nhất ✅ (2026-06-10)

- Fix bug: `api.techDocuments.inspect()` gửi sai `{action, note}` → backend nhận `{approve: boolean, note}` → bấm "Duyệt" luôn ghi `Approve=false`
- `/documents` đọc query params `partRevId`/`partOpId`/`jobId`/`backHref` → breadcrumb/title động + nút "← Quay lại" + nút "⬆ Upload"
- Xoá `/parts/[id]/documents/page.tsx` — hợp nhất vào `/documents`

---

## Web UI — Fix scroll toàn bộ (main) routes ✅ (2026-06-10)

- Bug: trang không cuộn được — page root div thiếu `minHeight: 0` → bị `overflow: hidden` của layout clip
- Fix: thêm `minHeight: 0` vào page root style của 15 trang
- Bảng trong VACard: đổi `minHeight: <số cố định>` → `minHeight: 0`, bọc table trong `<div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>` + sticky `<th>`

---

## Web UI — Master Data CRUD ✅ (2026-06-10)

- Thêm `IsActive` (bool, default true) vào `MachineGroup`, `OpType`, `DimensionCategory`, `FileType` — migration `AddIsActiveToMasterDataLookups`
- `MasterDataController` mới: `/api/v1/machines`, `/api/v1/machine-groups`, `/api/v1/op-types`, `/api/v1/dimension-categories`, `/api/v1/tech-documents/file-types`
- `LookupsController` chỉ còn Positions/UserTypes/WorkStatuses/NcrReasons
- Web `/master`: `MasterItemDialog` dùng chung cho cả 5 tab, cột "Trạng thái"

---

## Web UI — `/parts` CRUD + Excel import + i18n ✅ (2026-06-11)

- 3 dialog mới: `AddRevisionDialog`, `AddRoutingRevDialog`, `AddOpDialog`
- **Import Excel — Operations**: `POST /api/v1/operations/import` — upsert theo `OpNumber`
- **Import Excel — Dimensions**: `POST /api/v1/operations/{opId}/dimensions/import` — chỉ tạo mới, skip nếu BalloonNumber đã tồn tại
- Cả 2 trả `ImportResultDto { created, updated, skipped, errors }` — hiển thị kết quả + lỗi theo dòng
- `ExcelImportReader`: trả `List<Dictionary<string,string>>` (giá trị cell đã extract sẵn), KHÔNG trả `IXLRow` — vì XLWorkbook bị dispose trước khi LINQ chạy → `ObjectDisposedException`
- i18n: namespace `parts` đầy đủ cho cả 5 dialog mới

---

## Phase G — Part & Routing tách 2 trang ✅ (2026-06-12)

- **`/parts`**: master-detail + KPI strip + card "Bản vẽ 2D". OP table click row → `/parts/{id}/operations`
- **`/parts/[id]/operations`** (mới): danh sách OP trái + VASeg tabs "Tài liệu"/"Dimension" phải
- Xoá `app/(main)/parts/[id]/page.tsx` (thay thế hoàn toàn)
- **Lesson**: sau xoá file, cần `rm -rf .next` để xoá stale Turbopack type-check cache
- **Lesson**: KHÔNG chạy `npm run build` khi `npm run dev` đang chạy trên cùng `.next` — 2 process tranh nhau ghi

---

## Phase B — Dimension Sheet ✅ (2026-06-12)

- Backend: `GetDimensionsByRoutingRevQuery` → `RoutingRevDimensionDto`; endpoint `GET /api/v1/routing-revs/{id}/dimensions`
- Backend: `UpdateDimensionCommand` → `PUT /api/v1/dimensions/{id}`
- Frontend `/dimsheet`: panel trái search Part, panel phải cascading load Part→Rev→Routing→dims; inline-edit ✎/✓/✕
- **Lesson**: API process PHẢI restart sau khi thêm route mới — `dotnet run` đang chạy là build CŨ, `Stop-Process` rồi `dotnet run` lại

---

## Phase E — Documents redesign: flat-list filter ✅ (2026-06-13)

- User phản hồi UI cascading selector không đúng — viết lại theo mockup `va-docs.jsx` (flat-list)
- Migration `AddFileSizeToTechDocuments`: cột `file_size_bytes BIGINT NULL`
- Load TOÀN BỘ docs 1 lần; lọc client-side qua `useMemo`
- Filter bar: Part · Drawing Rev · Routing Rev · OP (cascading) + Loại + Trạng thái + tìm tên file
- Type legend chips 8 loại — click quick-filter

---

## `/documents` — VACombobox gõ để tìm ✅ (2026-06-13)

- Component mới `components/va/combobox.tsx` (`VACombobox`) — dùng `@base-ui/react` Combobox
- Áp dụng cho 6 combobox filter bar `/documents` và filter OP trong `/dimsheet`
- **Lesson**: `Combobox.Input` không tự select-all — fix: `onFocus={e => e.currentTarget.select()}`
- `Combobox.Root` controlled: `isItemEqualToValue` so theo `value` (không theo object ref), `itemToStringLabel` filter theo label

---

## Dimsheet redesign theo mockup `va-dimsheet.jsx` ✅ (2026-06-13)

- Header: part number (mono) + VABadge "Bản vẽ Rev {rev}" + description
- KPI strip (4): Tổng dim, Balloon unique, FAI Final, Số OP có dim
- Filter bar: VACombobox OP + category chips (LIN/ANG/THD/GEO/SFC, màu riêng) + checkbox "Chỉ FAI Final" + tìm balloon
- Table: balloon circle badge (viền đỏ nếu isCritical), OP badge, Max màu ok/Min màu err
- Empty states: noRouting, empty, noMatch

---

## `/documents` + `/jobs` — i18n English ✅ (2026-06-13)

- `/documents`: namespace đầy đủ `title/kpi/pendingBanner/upload/filter/table/actions/status`
- `STATUS_META` → tách `STATUS_KIND` (chỉ màu); label dùng `t('status.${d.status}')`
- `/jobs`: namespace đầy đủ cho `/jobs`, `/jobs/[id]/operations`, `CreateJobDialog`
- `jobStatus()` trả `{ statusKey, kind }` — label qua `t('status.${statusKey}')`
- Footer text có `<strong>` tách thành nhiều key (không dùng `t.rich()`)

---

## Phase D + F — Jobs: progress card + routing reference + serial grid ✅ (2026-06-13)

- Backend: `GetJobProgressQuery` → `JobProgressDto {totalDim, completeDim, passDim, failDim}`; endpoint `GET /api/v1/jobs/{id}/progress`
- `ProductDto` thêm `sessionStatus` ("none"/"claimed"/"inprogress") + `claimedByName`; endpoint `GET /api/v1/jobs/{id}/products`
- Frontend `JobDetail` viết lại: Header + KPI strip (5) + Progress card (stacked bar) + Routing card (OP strip ngang) + Serial/Product card (grid 4 trạng thái max 48)
- `/jobs/[id]/page.tsx` → redirect `router.replace('/jobs?jobId={id}')`
- Jobs — gộp card "Routing tham chiếu" + "Custom OPs": custom OP hiển thị viền nét đứt cam `va.accent`
- Jobs — `/jobs/[id]/operations` (mới): master-detail mirror `/parts/[id]/operations` cho ForJobOnly OPs

---

## Excel Template Download + Bulk Upload — `/documents` ✅ (2026-06-15)

- `ExcelTemplateBuilder.BuildOpsTemplate()`/`BuildDimensionsTemplate()` — sinh `.xlsx`; endpoints `GET /api/v1/operations/import/template` và `.../dimensions/import/template`
- `ResolveBulkUploadQuery`: nhận `List<ResolveBatchItem>`, match vào DB, trả `List<ResolveBatchResultDto>` với `existingSegments`
- `lib/bulk-upload-parser.ts`: parse filename convention → `resolveBatch()` → `applyClientChecks()` (Duplicate + SegmentIncomplete)
- `components/documents/bulk-upload-dialog.tsx`: 2 input (multi-file / webkitdirectory), pipeline parser, bảng status, upload chỉ row Ready
- `lib/doc-format.ts`: tách `FILE_TYPE_COLORS`, `formatBytes()`, `downloadBlob()` ra dùng chung
- Naming convention bắt buộc: `{PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}[-{i}_{n}].ext`
- **Giới hạn đã biết**: segment key theo `(FileTypeId, PartRevId, PartOpId, JobId)` — không tính `Code` (số program G-code). Giả định 1 OP = 1 G-code program chia segment.

---

## Jobs — Bulk Import từ Excel ✅ (2026-06-15)

- `ImportJobBatchCommandHandler`: nhóm dòng Excel theo JobNumber; 1 transaction/nhóm; `ChangeTracker.Clear()` + ghi lỗi khi exception
- Resolve/tạo Part → PartRev (mới → IsActive=true, deactivate cũ) → Routing + RoutingRev (R1) → upsert PartOp → Resolve/tạo Job
- `ExcelTemplateBuilder.BuildJobBatchTemplate()` — 13 cột; endpoints `POST /api/v1/jobs/import-batch` + `GET .../template`
- `BulkJobImportDialog`: 7 counter cards + danh sách lỗi
- **Quyết định đã chốt**: (1) PartRev mới → IsActive=true, deactivate cũ; (2) JobNumber tồn tại → update RunQty, tăng→tạo thêm Products; (3) RoutingRev: upsert vào active (không tạo rev mới); (4) OpType match case-insensitive, không match → warning + `OpTypeId=null`

---

## ERP Integration — Epicor OData ✅ (2026-06-16)

- `ErpConnection` entity: Name, ErpType, BaseUrl, Company, Username, Password, IsActive — migration `AddErpConnections`
- `IErpConnector` + `IErpConnectorFactory` interfaces (Application layer)
- `MockErpConnector` (10 hardcoded rows) + `EpicorConnector` (OData v4, Basic auth, `EstSetHours` × 60 → minutes)
- Reuse pattern: ERP preview → `ImportJobBatchRow` → gọi `ImportJobBatchCommand` handler có sẵn
- 6 endpoints trong `ErpController`; `IErpConnectorFactory` đăng ký `singleton`
- `ErpImportDialog`: 3 bước (Filter → Preview → Result)
- **Lưu ý Epicor**: `EstSetHours`/`ProdStandard` là giờ → × 60 ra phút; Basic auth Base64
- **Credential**: username/password lưu plaintext trong DB — acceptable cho factory intranet

---

## Dimension Approval + MeasureStage ✅ (2026-06-17)

### Nhóm 1 — Roles + Approval permissions

- `Lead Engineer` role: migration `SeedLeadEngineerRole` (Id=8)
- TechDocument approval: đổi role từ QC Inspector → `Lead Engineer|Manager|Administrator`
- `MeasureStage` enum: `InprocessFAI=0, QCInline=1, QCFinal=2`
- `Dimension.Status` + `ReviewedBy/ReviewedAt/ReviewNote`: migration `AddDimensionStatus` (existing rows default = Approved=1)
- `MeasureValue.MeasureStage`: migration `AddMeasureStage` (existing rows default = InprocessFAI=0)

### Nhóm 2 — Dimsheet bulk import + Approval UI

- `ImportBulkDimensionsCommand` + `BuildDimsheetTemplate()` — 11 cột; endpoints `POST /api/v1/routing-revs/{id}/dimensions/import-bulk` + template
- `ReviewDimensionCommand` (`PUT /api/v1/dimensions/{id}/review`) + `ReviewBatchDimensionsCommand` (`POST /api/v1/routing-revs/{id}/dimensions/review-batch`)
- `/dimsheet`: filter bar thống nhất + cột Status + pending banner "Duyệt tất cả / Từ chối tất cả" + per-row ✓✕
- `/documents`: Part list panel trái 280px + KPI strip tính từ `filtered`

### Nhóm 3 — MeasureStage API + QC Final progress

- `SaveMeasureCommand` nhận thêm `MeasureStage` (default InprocessFAI=0)
- `GetQcFinalProgressQuery` → `QcFinalProgressDto {TotalDim, CompleteDim, PassDim, FailDim}`; endpoint `GET /api/v1/products/{productId}/qcfinal-progress`
- **Lưu ý**: `MeasureValue.MeasuredAt` (KHÔNG phải `CreatedAt`) — MeasureValue không extends BaseEntity

### Còn thiếu (chưa implement)

- `*` balloon (kích thước trung gian) — visual style khác + toggle ẩn/hiện trên dimsheet
- Phase J: FAI "Chi tiết Balloon" panel — cần API `GET /api/v1/dimensions/{id}/measure-history`
- Nhóm 4: Desktop role-aware FAI (Operator=InprocessFAI, QC Inspector=QCInline, QCFinal, Gage selection)

---

## FAI View Redesign + OP INS aggregation fix ✅ (2026-06-18)

Spec: `docs/superpowers/specs/2026-06-18-fai-view-redesign-design.md`. Plan: `docs/superpowers/plans/2026-06-18-fai-view-redesign.md`. Triển khai bằng subagent-driven-development (test project mới + 9 task, mỗi task có code review riêng).

### Business rule mới — OP INS

Phát hiện từ dữ liệu legacy: `OpType.Code = "INS"` (OP kiểm tra) có thể xuất hiện **nhiều lần** trong 1 routing — điển hình quanh bước Coating/Finishing (ví dụ: `...OP100 STP → OP110 INS → OP120 PPG (Phosphating) → OP130 INS`). OP INS không sở hữu dimension riêng — nó dùng để QC đo lại dimension của các OP gia công đứng trước nó (theo `OpNumberSort`).

- `GetFaiSheetQueryHandler` + `GetJobOpsQueryHandler`: khi PartOp đang xét có `OpType.Code` khớp `"INS"` (case-insensitive, so khớp cố định) → gom dimension/đếm dimension của các PartOp có `OpNumberSort ?? 9999m` nhỏ hơn nó (không phải toàn bộ job). Nhiều OP INS liên tiếp quanh 1 bước Coating tự nhiên cho ra cùng kết quả vì OP trung gian không sở hữu dimension.
- `DimensionDto` thêm field `OpNumber` (optional, null khi xem OP thường, set = OP gốc khi xem qua OP INS) — FE hiện nhãn nhỏ "OP{n}" cạnh balloon để tránh nhầm khi nhiều OP gộp lại.
- `PartOpDto` thêm `OpTypeCode` (FE dùng để hiện icon 🔍 trong dropdown chọn OP) và `DimCount` được tính thật (trước đây hardcode 0).
- `JobDto` thêm `OpenNcrCount` (đếm `Ncr.Status=Open` theo JobId) cho job-list panel.

### Test project mới

- `ShopfloorManager.Application.Tests` (xUnit + EF Core InMemory) — project test đầu tiên của solution. `TestDbContextFactory.Create()` tạo DB cách ly mỗi lần gọi.
- 9 test cho 3 handler thay đổi, bao gồm scenario routing thật (2 OP INS quanh 1 bước coating) để verify đúng business rule.

### UI `/fai` redesign

- `FaiJobList` (panel trái 268px): search, status badge (Đúng hạn/Rủi ro/Xong/Trễ derive từ `isComplete`/`shipBy`), progress bar, badge NCR.
- `FaiOpSelect`: custom dropdown thay `<select>`, hiện "● sheet"/"chưa đo" theo `dimCount`, icon 🔍 cho OP INS.
- `FaiMatrix` viết lại toàn bộ: info bar + stats strip dạng card, balloon hiển thị vòng tròn màu theo `categoryCode` (LIN/ANG/THD/GEO/SFC), tooltip nổi theo con trỏ (thay native `title`), legend Pass/Fail/Chưa đo/NCR, sticky Serial/Kết quả border đậm hơn.
- `/jobs/[id]/fai` không có job-list panel (đã ở context 1 job), chỉ hưởng phần `FaiMatrix` chung.

### Lessons learned

- **Worktree branch tách từ git history đã commit sẽ thiếu mọi thay đổi uncommitted local** — nếu spec/plan được viết dựa trên việc đọc file uncommitted, worktree mới tạo sẽ KHÔNG có các thay đổi đó. Phải kiểm tra `git status` trước khi tạo worktree cho 1 feature đang có sửa đổi uncommitted ở đúng những file sẽ động tới.
- Subagent implementer cần xác nhận `pwd`/`git rev-parse --show-toplevel` ở đầu mỗi dispatch khi làm việc gần ranh giới worktree/working-tree — 1 lần subagent vô tình commit nhầm vào `master` thay vì worktree.

### Follow-up sau review UI thật (cùng ngày, làm trực tiếp — không qua plan/subagent)

So với mockup gốc, layout ban đầu đặt sai vị trí filter + nút export. Sửa trực tiếp, build+verify trên browser sau mỗi bước:

- Filter bar (Operation + Measure Stage) chuyển ra khỏi `FaiMatrix`, lift `stageFilter`/export logic lên page — khớp đúng mockup: Excel/Xuất FAI PDF nằm trong `VATopbar`, filter bar là 1 hàng riêng phía trên info bar.
- Filter bar đổi từ thanh dính liền topbar (`borderBottom` phẳng) sang card bo tròn + shadow, tách biệt — đồng bộ với info bar/stats card/matrix card.
- **"Tất cả OP"**: thêm lựa chọn trong `FaiOpSelect`, backend `GetFaiSheetQuery.PartOpId` đổi `int` → `int?` (null = gom Dimension toàn bộ routing của Job, xem §3.11 `06_dimensions_fai.md`). Mặc định chọn "Tất cả OP" ngay khi vào Job (trước đó mặc định OP đầu tiên có dimension).
- **Measure Stage**: bỏ lựa chọn "Tất cả" — luôn hiển thị đúng 1 stage, mặc định In-process FAI.
- Endpoint `GET /api/v1/fai`, `/export/excel`, `/export/pdf`: `partOpId` đổi thành query param tùy chọn (bỏ qua = "Tất cả OP").

---

## Các quyết định thiết kế quan trọng (theo thời gian)

| Ngày | Quyết định |
|---|---|
| 2026-06-12 | Phase G tách 2 trang: `/parts` + `/parts/[id]/operations` (thay vì 1 trang) |
| 2026-06-13 | Sidebar nhóm cuối = "Hệ thống" (không đổi thành "Master Data") |
| 2026-06-13 | Theme color giữ `#6D3B1A` (không đổi sang "caphe" preset) |
| 2026-06-13 | NCR redesign đầy đủ workflow 5 bước |
| 2026-06-13 | Phase E: flat-list filter (thay cascading selector) |
| 2026-06-15 | Bulk upload: naming convention bắt buộc cho auto-resolve |
| 2026-06-15 | Import batch: RoutingRev upsert vào active (không tạo rev mới) |
| 2026-06-17 | Approve/Reject Documents + Dimensions: `Lead Engineer|Manager|Admin` (không phải QC Inspector) |
| 2026-06-18 | OP INS nhận diện bằng `OpType.Code == "INS"` so khớp cố định (không thêm cờ schema mới); gom dimension theo `OpNumberSort` nhỏ hơn, không gom toàn bộ job |
| 2026-06-18 | Job-list panel `/fai` bỏ field "khách hàng" — domain chưa có bảng Customer để resolve tên, chỉ có `PoLine.CustomerId` |
