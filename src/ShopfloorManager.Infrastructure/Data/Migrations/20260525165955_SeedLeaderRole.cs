using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeaderRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[] { 7, "Leader" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 7);
        }
    }
}
