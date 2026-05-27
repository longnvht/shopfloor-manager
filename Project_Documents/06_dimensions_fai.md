# Dimensions & FAI (First Article Inspection)

## 1. Tổng quan

Module cốt lõi của hệ thống chất lượng — định nghĩa kích thước cần kiểm tra và ghi nhận kết quả đo kiểm thực tế.

**Người dùng liên quan:**
- **Engineer**: định nghĩa Dimension Sheet (kích thước, dung sai).
- **QC Inspector / Operator**: nhập kết quả đo tại máy (Desktop MES) hoặc văn phòng (Web).

---

## 2. Khái niệm cốt lõi

```
PartOP (Công đoạn)
 └── Dimension (Kích thước cần kiểm)     ← Engineer định nghĩa
      ├── BalloonNumber: "1", "2A", "3"...
      ├── NominalValue: 25.0000
      ├── MaxValue: 25.0200  (= Nominal + Tolerance+)
      ├── MinValue: 24.9800  (= Nominal - Tolerance-)
      └── MeasureValue (Kết quả đo)       ← Operator/Inspector nhập
           ├── Value: 25.0050
           ├── Result: pass / fail
           └── GageID (dụng cụ đo sử dụng)
```

---

## 3. Business Rules

### 3.1 Balloon Number
- Số hiệu kích thước trên bản vẽ (hình tròn bao quanh số → "balloon").
- Format tự do: "1", "2A", "3B", "10", "10A"...
- **UNIQUE trong phạm vi một PartOP** — không được trùng trong cùng một OP.
- `balloon_sort`: parse từ balloon_number để sort đúng thứ tự số (1, 2, 3... thay vì 1, 10, 2, 3...).

### 3.2 Kích thước số vs Kích thước text
- **Số** (phần lớn): lưu `nominal_value`, `max_value`, `min_value` dạng `DECIMAL(14,4)`.
  - Pass khi: `min_value ≤ measured_value ≤ max_value`.
- **Text** (`is_text_type = true`): kích thước không phải số — ren, ký hiệu hình học, note đặc biệt.
  - Ví dụ: "M10x1.5-6H", "Ra 0.8"
  - Lưu vào `nominal_text`, không có min/max.
  - Kết quả đo: operator chọn Pass/Fail thủ công.

### 3.3 Tolerance
- `tolerance_plus`: dung sai dương (trên) → `max_value = nominal + tolerance_plus`.
- `tolerance_minus`: dung sai âm (dưới) → `min_value = nominal - tolerance_minus`.
- Cả hai lưu dạng dương: dung sai ±0.02 → `tolerance_plus = 0.02`, `tolerance_minus = 0.02`.
- Dung sai lệch tâm: `+0.05 / -0.02` → `tolerance_plus = 0.05`, `tolerance_minus = 0.02`.

### 3.4 Final Dimension
- `is_final = true`: kích thước kiểm tra **lần cuối** sau tất cả rework.
- OP kiểm tra cuối thường có các dimension đánh dấu `is_final`.
- MeasureValue cho Final inspection lưu `is_final = true` + `final_op_id`.
- Phân biệt rõ: đo ban đầu (FAI) vs đo cuối sau rework (FAI Final).
- `IsFinal = true` → chỉ **QC Inspector** mới được nhập (Operator thấy nhưng disabled).
- Legacy: 90,061/94,100 dimensions (96%) có `FinalDimension = 1` → FAI Final là flow rất phổ biến.

### 3.5 Dimension Category
Mỗi dimension thuộc một category (phương pháp đo):

| Code | Tên | Gage thường dùng |
|---|---|---|
| `LIN` | Linear | Thước cặp (CAL), panme (MIC), bore gage (BOR), depth gage (DPG), height gage (HEG) |
| `ANG` | Angular | Thước góc (ANG) |
| `THD` | Thread | Dưỡng ren (PLG/RIG), pitch diameter gage (PDG), taper gage (ETG/IGT), thread height gage (THG) |
| `GEO` | Geometric | CMM, dial indicator (IND), profile projector (PPM), radius gage (RAD), visual (VIS) |
| `SFC` | Surface | Surface roughness machine (SRM), surface roughness template (SRT) |

Category quyết định gage type nào được chọn khi đo.

**Tần suất sử dụng thực tế (từ dữ liệu legacy 94,100 dimensions):**
PPM 34% · CAL 16% · CMM 7% · IND 6% · MIC 5% · VIS 4% · BOR 4% · DPG 4% · PLG 4% · SRM 4%

→ GEO (PPM+CMM+IND+VIS) chiếm ~51%, LIN (CAL+MIC+BOR+DPG+HEG) chiếm ~35%, THD ~8%, SFC ~4%.

### 3.6 Lịch sử thay đổi Dimension
- Mỗi khi thay đổi nominal/tolerance → tạo record trong `dimension_history`.
- Dữ liệu `measure_values` cũ vẫn giữ nguyên và valid (so với spec tại thời điểm đo).

### 3.7 Import từ Excel
- Engineer nhận Dimension Sheet từ bộ phận thiết kế dưới dạng Excel.
- Format chuẩn: `BalloonNumber | Nominal | TolPlus | TolMinus | CategoryCode`.
- Validation: BalloonNumber không trùng trong OP, Nominal phải là số (trừ `is_text_type`).
- Upload file Excel gốc lên MinIO sau khi import (lưu vết).

---

## 4. Workflow FAI tại máy (Desktop MES)

### 4.1 FAI lần đầu (First Article)

```
Operator login → chọn Máy
  → Màn hình Monitor: chọn Job → chọn OP → chọn Product Serial
  → Claim session → Bắt đầu (ghi started_at)
      → Hiện danh sách Balloon Number (sorted theo balloon_sort)
        Màu sắc: Xám=chưa đo | Xanh=Pass | Đỏ=Fail
        Counter: "5 / 20"
  → Chọn Balloon Number:
      → Hiện: Nominal, Tolerance (+/-), Category
      → Chọn Gage *(xem 4.3 — chưa implement, kế hoạch Phase 5)*
      → Nhập giá trị đo (bàn phím số cảm ứng)
        → Kích thước số: nhập giá trị, hệ thống tính Pass/Fail tự động
        → Kích thước text (is_text_type=true): chọn PASS / FAIL thủ công
      → Kết quả:
          Pass → Balloon xanh, tự động chuyển balloon tiếp theo
          Fail → Balloon đỏ, hiện dialog NCR (→ xem 07_ncr.md)
  → Tất cả đã đo → "Kết thúc" → ghi completed_at
```

**Dimension đã đo (IsInputLocked):** Khi chọn lại balloon đã có kết quả → hiển thị giá trị cũ, lock toàn bộ input (không cho nhập lại). Mỗi lần đo là 1 record mới — không overwrite (giữ lịch sử).

### 4.2 FAI Final (sau rework NCR)

```
NCR đã xử lý xong (rework/repair hoàn tất)
  → QC Inspector mở FAI Final cho Product Serial đó
  → Chỉ hiển thị các balloon có trạng thái Fail (màu đỏ)
  → Đo lại từng balloon:
      → Nhập giá trị đo mới
      → Lưu với is_final=true + final_op_id (OP kiểm tra cuối)
      → Pass → balloon chuyển xanh, NCR liên quan đóng tự động
      → Fail lại → tạo NCR mới, tiếp tục chu kỳ rework
```

> **Trạng thái implement:** FAI Final chưa có trên Desktop — ⏳ kế hoạch Phase 4b. Schema database (`is_final`, `final_op_id` trong `measure_values`) đã sẵn sàng.

**Phân quyền FAI Final:**
- `IsFinal = true` dimensions → chỉ **QC Inspector** nhập được
- Operator thấy dimension nhưng input bị disabled

### 4.3 Gage Selection (Chọn dụng cụ đo)

Mỗi kết quả đo nên ghi nhận dụng cụ nào đã dùng — phục vụ truy vết sau này khi gage bị thu hồi hoặc calibration lỗi.

```
Tap Balloon Number → Input Panel mở:
  ① Chọn Gage (tùy chọn):
       → GET /api/v1/gages?categoryId={dim.categoryId}&status=available
       → Hiện danh sách: GageNo, GageName, GageType
       → Filter: is_calibrated=true + status=available
       → Nếu không có gage phù hợp → cảnh báo, vẫn cho nhập không chọn gage
  ② Nhập giá trị
  ③ Confirm → POST với gageId (null nếu không chọn)
```

**Rules:**
- Filter theo `dimension.category_id` → chỉ show gage thuộc đúng loại (LIN/THD/GEO...)
- Gage phải `is_calibrated = true` và `status = available`
- Không bắt buộc (optional) — không block workflow nếu bỏ qua

> **Trạng thái implement:** Gage selection chưa có trên Desktop (⏳ Phase 5). API endpoint `GET /api/v1/gages` cần implement. Hiện tại `measure_values.gage_id` lưu null.

**Tầm quan trọng (từ legacy data):** Legacy Vinam-MES ghi `GageID` vào 100% trong 904,699 `measurevalue` records — được dùng để invalidate kết quả đo khi gage hỏng/hết hạn calibration.

---

## 5. Data Model

```sql
dimension_categories (id, code [UNIQUE], name, description)

dimensions (
    id [BIGSERIAL], part_op_id → part_ops,
    balloon_number, balloon_sort,
    nominal_value [DECIMAL(14,4)],
    max_value     [DECIMAL(14,4)],
    min_value     [DECIMAL(14,4)],
    tolerance_plus  [DECIMAL(14,4)],
    tolerance_minus [DECIMAL(14,4)],
    nominal_text  [VARCHAR(100)],
    is_text_type  [BOOLEAN],
    category_id   → dimension_categories,
    is_final      [BOOLEAN],
    created_by, created_at, updated_by, updated_at,
    deleted_at,
    UNIQUE(part_op_id, balloon_number)
)

dimension_history (
    id, dimension_id → dimensions,
    nominal_value, max_value, min_value,
    tolerance_plus, tolerance_minus,
    nominal_text, category_id, is_final,
    changed_by → users, changed_at, change_reason
)

measure_values (
    id [BIGSERIAL],
    dimension_id → dimensions,
    product_id   → products,
    part_op_id   → part_ops,
    value        [DECIMAL(14,4)],        -- NULL nếu is_text_type
    result       [measure_result ENUM],  -- 'pass' | 'fail'
    gage_id      → gages,
    machine_id   → machines,
    measured_by  → users,
    user_type    [VARCHAR(30)],          -- snapshot tại thời điểm đo
    measured_at,
    note,
    is_final     [BOOLEAN],
    final_op_id  → part_ops,
    has_ncr      [BOOLEAN],
    ncr_code     [VARCHAR(50)],
    updated_by, updated_at
)

measure_value_logs (
    id, measure_id → measure_values,
    old_value, new_value,
    old_result, new_result,
    gage_id, note,
    changed_by, changed_at
)
```

---

## 6. API Endpoints

```
-- Dimensions --
GET    /api/v1/ops/{opId}/dimensions
POST   /api/v1/ops/{opId}/dimensions
PUT    /api/v1/dimensions/{id}
DELETE /api/v1/dimensions/{id}
POST   /api/v1/ops/{opId}/dimensions/import
GET    /api/v1/dimensions/{id}/history

-- Measure Values --
GET    /api/v1/measure-values?dimensionId=&productId=
POST   /api/v1/measure-values
PUT    /api/v1/measure-values/{id}
GET    /api/v1/measure-values/{id}/logs

-- FAI Report --
GET    /api/v1/reports/fai/{jobId}
GET    /api/v1/reports/fai/{jobId}/pdf
GET    /api/v1/reports/fai/{jobId}/excel

-- MES --
GET    /api/v1/mes/ops/{opId}/dimensions?productSerial={serial}
POST   /api/v1/mes/measure-values
       Body: { dimensionId, productId, opId, gageId, value, machineId }
       Response: { result: 'pass'|'fail', measureValueId }
```

---

## 7. FAI Report (PDF)

Nội dung báo cáo:
- **Header**: Job Number, Part Number, Revision, Date, Inspector
- **Bảng kích thước**: BalloonNumber | Nominal | Tol(+/-) | Serial01 | Serial02 | ...
- Ô Pass: giá trị đo (màu đen)
- Ô Fail: giá trị đo (màu đỏ, bold)
- **Footer**: tổng Pass/Fail, chữ ký Inspector

**Ghi chú footnote**: Khi `nominal_text` > 7 ký tự → thay bằng N1\*, N2\*... và ghi đầy đủ cuối trang.

---

## 8. Edge Cases

- **Sửa MeasureValue sau khi đã có NCR**: cho phép nhưng ghi `measure_value_logs`. Nếu sửa fail → pass, NCR phải được đóng thủ công bởi Inspector.
- **Đo lại**: tạo record MeasureValue mới (không update cũ) — giữ toàn bộ lịch sử các lần đo.
- **Xóa Dimension đã có MeasureValue**: cấm tuyệt đối — ảnh hưởng audit trail chất lượng.
- **Tolerance = 0**: max = min = nominal — hiếm nhưng hợp lệ.
- **Offline tại MES**: measure values queue local (SQLite) → sync khi có mạng.
- **Value ngoài range rất xa**: cảnh báo "Có thể nhập sai" nếu ngoài ±10× tolerance, nhưng vẫn cho lưu.
- **Precision**: Legacy Vinam-MES lưu `MeasureValue` dạng `FLOAT` → mất chính xác ở chữ số thập phân 4+. Hệ thống mới dùng `DECIMAL(14,4)` cho tất cả giá trị đo — cải thiện quan trọng khi migrate/import dữ liệu cũ.
- **NominalDimension dạng text trong legacy**: Legacy lưu `NominalDimension` là `VARCHAR` chứa cả số lẫn ký tự (ví dụ `"72°"`, `"M10x1.5"`). Khi import → parse tách `is_text_type=true` và `nominal_text` thay vì ép sang số.
- **Gage thu hồi/hỏng**: Nếu gage bị kết luận hỏng sau khi đã đo → cần xác định tất cả `measure_values` dùng `gage_id` đó trong khoảng thời gian → xem xét re-inspect. `gage_id` trong `measure_values` là key truy vết quan trọng (hiện để null — cần implement gage selection).
