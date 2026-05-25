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

2. **API** — Start .NET API ở background, chờ "listening":
```powershell
Get-Process -Name "dotnet" | Stop-Process -Force -ErrorAction SilentlyContinue
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
$exe = "c:\Users\longn\source\repos\shopfloor-manager\src\ShopfloorManager.Desktop\bin\Debug\net9.0-windows\ShopfloorManager.Desktop.exe"
if (Test-Path $exe) {
    Start-Process -FilePath $exe
    Write-Host "Desktop app launched"
} else {
    Write-Host "EXE not found — build trước: dotnet build src/ShopfloorManager.Desktop/..."
}
```

## Kết quả mong đợi
- Docker: postgres, minio, mosquitto đều Up
- API: http://localhost:5066/swagger
- Web: http://localhost:3000
- Desktop: LoginWindow → Dashboard (chỉ khi test Phase 4)
- Login: admin / Admin@123
