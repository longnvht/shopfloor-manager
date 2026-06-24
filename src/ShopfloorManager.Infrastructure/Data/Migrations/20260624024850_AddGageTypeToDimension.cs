using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGageTypeToDimension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gage_type_id",
                table: "dimensions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_dimensions_gage_type_id",
                table: "dimensions",
                column: "gage_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_dimensions_gage_types_gage_type_id",
                table: "dimensions",
                column: "gage_type_id",
                principalTable: "gage_types",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dimensions_gage_types_gage_type_id",
                table: "dimensions");

            migrationBuilder.DropIndex(
                name: "ix_dimensions_gage_type_id",
                table: "dimensions");

            migrationBuilder.DropColumn(
                name: "gage_type_id",
                table: "dimensions");
        }
    }
}
