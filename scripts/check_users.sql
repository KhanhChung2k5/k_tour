-- Kiểm tra tất cả users và POIs của họ
SELECT
    u."Id",
    u."Username",
    u."Email",
    u."Role",
    u."IsActive",
    COUNT(p."Id") as TotalPOIs
FROM public."Users" u
LEFT JOIN public."POIs" p ON u."Id" = p."OwnerId"
GROUP BY u."Id", u."Username", u."Email", u."Role", u."IsActive"
ORDER BY u."Id";

-- Chi tiết POIs của từng owner
SELECT
    u."Id" as UserId,
    u."Username",
    u."Email",
    p."Id" as POIId,
    p."Name" as POIName
FROM public."Users" u
LEFT JOIN public."POIs" p ON u."Id" = p."OwnerId"
WHERE u."Role" = 2
ORDER BY u."Id", p."Id";
