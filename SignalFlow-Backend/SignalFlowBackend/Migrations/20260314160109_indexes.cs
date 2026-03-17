using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlowBackend.Migrations
{
    /// <inheritdoc />
    public partial class indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_participants_user_id",
                table: "participants");

            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_id",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participants_user_id_conversation_id",
                table: "participants",
                columns: new[] { "user_id", "conversation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id_sent_time",
                table: "messages",
                columns: new[] { "conversation_id", "sent_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_participants_user_id_conversation_id",
                table: "participants");

            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_id_sent_time",
                table: "messages");

            migrationBuilder.CreateIndex(
                name: "ix_participants_user_id",
                table: "participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id",
                table: "messages",
                column: "conversation_id");
        }
    }
}
