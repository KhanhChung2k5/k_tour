using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations
{
    public partial class AddPOIEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "POIs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "POIs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "POIs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TourId",
                table: "POIs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "POIs",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "TourId",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "POIs");
        }
    }
}
