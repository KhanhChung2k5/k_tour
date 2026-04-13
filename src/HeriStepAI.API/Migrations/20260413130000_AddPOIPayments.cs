using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HeriStepAI.API.Migrations;

public partial class AddPOIPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "POIPayments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                POIId = table.Column<int>(type: "integer", nullable: false),
                OwnerId = table.Column<int>(type: "integer", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                AmountVnd = table.Column<long>(type: "bigint", nullable: false),
                TransferRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                ReportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VerifiedByUserId = table.Column<int>(type: "integer", nullable: true),
                AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_POIPayments", x => x.Id);
                table.ForeignKey(
                    name: "FK_POIPayments_POIs_POIId",
                    column: x => x.POIId,
                    principalTable: "POIs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_POIPayments_Users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_POIPayments_POIId",
            table: "POIPayments",
            column: "POIId");

        migrationBuilder.CreateIndex(
            name: "IX_POIPayments_Status",
            table: "POIPayments",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_POIPayments_TransferRef",
            table: "POIPayments",
            column: "TransferRef",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "POIPayments");
    }
}
