using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkittyDo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipTypeToClueEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationshipType",
                table: "GameEvents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 49, DateTimeKind.Utc).AddTicks(8217));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 49, DateTimeKind.Utc).AddTicks(8221));

            migrationBuilder.UpdateData(
                table: "SimulationProfiles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 49, DateTimeKind.Utc).AddTicks(8223));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "ClueRetryLimit",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 48, DateTimeKind.Utc).AddTicks(8752));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "DefaultDifficulty",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 48, DateTimeKind.Utc).AddTicks(8751));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaintenanceMode",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 48, DateTimeKind.Utc).AddTicks(8753));

            migrationBuilder.UpdateData(
                table: "SiteConfigs",
                keyColumn: "Key",
                keyValue: "MaxSessionTtlHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 4, 11, 16, 25, 41, 48, DateTimeKind.Utc).AddTicks(8748));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipType",
                table: "GameEvents");

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
    }
}
