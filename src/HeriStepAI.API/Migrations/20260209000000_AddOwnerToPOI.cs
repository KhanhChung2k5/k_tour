using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStepAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "POIs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_POIs_OwnerId",
                table: "POIs",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_POIs_Users_OwnerId",
                table: "POIs",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_POIs_Users_OwnerId",
                table: "POIs");

            migrationBuilder.DropIndex(
                name: "IX_POIs_OwnerId",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "POIs");
        }
    }
}
