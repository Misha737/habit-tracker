using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Core.Infrastructure.Migrations
{
    public partial class AddOwnerUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_habits_owner_name", table: "habits");
            migrationBuilder.DropColumn(name: "owner_id", table: "habits");

            migrationBuilder.AddColumn<Guid>(
                name:         "owner_user_id",
                table:        "habits",
                type:         "uuid",
                nullable:     false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name:    "ix_habits_owner_name",
                table:   "habits",
                columns: new[] { "owner_user_id", "name" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_habits_owner_name", table: "habits");
            migrationBuilder.DropColumn(name: "owner_user_id", table: "habits");

            migrationBuilder.AddColumn<string>(
                name:         "owner_id",
                table:        "habits",
                type:         "character varying(200)",
                maxLength:    200,
                nullable:     false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name:    "ix_habits_owner_name",
                table:   "habits",
                columns: new[] { "owner_id", "name" });
        }
    }
}
