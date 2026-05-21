# migrate

Tạo EF Core migration mới và apply vào database dev.

## Usage
```
/migrate AddFeatureXyz
```
Nếu không truyền tên → chỉ apply migration hiện có (database update).

## Steps

### Tạo migration mới (nếu có tên)
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager\src"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add "$ARGUMENTS" `
  --project ShopfloorManager.Infrastructure `
  --startup-project ShopfloorManager.API
```

Kiểm tra output — nếu có `data loss warning` hãy review file migration trước khi apply.

### Apply vào DB
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager\src"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update `
  --project ShopfloorManager.Infrastructure `
  --startup-project ShopfloorManager.API
```

### Verify
```powershell
docker exec shopfloor-manager-dev-postgres-1 psql -U shopfloor -d shopfloor_dev -c 'SELECT migration_id FROM "__EFMigrationsHistory" ORDER BY migration_id;'
```

## Lưu ý
- Luôn đọc file migration được tạo ra trước khi apply nếu có cảnh báo data loss
- Sau khi apply, restart API để tránh stale model cache
- Nếu migration có lỗi: `dotnet ef migrations remove` để xoá và thử lại
