using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    public partial class AddEmailConfirmationToUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmado",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CodigoConfirmacion",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoExpiraEn",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoConfirmacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoExpiraEn",
                table: "Usuarios");
        }
    }
}