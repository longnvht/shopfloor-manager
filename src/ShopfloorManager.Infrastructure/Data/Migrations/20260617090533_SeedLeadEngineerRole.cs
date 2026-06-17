using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeadEngineerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Lead Engineer role seed ───────────────────────────────
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[] { 8, "Lead Engineer" });

            // ── Dimension approval workflow ───────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "review_note",
                table: "dimensions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                table: "dimensions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by",
                table: "dimensions",
                type: "integer",
                nullable: true);

            // defaultValue: 1 = Approved — existing rows không cần duyệt lại
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "dimensions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // ── MeasureStage: giai đoạn đo kiểm ─────────────────────
            // defaultValue: 0 = InprocessFAI — data cũ thuộc giai đoạn Operator
            migrationBuilder.AddColumn<int>(
                name: "measure_stage",
                table: "measure_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "measure_stage",
                table: "measure_values");

            migrationBuilder.DropColumn(
                name: "review_note",
                table: "dimensions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "dimensions");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "dimensions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "dimensions");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 8);
        }
    }
}
