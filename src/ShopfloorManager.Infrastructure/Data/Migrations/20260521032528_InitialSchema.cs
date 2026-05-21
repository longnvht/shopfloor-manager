using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dimension_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dimension_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    folder = table.Column<string>(type: "text", nullable: true),
                    is_segment = table.Column<bool>(type: "boolean", nullable: false),
                    is_gcode = table.Column<bool>(type: "boolean", nullable: false),
                    is_part_number = table.Column<bool>(type: "boolean", nullable: false),
                    is_revision = table.Column<bool>(type: "boolean", nullable: false),
                    is_op_number = table.Column<bool>(type: "boolean", nullable: false),
                    is_job_number = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_menus_menus_parent_id",
                        column: x => x.parent_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ncr_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ncr_reasons", x => x.id);
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
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
                name: "positions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    can_enter_value = table.Column<bool>(type: "boolean", nullable: false),
                    can_raise_ncr = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_working = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "part_revs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_id = table.Column<int>(type: "integer", nullable: false),
                    rev_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_released = table.Column<bool>(type: "boolean", nullable: false),
                    released_by = table.Column<int>(type: "integer", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_part_revs", x => x.id);
                    table.ForeignKey(
                        name: "fk_part_revs_parts_part_id",
                        column: x => x.part_id,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_menus",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    menu_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_menus", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_menus_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_menus_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_type_id = table.Column<int>(type: "integer", nullable: true),
                    position_id = table.Column<int>(type: "integer", nullable: true),
                    work_status_id = table.Column<int>(type: "integer", nullable: true),
                    role_id = table.Column<int>(type: "integer", nullable: true),
                    mes_role_id = table.Column<int>(type: "integer", nullable: true),
                    first_login = table.Column<bool>(type: "boolean", nullable: false),
                    reset_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_user_types_user_type_id",
                        column: x => x.user_type_id,
                        principalTable: "user_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_work_statuses_work_status_id",
                        column: x => x.work_status_id,
                        principalTable: "work_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "routings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_rev_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routings", x => x.id);
                    table.ForeignKey(
                        name: "fk_routings_part_revs_part_rev_id",
                        column: x => x.part_rev_id,
                        principalTable: "part_revs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    machine_id = table.Column<int>(type: "integer", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    logged_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    logged_out_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "routing_revs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    routing_id = table.Column<int>(type: "integer", nullable: false),
                    rev_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    change_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_released = table.Column<bool>(type: "boolean", nullable: false),
                    released_by = table.Column<int>(type: "integer", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routing_revs", x => x.id);
                    table.ForeignKey(
                        name: "fk_routing_revs_routings_routing_id",
                        column: x => x.routing_id,
                        principalTable: "routings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    part_rev_id = table.Column<int>(type: "integer", nullable: false),
                    routing_rev_id = table.Column<int>(type: "integer", nullable: false),
                    po_line_id = table.Column<int>(type: "integer", nullable: true),
                    run_qty = table.Column<int>(type: "integer", nullable: true),
                    ship_by = table.Column<DateOnly>(type: "date", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_jobs_part_revs_part_rev_id",
                        column: x => x.part_rev_id,
                        principalTable: "part_revs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_jobs_po_lines_po_line_id",
                        column: x => x.po_line_id,
                        principalTable: "po_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_jobs_routing_revs_routing_rev_id",
                        column: x => x.routing_rev_id,
                        principalTable: "routing_revs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "part_ops",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    routing_rev_id = table.Column<int>(type: "integer", nullable: true),
                    job_id = table.Column<int>(type: "integer", nullable: true),
                    for_job_only = table.Column<bool>(type: "boolean", nullable: false),
                    op_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    op_number_sort = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    op_type_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    setup_time = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    prod_time = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    completed_by = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
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
                        name: "fk_part_ops_routing_revs_routing_rev_id",
                        column: x => x.routing_rev_id,
                        principalTable: "routing_revs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "dimensions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_op_id = table.Column<int>(type: "integer", nullable: false),
                    balloon_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    balloon_sort = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nominal_value = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    tolerance_plus = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    tolerance_minus = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    min_value = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "mm"),
                    is_text_type = table.Column<bool>(type: "boolean", nullable: false),
                    nominal_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dimensions", x => x.id);
                    table.ForeignKey(
                        name: "fk_dimensions_dimension_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "dimension_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_dimensions_part_ops_part_op_id",
                        column: x => x.part_op_id,
                        principalTable: "part_ops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tech_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_type_id = table.Column<int>(type: "integer", nullable: false),
                    part_op_id = table.Column<int>(type: "integer", nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: true),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    machine_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "ncrs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ncr_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    year_code = table.Column<int>(type: "integer", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    job_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    part_op_id = table.Column<int>(type: "integer", nullable: true),
                    measure_value_id = table.Column<long>(type: "bigint", nullable: true),
                    reason_id = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    machine_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                        name: "fk_ncrs_ncr_reasons_reason_id",
                        column: x => x.reason_id,
                        principalTable: "ncr_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    part_op_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    result = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    measured_by = table.Column<int>(type: "integer", nullable: true),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    final_op_id = table.Column<int>(type: "integer", nullable: true),
                    has_ncr = table.Column<bool>(type: "boolean", nullable: false),
                    ncr_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    machine_id = table.Column<int>(type: "integer", nullable: true),
                    gage_id = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                        name: "fk_measure_values_part_ops_part_op_id",
                        column: x => x.part_op_id,
                        principalTable: "part_ops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "id", "code", "created_at", "name" },
                values: new object[,]
                {
                    { 1, "ADMIN", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Administration" },
                    { 2, "QC", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Quality Control" },
                    { 3, "PROD", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Production" },
                    { 4, "ENG", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Engineering" }
                });

            migrationBuilder.InsertData(
                table: "dimension_categories",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { 1, "LIN", "Thước cặp, panme", "Linear" },
                    { 2, "ANG", "Thước góc", "Angular" },
                    { 3, "THD", "Dưỡng ren, ring gauge", "Thread" },
                    { 4, "GEO", "CMM, dial indicator", "Geometric" },
                    { 5, "SFC", "Surface tester", "Surface" }
                });

            migrationBuilder.InsertData(
                table: "file_types",
                columns: new[] { "id", "code", "folder", "is_gcode", "is_job_number", "is_op_number", "is_part_number", "is_revision", "is_segment", "name", "sort_order" },
                values: new object[,]
                {
                    { 1, "DRW", "drawings", false, false, false, true, true, false, "Drawing", 1 },
                    { 2, "GCD", "gcodes", true, true, true, true, true, true, "G-Code", 2 },
                    { 3, "RTC", "routecards", false, true, true, true, true, false, "Route Card", 3 },
                    { 4, "FXT", "fixtures", false, true, true, true, true, false, "Fixture Drawing", 4 },
                    { 5, "THD", "threads", false, false, true, true, true, false, "Thread Drawing", 5 },
                    { 6, "TLS", "tools", false, false, true, true, true, false, "Tool List", 6 },
                    { 7, "CAM", "cam", false, false, true, true, true, false, "CAM File", 7 },
                    { 8, "CAD", "cad", false, false, false, true, true, false, "CAD Drawing", 8 }
                });

            migrationBuilder.InsertData(
                table: "ncr_reasons",
                columns: new[] { "id", "created_at", "department_id", "is_active", "name", "sort_order", "tag" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Tool wear", 1, "TOOL" },
                    { 2, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Setup error", 2, "SETUP" },
                    { 3, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Drawing error", 3, "DRW" },
                    { 4, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Wrong material", 4, "MAT" },
                    { 5, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Machine error", 5, "MACH" },
                    { 6, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "CMM error", 6, "CMM" },
                    { 7, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Other", 99, "OTHER" }
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

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Administrator" },
                    { 2, "Manager" },
                    { 3, "Engineer" },
                    { 4, "QC Inspector" },
                    { 5, "Operator" },
                    { 6, "Planner" }
                });

            migrationBuilder.InsertData(
                table: "work_statuses",
                columns: new[] { "id", "is_working", "name" },
                values: new object[,]
                {
                    { 1, true, "Active" },
                    { 2, false, "On Leave" },
                    { 3, false, "Resigned" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dimension_categories_code",
                table: "dimension_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dimensions_category_id",
                table: "dimensions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_dimensions_part_op_id_balloon_number",
                table: "dimensions",
                columns: new[] { "part_op_id", "balloon_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_job_number",
                table: "jobs",
                column: "job_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_part_rev_id",
                table: "jobs",
                column: "part_rev_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_po_line_id",
                table: "jobs",
                column: "po_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_routing_rev_id",
                table: "jobs",
                column: "routing_rev_id");

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_dimension_id_product_id_measured_at",
                table: "measure_values",
                columns: new[] { "dimension_id", "product_id", "measured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_measured_by",
                table: "measure_values",
                column: "measured_by");

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_part_op_id",
                table: "measure_values",
                column: "part_op_id");

            migrationBuilder.CreateIndex(
                name: "ix_measure_values_product_id",
                table: "measure_values",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_menus_code",
                table: "menus",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_menus_parent_id",
                table: "menus",
                column: "parent_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_ncrs_reason_id",
                table: "ncrs",
                column: "reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_job_id",
                table: "part_ops",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_op_type_id",
                table: "part_ops",
                column: "op_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_ops_routing_rev_id",
                table: "part_ops",
                column: "routing_rev_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_revs_part_id_rev_code",
                table: "part_revs",
                columns: new[] { "part_id", "rev_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parts_part_number",
                table: "parts",
                column: "part_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_job_id_serial_number",
                table: "products",
                columns: new[] { "job_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_menus_menu_id",
                table: "role_menus",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_menus_role_id_menu_id",
                table: "role_menus",
                columns: new[] { "role_id", "menu_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routing_revs_routing_id_rev_code",
                table: "routing_revs",
                columns: new[] { "routing_id", "rev_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routings_part_rev_id",
                table: "routings",
                column: "part_rev_id");

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
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_tech_documents_part_op_id",
                table: "tech_documents",
                column: "part_op_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_position_id",
                table: "users",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_user_login",
                table: "users",
                column: "user_login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_user_type_id",
                table: "users",
                column: "user_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_work_status_id",
                table: "users",
                column: "work_status_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "measure_values");

            migrationBuilder.DropTable(
                name: "ncr_logs");

            migrationBuilder.DropTable(
                name: "role_menus");

            migrationBuilder.DropTable(
                name: "tech_documents");

            migrationBuilder.DropTable(
                name: "dimensions");

            migrationBuilder.DropTable(
                name: "ncrs");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropTable(
                name: "file_types");

            migrationBuilder.DropTable(
                name: "dimension_categories");

            migrationBuilder.DropTable(
                name: "ncr_reasons");

            migrationBuilder.DropTable(
                name: "part_ops");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "op_types");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_types");

            migrationBuilder.DropTable(
                name: "work_statuses");

            migrationBuilder.DropTable(
                name: "po_lines");

            migrationBuilder.DropTable(
                name: "routing_revs");

            migrationBuilder.DropTable(
                name: "routings");

            migrationBuilder.DropTable(
                name: "part_revs");

            migrationBuilder.DropTable(
                name: "parts");
        }
    }
}
