# Giải thích JWT Settings trong .env

## Dòng 6-9 trong .env.example

```env
# JWT Settings
JWT_SECRET_KEY=YourSuperSecretKeyForJWTTokenGeneration12345!@#$%
JWT_ISSUER=HeriStepAI
JWT_AUDIENCE=HeriStepAIUsers
```

## Giải thích từng biến:

### 1. `JWT_SECRET_KEY` (Dòng 7)
- **Mục đích**: Secret key dùng để ký (sign) và xác thực JWT tokens
- **Tại sao cần**: Đảm bảo token không bị giả mạo
- **Yêu cầu**: 
  - Nên dài ít nhất 32 ký tự
  - Nên chứa ký tự đặc biệt, số, chữ hoa/thường
  - **QUAN TRỌNG**: Giữ bí mật, không chia sẻ công khai
- **Ví dụ tốt**: 
  ```
  MySuperSecretKey123!@#$%^&*()_+-=[]{}|;:,.<>?
  ```
- **Ví dụ không tốt**: 
  ```
  password123  (quá ngắn, dễ đoán)
  ```

### 2. `JWT_ISSUER` (Dòng 8)
- **Mục đích**: Tên của service/app phát hành token
- **Tại sao cần**: Xác định token được tạo bởi service nào
- **Ví dụ**: `HeriStepAI`, `MyApp`, `MyCompany`
- **Lưu ý**: Nên là tên duy nhất cho ứng dụng của bạn

### 3. `JWT_AUDIENCE` (Dòng 9)
- **Mục đích**: Tên của service/app sẽ nhận và sử dụng token
- **Tại sao cần**: Xác định token được dùng cho service nào
- **Ví dụ**: `HeriStepAIUsers`, `MyAppClients`
- **Lưu ý**: Thường giống với Issuer hoặc có thể khác nếu có nhiều services

## Cách hoạt động:

1. **Khi user đăng nhập**:
   - Server tạo JWT token với:
     - Secret key để ký token
     - Issuer = "HeriStepAI"
     - Audience = "HeriStepAIUsers"
   - Gửi token về cho client

2. **Khi client gửi request**:
   - Client gửi token trong header: `Authorization: Bearer <token>`
   - Server kiểm tra:
     - Token có được ký bằng đúng secret key không?
     - Issuer có đúng không?
     - Audience có đúng không?
     - Token còn hạn không?

3. **Nếu hợp lệ**: Cho phép truy cập
4. **Nếu không hợp lệ**: Từ chối (401 Unauthorized)

## Ví dụ thực tế:

```env
# Production - Nên dùng key mạnh và ngẫu nhiên
JWT_SECRET_KEY=K8#mP2$vL9@nQ5&wR7!tY3*uI6^oE4%aZ1
JWT_ISSUER=HeriStepAI-Production
JWT_AUDIENCE=HeriStepAI-Users

# Development - Có thể dùng key đơn giản hơn
JWT_SECRET_KEY=dev-secret-key-12345
JWT_ISSUER=HeriStepAI-Dev
JWT_AUDIENCE=HeriStepAI-Dev-Users
```

## Bảo mật:

⚠️ **QUAN TRỌNG**:
- **KHÔNG** commit file `.env` lên Git
- **KHÔNG** chia sẻ `JWT_SECRET_KEY` công khai
- Nên tạo key ngẫu nhiên mạnh cho production
- Có thể dùng tool để generate: https://randomkeygen.com/

## Tạo Secret Key mạnh:

### Cách 1: Online
- Truy cập: https://randomkeygen.com/
- Chọn "CodeIgniter Encryption Keys"
- Copy key dài 64 ký tự

### Cách 2: PowerShell
```powershell
-join ((48..57) + (65..90) + (97..122) + (33..47) | Get-Random -Count 64 | % {[char]$_})
```

### Cách 3: C# Code
```csharp
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
```

## Kết luận:

Ba biến này là **bắt buộc** để JWT authentication hoạt động. Nếu không set, ứng dụng sẽ dùng giá trị mặc định từ `appsettings.json`, nhưng nên set trong `.env` để dễ quản lý và bảo mật hơn.
