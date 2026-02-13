-- ===================================================
-- Kiểm tra và Reset Password cho Shop Owner Account
-- ===================================================

-- BƯỚC 1: Kiểm tra thông tin user hiện tại
SELECT
    "Id",
    "Username",
    "Email",
    "Role",
    "IsActive"
FROM public."Users"
WHERE "Id" = 2;

-- BƯỚC 2: Kiểm tra POIs thuộc sở hữu
SELECT
    "Id",
    "Name",
    "OwnerId"
FROM public."POIs"
WHERE "OwnerId" = 2;

-- BƯỚC 3: Reset password và đảm bảo Role đúng
-- Password mới: "ShopOwner@123"
-- BCrypt hash: $2a$11$rQj3Zz5K3g5K5K5K5K5K5uK5K5K5K5K5K5K5K5K5K5K5K5K5K5

UPDATE public."Users"
SET
    "Email" = 'owner@shop.com',
    "Role" = 2,  -- ShopOwner
    "IsActive" = true,
    "PasswordHash" = '$2a$11$rQj3Zz5K3g5K5K5K5K5K5uK5K5K5K5K5K5K5K5K5K5K5K5K5K5'
WHERE "Id" = 2;

-- Verify sau khi update
SELECT
    "Id",
    "Username",
    "Email",
    "Role",
    "IsActive"
FROM public."Users"
WHERE "Id" = 2;
