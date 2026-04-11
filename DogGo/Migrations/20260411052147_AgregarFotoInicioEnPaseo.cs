using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFotoInicioEnPaseo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoInicioUrl",
                table: "Paseos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoInicioUrl",
                table: "Paseos");
        }
    }
}
