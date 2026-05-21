# Shopfloor Manager — Tài liệu dự án

Tài liệu này mô tả chi tiết từng tính năng và Business Logic của hệ thống **Shopfloor Manager** — phần mềm quản lý nhà máy gia công cơ khí mã nguồn mở.

---

## Danh sách tài liệu

| File | Module | Mô tả |
|---|---|---|
| [01_auth.md](01_auth.md) | Auth & Permissions | Đăng nhập, JWT, phân quyền 3 cấp |
| [02_hr.md](02_hr.md) | HR & User Management | Nhân sự, phòng ban, vị trí, kỹ năng |
| [03_job_management.md](03_job_management.md) | Job Management | Job, Part, Product serial, PO |
| [04_routing_operations.md](04_routing_operations.md) | Routing & Operations | OP, routing card, technology sheet |
| [05_technical_documents.md](05_technical_documents.md) | Technical Documents | Upload, duyệt tài liệu kỹ thuật |
| [06_dimensions_fai.md](06_dimensions_fai.md) | Dimensions & FAI | Kích thước, đo kiểm, báo cáo FAI |
| [07_ncr.md](07_ncr.md) | NCR | Non-Conformance Report, CPAR, Rework |
| [08_gage_management.md](08_gage_management.md) | Gage Management | Dụng cụ đo, mượn/trả |
| [09_calibration.md](09_calibration.md) | Calibration | Hiệu chuẩn, lịch sử, vendor |
| [10_planning.md](10_planning.md) | Planning | Lập kế hoạch, Gantt chart, ca làm việc |
| [11_dashboard_reports.md](11_dashboard_reports.md) | Dashboard & Reports | Tổng quan KPI, báo cáo PDF |
| [12_cnc_mqtt.md](12_cnc_mqtt.md) | CNC Data & MQTT | Thu thập dữ liệu máy CNC real-time |
| [13_master_data.md](13_master_data.md) | Master Data | Nhà máy, máy móc, danh mục hệ thống |

---

## Quy ước viết tài liệu

Mỗi file tài liệu gồm các phần:

1. **Tổng quan** — Mục đích, người dùng liên quan
2. **Business Rules** — Các quy tắc nghiệp vụ cụ thể
3. **Workflow** — Luồng thao tác từng bước
4. **Data Model** — Các bảng và quan hệ liên quan
5. **API Endpoints** — Danh sách endpoint REST
6. **Edge Cases** — Các trường hợp đặc biệt cần xử lý

---

## Nguồn tham khảo

Tài liệu được xây dựng dựa trên phân tích hệ thống cũ:
- `ManageData` — WinForms app (văn phòng kỹ thuật)
- `Vinam-MES` — WinForms app (tại máy CNC)
- `pdmdata.sql` — MySQL schema gốc (~91 bảng, 429 stored procedures)
