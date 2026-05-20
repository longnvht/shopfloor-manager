# Shopfloor Manager

Phần mềm quản lý nhà máy gia công cơ khí — mã nguồn mở, self-hosted, xây dựng bằng .NET 9 + Next.js + PostgreSQL.

Được phát triển để thay thế hệ thống WinForms nội bộ (ManageData + Vinam-MES) tại các nhà máy gia công cơ khí quy mô 50–200 người.

---

## Tính năng

- **Quản lý sản xuất** — Job, Part, Operation, Product serial, routing card
- **Đo kiểm FAI** — Nhập kích thước đo tại máy CNC, so sánh với dung sai, báo cáo FAI (PDF)
- **NCR / CPAR / Rework** — Quản lý sự kiện không phù hợp từ xưởng đến văn phòng
- **Tài liệu kỹ thuật** — Upload/duyệt drawing, G-code, route card, fixture drawing (lưu MinIO)
- **Quản lý dụng cụ đo** — Mượn/trả, lịch hiệu chuẩn, nhắc nhở hết hạn
- **Kế hoạch sản xuất** — Gantt chart theo máy, ca làm việc
- **Giám sát máy CNC** — Thu thập dữ liệu real-time qua MQTT (FANUC FOCAS / MTConnect)
- **Dashboard** — KPI theo role, biểu đồ SPC (Cpk/Cp), real-time qua SignalR

---

## Kiến trúc hệ thống

```
┌─────────────────────────────────────────┐
│  Web App (Next.js PWA)                  │
│  Văn phòng kỹ thuật / Quản lý          │
└──────────────────┬──────────────────────┘
                   │ REST API + SignalR
┌──────────────────▼──────────────────────┐
│  ASP.NET Core Web API (.NET 9)          │
│  Business logic · Auth · File proxy     │
└────┬─────────────┬─────────────┬────────┘
     │             │             │
PostgreSQL       MinIO       SignalR Hub
                               │
┌──────────────────▼──────────────────────┐
│  Desktop App (WPF — Windows)            │
│  Tại máy CNC — màn hình cảm ứng        │
│  FAI đo kiểm · NCR · Xem G-code        │
│  Offline cache (SQLite) + sync          │
└──────────────────┬──────────────────────┘
                   │ MQTT
          Mosquitto Broker
     (CNC data: FANUC / MTConnect)
```

---

## Tech Stack

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core Web API (.NET 9) + MediatR + EF Core 9 |
| Database | PostgreSQL 16 |
| File storage | MinIO (S3-compatible) |
| Real-time | SignalR |
| CNC data | MQTTnet + Mosquitto |
| PDF reports | QuestPDF |
| Excel | ClosedXML |
| SPC / Math | MathNet.Numerics |
| Email | MailKit |
| Web client | Next.js 15 + TypeScript + shadcn/ui + Tailwind CSS v4 |
| Desktop client | WPF (.NET 9) + SQLite (offline) |
| Container | Docker + Docker Compose + Nginx |

Tất cả thư viện đều MIT/Apache 2.0 — không dependency thương mại.

---

## Bắt đầu nhanh (Development)

**Yêu cầu:** Docker Desktop, .NET 9 SDK, Node.js 20+

```bash
# 1. Clone repo
git clone https://github.com/longnvht/shopfloor-manager.git
cd shopfloor-manager

# 2. Khởi động infrastructure (PostgreSQL, MinIO, Mosquitto)
docker compose -f docker-compose.dev.yml up -d

# 3. Chạy API
cd src
dotnet run --project ShopfloorManager.API
# → http://localhost:5066
```

Dev credentials (không cần `.env`):
| Service | URL | Thông tin |
|---|---|---|
| PostgreSQL | `localhost:5432` | `shopfloor` / `dev_password` / `shopfloor_dev` |
| MinIO Console | `http://localhost:9001` | `minioadmin` / `minioadmin123` |
| MQTT | `localhost:1883` | Không auth |

---

## Production Deployment

```bash
# 1. Tạo file .env từ template
cp .env.example .env
# Chỉnh sửa .env với password thực

# 2. Chạy toàn bộ stack
docker compose up -d
```

Web app chạy tại `http://your-server` (Nginx reverse proxy).

---

## Cấu trúc dự án

```
shopfloor-manager/
├── src/                          # .NET 9 Solution (Clean Architecture)
│   ├── ShopfloorManager.API/     # Controllers, middleware, Program.cs
│   ├── ShopfloorManager.Application/  # Use cases, MediatR handlers
│   ├── ShopfloorManager.Domain/  # Entities, enums
│   ├── ShopfloorManager.Infrastructure/  # EF Core, MinIO, MQTT
│   └── ShopfloorManager.Shared/  # DTOs, constants, pagination
├── clients/
│   ├── web/                      # Next.js web app (chưa scaffold)
│   └── desktop/                  # WPF desktop app (chưa scaffold)
├── docker/
│   ├── postgres/init.sql         # Full database schema (40 bảng)
│   ├── nginx/nginx.conf
│   └── mosquitto/
├── docker-compose.yml            # Production
└── docker-compose.dev.yml        # Development (infrastructure only)
```

---

## Trạng thái dự án

| Phase | Nội dung | Trạng thái |
|---|---|---|
| Phase 0 | Foundation: DB schema, .NET scaffold, Docker | ✅ Hoàn tất |
| Phase 1 | Auth & HR: JWT, users, roles, SignalR | 🔄 Đang làm |
| Phase 2 | Production Core: Jobs, Parts, OPs, Documents | ⏳ |
| Phase 3 | Quality: Dimensions, FAI, NCR, SPC | ⏳ |
| Phase 4 | Desktop MES: WPF, offline, FAI tại máy | ⏳ |
| Phase 5 | Advanced: Gage, Planning, MQTT, Dashboard | ⏳ |

---

## License

MIT License — xem [LICENSE](LICENSE) để biết thêm chi tiết.
