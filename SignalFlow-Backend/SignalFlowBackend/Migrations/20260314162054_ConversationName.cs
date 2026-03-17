using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlowBackend.Migrations
{
    /// <inheritdoc />
    public partial class ConversationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "conversations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_name",
                table: "conversations",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_conversations_name",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "name",
                table: "conversations");
        }
    }
}
