using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bot.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsCurrent_Column_ToTaskUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "TaskUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "TaskUsers");
        }
    }
}
