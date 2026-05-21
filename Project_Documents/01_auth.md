# Auth & Permissions

## 1. Tổng quan

Module xử lý xác thực người dùng và kiểm soát quyền truy cập toàn hệ thống.

**Người dùng liên quan:** Tất cả users — Admin, Manager, Engineer, QC, Operator, Planner.

---

## 2. Business Rules

### 2.1 Đăng nhập
- Mật khẩu được hash bằng **bcrypt** (không dùng MD5 như hệ thống cũ).
- JWT token có thời hạn **8 giờ** (1 ca làm việc), cấu hình qua `JWT_EXPIRY_HOURS`.
- Refresh token để gia hạn mà không cần đăng nhập lại.
- Ghi `audit_log` mỗi lần login/logout với: UserID, MachineID (nếu từ Desktop MES), IP, timestamp.

### 2.2 First Login
- Tài khoản mới có `first_login = true`.
- Khi `first_login = true`, hệ thống **bắt buộc** chuyển đến màn hình đổi mật khẩu trước khi vào app.
- Sau khi đổi thành công, set `first_login = false`.

### 2.3 Quên mật khẩu
- User nhập email → hệ thống tạo `reset_code` (6 ký tự, ngẫu nhiên) → gửi qua MailKit SMTP.
- `reset_code` có hiệu lực **15 phút** (lưu timestamp kèm theo).
- User nhập code + mật khẩu mới → xóa `reset_code`, hash mật khẩu mới.

### 2.4 Phân quyền — PDM (Web App)
Hệ thống phân quyền **3 cấp**:

```
Role
 └── Menu (cấp 1 — thanh menu chính)
      └── SubMenu (cấp 2 — menu con)
           └── EndMenu (cấp 3 — action cụ thể: Add, Edit, Delete, Approve...)
```

- Mỗi user có `role_id` → role có danh sách menu được phép.
- API kiểm tra quyền theo `[Authorize(Policy = "CanCreate")]` hoặc middleware tùy chỉnh.
- **Không cache** permission trong token — kiểm tra realtime từ DB để revoke ngay lập tức.

### 2.5 Phân quyền — MES (Desktop App)
- User có `mes_role_id` riêng biệt với PDM role.
- `mes_menus` định nghĩa menu nào hiện trên màn hình cảm ứng tại máy CNC.
- `mes_menu_op_types` xác định menu nào áp dụng cho OP type nào.
  - Ví dụ: Menu "FAI" chỉ hiện khi OP thuộc type "Inspection" hoặc "CNC Turning".

### 2.6 User Types & Quyền đặc biệt
Hai quyền quan trọng nằm trực tiếp trên `user_types`:

| Quyền | Cột | Ý nghĩa |
|---|---|---|
| `can_enter_value` | Boolean | Được nhập kết quả đo kiểm (measure value) |
| `can_raise_ncr` | Boolean | Được tạo NCR khi có dimension fail |

- Inspector/QC thường có cả hai.
- Operator có thể có `can_enter_value` nhưng không có `can_raise_ncr`.
- Engineer không có cả hai (chỉ xem).

---

## 3. Workflow

### Đăng nhập
```
User nhập user_login + password
  → Hash password bằng bcrypt, verify với password_hash trong DB
  → Nếu sai: trả về 401, ghi thất bại (không ghi audit_log)
  → Nếu đúng:
      → Kiểm tra is_active = true (tài khoản không bị khóa)
      → Kiểm tra first_login:
          true  → trả về token tạm + flag yêu cầu đổi mật khẩu
          false → tạo JWT + Refresh Token
      → Ghi audit_log (login_at, machine_id, ip)
      → Trả về: { token, refreshToken, user: { id, name, role, userType } }
```

### Refresh Token
```
Client gửi refreshToken (hết hạn JWT)
  → Verify refreshToken còn hiệu lực
  → Tạo JWT mới
  → Không tạo refreshToken mới (giữ nguyên)
```

### Logout
```
Client gọi POST /auth/logout
  → Cập nhật audit_log.logged_out_at = NOW()
  → Invalidate refreshToken (xóa khỏi DB)
```

---

## 4. Data Model

```sql
users (
    id, user_login, password_hash, name, email,
    user_type_id → user_types,
    role_id      → roles,         -- PDM permission
    mes_role_id  → mes_role_menus, -- MES permission
    first_login, reset_code, is_active
)

roles (id, name)
menus (id, code, name, parent_id, level, sort_order)  -- self-referential tree
role_menus (role_id, menu_id)

mes_menus (id, code, description, menu_type, is_active)
mes_role_menus (id, role_name, menu_ids[])
mes_menu_op_types (mes_menu_id, op_type_id)

audit_logs (id, user_id, machine_id, ip_address, logged_in_at, logged_out_at)
```

---

## 5. API Endpoints

```
POST   /api/v1/auth/login
       Body: { userLogin, password }
       Response: { token, refreshToken, user, requirePasswordChange }

POST   /api/v1/auth/refresh
       Body: { refreshToken }
       Response: { token }

POST   /api/v1/auth/logout
       Auth: Bearer token
       Response: 204 No Content

POST   /api/v1/auth/forgot-password
       Body: { email }
       Response: 204 (không tiết lộ email có tồn tại hay không)

POST   /api/v1/auth/reset-password
       Body: { email, resetCode, newPassword }
       Response: 204

POST   /api/v1/auth/change-password
       Auth: Bearer token
       Body: { currentPassword, newPassword }
       Response: 204

GET    /api/v1/auth/me
       Auth: Bearer token
       Response: { id, name, email, role, permissions[] }

GET    /api/v1/auth/permissions
       Auth: Bearer token
       Response: { menus: [{ code, children[] }] }
```

---

## 6. Edge Cases

- **Tài khoản bị khóa** (`is_active = false`): trả về 401 với message "Tài khoản đã bị khóa. Liên hệ Admin."
- **Đăng nhập từ Desktop MES**: phải truyền `machineId` trong request body để ghi vào `audit_log.machine_id`.
- **Reset code hết hạn**: trả về lỗi cụ thể để user biết cần gửi lại yêu cầu.
- **Concurrent sessions**: cho phép đăng nhập nhiều thiết bị cùng lúc (không single-session).
- **Menu permission thay đổi**: phản ánh ngay ở lần gọi `/auth/permissions` tiếp theo, không cần logout.
