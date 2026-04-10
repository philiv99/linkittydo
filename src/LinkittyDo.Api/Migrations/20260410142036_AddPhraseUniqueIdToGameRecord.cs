using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkittyDo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhraseUniqueIdToGameRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhraseUniqueId",
                table: "GameRecords",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 23, DateTimeKind.Utc).AddTicks(7357));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 23, DateTimeKind.Utc).AddTicks(7361));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 23, DateTimeKind.Utc).AddTicks(7362));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "ClueRetryLimit",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 22, DateTimeKind.Utc).AddTicks(8657));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "DefaultDifficulty",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 22, DateTimeKind.Utc).AddTicks(8656));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaintenanceMode",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 22, DateTimeKind.Utc).AddTicks(8658));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaxSessionTtlHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 14, 20, 36, 22, DateTimeKind.Utc).AddTicks(8653));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhraseUniqueId",
                table: "GameRecords");

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 117, DateTimeKind.Utc).AddTicks(7648));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 117, DateTimeKind.Utc).AddTicks(7652));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 117, DateTimeKind.Utc).AddTicks(7654));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "ClueRetryLimit",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 116, DateTimeKind.Utc).AddTicks(8983));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "DefaultDifficulty",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 116, DateTimeKind.Utc).AddTicks(8982));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaintenanceMode",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 116, DateTimeKind.Utc).AddTicks(8983));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaxSessionTtlHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 10, 11, 5, 23, 116, DateTimeKind.Utc).AddTicks(8979));
        }
    }
}
