using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Migrations
{
    public partial class InitialNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    event_id       = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    core_item_id   = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id  = table.Column<Guid>(type: "uuid", nullable: false),
                    summary        = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload        = table.Column<string>(type: "text", nullable: false),
                    created_at     = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_notifications", x => x.event_id));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DropTable(name: "notifications");
    }
}
