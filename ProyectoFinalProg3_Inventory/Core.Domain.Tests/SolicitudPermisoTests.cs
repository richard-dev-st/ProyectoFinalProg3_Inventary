using System;
using Core.Domain.Entities;
using Core.Domain.Entities.Enums;

namespace Core.Domain.Tests
{
    public class SolicitudPermisoTests
    {
        [Fact]
        public void RF_GP_03_Aprobar_SolicitudPendienteDeOtroUsuario_LaDejaAplicadaYRegistraQuienYCuando()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var admin = Guid.NewGuid();
            //Declarar la fecha de resolucion.
            var fecha = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");

            //Actuar
            solicitud.Aprobar(admin, fecha);

            //Verificar
            Assert.Equal(EstadoSolicitudPermiso.Aplicada, solicitud.Estado);
            Assert.Equal(admin, solicitud.ResueltoPorUsuarioId);
            Assert.Equal(fecha, solicitud.FechaResolucion);
        }

        [Fact]
        public void RF_GP_04_Rechazar_SolicitudPendienteConMotivo_LaDejaRechazadaYRegistraElMotivo()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var motivo = "No cumple con los requisitos.";
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");

            //Actuar
            solicitud.Rechazar(admin, motivo, DateTime.UtcNow);

            //Verificar
            Assert.Equal(EstadoSolicitudPermiso.Rechazada, solicitud.Estado);
            Assert.Equal(admin, solicitud.ResueltoPorUsuarioId);
            Assert.Equal(motivo, solicitud.MotivoRechazo);
        }

        [Fact]
        public void RF_GP_06_Aprobar_SolicitudYaAplicada_LanzaYElEstadoNoCambia()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var fecha = DateTime.UtcNow;
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");
            solicitud.Aprobar(admin, fecha);

            //Actuar
            var exception = Assert.Throws<InvalidOperationException>(() => solicitud.Aprobar(admin, fecha));

            //Verificar
            Assert.Contains("Transición inválida", exception.Message);
            Assert.Equal(EstadoSolicitudPermiso.Aplicada, solicitud.Estado);
        }

        [Fact]
        public void RF_GP_06_Aprobar_SolicitudRechazada_LanzaYElEstadoNoCambia()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var motivo = "No cumple con los requisitos.";
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");
            solicitud.Rechazar(admin, motivo, DateTime.UtcNow);

            //Actuar
            var exception = Assert.Throws<InvalidOperationException>(() => solicitud.Aprobar(admin, DateTime.UtcNow));

            //Verificar
            Assert.Contains("Transición inválida", exception.Message);
            Assert.Equal(EstadoSolicitudPermiso.Rechazada, solicitud.Estado);
        }

        [Fact]
        public void RF_GP_07_Aprobar_SolicitudCreadaPorElMismoAdministrador_LanzaYElEstadoNoCambia()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var fecha = DateTime.UtcNow;
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");

            //Actuar
            var exception = Assert.Throws<InvalidOperationException>(() => solicitud.Aprobar(solicitante, fecha));

            //Verificar
            Assert.Equal("El solicitante no puede aprobar su propia solicitud.", exception.Message);
            Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
        }

        [Fact]
        public void RF_GP_04_Rechazar_SolicitudSinMotivo_LanzaYElEstadoNoCambia()
        {
            //Preparar
            var solicitante = Guid.NewGuid();
            var admin = Guid.NewGuid();
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");

            //Actuar
            var exception = Assert.Throws<InvalidOperationException>(() => solicitud.Rechazar(admin, "", DateTime.UtcNow));

            //Verificar
            Assert.Equal("El motivo de rechazo es obligatorio.", exception.Message);
            Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
        }

        [Fact]
        public void RF_GP_01_CrearSolicitud_NaceEnEstadoPendiente()
        {
            //Preparar
            var solicitante = Guid.NewGuid();

            //Actuar
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Necesito permiso para salir temprano.");

            //Verificar
            Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitud.Estado);
        }

        [Fact]
        public void RF_GP_01_CrearSolicitud_GuardarElPermisoPedido()
        {
            //Preparar
            var solicitante = Guid.NewGuid();

            //Actuar
            var solicitud = new SolicitudPermiso(solicitante, "exportar-inventario", "Para cerrar el mes.");

            //Verificar
            Assert.Equal("exportar-inventario", solicitud.Permiso);
        }

        [Fact]
        public void RF_GP_01_CrearSolicitud_SinPermiso_Lanza()
        {
            //Preparar
            var solicitante = Guid.NewGuid();

            //Actuar
            var exception = Assert.Throws<InvalidOperationException>(() => new SolicitudPermiso(solicitante, "", "Descripción."));

            //Verificar
            Assert.Equal("El permiso solicitado es obligatorio.", exception.Message);
        }
    }
}
