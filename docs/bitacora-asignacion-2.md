# Bitácora de sesión con el agente — Asignación 2

Pieza: **Gestión de permisos y su primera suite de pruebas** (RF-GP-01 a RF-GP-07 y RD-04/06/07/09/12).

Herramienta: agente de IA en la terminal (opencode) sobre el repositorio del proyecto. Yo (Richard) decidía qué delegar, revisaba cada salida y ejecutaba los comandos (`dotnet`, `git`, `sqlcmd`). El agente guiaba y, cuando se lo pedí, aplicaba cambios de código; yo revisaba y corregía.

---

## Tarea Designada — Diseñar la entidad y el enum de la pieza

**Qué le pedí:** que me propusiera la estructura de la entidad `SolicitudPermiso`, el enum de estados y dónde poner las transiciones para cumplir RD-04 (un solo punto del código).

**Qué me devolvió:** la entidad con `Estado`, `SolicitanteUsuarioId`, `Permiso`, `Descripcion`, `CreadoEn`, `ResueltoPorUsuarioId`, `FechaResolucion` y `MotivoRechazo`; el diccionario `TransicionesPermitidas` y un método privado `CambiarEstado` como único escritor del estado, con los métodos `Aprobar`/`Rechazar` por encima.

**Revisión:** lo acepté; es coherente con RD-04 y con el patrón de `OrdenCompra` de la Práctica 1 (enums como string, `InvalidOperationException`).

---

## Error del agente — Omitió el requisito RF-GP-02

**Qué le pedí:** que enumerara los requisitos de la asignación para planificar el trabajo.

**Qué me devolvió:** una lista que incluía RF-GP-01 y RF-GP-03 a RF-GP-07, **sin RF-GP-02**.

**Cómo lo detecté:** al pegarle el enunciado oficial en PDF, comparé requisito por requisito y vi que faltaba RF-GP-02 («Un Administrador consulta las solicitudes pendientes. Un usuario Estándar no puede ver las solicitudes de otros usuarios»). La lista del agente "salteaba" justo el requisito que obliga a dos comportamientos distintos (admin ve todas; estándar solo las propias).

**Cómo lo corregí:** agregué al plan el endpoint `GET /api/solicitudes/pendientes` restringido a `Administrador` y el filtrado por solicitante en `GET /api/solicitudes/mias` (el id se toma del token, no del cliente). Quedó cubierto por el servicio (`PendientesAsync`, `MiasAsync`) y con un `[Authorize(Roles = "Administrador")]` en el controller. Este es el caso que el enunciado exige documentar: no todo salió bien a la primera.

---
