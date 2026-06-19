using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUcars.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCarIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cars_SellerId",
                table: "Cars");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_SellerId_Status_CreatedAt",
                table: "Cars",
                columns: new[] { "SellerId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Status_Brand",
                table: "Cars",
                columns: new[] { "Status", "Brand" });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Status_CreatedAt",
                table: "Cars",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Status_Price",
                table: "Cars",
                columns: new[] { "Status", "Price" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cars_SellerId_Status_CreatedAt",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Status_Brand",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Status_CreatedAt",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Status_Price",
                table: "Cars");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_SellerId",
                table: "Cars",
                column: "SellerId");
        }
    }
}
