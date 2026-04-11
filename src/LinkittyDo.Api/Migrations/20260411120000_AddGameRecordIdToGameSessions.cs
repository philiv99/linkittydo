using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkittyDo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRecordIdToGameSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameRecordId",
                table: "GameSessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameRecordId",
                table: "GameSessions");
        }
    }
}
