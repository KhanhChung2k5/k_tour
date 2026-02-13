# 🚀 Quick Start: PostgreSQL cho HeriStepAI

## ⚡ TL;DR - Setup trong 5 phút

### 1. Cài PostgreSQL
```powershell
# Download: https://www.postgresql.org/download/windows/
# Install với password: admin123
```

### 2. Chạy setup script
```powershell
# Tự động tạo database và .env files
.\scripts\setup-local-db.ps1
```

### 3. Run API & Web
```powershell
# Terminal 1: API
cd src\HeriStepAI.API
dotnet run

# Terminal 2: Web
cd src\HeriStepAI.Web
dotnet run
```

### 4. Done! 🎉
- API: http://localhost:5000/swagger
- Web: http://localhost:5001
- pgAdmin: Start Menu → pgAdmin 4

---

## 📚 Chi tiết hơn

### Cài đặt PostgreSQL

1. **Download**: https://www.postgresql.org/download/windows/
2. **Install Settings**:
   - Port: `5432`
   - Password: `admin123` (hoặc password của bạn)
   - Locale: Default
   - ✅ Tick pgAdmin 4

### Setup Database

**Option A: Dùng script (Recommended)**
```powershell
.\scripts\setup-local-db.ps1
```

**Option B: Manual**
```powershell
# 1. Mở pgAdmin 4
# 2. Right-click Databases → Create → Database
# 3. Name: heristepai_db
# 4. Save

# 5. Tạo .env files
cd src\HeriStepAI.API
copy .env.example .env

cd ..\HeriStepAI.Web
copy .env.example .env
```

### Chạy ứng dụng

```powershell
# API (Terminal 1)
cd src\HeriStepAI.API
dotnet run
# → http://localhost:5000/swagger

# Web (Terminal 2)
cd src\HeriStepAI.Web
dotnet run
# → http://localhost:5001

# API sẽ tự động:
# - Tạo tables từ models
# - Seed admin user (admin@heristepai.com / Admin@123)
# - Seed sample POIs
```

---

## 👥 Làm việc với Teammate

### Member 1 (bạn) - Setup lần đầu

```powershell
# 1. Setup database
.\scripts\setup-local-db.ps1

# 2. Chạy API để tạo tables
cd src\HeriStepAI.API
dotnet run

# 3. Export schema để share
.\scripts\export-schema.ps1

# 4. Commit lên Git
git add database_schema.sql
git commit -m "Add database schema"
git push
```

### Member 2 (teammate) - Pull và setup

```powershell
# 1. Pull code
git pull

# 2. Setup database
.\scripts\setup-local-db.ps1

# 3. Import schema
.\scripts\import-schema.ps1

# 4. Chạy API
cd src\HeriStepAI.API
dotnet run
```

---

## 🔄 Workflow hàng ngày

### Khi có thay đổi Database Schema

**Người tạo migration**:
```powershell
cd src\HeriStepAI.API

# Tạo migration
dotnet ef migrations add AddNewFeature

# Apply migration
dotnet ef database update

# Export schema mới
cd ..\..
.\scripts\export-schema.ps1

# Commit
git add Migrations/ database_schema.sql
git commit -m "Add migration: AddNewFeature"
git push
```

**Người kia pull và update**:
```powershell
git pull

cd src\HeriStepAI.API
dotnet ef database update
```

---

## 🌐 Shared Database (Optional)

### Dùng Neon.tech FREE để cùng test

1. **Tạo account**: https://neon.tech
2. **Create project**: `heristepai-staging`
3. **Copy connection string**:
   ```
   Host=ep-xxx.region.aws.neon.tech;Port=5432;Database=heristepai;Username=neondb_owner;Password=xxx;SSL Mode=Require
   ```

4. **Tạo file `.env.staging`**:
   ```bash
   SUPABASE_CONNECTION_STRING=Host=ep-xxx...
   ```

5. **Share với teammate** (qua Discord/Slack, KHÔNG commit)

6. **Connect pgAdmin**:
   - Add New Server → Neon - HeriStepAI
   - Host: `ep-xxx.region.aws.neon.tech`
   - Username/Password: từ connection string
   - SSL Mode: Require

---

## 🛠️ Common Commands

### Database Management

```powershell
# Export schema (share với team)
.\scripts\export-schema.ps1

# Import schema
.\scripts\import-schema.ps1

# Backup full database (schema + data)
pg_dump -U postgres -d heristepai_db -f backup.sql

# Restore
psql -U postgres -d heristepai_db -f backup.sql
```

### EF Core Migrations

```powershell
cd src\HeriStepAI.API

# Tạo migration mới
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback 1 migration
dotnet ef database update PreviousMigrationName

# List migrations
dotnet ef migrations list
```

### pgAdmin Quick Actions

- **View data**: Tables → Right-click → View/Edit Data → All Rows
- **Run SQL**: Tools → Query Tool
- **Export CSV**: Right-click table → Export

---

## 📊 Connection Strings

### Local Development
```bash
Host=localhost;Port=5432;Database=heristepai_db;Username=postgres;Password=admin123;SSL Mode=Prefer
```

### Neon.tech (Staging)
```bash
Host=ep-xxx.region.aws.neon.tech;Port=5432;Database=heristepai;Username=neondb_owner;Password=xxx;SSL Mode=Require
```

### Supabase (Production - optional)
```bash
postgresql://postgres.xxx:password@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres?sslmode=require
```

---

## 🔍 Troubleshooting

### "password authentication failed"
```sql
-- Reset password
ALTER USER postgres WITH PASSWORD 'admin123';
```

### "database does not exist"
```sql
CREATE DATABASE heristepai_db;
```

### "relation does not exist"
```powershell
# Tables chưa được tạo
cd src\HeriStepAI.API
dotnet ef database update
```

### Port 5432 already in use
```powershell
# Check process using port
netstat -ano | findstr :5432

# Kill process (replace PID)
taskkill /PID <PID> /F
```

---

## 📚 Tài liệu đầy đủ

- [POSTGRESQL_SETUP.md](POSTGRESQL_SETUP.md) - Setup chi tiết + pgAdmin
- [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - Migration từ Supabase
- [MOBILE_APK_FIXES.md](MOBILE_APK_FIXES.md) - Mobile app fixes

---

## ✅ Checklist

- [ ] PostgreSQL 16 installed
- [ ] pgAdmin 4 installed
- [ ] Database `heristepai_db` created
- [ ] `.env` files created (API & Web)
- [ ] API runs successfully (localhost:5000)
- [ ] Web runs successfully (localhost:5001)
- [ ] Login works (admin@heristepai.com / Admin@123)
- [ ] pgAdmin can connect to database
- [ ] (Optional) Neon.tech staging setup
- [ ] (Optional) Teammate can connect

---

**Cần trợ giúp?** Check [POSTGRESQL_SETUP.md](POSTGRESQL_SETUP.md) hoặc issues trên GitHub!
