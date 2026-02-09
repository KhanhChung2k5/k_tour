using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations
{
    public partial class AddFoodTypeAndPriceToPOI : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FoodType",
                table: "POIs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "PriceMin",
                table: "POIs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PriceMax",
                table: "POIs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FoodType", table: "POIs");
            migrationBuilder.DropColumn(name: "PriceMin", table: "POIs");
            migrationBuilder.DropColumn(name: "PriceMax", table: "POIs");
        }
    }
}
