using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlowBackend.Migrations
{
    /// <inheritdoc />
    public partial class ConversationParticipantRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "participants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "participants");
        }
    }
}
