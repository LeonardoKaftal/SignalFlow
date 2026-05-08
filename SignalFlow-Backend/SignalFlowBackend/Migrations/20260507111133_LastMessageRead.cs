using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalFlowBackend.Migrations
{
    /// <inheritdoc />
    public partial class LastMessageRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_access",
                table: "participants");

            migrationBuilder.AddColumn<Guid>(
                name: "last_message_read",
                table: "participants",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_message_read",
                table: "participants");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_access",
                table: "participants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
