using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGageAndCalibration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calib_procedures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    revision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rev_date = table.Column<DateOnly>(type: "date", nullable: true),
                    doc_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_latest = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calib_procedures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calib_vendors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calib_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gage_locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gage_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gage_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    default_procedure_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gage_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_gage_types_calib_procedures_default_procedure_id",
                        column: x => x.default_procedure_id,
                        principalTable: "calib_procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gage_types_dimension_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "dimension_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gage_slots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gage_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_gage_slots_gage_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "gage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gage_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    serial_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    measuring_range = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    accuracy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "mm"),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calib_frequency_days = table.Column<int>(type: "integer", nullable: true),
                    last_calibration = table.Column<DateOnly>(type: "date", nullable: true),
                    in_service_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "VALID"),
                    gage_type_id = table.Column<int>(type: "integer", nullable: true),
                    default_location_id = table.Column<int>(type: "integer", nullable: true),
                    default_slot_id = table.Column<int>(type: "integer", nullable: true),
                    current_location_id = table.Column<int>(type: "integer", nullable: true),
                    current_slot_id = table.Column<int>(type: "integer", nullable: true),
                    vendor_id = table.Column<int>(type: "integer", nullable: true),
                    is_borrowed = table.Column<bool>(type: "boolean", nullable: false),
                    has_pending_calib = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gages", x => x.id);
                    table.ForeignKey(
                        name: "fk_gages_calib_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "calib_vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gages_gage_locations_current_location_id",
                        column: x => x.current_location_id,
                        principalTable: "gage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gages_gage_locations_default_location_id",
                        column: x => x.default_location_id,
                        principalTable: "gage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gages_gage_slots_current_slot_id",
                        column: x => x.current_slot_id,
                        principalTable: "gage_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gages_gage_slots_default_slot_id",
                        column: x => x.default_slot_id,
                        principalTable: "gage_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gages_gage_types_gage_type_id",
                        column: x => x.gage_type_id,
                        principalTable: "gage_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "borrow_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gage_id = table.Column<int>(type: "integer", nullable: false),
                    borrower_id = table.Column<int>(type: "integer", nullable: false),
                    manager_id = table.Column<int>(type: "integer", nullable: false),
                    borrow_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    from_location_id = table.Column<int>(type: "integer", nullable: true),
                    from_slot_id = table.Column<int>(type: "integer", nullable: true),
                    use_location_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_borrow_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_borrow_transactions_gage_locations_from_location_id",
                        column: x => x.from_location_id,
                        principalTable: "gage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_borrow_transactions_gage_locations_use_location_id",
                        column: x => x.use_location_id,
                        principalTable: "gage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_borrow_transactions_gages_gage_id",
                        column: x => x.gage_id,
                        principalTable: "gages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_borrow_transactions_users_borrower_id",
                        column: x => x.borrower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_borrow_transactions_users_manager_id",
                        column: x => x.manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calib_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gage_id = table.Column<int>(type: "integer", nullable: false),
                    vendor_id = table.Column<int>(type: "integer", nullable: true),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calib_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_calib_requests_calib_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "calib_vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_calib_requests_gages_gage_id",
                        column: x => x.gage_id,
                        principalTable: "gages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calib_requests_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calib_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    calib_request_id = table.Column<int>(type: "integer", nullable: false),
                    procedure_id = table.Column<int>(type: "integer", nullable: true),
                    calibrated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calibration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    as_found_conditions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    adjustment_made = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    temperature = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    humidity = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calib_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_calib_records_calib_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalTable: "calib_procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_calib_records_calib_requests_calib_request_id",
                        column: x => x.calib_request_id,
                        principalTable: "calib_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calib_records_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_borrow_transactions_borrower_id",
                table: "borrow_transactions",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "ix_borrow_transactions_from_location_id",
                table: "borrow_transactions",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_borrow_transactions_gage_id",
                table: "borrow_transactions",
                column: "gage_id");

            migrationBuilder.CreateIndex(
                name: "ix_borrow_transactions_manager_id",
                table: "borrow_transactions",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_borrow_transactions_use_location_id",
                table: "borrow_transactions",
                column: "use_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_calib_records_calib_request_id",
                table: "calib_records",
                column: "calib_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calib_records_created_by",
                table: "calib_records",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_calib_records_procedure_id",
                table: "calib_records",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_calib_requests_created_by",
                table: "calib_requests",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_calib_requests_gage_id",
                table: "calib_requests",
                column: "gage_id");

            migrationBuilder.CreateIndex(
                name: "ix_calib_requests_vendor_id",
                table: "calib_requests",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_gage_locations_code",
                table: "gage_locations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gage_slots_location_id",
                table: "gage_slots",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_gage_types_category_id",
                table: "gage_types",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_gage_types_code",
                table: "gage_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gage_types_default_procedure_id",
                table: "gage_types",
                column: "default_procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_current_location_id",
                table: "gages",
                column: "current_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_current_slot_id",
                table: "gages",
                column: "current_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_default_location_id",
                table: "gages",
                column: "default_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_default_slot_id",
                table: "gages",
                column: "default_slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_gage_no",
                table: "gages",
                column: "gage_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gages_gage_type_id",
                table: "gages",
                column: "gage_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_gages_vendor_id",
                table: "gages",
                column: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "borrow_transactions");

            migrationBuilder.DropTable(
                name: "calib_records");

            migrationBuilder.DropTable(
                name: "calib_requests");

            migrationBuilder.DropTable(
                name: "gages");

            migrationBuilder.DropTable(
                name: "calib_vendors");

            migrationBuilder.DropTable(
                name: "gage_slots");

            migrationBuilder.DropTable(
                name: "gage_types");

            migrationBuilder.DropTable(
                name: "gage_locations");

            migrationBuilder.DropTable(
                name: "calib_procedures");
        }
    }
}
