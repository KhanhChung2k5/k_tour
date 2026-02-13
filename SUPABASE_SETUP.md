# Hướng dẫn Cấu hình Supabase

## 1. Tạo Project trên Supabase

1. Truy cập [supabase.com](https://supabase.com)
2. Đăng ký/Đăng nhập tài khoản
3. Tạo project mới
4. Chờ project được tạo (khoảng 2-3 phút)

## 2. Lấy Connection String

1. Vào **Settings** > **Database**
2. Tìm phần **Connection string** > **URI**
3. Copy connection string có dạng:
   ```
   postgresql://postgres:[YOUR-PASSWORD]@[PROJECT-REF].supabase.co:5432/postgres
   ```
4. Thay `[YOUR-PASSWORD]` bằng password bạn đã set khi tạo project

## 3. Cấu hình .env file

1. Copy file `.env.example` thành `.env`:
   ```bash
   cp .env.example .env
   ```

2. Mở file `.env` và điền thông tin:

```env
# Supabase Database Connection
SUPABASE_CONNECTION_STRING=postgresql://postgres:your-password@xxxxx.supabase.co:5432/postgres

# JWT Settings
JWT_SECRET_KEY=YourSuperSecretKeyForJWTTokenGeneration12345!@#$%
JWT_ISSUER=HeriStepAI
JWT_AUDIENCE=HeriStepAIUsers

# Supabase API Keys (tùy chọn - nếu cần dùng Supabase API)
SUPABASE_URL=https://xxxxx.supabase.co
SUPABASE_ANON_KEY=your-anon-key-here
SUPABASE_SERVICE_ROLE_KEY=your-service-role-key-here
```

## 4. Lấy Supabase API Keys (nếu cần)

1. Vào **Settings** > **API**
2. Copy các keys:
   - **Project URL** → `SUPABASE_URL`
   - **anon public** → `SUPABASE_ANON_KEY`
   - **service_role** → `SUPABASE_SERVICE_ROLE_KEY` (giữ bí mật!)

## 5. Tạo Database Tables

Sau khi chạy ứng dụng lần đầu, Entity Framework sẽ tự động tạo các bảng:

- `Users`
- `POIs`
- `POIContents`
- `VisitLogs`
- `Analytics`

Hoặc bạn có thể chạy migrations thủ công:

```bash
cd src/HeriStepAI.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 6. Kiểm tra kết nối

1. Chạy API:
   ```bash
   cd src/HeriStepAI.API
   dotnet run
   ```

2. Nếu thành công, bạn sẽ thấy:
   - Database được tạo tự động
   - Seed data được thêm vào
   - API chạy tại `https://localhost:7001`

## 7. Troubleshooting

### Lỗi kết nối database
- Kiểm tra connection string trong `.env`
- Đảm bảo password đúng
- Kiểm tra firewall/network có chặn port 5432 không
- Thử ping đến `[PROJECT-REF].supabase.co`

### Lỗi SSL
Nếu gặp lỗi SSL, thêm vào connection string:
```
?sslmode=require
```

Ví dụ:
```
postgresql://postgres:password@xxx.supabase.co:5432/postgres?sslmode=require
```

### Reset password
1. Vào **Settings** > **Database**
2. Click **Reset database password**
3. Copy password mới và cập nhật vào `.env`

## 8. Supabase Dashboard

Bạn có thể xem và quản lý data trực tiếp trên Supabase Dashboard:
- **Table Editor**: Xem/sửa data
- **SQL Editor**: Chạy SQL queries
- **Database**: Xem schema và relationships

## Lưu ý bảo mật

⚠️ **QUAN TRỌNG:**
- **KHÔNG** commit file `.env` lên Git
- File `.env` đã được thêm vào `.gitignore`
- Chia sẻ `.env.example` thay vì `.env`
- Giữ bí mật `SUPABASE_SERVICE_ROLE_KEY` và database password
