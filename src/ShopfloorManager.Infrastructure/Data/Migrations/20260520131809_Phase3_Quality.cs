using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_Quality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dimensions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_op_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nominal = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    upper_tol = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    lower_tol = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "mm"),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dimensions", x => x.id);
                    table.ForeignKey(
                        name: "fk_dimensions_part_ops_part_op_id",
                        column: x => x.part_op_id,
                        principalTable: "part_ops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ncrs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ncr_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    part_op_id = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    raised_by = table.Column<int>(type: "integer", nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_by = table.Column<int>(type: "integer", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ncrs", x => x.id);
                    table.ForeignKey(
                        name: "fk_ncrs_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ncrs_part_ops_part_op_id",
                        column: x => x.part_op_id,
                        principalTable: "part_ops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ncrs_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ncrs_users_closed_by",
                        column: x => x.closed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ncrs_users_raised_by",
                        column: x => x.raised_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "measure_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dimension_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    result = table.Column<int>(type: "integer", nullable: false),
                    measured_by = table.Column<int>(type: "integer", nullable: true),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_measure_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_measure_values_dimensions_dimension_id",
                        column: x => x.dimension_id,
                        principalTable: "dimensions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_measure_values_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_measure_values_users_measured_by",
                        column: x => x.measured_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ncr_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ncr_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    action_by = table.Column<int>(type: "integer", nullable: false),
                    action_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ncr_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_ncr_logs_ncrs_ncr_id",
                        column: x => x.ncr_id,
                        principalTable: "ncrs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ncr_logs_users_action_by",
                        column: x => x.action_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dimensions_part_op_id_code",
                table: "dimensions",
                columns: new[] { "part_op_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_dimension_id_product_id",
                table: "measure_values",
                columns: new[] { "dimension_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_measured_by",
                table: "measure_values",
                column: "measured_by");

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_product_id",
                table: "measure_values",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_ncr_logs_action_by",
                table: "ncr_logs",
                column: "action_by");

            migrationBuilder.CreateIndex(
                name: "ix_ncr_logs_ncr_id",
                table: "ncr_logs",
                column: "ncr_id");

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_closed_by",
                table: "ncrs",
                column: "closed_by");

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_job_id",
                table: "ncrs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_ncr_number",
                table: "ncrs",
                column: "ncr_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_part_op_id",
                table: "ncrs",
                column: "part_op_id");

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_product_id",
                table: "ncrs",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_raised_by",
                table: "ncrs",
                column: "raised_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "measure_values");

            migrationBuilder.DropTable(
                name: "ncr_logs");

            migrationBuilder.DropTable(
                name: "dimensions");

            migrationBuilder.DropTable(
                name: "ncrs");
        }
    }
}
