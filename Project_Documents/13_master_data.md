# Master Data — Dữ liệu danh mục hệ thống

## 1. Tổng quan

Các bảng dữ liệu nền tảng (lookup/reference data) mà tất cả module khác phụ thuộc vào. Thường được thiết lập một lần khi cài đặt hệ thống, thay đổi ít.

**Người dùng liên quan:** Admin, Manager — thiết lập ban đầu.

---

## 2. Danh mục

### 2.1 Factory (Nhà máy)
- Mỗi instance hệ thống phục vụ 1 nhà máy (tương lai hỗ trợ multi-factory).
- Lưu: tên, địa chỉ, số điện thoại.
- Gắn với: Machine, GageLocation.

### 2.2 Machine Group & Machine

**Trạng thái implement:** `Machine` entity đã được implement (Phase 4, migration `AddMachines`). Các phần còn lại là Phase 5.

**✅ Đã implement — `machines` table (schema tối giản):**

| Thông số | Mô tả |
|---|---|
| `code` | Mã máy — UNIQUE, dùng làm identifier trong MQTT, session, local.json |
| `name` | Tên máy |
| `machine_type` | TypeCode legacy (LLA40, MIL, SLA...) — tham khảo, không validate |
| `is_cnc` | CNC (true) hay máy thủ công/process (false) |
| `is_active` | Lọc trong Settings ComboBox và API |

- **115 máy active** đã seed từ legacy Vinam-MES (migration `AddMachines`)
- API: `GET /api/v1/machines?activeOnly=true` đã implement
- Desktop Settings page dùng ComboBox chọn máy từ danh sách này

**⏳ Phase 5 — chưa implement:**
- `machine_groups`: nhóm máy theo loại/khu vực
- Thêm trường: `serial_number`, `travel_x/y/z`, `max_od`, `length`, `dia`, `factory_id`
- `machine_configs`: cấu hình MQTT/serial port cho MDC agent

**Cấu hình máy trên Desktop MES (thực tế):**
Desktop MES KHÔNG dùng `machine_configs` table. Thay vào đó mỗi PC có file `local.json`:
```json
{
  "MachineCode": "3100L",
  "MachineName": "3100L LONG LATHE MACHINE",
  "ApiBaseUrl": "http://192.168.0.100:5066"
}
```
Admin chỉnh qua Settings page (shortcut "Cài đặt" trên Dashboard). `MachineCode` phải khớp với `machines.code` trong DB.

### 2.3 OP Types
- Loại công đoạn sản xuất.
- Liên kết với `mes_menu_op_types` để xác định menu nào hiện trên Desktop MES cho từng OP type.
- Khi thêm OP type mới → phải cấu hình `mes_menu_op_types` tương ứng.

**Seed hiện tại (6 loại — DbContext):**

| Code | Tên | Legacy tương ứng |
|---|---|---|
| CNC | CNC Machining | MLA, LLA, LLA40, LLA60, MLA36, MLA60, SLA, TLA, MIL, 5MI |
| INSP | Inspection | INS |
| GRIND | Grinding | GRP, HNG |
| WIRE | Wire EDM | WED |
| MILL | Milling | MIL (manual) |
| TURN | Turning | MAL (manual lathe) |

**Chưa seed (có thể thêm khi cần):**

| Code | Tên | Legacy tương ứng |
|---|---|---|
| HT | Heat Treatment | HTR, SRP, IDH |
| CLEAN | Cleaning | PPG, CGP, COP, MLK, QPQ, XYL, FLUO, DMC, NCO, BDG |
| COAT | Coating | (như CLEAN — process ngoài) |

**Legacy reference:** Vinam-MES cũ có 42 loại cụ thể. Toàn bộ đã được migrate vào bảng `machines` (machine_type field). Khi import PartOp từ legacy, map `OPType` theo bảng trên.

### 2.4 Dimension Categories
- Phương pháp đo kiểm nhóm theo dụng cụ sử dụng: `LIN`, `ANG`, `THD`, `GEO`, `SFC`.
- Liên kết với `gage_types` để filter gage phù hợp khi đo.
- Thêm mới khi có phương pháp đo mới, không sửa code cũ (data-driven).

**Mapping từ legacy `gagetypefix` (48 loại cụ thể) sang category mới:**

| Category | Legacy codes |
|---|---|
| `LIN` | CAL, MIC, BOR, DPG, HEG, LHG |
| `ANG` | ANG |
| `THD` | PLG, RIG, RGA, IPG, ETG, IGT, THG, PDG, PGA, GMT |
| `GEO` | CMM, IND, PPM, RAD, VIS |
| `SFC` | SRM, SRT |

### 2.5 Fixture Management
Phân cấp quản lý đồ gá:

```
FixtureType (Loại đồ gá: Horizontal Vise, V-Block, Custom Fixture...)
FixtureLocation (Khu lưu trữ: "Kho Đồ gá A", "Tủ B")
 └── FixtureSlot (Vị trí cụ thể: "Kệ 1", "Ngăn 3")
FixtureCategory (Danh mục: kết hợp Type + Location, có min/max range)
```

- `gcode_library.fixture_category_id` → liên kết G-code thư viện với loại đồ gá.

### 2.6 Document Types
- Loại tài liệu hệ thống (ISO, quy trình nội bộ, hướng dẫn công việc...).
- Dùng trong module `documents` (tài liệu ISO/QMS, khác với `tech_documents`).

### 2.7 Work Statuses
- Trạng thái nhân viên: `Working`, `On Leave`, `Resigned`.
- `is_working = true/false` → filter user hiện trong dropdown gán việc.

---

## 3. Business Rules

### 3.1 Không xóa dữ liệu đang được tham chiếu
- Trước khi xóa bất kỳ master data nào: kiểm tra FK constraint.
- Nếu đang được dùng → trả về lỗi "Không thể xóa, đang được sử dụng bởi X records".
- Giải pháp: `is_active = false` thay vì xóa (ẩn khỏi dropdown nhưng giữ data cũ).

### 3.2 Seed Data
Dữ liệu mặc định được seed qua EF Core `HasData` trong `ShopfloorDbContext.SeedStaticData()` (không dùng `init.sql`):

| Bảng | Nội dung | Số lượng |
|---|---|---|
| `work_statuses` | Active, On Leave, Resigned | 3 |
| `departments` | ADMIN, QC, PROD, ENG | 4 |
| `roles` | Administrator, Manager, Engineer, QC Inspector, Operator, Planner, **Leader** | **7** |
| `op_types` | CNC, INSP, GRIND, WIRE, MILL, TURN | 6 |
| `file_types` | DRW, GCD, RTC, FXT, THD, TLS, CAM, CAD | 8 |
| `dimension_categories` | LIN, ANG, THD, GEO, SFC | 5 |
| `ncr_reasons` | 15 lý do theo phòng ban (PROD×6, QC×3, ENG×5, Other×1) | 15 |
| `machines` | 115 máy active từ legacy (migration `AddMachines`, 2026-06-04) | 115 |

**Leader role (Id=7):** Tổ trưởng — có quyền force-finish session của người khác trên cùng máy. Xem thêm Desktop MES phân quyền.

### 3.3 Machine Config — Desktop MES (thực tế)

Desktop MES KHÔNG tự động detect máy qua `pc_name`. Cấu hình được set thủ công bởi Admin qua **Settings page** (shortcut "Cài đặt" trên Dashboard, chỉ hiện với role Administrator).

Cấu hình lưu trong `local.json` tại thư mục chạy app:
```json
{
  "ApiBaseUrl": "http://192.168.0.100:5066",
  "MachineCode": "3100L",
  "MachineName": "3100L LONG LATHE MACHINE"
}
```

- `MachineCode` phải khớp với `machines.code` trong DB → ComboBox trong Settings page load từ `GET /api/v1/machines`
- `ApiBaseUrl` thay đổi cần restart app (HttpClient singleton)
- `MachineCode`/`MachineName` áp dụng ngay sau save

---

## 4. API Endpoints

✅ = đã implement | ⏳ = Phase 5

```
-- Machines --
✅ GET    /api/v1/machines?activeOnly=true
✅ GET    /api/v1/machines/{machineCode}/active-session
✅ GET    /api/v1/machines/{machineCode}/daily-summary?date=
⏳ POST   /api/v1/machines
⏳ PUT    /api/v1/machines/{id}
⏳ GET    /api/v1/machine-groups

-- OP Types --
✅ GET    /api/v1/op-types          (read-only, dùng trong dropdown chọn OpType khi tạo PartOp)
⏳ POST   /api/v1/op-types
⏳ PUT    /api/v1/op-types/{id}

-- Dimension Categories --
✅ GET    /api/v1/dimension-categories
⏳ POST   /api/v1/dimension-categories
⏳ PUT    /api/v1/dimension-categories/{id}

-- File Types --
✅ GET    /api/v1/tech-documents/file-types

-- Factories, Fixture, Document Types, MES Menus --
⏳ Tất cả (Phase 5)
```

---

## 5. Legacy Migration Notes

### 5.1 FileType mapping khi import từ Vinam-MES

Legacy dùng 13 loại file (`filestype` table). Mapping sang hệ thống mới:

| Legacy `FileCode` | Tên | → Mới `Code` | Ghi chú |
|---|---|---|---|
| `DRW` | Bản vẽ chi tiết | `DRW` | ✅ 1:1 |
| `TD` | Bản vẽ công nghệ | `RTC` | Gộp với RC |
| `RC` | Route Card | `RTC` | ✅ |
| `FIX` | Bản vẽ đồ gá | `FXT` | ✅ đổi tên |
| `THD` | Bản vẽ ren | `THD` | ✅ 1:1 |
| `GCODE` | Chương trình gia công | `GCD` | ✅, `MachineType="FANUC"` |
| `WC` | Wire Cut G-code | `GCD` | `MachineType="WC"` |
| `MAZAK` | Mazak G-code | `GCD` | `MachineType="MAZAK"` |
| `FGCODE` | GCode Fixture | `GCD` | `MachineType="FIXTURE"` |
| `CAM` | Mô phỏng CAM | `CAM` | ✅ 1:1 |

`TechDocument.MachineType` field (max 20 chars) phân biệt các loại G-code khi import.

### 5.2 TechDocument.Status mapping

| Legacy `Status` | Tên | → Mới `FileStatus` |
|---|---|---|
| `1` | Approve | `Approved = 1` |
| `2` | Reject | `Rejected = 2` |
| `3` | Pending | `Pending = 0` |

**Lưu ý:** Thứ tự enum khác nhau — phải map theo tên, không map theo số.

### 5.3 PartOp.OpNumber format

Legacy: `OPNumber` là varchar, ví dụ "10", "20", "25". Hệ thống mới thêm `OpNumberSort` (decimal) để sort đúng (tránh sort string "100" < "20").

Import rule: `OpNumberSort = decimal.Parse(OpNumber)` nếu parse được, nếu không → null.

---

## 6. Edge Cases

- **Đổi tên machine code**: ảnh hưởng đến MQTT topic (phải update cả cấu hình trên MDC Agent). Hiện cảnh báo khi đổi.
- **Xóa OP Type đang có PartOP**: không cho xóa.
- **Xóa Dimension Category đang có Dimensions**: không cho xóa.
- **Machine config không tìm thấy**: Desktop MES hiện màn hình chọn máy thủ công — không crash.
- **Multi-factory (tương lai)**: đã chuẩn bị `factory_id` trên các bảng liên quan, nhưng Phase 0-4 chỉ support 1 factory.
