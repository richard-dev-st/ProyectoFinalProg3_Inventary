using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposRecuperacionPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacion",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoRecuperacionExpiracion",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoCambioPassword",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoRecuperacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoRecuperacionExpiracion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaUltimoCambioPassword",
                table: "Usuarios");
        }
    }
}
