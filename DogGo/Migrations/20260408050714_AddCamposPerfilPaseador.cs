using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposPerfilPaseador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperienciaAnios",
                table: "Paseadores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Paseadores",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZonaServicio",
                table: "Paseadores",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExperienciaAnios",
                table: "Paseadores");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Paseadores");

            migrationBuilder.DropColumn(
                name: "ZonaServicio",
                table: "Paseadores");
        }
    }
}
