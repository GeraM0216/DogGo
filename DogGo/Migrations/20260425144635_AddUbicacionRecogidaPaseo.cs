using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    /// <inheritdoc />
    public partial class AddUbicacionRecogidaPaseo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DireccionRecogida",
                table: "Paseos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "LatitudRecogida",
                table: "Paseos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LongitudRecogida",
                table: "Paseos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciasRecogida",
                table: "Paseos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZonaRecogida",
                table: "Paseos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DireccionRecogida",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "LatitudRecogida",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "LongitudRecogida",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "ReferenciasRecogida",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "ZonaRecogida",
                table: "Paseos");
        }
    }
}
