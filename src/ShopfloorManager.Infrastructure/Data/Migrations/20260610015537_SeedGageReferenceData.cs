using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedGageReferenceData : Migration
    {
        private static readonly DateTimeOffset SeedDate =
            new(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // gage_types và gage_locations đã có dữ liệu thực từ legacy import — không seed thêm.
            // calib_vendors / calib_procedures / gage_slots còn trống — seed reference data tối thiểu.

            migrationBuilder.InsertData(
                table: "calib_procedures",
                columns: new[] { "id", "created_at", "updated_at", "name", "description", "revision", "is_latest" },
                values: new object[,]
                {
                    { 1, SeedDate, SeedDate, "Hiệu chuẩn thước cặp / panme cơ khí", "Quy trình hiệu chuẩn dụng cụ đo chiều dài cầm tay", "R1", true },
                    { 2, SeedDate, SeedDate, "Hiệu chuẩn dưỡng ren (Thread Gauge)", "Quy trình hiệu chuẩn dưỡng ren trụ trong/ngoài", "R1", true },
                    { 3, SeedDate, SeedDate, "Hiệu chuẩn đồng hồ so (Dial Indicator)", "Quy trình hiệu chuẩn đồng hồ so cơ/điện tử", "R1", true },
                });

            migrationBuilder.InsertData(
                table: "calib_vendors",
                columns: new[] { "id", "created_at", "updated_at", "name", "contact", "phone", "email" },
                values: new object[,]
                {
                    { 1, SeedDate, SeedDate, "Trung tâm Kỹ thuật Tiêu chuẩn Đo lường Chất lượng 3 (QUATEST 3)", "Phòng Hiệu chuẩn Cơ khí", "028-3895-9111", "info@quatest3.com.vn" },
                    { 2, SeedDate, SeedDate, "Vinacontrol Calibration", "Bộ phận Hiệu chuẩn", "024-3934-0980", "calib@vinacontrol.com.vn" },
                });

            // GAGE ROOM (gage_locations.id = 44, từ legacy import) — thêm các ngăn lưu trữ
            migrationBuilder.InsertData(
                table: "gage_slots",
                columns: new[] { "id", "created_at", "updated_at", "code", "description", "location_id" },
                values: new object[,]
                {
                    { 1, SeedDate, SeedDate, "A1", "Ngăn A1", 44 },
                    { 2, SeedDate, SeedDate, "A2", "Ngăn A2", 44 },
                    { 3, SeedDate, SeedDate, "A3", "Ngăn A3", 44 },
                    { 4, SeedDate, SeedDate, "B1", "Ngăn B1", 44 },
                    { 5, SeedDate, SeedDate, "B2", "Ngăn B2", 44 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "gage_slots", keyColumn: "id", keyValues: new object[] { 1, 2, 3, 4, 5 });
            migrationBuilder.DeleteData(table: "calib_vendors", keyColumn: "id", keyValues: new object[] { 1, 2 });
            migrationBuilder.DeleteData(table: "calib_procedures", keyColumn: "id", keyValues: new object[] { 1, 2, 3 });
        }
    }
}
