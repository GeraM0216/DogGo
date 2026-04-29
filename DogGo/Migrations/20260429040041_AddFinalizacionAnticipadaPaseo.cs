using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogGo.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalizacionAnticipadaPaseo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionRealMinutos",
                table: "Paseos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRespuestaFinalizacionAnticipada",
                table: "Paseos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSolicitudFinalizacionAnticipada",
                table: "Paseos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FinalizacionAnticipadaAprobada",
                table: "Paseos",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FinalizacionAnticipadaSolicitada",
                table: "Paseos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoFinalizacionAnticipada",
                table: "Paseos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracionRealMinutos",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "FechaRespuestaFinalizacionAnticipada",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "FechaSolicitudFinalizacionAnticipada",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "FinalizacionAnticipadaAprobada",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "FinalizacionAnticipadaSolicitada",
                table: "Paseos");

            migrationBuilder.DropColumn(
                name: "MotivoFinalizacionAnticipada",
                table: "Paseos");
        }
    }
}
