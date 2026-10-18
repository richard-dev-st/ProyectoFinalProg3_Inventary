using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSolicitudesPermiso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "OrdenesCompra",
                type: "int",
                nullable: false,
                defaultValue: 1); //EstadoOrden.Borrador = 1 (0 no es un valor valido del enum)

            migrationBuilder.CreateTable(
                name: "SolicitudesPermiso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SolicitanteUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Permiso = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResueltoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermiso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_ResueltoPorUsuarioId",
                        column: x => x.ResueltoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesPermiso_Usuarios_SolicitanteUsuarioId",
                        column: x => x.SolicitanteUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermisosUsuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permiso = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConcedidoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermisosUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermisosUsuario_SolicitudesPermiso_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "SolicitudesPermiso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermisosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermisosUsuario_SolicitudId",
                table: "PermisosUsuario",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_PermisosUsuario_UsuarioId",
                table: "PermisosUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_ResueltoPorUsuarioId",
                table: "SolicitudesPermiso",
                column: "ResueltoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermiso_SolicitanteUsuarioId",
                table: "SolicitudesPermiso",
                column: "SolicitanteUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermisosUsuario");

            migrationBuilder.DropTable(
                name: "SolicitudesPermiso");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "OrdenesCompra");
        }
    }
}
