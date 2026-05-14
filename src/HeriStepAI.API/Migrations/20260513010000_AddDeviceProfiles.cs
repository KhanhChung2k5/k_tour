using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HeriStepAI.API.Migrations;

public partial class AddDeviceProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeviceProfiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DeviceId  = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Profile   = table.Column<int>(type: "integer", nullable: false),
                Cores     = table.Column<int>(type: "integer", nullable: true),
                RamMb     = table.Column<long>(type: "bigint", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeviceProfiles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DeviceProfiles_DeviceId",
            table: "DeviceProfiles",
            column: "DeviceId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DeviceProfiles");
    }
}
