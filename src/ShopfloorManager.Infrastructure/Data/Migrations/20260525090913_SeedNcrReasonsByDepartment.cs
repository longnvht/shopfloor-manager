using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedNcrReasonsByDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "department_id", "name" },
                values: new object[] { 3, "Mòn dụng cụ cắt" });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "department_id", "name" },
                values: new object[] { 3, "Lỗi gá đặt" });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { 4, "Lỗi bản vẽ / dung sai", 1 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { 4, "Sai vật liệu", 2 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { 3, "Lỗi máy gia công", 3 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { 2, "Lỗi thiết bị đo CMM", 1 });

            migrationBuilder.InsertData(
                table: "ncr_reasons",
                columns: new[] { "id", "created_at", "department_id", "is_active", "name", "sort_order", "tag" },
                values: new object[,]
                {
                    { 8, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, true, "Lỗi đồ gá / fixture", 4, "FXT" },
                    { 9, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, true, "Lỗi vận hành / thao tác", 5, "OPR" },
                    { 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, true, "Sai thông số cắt gọt", 6, "PARAM" },
                    { 11, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, true, "Dụng cụ đo chưa hiệu chuẩn", 2, "CALIB" },
                    { 12, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, true, "Sai phương pháp kiểm tra", 3, "INSP" },
                    { 13, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, true, "Lỗi lập trình CAM/G-code", 3, "CAM" },
                    { 14, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, true, "Lỗi quy trình công nghệ", 4, "PROC" },
                    { 15, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, true, "Dung sai thiết kế quá chặt", 5, "TOL" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "department_id", "name" },
                values: new object[] { null, "Tool wear" });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "department_id", "name" },
                values: new object[] { null, "Setup error" });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { null, "Drawing error", 3 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { null, "Wrong material", 4 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { null, "Machine error", 5 });

            migrationBuilder.UpdateData(
                table: "ncr_reasons",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "department_id", "name", "sort_order" },
                values: new object[] { null, "CMM error", 6 });
        }
    }
}
