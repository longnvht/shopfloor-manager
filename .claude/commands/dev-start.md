# dev-start

Start toàn bộ development environment: Docker containers, API, và Web client.

## Steps

1. **Docker** — Start PostgreSQL, MinIO, Mosquitto:
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager"
docker compose -f docker-compose.dev.yml up -d
Start-Sleep -Seconds 4
docker ps --format "{{.Names}} {{.Status}}"
```

2. **API** — Kiểm tra nếu API đang chạy; nếu không thì start:
```powershell
$apiUp = $false
try { $r = Invoke-WebRequest -Uri "http://localhost:5066/swagger" -UseBasicParsing -TimeoutSec 3; $apiUp = $true } catch {}
if ($apiUp) {
    Write-Host "API already running: http://localhost:5066"
} else {
    # Chỉ kill dotnet process của ShopfloorManager.API (không kill Desktop hay process khác)
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.MainWindowTitle -eq "" -and $_.CPU -lt 5
    } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Remove-Item "C:\Temp\api_out.txt","C:\Temp\api_err.txt" -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path "C:\Temp" -Force | Out-Null
    Set-Location "c:\Users\longn\source\repos\shopfloor-manager\src"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    Start-Process -FilePath "dotnet" -ArgumentList "run --project ShopfloorManager.API" -RedirectStandardOutput "C:\Temp\api_out.txt" -RedirectStandardError "C:\Temp\api_err.txt" -WindowStyle Hidden
    # Chờ API ready
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 2
        $content = Get-Content "C:\Temp\api_out.txt" -ErrorAction SilentlyContinue
        if ($content | Select-String "listening") { $ready = $true; break }
    }
    if ($ready) { Write-Host "API ready: http://localhost:5066" } else { Write-Host "API timeout — check C:\Temp\api_out.txt" }
}
```

3. **Web** — Start Next.js:
```powershell
Get-Process -Name "node" | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Set-Location "c:\Users\longn\source\repos\shopfloor-manager\clients\web"
Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm run dev > C:\Temp\web_out.txt 2>&1" -WindowStyle Hidden
Start-Sleep -Seconds 12
$web = Get-Content "C:\Temp\web_out.txt" -ErrorAction SilentlyContinue | Select-String "Ready|localhost:3000"
if ($web) { Write-Host "Web ready: http://localhost:3000" } else { Write-Host "Web starting... check C:\Temp\web_out.txt" }
```

4. **Desktop WPF** (tùy chọn — chỉ khi test Phase 4):
```powershell
# Đảm bảo API đang chạy trước khi launch Desktop
$apiUp = $false
try { $r = Invoke-WebRequest -Uri "http://localhost:5066/swagger" -UseBasicParsing -TimeoutSec 3; $apiUp = $true } catch {}
if (-not $apiUp) {
    Write-Host "API chưa chạy — chạy bước 2 trước"
} else {
    $exe = "c:\Users\longn\source\repos\shopfloor-manager\src\ShopfloorManager.Desktop\bin\Debug\net9.0-windows\ShopfloorManager.Desktop.exe"
    if (Test-Path $exe) {
        Start-Process -FilePath $exe
        Write-Host "Desktop app launched"
    } else {
        Write-Host "EXE not found — build trước: dotnet build src/ShopfloorManager.Desktop/..."
    }
}
```

## Kết quả mong đợi
- Docker: postgres, minio, mosquitto đều Up
- API: http://localhost:5066/swagger
- Web: http://localhost:3000
- Desktop: LoginWindow → Dashboard (chỉ khi test Phase 4)
- Login: admin / Admin@123

## Lưu ý
- Khi build lại Desktop app, chỉ kill `ShopfloorManager.Desktop` process, **không** kill toàn bộ dotnet (sẽ tắt API)
- API là dotnet process riêng biệt, chạy độc lập với Desktop app
