using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineGroupAndFactoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "factory_id",
                table: "machines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "machine_group_id",
                table: "machines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "serial_number",
                table: "machines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "machine_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_machine_groups", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 11,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 15,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 16,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 17,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 18,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 19,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 20,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 21,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 22,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 23,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 24,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 25,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 26,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 27,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 28,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 29,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 30,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 31,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 32,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 33,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 34,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 35,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 36,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 37,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 38,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 39,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 40,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 41,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 42,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 43,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 44,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 45,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 46,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 47,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 48,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 49,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 50,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 51,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 52,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 53,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 54,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 55,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 56,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 57,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 58,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 59,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 60,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 61,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 62,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 63,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 64,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 65,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 66,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 67,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 68,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 69,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 70,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 71,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 72,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 73,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 74,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 75,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 76,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 77,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 78,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 79,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 80,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 81,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 82,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 83,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 84,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 85,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 86,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 87,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 88,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 89,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 90,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 91,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 92,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 93,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 94,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 95,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 96,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 97,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 98,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 99,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 100,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 101,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 102,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 103,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 104,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 105,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 106,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 107,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 108,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 109,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 110,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 111,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 112,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 113,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 114,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 115,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 116,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 117,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 118,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 119,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "machines",
                keyColumn: "id",
                keyValue: 120,
                columns: new[] { "factory_id", "machine_group_id", "serial_number" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "ix_machines_machine_group_id",
                table: "machines",
                column: "machine_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_machine_groups_code",
                table: "machine_groups",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_machines_machine_groups_machine_group_id",
                table: "machines",
                column: "machine_group_id",
                principalTable: "machine_groups",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_machines_machine_groups_machine_group_id",
                table: "machines");

            migrationBuilder.DropTable(
                name: "machine_groups");

            migrationBuilder.DropIndex(
                name: "ix_machines_machine_group_id",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "factory_id",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "machine_group_id",
                table: "machines");

            migrationBuilder.DropColumn(
                name: "serial_number",
                table: "machines");
        }
    }
}
