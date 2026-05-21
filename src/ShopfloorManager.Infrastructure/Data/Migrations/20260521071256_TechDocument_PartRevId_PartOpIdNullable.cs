using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TechDocument_PartRevId_PartOpIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "part_op_id",
                table: "tech_documents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "part_rev_id",
                table: "tech_documents",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 2,
                column: "is_job_number",
                value: false);

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "is_part_number", "is_revision" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "is_part_number", "is_revision" },
                values: new object[] { false, false });

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_part_rev_id",
                table: "tech_documents",
                column: "part_rev_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tech_documents_part_revs_part_rev_id",
                table: "tech_documents",
                column: "part_rev_id",
                principalTable: "part_revs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tech_documents_part_revs_part_rev_id",
                table: "tech_documents");

            migrationBuilder.DropIndex(
                name: "ix_tech_documents_part_rev_id",
                table: "tech_documents");

            migrationBuilder.DropColumn(
                name: "part_rev_id",
                table: "tech_documents");

            migrationBuilder.AlterColumn<int>(
                name: "part_op_id",
                table: "tech_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 2,
                column: "is_job_number",
                value: true);

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "is_part_number", "is_revision" },
                values: new object[] { true, true });

            migrationBuilder.UpdateData(
                table: "file_types",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "is_part_number", "is_revision" },
                values: new object[] { true, true });
        }
    }
}
