-- Migration: Add FoodType and Price columns to POIs table
-- Date: 2026-02-08
-- Description: Adds FoodType, PriceMin, and PriceMax columns for food establishments

-- Add FoodType column (0=Other, 1=Seafood, 2=Vegetarian, 3=Specialty, 4=Street, 5=Grilled, 6=Noodles)
ALTER TABLE public."POIs"
ADD COLUMN IF NOT EXISTS "FoodType" integer NOT NULL DEFAULT 0;

-- Add PriceMin column (minimum price in VND)
ALTER TABLE public."POIs"
ADD COLUMN IF NOT EXISTS "PriceMin" bigint NOT NULL DEFAULT 0;

-- Add PriceMax column (maximum price in VND)
ALTER TABLE public."POIs"
ADD COLUMN IF NOT EXISTS "PriceMax" bigint NOT NULL DEFAULT 0;

-- Update existing rows (if any) to have default values
UPDATE public."POIs"
SET "FoodType" = 0, "PriceMin" = 0, "PriceMax" = 0
WHERE "FoodType" IS NULL OR "PriceMin" IS NULL OR "PriceMax" IS NULL;

-- Verify the changes
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'POIs'
  AND column_name IN ('FoodType', 'PriceMin', 'PriceMax')
ORDER BY column_name;
