# 🔄 Migration Guide: Supabase → PostgreSQL Local

## Chuyển từ Supabase sang PostgreSQL local

### Bước 1: Cài PostgreSQL + pgAdmin

Xem chi tiết tại: [POSTGRESQL_SETUP.md](POSTGRESQL_SETUP.md)

**TL;DR**:
1. Download PostgreSQL 16 từ https://www.postgresql.org/download/windows/
2. Install với password `admin123` cho user `postgres`
3. Mở pgAdmin 4
4. Tạo database: `heristepai_db`

---

### Bước 2: Update Connection String

#### API Project

1. Copy `.env.example` → `.env`:
   ```bash
   cd src/HeriStepAI.API
   copy .env.example .env
   ```

2. Edit `.env`:
   ```bash
   # Local PostgreSQL
   SUPABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=heristepai_db;Username=postgres;Password=admin123;SSL Mode=Prefer
   ```

#### Web Project

1. Copy `.env.example` → `.env`:
   ```bash
   cd src/HeriStepAI.Web
   copy .env.example .env
   ```

2. Edit `.env`:
   ```bash
   # Local PostgreSQL
   SUPABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=heristepai_db;Username=postgres;Password=admin123;SSL Mode=Prefer

   # Local API
   API_BASE_URL=http://localhost:5000/api/
   ```

---

### Bước 3: Migrate Database Schema

**Option A: Tự động tạo tables qua EF Core**

Chỉ cần chạy API, EF Core sẽ tự tạo tables:

```bash
cd src/HeriStepAI.API
dotnet run
```

API sẽ:
1. Tạo tất cả tables từ models
2. Seed initial data (Admin user, sample POIs)
3. Ready to use!

**Option B: Dùng EF Migrations (Recommended cho production)**

```bash
cd src/HeriStepAI.API

# Tạo migration mới (nếu chưa có)
dotnet ef migrations add InitialCreate

# Apply migration vào database
dotnet ef database update
```

**Option C: Import từ Supabase (nếu có data cũ)**

1. Export data từ Supabase:
   - Vào Supabase Dashboard → SQL Editor
   - Chạy:
     ```sql
     -- Export all data
     COPY (SELECT * FROM "POI") TO STDOUT WITH CSV HEADER;
     ```
   - Copy kết quả

2. Import vào PostgreSQL local:
   - Mở pgAdmin → heristepai_db → Query Tool
   - Paste SQL và run

---

### Bước 4: Test kết nối

**Test API**:
```bash
cd src/HeriStepAI.API
dotnet run
```

Mở browser: http://localhost:5000/swagger

**Test Web**:
```bash
cd src/HeriStepAI.Web
dotnet run
```

Mở browser: http://localhost:5001

---

## 🔄 Workflow cho 2 người

### Setup lần đầu

**Member 1** (bạn):
1. Cài PostgreSQL local
2. Chạy API → EF tạo tables
3. Export schema:
   ```bash
   pg_dump -U postgres -d heristepai_db --schema-only -f database_schema.sql
   ```
4. Commit `database_schema.sql` lên Git
5. Update `.gitignore`:
   ```
   .env
   .env.local
   ```

**Member 2** (teammate):
1. Pull code từ Git
2. Cài PostgreSQL local
3. Tạo database `heristepai_db`
4. Import schema:
   ```bash
   psql -U postgres -d heristepai_db -f database_schema.sql
   ```
5. Copy `.env.example` → `.env`
6. Chạy API & Web

---

### Khi có thay đổi Database Schema

**Member 1** thay đổi model:

```bash
# Tạo migration
cd src/HeriStepAI.API
dotnet ef migrations add AddNewFeature

# Apply vào local DB
dotnet ef database update

# Commit migration files
git add Migrations/
git commit -m "Add migration: AddNewFeature"
git push
```

**Member 2** pull và update:

```bash
git pull

# Apply migration
cd src/HeriStepAI.API
dotnet ef database update
```

---

## 🌐 Shared Database cho Testing (Optional)

### Dùng Neon.tech FREE

1. Tạo account: https://neon.tech
2. Create project: `heristepai-staging`
3. Copy connection string
4. Tạo file `.env.staging`:
   ```bash
   SUPABASE_CONNECTION_STRING=Host=ep-xxx.region.aws.neon.tech;Port=5432;Database=heristepai;Username=neondb_owner;Password=xxx;SSL Mode=Require
   ```
5. Share file này với teammate (qua Discord/Slack, KHÔNG commit lên Git)

**Chạy với staging DB**:
```bash
# Set environment variable trước khi chạy
$env:ASPNETCORE_ENVIRONMENT="Staging"
dotnet run
```

Hoặc tạo `appsettings.Staging.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-xxx.region.aws.neon.tech;Port=5432;..."
  }
}
```

---

## 🔍 Troubleshooting

### Lỗi: "password authentication failed"

```bash
# Reset password trong pgAdmin:
# Right-click server → Properties → Connection → Password
# Hoặc dùng SQL:
ALTER USER postgres WITH PASSWORD 'admin123';
```

### Lỗi: "database does not exist"

```bash
# Tạo database qua pgAdmin hoặc SQL:
CREATE DATABASE heristepai_db;
```

### Lỗi: "relation does not exist"

```bash
# Tables chưa được tạo, chạy:
cd src/HeriStepAI.API
dotnet ef database update
```

### Lỗi: "max connections reached"

Đã fix trong [API Program.cs](src/HeriStepAI.API/Program.cs) với connection pooling. Nếu vẫn lỗi:

```sql
-- Check số connections hiện tại
SELECT count(*) FROM pg_stat_activity;

-- Kill idle connections
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle' AND state_change < current_timestamp - INTERVAL '5 minutes';
```

---

## 📊 So sánh Supabase vs Local PostgreSQL

| Feature | Supabase | PostgreSQL Local |
|---------|----------|------------------|
| **Setup** | Sign up online | Cài trên máy |
| **Free tier** | 500MB, 2GB bandwidth | Unlimited |
| **Latency** | 100-300ms (cloud) | <10ms (local) |
| **Collaboration** | Shared by default | Cần setup remote/cloud |
| **pgAdmin** | ✅ Compatible | ✅ Native |
| **Migrations** | Manual SQL | EF Core migrations |
| **Backup** | Auto daily | Manual (pg_dump) |
| **Best for** | Production, Staging | Development |

**Khuyến nghị**:
- Development: **PostgreSQL Local**
- Staging: **Neon.tech** (free, shared)
- Production: **Supabase** hoặc **Render PostgreSQL**

---

## ✅ Checklist Migration

- [ ] Cài PostgreSQL 16 + pgAdmin
- [ ] Tạo database `heristepai_db`
- [ ] Copy `.env.example` → `.env` cho API và Web
- [ ] Update connection string trong `.env`
- [ ] Chạy API → Verify tables được tạo
- [ ] Chạy Web → Login thành công
- [ ] Test Mobile → API connection OK
- [ ] Export schema và commit lên Git
- [ ] Share setup guide với teammate
- [ ] (Optional) Setup Neon.tech cho shared testing

---

**Done!** 🎉 Bây giờ bạn đã chuyển sang PostgreSQL local và có thể làm việc độc lập, nhanh hơn, và dễ debug hơn!
