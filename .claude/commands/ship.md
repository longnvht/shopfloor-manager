# ship

Build, kiểm tra, rồi commit các thay đổi hiện tại theo convention của project.

## Usage
```
/ship "mô tả ngắn về thay đổi"
```

## Steps

### 1. Build kiểm tra lỗi compile
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager\src"
dotnet build ShopfloorManager.sln --no-restore 2>&1 | Select-String " error " | Select-Object -First 10
```
Nếu có lỗi → dừng lại, fix trước khi commit.

### 2. Xem thay đổi
```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager"
git diff --stat HEAD
git status --short
```

### 3. Commit
Dùng conventional commits: `feat`, `fix`, `refactor`, `docs`, `chore`.
Scope gợi ý: `domain`, `infra`, `api`, `web`, `phase1`, `phase2`, `phase3`.

```powershell
Set-Location "c:\Users\longn\source\repos\shopfloor-manager"
git add -A
git commit -m @"
<type>(<scope>): $ARGUMENTS

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
"@
```

## Ví dụ commit messages từ project
```
feat(phase2+3): add TechDocument part-level ownership
fix(infra): handle empty-string MinIO credentials
refactor(tech-documents): align ownership với business logic
docs(claude.md): cập nhật TechDocument ownership model
```

## Lưu ý
- KHÔNG commit `.env` hay `appsettings.Development.json`
- KHÔNG dùng `--no-verify`
- Nếu có pending migration chưa apply → mention trong commit message
