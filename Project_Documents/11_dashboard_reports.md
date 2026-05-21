# Dashboard & Reports

## 1. Tổng quan

Module tổng hợp chỉ số KPI và xuất báo cáo PDF/Excel cho các bên liên quan.

**Người dùng liên quan:** Manager, QC, Engineer, Planner — mỗi role thấy dashboard phù hợp.

---

## 2. Các Dashboard

### 2.1 Dashboard Tổng quan (General)
Chỉ số toàn hệ thống trong khoảng thời gian (tuần/tháng):

| Chỉ số | Cách tính |
|---|---|
| Tổng file kỹ thuật | COUNT(tech_documents) theo khoảng thời gian |
| Tỉ lệ Approve | approved / total × 100% |
| Tỉ lệ Reject | rejected / total × 100% |
| Số Part đang sản xuất | COUNT(jobs WHERE NOT is_complete) |
| Số OP đang sản xuất | COUNT(part_ops WHERE NOT is_complete) |

Biểu đồ: cột (upload theo ngày), tròn (phân bố file type).

### 2.2 Dashboard Sản xuất (Production)
Theo dõi tiến độ sản xuất thực tế:

| Chỉ số | Nguồn dữ liệu |
|---|---|
| Số máy đang chạy | machine_events (RunMode = ACTIVE trong ngày) |
| Job đang thực hiện | planning_items (startTime ≤ NOW ≤ endTime) |
| Operator theo ca | shift_assignments ngày hôm nay |
| Sản phẩm hoàn thành | products WHERE is_complete = true (ngày/tuần) |
| Tiến độ đo kiểm | v_product_completion tổng hợp |

### 2.3 Dashboard Chất lượng (Quality)
Phân tích chất lượng đo kiểm:

| Chỉ số | Nguồn |
|---|---|
| Tổng Dimension đo | COUNT(measure_values) |
| Pass rate | pass / total × 100% |
| Fail rate | fail / total × 100% |
| Top 10 balloon fail nhiều nhất | GROUP BY dimension_id, ORDER BY fail_count DESC |
| NCR theo phòng ban | GROUP BY ncrs.department_id |
| NCR theo lý do | GROUP BY ncrs.reason_id |
| NCR status (Open/Closed) | GROUP BY ncr_codes.status |

Biểu đồ: cột (fail theo ngày), tròn (fail theo phòng ban).

### 2.4 Dashboard Dụng cụ đo (Gage)
Theo dõi tình trạng dụng cụ đo:

| Chỉ số | Nguồn |
|---|---|
| Phân loại theo GageType | GROUP BY gage_type_id |
| Gage sắp hết hạn (≤30 ngày) | v_gage_calib_due WHERE days_remaining ≤ 30 |
| Gage đã hết hạn | v_gage_calib_due WHERE days_remaining < 0 |
| Top 10 gage dùng nhiều nhất | COUNT(measure_values.gage_id) GROUP BY gage_id |
| Tiến độ hiệu chuẩn tháng này | calib_records WHERE calibration_date IN current_month |

### 2.5 Dashboard Inspector (QC)
Dành riêng cho Inspector:

| Chỉ số | Nguồn |
|---|---|
| File đang pending cần duyệt | tech_documents WHERE status = 'pending' |
| File đã duyệt trong tuần | tech_documents WHERE inspected_at IN week |
| Tốc độ duyệt (avg giờ) | AVG(inspected_at - created_at) |
| NCR cần xử lý | ncr_codes WHERE status = 'open' |

---

## 3. Business Rules

### 3.1 Phân quyền Dashboard
- User chỉ thấy dashboard phù hợp với role của mình.
- Dữ liệu không lọc theo user (xem toàn bộ nhà máy), trừ Dashboard Inspector (chỉ file của mình).

### 3.2 Real-time vs Cached
- Dashboard **không** query trực tiếp mỗi lần load (quá nặng).
- Cache kết quả trong bộ nhớ (DistributedMemoryCache hoặc Redis sau này), TTL = 5 phút.
- SignalR push update khi có thay đổi quan trọng (NCR mới, file mới được duyệt...).

### 3.3 Khoảng thời gian
- Mặc định: **tuần hiện tại** (Thứ 2 → Chủ nhật).
- Cho phép chọn: ngày, tuần, tháng, quý, tùy chọn.

---

## 4. Reports (PDF/Excel)

### 4.1 FAI Report
Xem chi tiết tại [06_dimensions_fai.md](06_dimensions_fai.md#7-fai-report-pdf).

### 4.2 NCR Report
Nội dung:
- Danh sách NCR trong khoảng thời gian.
- Filter: theo Job, theo phòng ban, theo trạng thái.
- Mỗi NCR: NCRCode, Job, Part, OP, Balloon, giá trị fail, lý do, phòng ban, trạng thái CPAR/Rework.
- Tổng hợp: số lượng Open/Closed, theo lý do, theo phòng ban.

### 4.3 Calibration Report
- Lịch sử hiệu chuẩn của tất cả gage trong kỳ.
- Gage sắp hết hạn.
- Chi phí hiệu chuẩn theo vendor (nếu có).

### 4.4 Dimension Export (Excel)
- Export toàn bộ Dimension Sheet của một Job/OP ra Excel.
- Format: BalloonNumber | Nominal | Tol(+/-) | Category | Serial01_Value | Serial01_Result | Serial02...

---

## 5. API Endpoints

```
-- Dashboard data --
GET    /api/v1/dashboard/overview?startDate=&endDate=
GET    /api/v1/dashboard/production?startDate=&endDate=
GET    /api/v1/dashboard/quality?startDate=&endDate=
GET    /api/v1/dashboard/gage?startDate=&endDate=
GET    /api/v1/dashboard/inspector?startDate=&endDate=

-- Reports --
GET    /api/v1/reports/fai/{jobId}/pdf
GET    /api/v1/reports/fai/{jobId}/excel

GET    /api/v1/reports/ncr/pdf?startDate=&endDate=&departmentId=&status=
GET    /api/v1/reports/ncr/excel?startDate=&endDate=

GET    /api/v1/reports/calibration/pdf?startDate=&endDate=
GET    /api/v1/reports/calibration/excel?startDate=&endDate=

GET    /api/v1/reports/dimension-export/{jobId}/excel
```

---

## 6. Edge Cases

- **Dashboard load chậm**: implement caching ngay từ đầu, không để query raw mỗi request.
- **Kỳ báo cáo không có data**: trả về 0 cho tất cả chỉ số, không lỗi.
- **FAI report Job chưa đo xong**: vẫn export được, ô chưa đo để trống.
- **PDF font tiếng Việt**: QuestPDF phải cấu hình font hỗ trợ Unicode để hiển thị đúng tiếng Việt.
