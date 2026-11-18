using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReintentosColaCorreos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoIntento",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intentos",
                table: "CorreosEnCola",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UltimoError",
                table: "CorreosEnCola",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorreosEnCola_Estado_FechaCreacion",
                table: "CorreosEnCola",
                columns: new[] { "Estado", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CorreosEnCola_Estado_FechaCreacion",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "FechaUltimoIntento",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "Intentos",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "UltimoError",
                table: "CorreosEnCola");
        }
    }
}
