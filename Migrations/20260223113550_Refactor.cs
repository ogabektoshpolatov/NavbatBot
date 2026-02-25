using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace bot.Migrations
{
    /// <inheritdoc />
    public partial class Refactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramGroupId = table.Column<long>(type: "bigint", nullable: true),
                    SendToGroup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    InviteToken = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InviteIsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    NotifyIntervalDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    NotifyTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmationHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 24)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "TaskUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    QueuePosition = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPendingConfirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UserQueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PendingConfirmationSince = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalServedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RejectionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskUsers_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_InviteToken",
                table: "Tasks",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskUsers_TaskId_QueuePosition",
                table: "TaskUsers",
                columns: new[] { "TaskId", "QueuePosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskUsers_UserId",
                table: "TaskUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserId",
                table: "Users",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskUsers");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
