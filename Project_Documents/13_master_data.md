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
- `machine_groups`: nhóm máy theo loại/khu vực (ví dụ: "Khu CNC", "Khu Mài", "CMM").
- `machines`: từng máy cụ thể với thông số kỹ thuật đầy đủ:

| Thông số | Mô tả |
|---|---|
| `code` | Mã máy (ví dụ: "MC-01", "LATHE-03") — UNIQUE |
| `name` | Tên máy (ví dụ: "FANUC 0i-MF Plus") |
| `machine_type` | Loại (CNC Turning, CNC Milling, Grinding, CMM...) |
| `serial_number` | Số serial máy |
| `travel_x/y/z` | Hành trình các trục |
| `max_od`, `length`, `dia` | Kích thước gia công tối đa |
| `is_cnc` | CNC hay máy thông thường |
| `factory_id` | Thuộc nhà máy nào |

- `machine_configs`: cấu hình serial port (COM port, baud rate...) cho mỗi PC Desktop MES.
  - Mỗi máy CNC có 1 PC cạnh đó → 1 config row.
  - Khi Desktop MES khởi động: load config theo `pc_name = Environment.MachineName`.

### 2.3 OP Types
- Loại công đoạn sản xuất (ví dụ: `CNC_TURNING`, `CNC_MILLING`, `CMM_INSPECTION`).
- Liên kết với `mes_menu_op_types` để xác định menu nào hiện trên Desktop MES cho từng OP type.
- Khi thêm OP type mới → phải cấu hình `mes_menu_op_types` tương ứng.

### 2.4 Dimension Categories
- Phương pháp đo kiểm: `LIN`, `ANG`, `THD`, `GEO`, `SFC`.
- Liên kết với `gage_types` để filter gage phù hợp khi đo.
- Thêm mới khi có phương pháp đo mới, không sửa code cũ (data-driven).

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
Các bảng sau có dữ liệu mặc định khi cài đặt (`init.sql`):
- `work_statuses`: Working, On Leave, Resigned
- `user_types`: Admin, Manager, Engineer, QC, Operator, Inspector, Planner
- `gage_statuses`: VALID, EXPIRED, DAMAGED, BORROWED, CALIB
- `departments`: MGMT, QC, PROD, ME, PLAN, MAINT
- `roles`: Administrator, Manager, Engineer, QC Inspector, Operator, Planner
- `file_types`: DRW, GCD, RTC, FXT, THD, TLS, CAM, CAD

### 3.3 Machine Config — Auto-detect tại MES
```csharp
// Desktop MES khởi động:
string pcName = Environment.MachineName;
var config = await api.GetMachineConfigByPcName(pcName);
if (config != null)
    // Tự động chọn máy CNC tương ứng
else
    // Hiện màn hình chọn máy thủ công
```

---

## 4. API Endpoints

```
-- Factories --
GET    /api/v1/factories
POST   /api/v1/factories
PUT    /api/v1/factories/{id}

-- Machine Groups & Machines --
GET    /api/v1/machine-groups
POST   /api/v1/machine-groups
GET    /api/v1/machines?groupId=&isCnc=&factoryId=
POST   /api/v1/machines
PUT    /api/v1/machines/{id}
GET    /api/v1/machines/{id}/config        -- Lấy machine config theo PC name
PUT    /api/v1/machines/{id}/config

-- OP Types --
GET    /api/v1/op-types
POST   /api/v1/op-types
PUT    /api/v1/op-types/{id}

-- Dimension Categories --
GET    /api/v1/dimension-categories
POST   /api/v1/dimension-categories
PUT    /api/v1/dimension-categories/{id}

-- Fixture --
GET    /api/v1/fixture-types
GET    /api/v1/fixture-locations
GET    /api/v1/fixture-slots?locationId=
GET    /api/v1/fixture-categories?typeId=
POST/PUT cho tất cả

-- Document Types --
GET    /api/v1/document-types
POST   /api/v1/document-types

-- MES Menus --
GET    /api/v1/mes-menus
PUT    /api/v1/mes-menus/{id}
GET    /api/v1/mes-role-menus
PUT    /api/v1/mes-role-menus/{id}
GET    /api/v1/mes-menu-op-types?opTypeId=
POST   /api/v1/mes-menu-op-types
DELETE /api/v1/mes-menu-op-types/{id}
```

---

## 5. Edge Cases

- **Đổi tên machine code**: ảnh hưởng đến MQTT topic (phải update cả cấu hình trên MDC Agent). Hiện cảnh báo khi đổi.
- **Xóa OP Type đang có PartOP**: không cho xóa.
- **Xóa Dimension Category đang có Dimensions**: không cho xóa.
- **Machine config không tìm thấy**: Desktop MES hiện màn hình chọn máy thủ công — không crash.
- **Multi-factory (tương lai)**: đã chuẩn bị `factory_id` trên các bảng liên quan, nhưng Phase 0-4 chỉ support 1 factory.
