using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopfloorManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionClaimedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_production_sessions_part_ops_part_op_id",
                table: "production_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_production_sessions_users_cancelled_by_user_id",
                table: "production_sessions");

            migrationBuilder.DropIndex(
                name: "ix_production_sessions_cancelled_by_user_id",
                table: "production_sessions");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                table: "production_sessions");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "production_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "note",
                table: "production_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "machine_code",
                table: "production_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "claimed_by",
                table: "production_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_production_sessions_cancelled_by",
                table: "production_sessions",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "ix_production_sessions_claimed_by",
                table: "production_sessions",
                column: "claimed_by");

            migrationBuilder.CreateIndex(
                name: "ix_production_sessions_machine_code",
                table: "production_sessions",
                column: "machine_code");

            migrationBuilder.CreateIndex(
                name: "ix_production_sessions_machine_code_status",
                table: "production_sessions",
                columns: new[] { "machine_code", "status" });

            migrationBuilder.AddForeignKey(
                name: "fk_production_sessions_part_ops_part_op_id",
                table: "production_sessions",
                column: "part_op_id",
                principalTable: "part_ops",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_production_sessions_users_cancelled_by",
                table: "production_sessions",
                column: "cancelled_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_production_sessions_users_claimed_by",
                table: "production_sessions",
                column: "claimed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_production_sessions_part_ops_part_op_id",
                table: "production_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_production_sessions_users_cancelled_by",
                table: "production_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_production_sessions_users_claimed_by",
                table: "production_sessions");

            migrationBuilder.DropIndex(
                name: "ix_production_sessions_cancelled_by",
                table: "production_sessions");

            migrationBuilder.DropIndex(
                name: "ix_production_sessions_claimed_by",
                table: "production_sessions");

            migrationBuilder.DropIndex(
                name: "ix_production_sessions_machine_code",
                table: "production_sessions");

            migrationBuilder.DropIndex(
                name: "ix_production_sessions_machine_code_status",
                table: "production_sessions");

            migrationBuilder.DropColumn(
                name: "claimed_by",
                table: "production_sessions");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "production_sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "note",
                table: "production_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "machine_code",
                table: "production_sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "cancelled_by_user_id",
                table: "production_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_production_sessions_cancelled_by_user_id",
                table: "production_sessions",
                column: "cancelled_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_production_sessions_part_ops_part_op_id",
                table: "production_sessions",
                column: "part_op_id",
                principalTable: "part_ops",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_production_sessions_users_cancelled_by_user_id",
                table: "production_sessions",
                column: "cancelled_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
