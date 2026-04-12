using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations;

public class AddUserApprovalStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ApprovalStatus",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 2);

        migrationBuilder.Sql("""
            UPDATE "Users" SET "ApprovalStatus" = 0 WHERE "Role" = 1;
            UPDATE "Users" SET "ApprovalStatus" = 0 WHERE "Role" = 3;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ApprovalStatus",
            table: "Users");
    }
}
