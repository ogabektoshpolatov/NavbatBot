using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bot.Migrations
{
    /// <inheritdoc />
    public partial class Add_QueueConfirmation_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TaskUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPendingConfirmation",
                table: "TaskUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingConfirmationSince",
                table: "TaskUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionCount",
                table: "TaskUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TaskUsers");

            migrationBuilder.DropColumn(
                name: "IsPendingConfirmation",
                table: "TaskUsers");

            migrationBuilder.DropColumn(
                name: "PendingConfirmationSince",
                table: "TaskUsers");

            migrationBuilder.DropColumn(
                name: "RejectionCount",
                table: "TaskUsers");
        }
    }
}
