-- ===================================================
-- Update All Shop Owner Passwords
-- New Password: ShopOwner@123
-- BCrypt Hash: $2a$12$XLY2K762ta2eL.ZaQOkXheLZasLyqTt/eWxhhPn0wZ0cU.dME9FwW
-- ===================================================

-- BƯỚC 1: Kiểm tra tất cả Shop Owners hiện tại
SELECT
    "Id",
    "Username",
    "Email",
    "Role",
    "IsActive",
    "CreatedAt"
FROM public."Users"
WHERE "Role" = 2
ORDER BY "Id";

-- BƯỚC 2: Update password cho TẤT CẢ Shop Owners
UPDATE public."Users"
SET
    "PasswordHash" = '$2a$12$XLY2K762ta2eL.ZaQOkXheLZasLyqTt/eWxhhPn0wZ0cU.dME9FwW',
    "IsActive" = true
WHERE "Role" = 2;

-- BƯỚC 3: Verify - Xem danh sách sau khi update
SELECT
    "Id",
    "Username",
    "Email",
    "Role",
    "IsActive",
    LEFT("PasswordHash", 20) as "PasswordHashPreview"
FROM public."Users"
WHERE "Role" = 2
ORDER BY "Id";

-- BƯỚC 4: Kiểm tra Shop Owners và POIs của họ
SELECT
    u."Id" as UserId,
    u."Username",
    u."Email",
    u."IsActive",
    COUNT(p."Id") as TotalPOIs
FROM public."Users" u
LEFT JOIN public."POIs" p ON u."Id" = p."OwnerId"
WHERE u."Role" = 2
GROUP BY u."Id", u."Username", u."Email", u."IsActive"
ORDER BY u."Id";

-- ===================================================
-- KẾT QUẢ MONG ĐỢI:
-- ===================================================
-- - Tất cả users có Role = 2 (ShopOwner)
-- - Password đã được update thành hash mới
-- - Tất cả accounts đều IsActive = true
-- - Có thể login với password: ShopOwner@123
-- ===================================================
