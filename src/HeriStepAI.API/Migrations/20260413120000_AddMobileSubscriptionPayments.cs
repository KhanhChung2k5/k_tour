using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HeriStepAI.API.Migrations;

public class AddMobileSubscriptionPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MobileSubscriptionPayments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DeviceKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                TransferRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PlanCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                PlanLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                AmountVnd = table.Column<int>(type: "integer", nullable: false),
                SubscriptionExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                ReportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VerifiedByUserId = table.Column<int>(type: "integer", nullable: true),
                AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MobileSubscriptionPayments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MobileSubscriptionPayments_ReportedAtUtc",
            table: "MobileSubscriptionPayments",
            column: "ReportedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_MobileSubscriptionPayments_Status",
            table: "MobileSubscriptionPayments",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_MobileSubscriptionPayments_TransferRef",
            table: "MobileSubscriptionPayments",
            column: "TransferRef");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MobileSubscriptionPayments");
    }
}
