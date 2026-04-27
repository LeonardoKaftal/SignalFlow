using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlowBackend.Migrations
{
    /// <inheritdoc />
    public partial class SenderIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_participants_sender_id",
                table: "messages");

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_id",
                table: "messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "conversation_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_participants_sender_id",
                table: "messages",
                column: "sender_id",
                principalTable: "participants",
                principalColumn: "conversation_participant_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_participants_sender_id",
                table: "messages");

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_id",
                table: "messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_conversations_conversation_id",
                table: "messages",
                column: "conversation_id",
                principalTable: "conversations",
                principalColumn: "conversation_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_participants_sender_id",
                table: "messages",
                column: "sender_id",
                principalTable: "participants",
                principalColumn: "conversation_participant_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
