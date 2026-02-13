-- ===================================================
-- Script: Tạo Shop Owner Accounts và Gán quyền quản lý POIs
-- Date: 2026-02-09
-- Description: Insert shop owner users (Role=2) và assign OwnerId cho POIs
-- ===================================================

-- Bước 1: Insert Shop Owner Accounts
-- Password mặc định: "ShopOwner@123" (cần hash bằng BCrypt)
-- Hash BCrypt cho password: $2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5

INSERT INTO public."Users"
("Username", "Email", "PasswordHash", "Role", "CreatedAt", "IsActive")
VALUES
-- POI 4: Đặc sản Huế 10 Thương
('dacsanhue_10thuong', 'dacsanhue10@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 11: Bánh khọt (bánh xèo, Hồ Thị Kỷ)
('banhkhot_hothiky', 'banhkhot.hothiky@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 12: Tiệm Bánh Family
('tiembanhfamily', 'family.bakery@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 13: Bánh Tráng Nướng Thanh Phan Rang
('banhtrang_thanhphanrang', 'banhtrang.thanh@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 14: Linh Bún Thái
('linhbunthai', 'linhbunthai@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 15: Cup Đẹp - Beauty In A Cup
('cupdep_beautyinacup', 'cupdep.beauty@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 16: Bo nướng Cambodia
('bonuong_cambodia', 'bonuong.cambodia@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 17: Trà sữa mr tea
('trasua_mrtea', 'mrtea.trasua@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 18: Hủ tiếu trộn A.Hùng
('hutieu_ahung', 'hutieu.ahung@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 19: Khoai Lang Bong Bóng - Đường Phố Food
('khoailang_duongpho', 'khoailang.streetfood@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 20: ĐẬU HỦ THÚI & TRÀ SỮA CÔ ÚT
('dauhuthui_cout', 'dauhuthui.cout@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 21: Cháo lòng - lòng nhậu Vinh
('chaolong_vinh', 'chaolong.vinh@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 22: Heo mọi nướng giả cầy
('heomoi_giacay', 'heomoi.giacay@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 23: HỦ TIẾU SƯỜN SỤN LIÊN
('hutieu_suonsun', 'hutieu.lien@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 24: Khoai Lang Bong Bóng - Yam - Hồ Thị Kỷ (cùng owner với 19)
-- Sẽ dùng chung owner với POI 19

-- POI 25: Chén trứng nướng
('chentrung_nuong', 'chentrung.nuong@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 26: Xiên Nướng Bin Bo
('xiennuong_binbo', 'xiennuong.binbo@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 27: Hủ Tiếu Nam Vang Phú Quý
('hutieu_phuquy', 'hutieu.phuquy@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 28: Lẩu Bò Hoàng Thu
('laubo_hoangthu', 'laubo.hoangthu@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 29: Lẩu bò Cô Thảo
('laubo_cothao', 'laubo.cothao@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 30: Ốc Hồng
('oc_hong', 'oc.hong@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 31: Bò lụi sả - Chợ Hồ Thị Kỷ
('bolui_sa', 'bolui.sa@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true),

-- POI 32: Quán Cơm Chay Hoa Sen
('comchay_hoasen', 'comchay.hoasen@gmail.com', '$2a$11$9KxZQX5fYGH0KQvZ6pFGDeXZ6hqJ6vHXqN5P5xJ8YqF5kJ5L5J5L5', 2, NOW(), true);

-- ===================================================
-- Bước 2: Gán OwnerId cho các POIs
-- ===================================================

-- POI 4: Đặc sản Huế 10 Thương (đã có OwnerId=2, giữ nguyên)
-- UPDATE public."POIs" SET "OwnerId" = 2 WHERE "Id" = 4;

-- POI 11: Bánh khọt
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'banhkhot_hothiky')
WHERE "Id" = 11;

-- POI 12: Tiệm Bánh Family
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'tiembanhfamily')
WHERE "Id" = 12;

-- POI 13: Bánh Tráng Nướng Thanh Phan Rang
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'banhtrang_thanhphanrang')
WHERE "Id" = 13;

-- POI 14: Linh Bún Thái
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'linhbunthai')
WHERE "Id" = 14;

-- POI 15: Cup Đẹp
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'cupdep_beautyinacup')
WHERE "Id" = 15;

-- POI 16: Bo nướng Cambodia
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'bonuong_cambodia')
WHERE "Id" = 16;

-- POI 17: Trà sữa mr tea
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'trasua_mrtea')
WHERE "Id" = 17;

-- POI 18: Hủ tiếu trộn A.Hùng
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'hutieu_ahung')
WHERE "Id" = 18;

-- POI 19: Khoai Lang Bong Bóng - Đường Phố Food
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'khoailang_duongpho')
WHERE "Id" = 19;

-- POI 20: ĐẬU HỦ THÚI & TRÀ SỮA CÔ ÚT
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'dauhuthui_cout')
WHERE "Id" = 20;

-- POI 21: Cháo lòng - lòng nhậu Vinh
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'chaolong_vinh')
WHERE "Id" = 21;

-- POI 22: Heo mọi nướng giả cầy
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'heomoi_giacay')
WHERE "Id" = 22;

-- POI 23: HỦ TIẾU SƯỜN SỤN LIÊN
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'hutieu_suonsun')
WHERE "Id" = 23;

-- POI 24: Khoai Lang Bong Bóng - Yam (cùng chủ với POI 19)
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'khoailang_duongpho')
WHERE "Id" = 24;

-- POI 25: Chén trứng nướng
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'chentrung_nuong')
WHERE "Id" = 25;

-- POI 26: Xiên Nướng Bin Bo
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'xiennuong_binbo')
WHERE "Id" = 26;

-- POI 27: Hủ Tiếu Nam Vang Phú Quý
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'hutieu_phuquy')
WHERE "Id" = 27;

-- POI 28: Lẩu Bò Hoàng Thu
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'laubo_hoangthu')
WHERE "Id" = 28;

-- POI 29: Lẩu bò Cô Thảo
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'laubo_cothao')
WHERE "Id" = 29;

-- POI 30: Ốc Hồng
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'oc_hong')
WHERE "Id" = 30;

-- POI 31: Bò lụi sả
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'bolui_sa')
WHERE "Id" = 31;

-- POI 32: Quán Cơm Chay Hoa Sen
UPDATE public."POIs"
SET "OwnerId" = (SELECT "Id" FROM public."Users" WHERE "Username" = 'comchay_hoasen')
WHERE "Id" = 32;

-- ===================================================
-- Bước 3: Verify kết quả
-- ===================================================

-- Kiểm tra số lượng shop owners đã tạo
SELECT COUNT(*) as total_shop_owners
FROM public."Users"
WHERE "Role" = 2;

-- Kiểm tra POIs đã có owner
SELECT
    p."Id",
    p."Name",
    p."OwnerId",
    u."Username",
    u."Email"
FROM public."POIs" p
LEFT JOIN public."Users" u ON p."OwnerId" = u."Id"
ORDER BY p."Id";

-- Thống kê owners và số lượng POIs của họ
SELECT
    u."Id",
    u."Username",
    u."Email",
    COUNT(p."Id") as total_pois
FROM public."Users" u
LEFT JOIN public."POIs" p ON u."Id" = p."OwnerId"
WHERE u."Role" = 2
GROUP BY u."Id", u."Username", u."Email"
ORDER BY total_pois DESC;

-- ===================================================
-- LƯU Ý QUAN TRỌNG:
-- ===================================================
-- 1. Password mặc định: "ShopOwner@123"
-- 2. Hash BCrypt sử dụng trong script là PLACEHOLDER
--    Thay thế bằng hash thật từ BCrypt.Net hoặc tool tương tự
-- 3. Để generate password hash thực:
--    - C#: BCrypt.Net.BCrypt.HashPassword("ShopOwner@123")
--    - Online: https://bcrypt-generator.com/
-- 4. Khuyến nghị: Sau khi chạy script, gửi email cho shop owners
--    để họ đổi password lần đầu đăng nhập
-- ===================================================
