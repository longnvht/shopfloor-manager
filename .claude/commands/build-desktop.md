# build-desktop

Kill Desktop app (nếu đang chạy), build lại, rồi launch.
Dùng khi đã có API running và chỉ cần rebuild Desktop sau khi sửa code.

## Steps

### 1. Kill process cũ
```powershell
Get-Process -Name "ShopfloorManager.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Write-Host "Desktop stopped (or was not running)"
```

### 2. Build
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager\src"
dotnet build ShopfloorManager.Desktop/ShopfloorManager.Desktop.csproj 2>&1 | Select-String -Pattern "error|warning|Build succeeded|FAILED" | Select-Object -Last 20
```

Nếu có lỗi compile → dừng, fix trước khi tiếp tục. KHÔNG launch nếu build fail.

### 3. Kiểm tra API còn running
```powershell
$apiUp = $false
try { Invoke-WebRequest -Uri "http://localhost:5066/swagger" -UseBasicParsing -TimeoutSec 3 | Out-Null; $apiUp = $true } catch {}
if (-not $apiUp) { Write-Host "⚠️  API chưa chạy — chạy /dev-start trước"; exit 1 }
Write-Host "API OK"
```

### 4. Launch
```powershell
$exe = "c:\Users\longn\source\repos\shopfloor-manager\src\ShopfloorManager.Desktop\bin\Debug\net9.0-windows\ShopfloorManager.Desktop.exe"
if (Test-Path $exe) {
    Start-Process -FilePath $exe
    Write-Host "Desktop launched — login: admin / Admin@123"
} else {
    Write-Host "EXE not found — build có thể đã fail"
}
```

## Lưu ý
- Chỉ kill `ShopfloorManager.Desktop`, KHÔNG kill `dotnet` (sẽ tắt API)
- Nếu API chưa chạy → dùng `/dev-start` trước
- Build target là `Debug` — không cần `--configuration Release` khi test thủ công
