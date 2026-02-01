using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations
{
    public partial class AddAddressToPOI : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "POIs",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "POIs");
        }
    }
}
