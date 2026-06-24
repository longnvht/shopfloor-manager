using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameDimensionCategoryToGageCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_gage_types_dimension_categories_category_id",
                table: "gage_types");

            migrationBuilder.DropTable(
                name: "dimension_categories");

            migrationBuilder.CreateTable(
                name: "gage_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gage_categories", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "gage_categories",
                columns: new[] { "id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "LIN", "Thước cặp, panme", true, "Linear" },
                    { 2, "ANG", "Thước góc", true, "Angular" },
                    { 3, "THD", "Dưỡng ren, ring gauge", true, "Thread" },
                    { 4, "GEO", "CMM, dial indicator", true, "Geometric" },
                    { 5, "SFC", "Surface tester", true, "Surface" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_gage_categories_code",
                table: "gage_categories",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_gage_types_gage_categories_category_id",
                table: "gage_types",
                column: "category_id",
                principalTable: "gage_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_gage_types_gage_categories_category_id",
                table: "gage_types");

            migrationBuilder.DropTable(
                name: "gage_categories");

            migrationBuilder.CreateTable(
                name: "dimension_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dimension_categories", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "dimension_categories",
                columns: new[] { "id", "code", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "LIN", "Thước cặp, panme", true, "Linear" },
                    { 2, "ANG", "Thước góc", true, "Angular" },
                    { 3, "THD", "Dưỡng ren, ring gauge", true, "Thread" },
                    { 4, "GEO", "CMM, dial indicator", true, "Geometric" },
                    { 5, "SFC", "Surface tester", true, "Surface" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_dimension_categories_code",
                table: "dimension_categories",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_gage_types_dimension_categories_category_id",
                table: "gage_types",
                column: "category_id",
                principalTable: "dimension_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
