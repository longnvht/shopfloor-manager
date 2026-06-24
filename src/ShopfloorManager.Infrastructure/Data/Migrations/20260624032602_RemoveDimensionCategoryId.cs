using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDimensionCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dimensions_dimension_categories_category_id",
                table: "dimensions");

            migrationBuilder.DropIndex(
                name: "ix_dimensions_category_id",
                table: "dimensions");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "dimensions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "dimensions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_dimensions_category_id",
                table: "dimensions",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_dimensions_dimension_categories_category_id",
                table: "dimensions",
                column: "category_id",
                principalTable: "dimension_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
