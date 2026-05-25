# test-api

Chạy integration test toàn bộ Phase 1–3 qua REST API.
Yêu cầu: API đang chạy tại localhost:5066, DB có data từ DbSeeder.

## Steps

```powershell
$loginBody = '{"userLogin":"admin","password":"Admin@123"}'
$loginResp = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $loginBody
$token = $loginResp.data.token
if (-not $token) { Write-Host "LOGIN FAILED"; exit 1 }
$h = @{ Authorization = "Bearer $token" }
Write-Host "Login OK"

$pass = 0; $fail = 0
function Check($label, $actual, $expected) {
    if ($actual -eq $expected) { Write-Host "  ✅ $label ($actual)"; $script:pass++ }
    else { Write-Host "  ❌ $label — got $actual, want $expected"; $script:fail++ }
}

Write-Host "`n[ Phase 1: Auth & HR ]"
$roles = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/roles" -Headers $h
Check "Roles" $roles.data.Count 6
$depts = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/departments" -Headers $h
Check "Departments" $depts.data.Count 4
$ws = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/work-statuses" -Headers $h
Check "WorkStatuses" $ws.data.Count 3

Write-Host "`n[ Phase 2: Lookups ]"
$ot = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/op-types" -Headers $h
Check "OpTypes" $ot.data.Count 6
$dc = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/dimension-categories" -Headers $h
Check "DimCategories" $dc.data.Count 5
$nr = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/ncr-reasons" -Headers $h
Check "NcrReasons" $nr.data.Count 7
$ft = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/tech-documents/file-types" -Headers $h
Check "FileTypes" $ft.data.Count 8

Write-Host "`n[ Phase 2: Parts & Jobs ]"
$parts = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/parts" -Headers $h
if ($parts.data.Count -gt 0) {
    $p = $parts.data[0]
    Write-Host "  ✅ Part: $($p.partNumber) (ID=$($p.id))"; $pass++
    $prods = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/jobs/1/products" -Headers $h
    Write-Host "  ✅ Products for Job1: $($prods.data.Count)"; $pass++
    $ops = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/jobs/1/operations" -Headers $h
    Write-Host "  ✅ Operations for Job1: $($ops.data.Count)"; $pass++
} else { Write-Host "  ⚠️  No parts found — seed data missing?"; $fail++ }

Write-Host "`n[ Phase 3: FAI ]"
try {
    $fai = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/fai?jobId=1&partOpId=1" -Headers $h
    Write-Host "  ✅ FAI Sheet: $($fai.data.dimensions.Count) dims, $($fai.data.rows.Count) rows"; $pass++
} catch { Write-Host "  ❌ FAI: $_"; $fail++ }

Write-Host "`n[ Phase 3: NCR ]"
$ncrs = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/ncrs" -Headers $h
Write-Host "  ✅ NCRs: $($ncrs.data.Count)"; $pass++

Write-Host "`n[ TechDocuments ]"
$docs = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/tech-documents?partRevId=1" -Headers $h
Write-Host "  ✅ Part-level docs (partRevId=1): $($docs.data.Count)"; $pass++
$docs2 = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/tech-documents?partOpId=1" -Headers $h
Write-Host "  ✅ OP-level docs (partOpId=1): $($docs2.data.Count)"; $pass++

Write-Host "`n[ Phase 4: Desktop MES APIs ]"
# ProductionSession
try {
    $sessions = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/production-sessions?jobId=1" -Headers $h
    Write-Host "  ✅ ProductionSessions: $($sessions.data.Count)"; $pass++
} catch { Write-Host "  ❌ ProductionSessions: $_"; $fail++ }
# NCR list + filter by department
try {
    $ncrs2 = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/ncrs?page=1&pageSize=5" -Headers $h
    Write-Host "  ✅ NCRs (paged): $($ncrs2.data.Count)"; $pass++
} catch { Write-Host "  ❌ NCRs paged: $_"; $fail++ }
# Departments lookup (used by NCR dialog)
try {
    $depts2 = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/departments" -Headers $h
    Check "Departments for NCR" $depts2.data.Count 4
} catch { Write-Host "  ❌ Departments: $_"; $fail++ }
# NCR Reasons filtered by department
try {
    $nrResp = Invoke-RestMethod -Uri "http://localhost:5066/api/v1/ncr-reasons" -Headers $h
    Write-Host "  ✅ NcrReasons (all): $($nrResp.data.Count)"; $pass++
} catch { Write-Host "  ❌ NcrReasons: $_"; $fail++ }

Write-Host "`n========================================"
Write-Host "  PASS: $pass   FAIL: $fail"
Write-Host "========================================"
```

## Kết quả mong đợi
Tất cả ✅, FAIL = 0. Nếu có ❌ → đọc error message và fix trước khi tiếp tục.
