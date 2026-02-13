# 🐘 PostgreSQL + pgAdmin Setup Guide

## Cài đặt PostgreSQL trên máy local

### 1. Download và Install

1. Download PostgreSQL 16.x từ: https://www.postgresql.org/download/windows/
2. Chạy installer với các settings:
   - **Port**: `5432` (default)
   - **Password** cho user `postgres`: `admin123` (hoặc password của bạn)
   - **Locale**: Default
   - ✅ **Tick chọn pgAdmin 4**

### 2. Tạo Database trong pgAdmin

1. Mở **pgAdmin 4** từ Start Menu
2. Kết nối vào server `PostgreSQL 16` (nhập password `admin123`)
3. Right-click **Databases** → **Create** → **Database**
4. Nhập tên database: `heristepai_db`
5. Click **Save**

### 3. Tạo User cho application (optional - recommended)

```sql
-- Open Query Tool (Right-click database → Query Tool)

-- Create user
CREATE USER heristep_user WITH PASSWORD 'heristep2024';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE heristepai_db TO heristep_user;

-- Connect to heristepai_db first, then:
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO heristep_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO heristep_user;
GRANT ALL PRIVILEGES ON SCHEMA public TO heristep_user;
```

### 4. Connection String cho local

**Development (localhost)**:
```
Host=localhost;Port=5432;Database=heristepai_db;Username=heristep_user;Password=heristep2024;SSL Mode=Prefer
```

Hoặc dùng user `postgres` (superuser):
```
Host=localhost;Port=5432;Database=heristepai_db;Username=postgres;Password=admin123;SSL Mode=Prefer
```

---

## 🌐 Setup cho 2 người cùng làm việc

### Option 1: Mỗi người có Local DB riêng (Recommended for Development)

**Ưu điểm**:
- Độc lập, không conflict
- Test thoải mái không sợ ảnh hưởng người khác
- Nhanh hơn (local)

**Cách làm**:
1. Mỗi người cài PostgreSQL + pgAdmin trên máy riêng
2. Share **migration scripts** hoặc **SQL schema file** qua Git
3. Khi có thay đổi schema → Export SQL → Commit lên Git
4. Người kia pull và chạy SQL script

**Export schema**:
```sql
-- Right-click database → Backup
-- Format: Plain (SQL)
-- Chỉ tick "Schema only" (không cần data)
-- Save file: schema.sql
```

**Import schema**:
```bash
# Trong pgAdmin Query Tool hoặc command line
psql -U postgres -d heristepai_db -f schema.sql
```

---

### Option 2: Shared PostgreSQL trên Cloud (FREE)

**Ưu điểm**:
- Cùng xem data real-time
- Phù hợp cho staging/testing
- Miễn phí (có giới hạn)

#### Khuyến nghị: Neon.tech (FREE tier)

**Neon.tech** - Serverless PostgreSQL miễn phí:
- 0.5 GB storage
- Unlimited databases
- Auto-suspend khi không dùng
- pgAdmin compatible

**Cách setup Neon.tech**:

1. Truy cập https://neon.tech
2. Sign up (Google/GitHub login)
3. Create new project: `heristepai`
4. Copy connection string:
   ```
   postgres://username:password@ep-xxx.region.aws.neon.tech/heristepai?sslmode=require
   ```

5. Mở pgAdmin → Add New Server:
   - **Name**: Neon - HeriStepAI
   - **Host**: `ep-xxx.region.aws.neon.tech`
   - **Port**: `5432`
   - **Database**: `heristepai`
   - **Username**: từ connection string
   - **Password**: từ connection string
   - **SSL Mode**: Require

6. Share connection string với member (qua file .env riêng, KHÔNG commit lên Git)

---

### Option 3: Remote Access vào PostgreSQL của 1 người (NOT Recommended)

**Chỉ dùng nếu**:
- Không muốn dùng cloud
- Có 1 máy chạy 24/7

**Cách config**:

1. **Máy host** (máy chạy PostgreSQL):

   Edit `postgresql.conf`:
   ```
   # Thường ở: C:\Program Files\PostgreSQL\16\data\postgresql.conf
   listen_addresses = '*'
   ```

   Edit `pg_hba.conf`:
   ```
   # Thêm dòng này (cho phép IP của member)
   host    all    all    0.0.0.0/0    md5
   ```

   Restart PostgreSQL service

2. **Firewall**:
   - Mở port `5432` trong Windows Firewall
   - Nếu có router, forward port `5432` → IP máy host

3. **Member** connect qua pgAdmin:
   - Host: `<Public IP của máy host>`
   - Port: `5432`
   - Username/Password: như local

⚠️ **Cảnh báo**: Không an toàn nếu không dùng VPN/SSH tunnel!

---

## 🚀 Recommended Setup cho Team 2 người

### Development Workflow

1. **Local Development**:
   - Mỗi người: PostgreSQL local + pgAdmin
   - Connection string: `localhost:5432`
   - Test thoải mái không ảnh hưởng nhau

2. **Shared Staging/Testing**:
   - Deploy lên **Neon.tech** (free)
   - Cả 2 người connect qua pgAdmin
   - Dùng để test tích hợp, demo, xem data chung

3. **Production**:
   - Deploy lên Render.com hoặc Railway (có free tier)
   - Hoặc upgrade Neon.tech ($19/month)

### Migration Management

**Dùng Entity Framework Migrations** (đã có sẵn):

```bash
# Khi có thay đổi model
cd src/HeriStepAI.API
dotnet ef migrations add AddNewFeature
dotnet ef database update

# Commit migration files lên Git
git add Migrations/
git commit -m "Add migration: AddNewFeature"
```

Member kia pull code và chạy:
```bash
dotnet ef database update
```

---

## 📝 Connection Strings cho từng môi trường

### Local Development
```bash
# .env hoặc appsettings.Development.json
SUPABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=heristepai_db;Username=postgres;Password=admin123;SSL Mode=Prefer"
```

### Shared Neon.tech (Staging)
```bash
# .env.staging (KHÔNG commit)
SUPABASE_CONNECTION_STRING="Host=ep-xxx.region.aws.neon.tech;Port=5432;Database=heristepai;Username=neondb_owner;Password=xxx;SSL Mode=Require"
```

### Production (Render/Railway)
```bash
# Set trực tiếp trong Environment Variables của hosting
SUPABASE_CONNECTION_STRING="postgres://user:pass@host:5432/dbname?sslmode=require"
```

---

## 🔧 Tools hữu ích

### pgAdmin 4 Tips

1. **View data**: Tables → Right-click → View/Edit Data → All Rows
2. **Run SQL**: Tools → Query Tool
3. **Export data**: Right-click table → Export
4. **Import CSV**: Right-click table → Import/Export

### SQL Scripts cho việc thường dùng

**Backup database**:
```bash
pg_dump -U postgres -d heristepai_db -f backup.sql
```

**Restore database**:
```bash
psql -U postgres -d heristepai_db -f backup.sql
```

**Export chỉ schema**:
```bash
pg_dump -U postgres -d heristepai_db --schema-only -f schema.sql
```

---

## ⚡ Quick Start cho 2 người

### Member 1 (You)
1. Cài PostgreSQL + pgAdmin
2. Tạo database `heristepai_db`
3. Chạy API → EF Core tự tạo tables
4. Export schema: `pg_dump --schema-only > schema.sql`
5. Commit `schema.sql` lên Git

### Member 2 (Your teammate)
1. Cài PostgreSQL + pgAdmin
2. Tạo database `heristepai_db`
3. Pull code từ Git
4. Import schema: `psql -f schema.sql`
5. Chạy API → Ready to work

### Shared Testing (Optional)
1. Tạo Neon.tech project (free)
2. Share connection string qua Discord/Slack (KHÔNG commit)
3. Cả 2 connect pgAdmin vào Neon
4. Deploy API lên Render với Neon connection string

---

**Khuyến nghị cuối cùng**:
- **Development**: Local PostgreSQL
- **Testing/Collaboration**: Neon.tech free tier
- **Production**: Render hoặc Railway

Cách này cho phép làm việc độc lập nhưng vẫn có shared environment để test!
