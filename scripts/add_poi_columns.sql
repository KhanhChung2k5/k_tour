-- Chạy script này trên Supabase SQL Editor nếu dotnet ef database update không hoạt động
-- Thêm các cột mới vào bảng "POIs"

ALTER TABLE "POIs" ADD COLUMN IF NOT EXISTS "Rating" double precision;
ALTER TABLE "POIs" ADD COLUMN IF NOT EXISTS "ReviewCount" integer NOT NULL DEFAULT 0;
ALTER TABLE "POIs" ADD COLUMN IF NOT EXISTS "Category" integer NOT NULL DEFAULT 0;
ALTER TABLE "POIs" ADD COLUMN IF NOT EXISTS "TourId" integer;
ALTER TABLE "POIs" ADD COLUMN IF NOT EXISTS "EstimatedMinutes" integer NOT NULL DEFAULT 30;
