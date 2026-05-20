using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_ProductionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    folder = table.Column<string>(type: "text", nullable: true),
                    is_gcode = table.Column<bool>(type: "boolean", nullable: false),
                    is_segment = table.Column<bool>(type: "boolean", nullable: false),
                    requires_job_number = table.Column<bool>(type: "boolean", nullable: false),
                    requires_part_number = table.Column<bool>(type: "boolean", nullable: false),
                    requires_op_number = table.Column<bool>(type: "boolean", nullable: false),
                    requires_revision = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "op_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_op_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    revision = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    routing_revision = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    confirmed_by = table.Column<int>(type: "integer", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "po_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    po_number = table.Column<string>(type: "text", nullable: true),
                    po_line_number = table.Column<string>(type: "text", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_po_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    run_qty = table.Column<int>(type: "integer", nullable: true),
                    ship_by = table.Column<DateOnly>(type: "date", nullable: true),
                    part_id = table.Column<int>(type: "integer", nullable: false),
                    po_line_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_jobs_parts_part_id",
                        column: x => x.part_id,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_jobs_po_lines_po_line_id",
                        column: x => x.po_line_id,
                        principalTable: "po_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "part_ops",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    op_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    op_number_sort = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    part_id = table.Column<int>(type: "integer", nullable: true),
                    op_type_id = table.Column<int>(type: "integer", nullable: true),
                    job_id = table.Column<int>(type: "integer", nullable: true),
                    is_for_job_only = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    setup_time = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    prod_time = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    completed_by = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_part_ops", x => x.id);
                    table.ForeignKey(
                        name: "fk_part_ops_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_part_ops_op_types_op_type_id",
                        column: x => x.op_type_id,
                        principalTable: "op_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_part_ops_parts_part_id",
                        column: x => x.part_id,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    serial_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tech_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_type_id = table.Column<int>(type: "integer", nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: true),
                    part_id = table.Column<int>(type: "integer", nullable: true),
                    part_op_id = table.Column<int>(type: "integer", nullable: true),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    inspector_id = table.Column<int>(type: "integer", nullable: true),
                    inspected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inspect_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tech_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_tech_documents_file_types_file_type_id",
                        column: x => x.file_type_id,
                        principalTable: "file_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tech_documents_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tech_documents_part_ops_part_op_id",
                        column: x => x.part_op_id,
                        principalTable: "part_ops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tech_documents_parts_part_id",
                        column: x => x.part_id,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tech_documents_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tech_documents_users_inspector_id",
                        column: x => x.inspector_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "file_types",
                columns: new[] { "id", "code", "folder", "is_gcode", "is_segment", "name", "requires_job_number", "requires_op_number", "requires_part_number", "requires_revision", "sort_order" },
                values: new object[,]
                {
                    { 1, "DRAWING", "drawings", false, false, "Drawing", true, false, true, false, 1 },
                    { 2, "GCODE", "gcodes", true, false, "G-code Program", true, true, true, false, 2 },
                    { 3, "ROUTECARD", "routecards", false, false, "Route Card", true, true, true, false, 3 },
                    { 4, "FIXTURE", "fixtures", false, false, "Fixture Drawing", true, true, true, false, 4 },
                    { 5, "SETUP", "setups", false, false, "Setup Sheet", true, true, true, false, 5 }
                });

            migrationBuilder.InsertData(
                table: "op_types",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { 1, "CNC", "CNC milling/turning", "CNC Machining" },
                    { 2, "INSP", "Quality inspection", "Inspection" },
                    { 3, "GRIND", "Surface/cylindrical grinding", "Grinding" },
                    { 4, "WIRE", "Wire electrical discharge", "Wire EDM" },
                    { 5, "MILL", "Manual milling", "Milling" },
                    { 6, "TURN", "Manual turning", "Turning" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_job_number",
                table: "jobs",
                column: "job_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_part_id",
                table: "jobs",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_po_line_id",
                table: "jobs",
                column: "po_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_job_id",
                table: "part_ops",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_op_type_id",
                table: "part_ops",
                column: "op_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_part_id",
                table: "part_ops",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "ix_parts_part_number_revision",
                table: "parts",
                columns: new[] { "part_number", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_job_id",
                table: "products",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_serial_number_job_id",
                table: "products",
                columns: new[] { "serial_number", "job_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_created_by",
                table: "tech_documents",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_file_type_id",
                table: "tech_documents",
                column: "file_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_inspector_id",
                table: "tech_documents",
                column: "inspector_id");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_job_id",
                table: "tech_documents",
                column: "job_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_part_id",
                table: "tech_documents",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_part_op_id",
                table: "tech_documents",
                column: "part_op_id",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "tech_documents");

            migrationBuilder.DropTable(
                name: "file_types");

            migrationBuilder.DropTable(
                name: "part_ops");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "op_types");

            migrationBuilder.DropTable(
                name: "parts");

            migrationBuilder.DropTable(
                name: "po_lines");
        }
    }
}
