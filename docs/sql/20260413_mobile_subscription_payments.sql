-- HeriStepAI: bảng báo thanh toán gói mobile (đối soát CK). Chạy trên Supabase nếu không dùng dotnet ef database update.
CREATE TABLE IF NOT EXISTS "MobileSubscriptionPayments" (
    "Id" SERIAL PRIMARY KEY,
    "DeviceKey" VARCHAR(16) NOT NULL,
    "TransferRef" VARCHAR(64) NOT NULL,
    "PlanCode" VARCHAR(8) NOT NULL,
    "PlanLabel" VARCHAR(64) NULL,
    "AmountVnd" INTEGER NOT NULL,
    "SubscriptionExpiresAtUtc" TIMESTAMPTZ NULL,
    "Platform" VARCHAR(32) NULL,
    "Status" INTEGER NOT NULL DEFAULT 0,
    "ReportedAtUtc" TIMESTAMPTZ NOT NULL,
    "VerifiedAtUtc" TIMESTAMPTZ NULL,
    "VerifiedByUserId" INTEGER NULL,
    "AdminNote" VARCHAR(500) NULL
);
CREATE INDEX IF NOT EXISTS "IX_MobileSubscriptionPayments_TransferRef" ON "MobileSubscriptionPayments" ("TransferRef");
CREATE INDEX IF NOT EXISTS "IX_MobileSubscriptionPayments_Status" ON "MobileSubscriptionPayments" ("Status");
CREATE INDEX IF NOT EXISTS "IX_MobileSubscriptionPayments_ReportedAtUtc" ON "MobileSubscriptionPayments" ("ReportedAtUtc");
