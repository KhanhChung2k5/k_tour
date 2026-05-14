using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations;

public partial class AddDeviceProfileToUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DeviceProfile",
            table: "Users",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DeviceCores",
            table: "Users",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "DeviceRamMb",
            table: "Users",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeviceProfileAt",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DeviceProfile",   table: "Users");
        migrationBuilder.DropColumn(name: "DeviceCores",     table: "Users");
        migrationBuilder.DropColumn(name: "DeviceRamMb",     table: "Users");
        migrationBuilder.DropColumn(name: "DeviceProfileAt", table: "Users");
    }
}
