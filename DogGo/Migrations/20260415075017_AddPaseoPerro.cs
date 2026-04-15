using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    /// <inheritdoc />
    public partial class AddPaseoPerro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaseoPerros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PaseoId = table.Column<int>(type: "int", nullable: false),
                    PerroId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaseoPerros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaseoPerros_Paseos_PaseoId",
                        column: x => x.PaseoId,
                        principalTable: "Paseos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaseoPerros_Perros_PerroId",
                        column: x => x.PerroId,
                        principalTable: "Perros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PaseoPerros_PaseoId_PerroId",
                table: "PaseoPerros",
                columns: new[] { "PaseoId", "PerroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaseoPerros_PerroId",
                table: "PaseoPerros",
                column: "PerroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaseoPerros");
        }
    }
}
