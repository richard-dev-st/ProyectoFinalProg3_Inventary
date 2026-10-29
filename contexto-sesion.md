# Contexto de sesión — Práctica 3 (módulo de negocio, pruebas e integración)

**Proyecto:** `ProyectoFinalProg3_Inventary` — Sistema de inventario (Programación III, ITLA)
**Autor:** Richard José Hernández Valdez
**Repositorio:** https://github.com/richard-dev-st/ProyectoFinalProg3_Inventary
**Fecha del corte:** 29/10/2026
**Rama de trabajo principal:** `main`
**Solución única:** `ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` (.NET 10)

---

## 1. Resumen ejecutivo

La Práctica 3 está **completa y publicada**. El módulo de negocio (`OrdenCompra`) tiene una máquina de
estados de 5 estados declarada en un solo lugar, cinco operaciones separables expuestas por API,
una suite de **37 pruebas** que pasa entera sobre el tag `practica-3`, una **prueba de integración**
entre Gestión de Permisos y el módulo de negocio (con componentes reales y las dos caras: rechazo antes
de la aprobación y permiso después), y **4 defectos plantados** en tags hijos que fallan exactamente
la prueba prevista.

Estado de verificación al cierre de esta sesión:

```
Passed! - Failed: 0, Passed: 12  - Core.Domain.Tests.dll
Passed! - Failed: 0, Passed:  7  - Core.Application.Tests.dll
Passed! - Failed: 0, Passed: 18  - Bussiness.inventory.Tests.dll
                                              ── 37 en total
```

`practica-3` y `main` tienen **árbol idéntico** salvo este mismo documento de handoff, que se añade
solo en `main` y no forma parte de la entrega etiquetada (verificado con `git diff --stat practica-3 main`:
únicamente `contexto-sesion.md`). Todo está subido a `origin`, incluidos los tags (force-push de tags, ver §6).

---

## 2. Arquitectura actual

### 2.1 Capas y dependencias

```
Presentation.API  (ASP.NET Core Web API, JWT, controladores)
      │
      ▼
Infrastructure    (EF Core + SQL Server, DbContext, repositorios, migraciones)
      │
      ├──────────────► Core.Application  (servicios de caso de uso, DTOs, interfaces)
      │                        │
      ▼                        ▼
Business.Inventory  ◄──── Core.Domain  (entidades y enums compartidos)
```

| Proyecto | Responsabilidad | Referencia a |
|---|---|---|
| `Core.Domain` | Entidades transversales: `Usuario`, `SolicitudPermiso`, `PermisoUsuario`, `CorreoEnCola`, `TokenRevocado`; enums `RolUsuario`, `EstadoSolicitudPermiso`, `EstadoOrden`. Sin dependencias. | — |
| `Business.Inventory` | Entidades del negocio: `OrdenCompra`, `DetalleOrden`, `Producto`, `Proveedor`. Contiene la **máquina de estados** de `OrdenCompra`. | `Core.Domain` |
| `Core.Application` | Casos de uso (`AuthService`, `SolicitudPermisoService`, `OrdenCompraService`, `AdminUsuarioService`), DTOs e **interfaces** (`IApplicationDbContext`, `IOrdenCompraRepository`, `IOrdenCompraService`, etc.). | `Business.Inventory`, `Core.Domain` |
| `Infrastructure` | `ApplicationDbContext`, repositorio del negocio (`OrdenCompraRepository`), migraciones EF Core y servicios de infraestructura (`PasswordService`, `JwtService`, `EmailService`). | `Core.Application`, `Business.Inventory`, `Core.Domain` |
| `Presentation.API` | Controladores REST, autorización por rol, mapeo de excepciones a códigos HTTP, `Program.cs` con DI y JWT. | `Infrastructure` |
| `EmailWorker` | Worker que vacía `CorreosEnCola` contra SMTP. | `Infrastructure` |
| `Core.Domain.Tests` | 12 pruebas de `SolicitudPermiso`. | `Core.Domain` |
| `Core.Application.Tests` | 7 pruebas de `SolicitudPermisoService` con EF Core InMemory. | `Core.Application`, `Infrastructure` |
| `Bussiness.inventory.Tests` | 18 pruebas del módulo de negocio (17 de máquina de estados + 1 de integración). | `Business.Inventory`, `Infrastructure` |

> Nota de nombres: el proyecto de pruebas del negocio se llama `Bussiness.inventory.Tests`
> (con la errata histórica en `Bussiness`). No se renombró para no romper la solución ni el historial.

### 2.2 Modelo de datos (EF Core)

- `Usuarios` (`DbSet<Usuario>`): email, hash de contraseña, rol, activo, intentos fallidos, bloqueo,
  `TokenActivacion`, `CodigoRecuperacion` y expiración.
- `OrdenesCompra` / `DetallesOrden` / `Productos` / `Proveedores`: módulo de negocio.
  `OrdenCompra.Estado` es `int` con valor por defecto **1 = `Borrador`**.
- `SolicitudesPermiso` / `PermisosUsuario`: Gestión de permisos (Core). Al aprobar una solicitud se
  inserta la fila en `PermisosUsuario` (RF-GP-05).
- `CorreosEnCola`: cola de correos (activación, recuperación, avisos).
- `TokensRevocados`: soporte de `logout` (el JWT deja de servir).

Migraciones existentes (ninguna pendiente — verificado con
`dotnet ef migrations has-pending-model-changes`):

```
20260926215935_MigracionInicial
20260930003057_AgregarCamposBloqueoUsuario
20260930201107_AddTokensRevocados
20260930210838_UpdateTokenRevocadoUsuarioId
20261001215443_FixRolEnumConversion
20261002160247_AgregarCamposRecuperacionPassword
20261017222054_AgregarSolicitudesPermiso   ← añade Estado a OrdenesCompra y las tablas de permisos
```

### 2.3 Máquina de estados de `OrdenCompra` (núcleo de la Práctica 3)

Archivo: `ProyectoFinalProg3_Inventory/Business.Inventory/Entities/OrdenCompra.cs`

- `Estado` tiene `set` privado → nadie cambia el estado por fuera de la entidad.
- `TransicionesPermitidas` es un `Dictionary<EstadoOrden, List<EstadoOrden>>` **estático y privado**:
  único punto donde se declaran las transiciones (RD-04).
- Todos los métodos (`EnviarAAprobacion`, `Aprobar`, `Recibir`, `Cancelar`) delegan en
  `CambiarEstado(...)`, que lanza `InvalidOperationException` y **no modifica el estado** cuando la
  transición no está declarada (RD-07).
- `EnviarAAprobacion` valida además la **condición** de la transición: al menos un detalle y
  `ProveedorId > 0`.

| Desde | Hacia permitidas |
|---|---|
| `Borrador` | `PendienteAprobacion`, `Cancelada` |
| `PendienteAprobacion` | `Aprobada`, `Cancelada` |
| `Aprobada` | `Recibida`, `Cancelada` |
| `Recibida` | *(ninguna — terminal)* |
| `Cancelada` | *(ninguna — terminal)* |

Detalle completo (estados, tabla de permitidas, tabla de prohibidas, diagrama Mermaid):
`docs/maquina-de-estados.md`.

### 2.4 Integración negocio ↔ Gestión de Permisos

`Recibir` es la única operación del negocio que exige un **permiso concedido** además del rol:

1. El usuario crea una `SolicitudPermiso` con `Permiso = "recibir-orden"` (nace `Pendiente`).
2. Un Administrador la aprueba → se inserta en `PermisosUsuario` (RF-GP-05).
3. `OrdenCompraService.RecibirAsync(id, usuarioId)` consulta `PermisosUsuario`; si no hay fila lanza
   `UnauthorizedAccessException`; el controlador la traduce a **403**.
4. Si hay permiso, se ejecuta la transición `Aprobada → Recibida`.

**Decisión clave de esta sesión:** la verificación del permiso vivía **solo en el controlador**.
Como la prueba de integración no levanta HTTP, era imposible que detectara el defecto 4. Se movió al
servicio (`RecibirAsync(int id, Guid usuarioId)`), lo que hace la regla comprobable sin la app completa
(RD-12) y mantiene el `403` en la API.

### 2.5 API del módulo de negocio

`ProyectoFinalProg3_Inventory/Presentation.API/Controllers/OrdenesCompraController.cs`
(todas las rutas requieren JWT: `[Authorize]` a nivel de clase → RF-NEG-06)

| Método | Ruta | Roles | Cuerpo / parámetros | Respuesta |
|---|---|---|---|---|
| POST | `/api/OrdenesCompra/crear-borrador` | Estándar, Administrador | `CrearOrdenCompraDto` + `?usuarioId=` | `200 { ordenId }` |
| POST | `/api/OrdenesCompra/{id}/enviar-aprobacion` | Estándar, Administrador | — | `200` |
| POST | `/api/OrdenesCompra/{id}/aprobar` | **Administrador** | — | `200` |
| POST | `/api/OrdenesCompra/{id}/recibir` | Estándar, Administrador + permiso `recibir-orden` | — | `200` / `403` |
| POST | `/api/OrdenesCompra/{id}/cancelar` | **Administrador** | — | `200` |

Errores de transición → `400 Bad Request` con el mensaje de `InvalidOperationException`.
Falta de permiso → `403`.

---

## 3. Cumplimiento de los requisitos de la Práctica 3

### 3.1 Punto 1.1 — Módulo de negocio

| Requisito | Evidencia | Veredicto |
|---|---|---|
| **RF-NEG-02** — ≥5 funcionalidades separables | 5 endpoints independientes: crear borrador, enviar a aprobación, aprobar, recibir, cancelar (tabla §2.5) | ✅ Cumple |
| **RF-NEG-03** — Máquina de estados propia (5 estados) | `EstadoOrden` con `Borrador(1)`, `PendienteAprobacion(2)`, `Aprobada(3)`, `Recibida(4)`, `Cancelada(5)`; declarada en `OrdenCompra.TransicionesPermitidas` | ✅ Cumple |
| **RF-NEG-04** — Transiciones prohibidas rechazadas y estado intacto | `CambiarEstado` valida contra el mapa; 17 pruebas cubren permitidas, prohibidas y terminales | ✅ Cumple |
| **RF-NEG-05** — Estados terminales | `Recibida` y `Cancelada` sin salidas en el mapa; pruebas `Recibida_Recibir_...`, `Recibida_Aprobar_...`, `Cancelada_Aprobar_...`, `Cancelada_EnviarAAprobacion_...` | ✅ Cumple |
| **RF-NEG-06** — Protección por Control de Acceso | `[Authorize]` en el controlador + roles por endpoint + permiso `recibir-orden` | ⚠️ Cumple, con observación (§9.2) |
| **RF-NEG-09** — Independencia de las dos máquinas de estados | `OrdenCompra` no conoce `SolicitudPermiso`; la relación es por la tabla `PermisosUsuario`, no por transiciones compartidas | ✅ Cumple |
| **RD-04** — Transiciones en un solo lugar | Único diccionario privado `TransicionesPermitidas` en la entidad; los servicios no reimplementan la regla | ✅ Cumple |
| **RD-07** — Estado no cambia ante transición inválida | `CambiarEstado` lanza antes de asignar; las pruebas lo verifican con `Assert.Equal(estadoEsperado, orden.Estado)` | ✅ Cumple |

### 3.2 Punto 1.2 — Suite de pruebas

| Requisito | Evidencia | Veredicto |
|---|---|---|
| ≥1 prueba por **fila de la tabla de permitidas** | 8 filas (incluidas las dos terminales) → cubiertas por las 17 pruebas de `OrdenCompraTests.cs` | ✅ Cumple |
| ≥1 prueba por **fila de la tabla de prohibidas** | 8 filas de `docs/maquina-de-estados.md` → cada una tiene al menos una prueba (ver §4.1) | ✅ Cumple |
| Prueba de **estado terminal** | `Recibida_Recibir_...` (terminal real) y `Cancelada_...` | ✅ Cumple |
| **Tres fases identificables** (Arrange / Act / Assert) | Todas las pruebas llevan los comentarios `//Arrange`, `//Act`, `//Assert` | ✅ Cumple |
| Pruebas **independientes** | Cada prueba construye su propia `OrdenCompra`; no hay estado compartido ni orden de ejecución | ✅ Cumple |
| **Un único comando documentado** en README §Pruebas | `dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` | ✅ Cumple |
| Sin levantar la app completa (RD-12) | xUnit + EF Core InMemory | ✅ Cumple |

### 3.3 Punto 1.3 — Defectos plantados

| Requisito | Evidencia | Veredicto |
|---|---|---|
| 4 defectos, uno por tag | `practica-3-defecto-1..4` | ✅ Cumple |
| Cada tag es **hijo directo** de `practica-3` | Padres verificados: `2d4ffcd` en los cuatro | ✅ Cumple |
| Solo tocan **código de producción** | `git show --name-only` lista únicamente `OrdenCompra.cs` (1-3) y `OrdenCompraService.cs` (4) | ✅ Cumple |
| Mensaje de commit nombra **archivo y línea** | Ej.: `defecto-1: ... en Business.Inventory/Entities/OrdenCompra.cs linea 29` | ✅ Cumple |
| Cada defecto hace **fallar ≥1 prueba** | Verificado con `dotnet test` sobre cada tag (§6) | ✅ Cumple |
| El defecto 4 lo detecta la **prueba de integración** | `Recibir_SinPermisoAprobado_Rechaza_YConPermisoAprobado_Permite` [FAIL] | ✅ Cumple |

### 3.4 Punto 1.4 — Prueba de integración

| Requisito | Evidencia | Veredicto |
|---|---|---|
| Componentes **reales**, sin mocks/dobles | `SolicitudPermisoService`, `OrdenCompraService`, `OrdenCompraRepository` y `ApplicationDbContext` (InMemory) reales | ✅ Cumple |
| **No** levanta la app completa | No hay `WebApplicationFactory`, ni HTTP, ni servidor | ✅ Cumple |
| Verifica **las dos caras** | Cara 1: sin permiso → `UnauthorizedAccessException` y estado sigue `Aprobada`. Cara 2: tras aprobar → `Recibida` | ✅ Cumple |
| Atraviesa **ambos módulos** | Solicitud creada por Estándar, aprobada por Administrador, y esa aprobación habilita la operación del negocio | ✅ Cumple |

### 3.5 Punto 1.5 — Revisión acumulada (los cinco frentes)

| Frente | Documentado en README | Veredicto |
|---|---|---|
| 1. Registro con activación por correo | §«Frente 1» (incluye plan B sin SMTP leyendo `Usuarios.TokenActivacion`) | ✅ Documentado |
| 2. Inicio y cierre de sesión | §«Frente 2» (`login` → `me` → `logout` → token revocado da `401`) | ✅ Documentado |
| 3. Estándar no ejecuta operaciones de Administrador | §«Frente 3» (`403` en `pendientes`, cambio de rol y forzar restablecimiento) | ✅ Documentado |
| 4. Recuperación de contraseña | §«Frente 4» (código de 6 dígitos, 15 min, plan B por SQL) | ✅ Documentado |
| 5. Gestión de permisos completa | §«Frente 5» (remite a RF-GP-01..07, ya detallados) | ✅ Documentado |
| Integración negocio ↔ permisos | §«Integración negocio ↔ permisos (punto 1.4)» con el recorrido de 5 pasos | ✅ Documentado |

> La revisión 1.5 es **manual del docente**: el README ya describe el recorrido exacto y cómo obtener
> tokens/códigos sin SMTP, así que no hay nada que adivinar al arrancar el proyecto.

### 3.6 Requisitos de entrega / README

| Requisito | Evidencia | Veredicto |
|---|---|---|
| Sección **«Pruebas»** con un solo comando | README líneas ~197-213 | ✅ Cumple |
| Sección **«Defectos»** que mapea cada defecto a su prueba | README §«Defectos plantados (Práctica 3)», tabla de 4 filas | ✅ Cumple |
| **Cómo provocar cada funcionalidad** | README §«Cómo provocar cada funcionalidad del Módulo de Negocio» (5 pasos + transición prohibida) | ✅ Cumple |
| Documentación de la máquina de estados | `docs/maquina-de-estados.md` (estados, métodos, permitidas, prohibidas, RD-07, diagrama) | ✅ Cumple |
| Conteos del README coherentes con la suite | 12 + 7 + 18 = `Passed: 37` | ✅ Cumple |

**Veredicto global: la Práctica 3 cumple todos los requisitos verificables en el repositorio.**
Lo único que no puede certificar un agente es la revisión manual 1.5 (el docente ejecuta los recorridos);
el README la deja preparada de principio a fin.

---

## 4. Detalle de la suite de pruebas

### 4.1 `Bussiness.inventory.Tests/OrdenCompraTests.cs` — 17 pruebas

| # | Prueba | Qué cubre |
|---|---|---|
| 1 | `Borrador_EnviarAAprobacion_PasaAPendienteAprobacion` | Permitida `Borrador → PendienteAprobacion` |
| 2 | `PendienteAprobacion_Aprobar_PasaAAprobada` | Permitida `PendienteAprobacion → Aprobada` |
| 3 | `Aprobada_Recibir_PasaARecibida` | Permitida `Aprobada → Recibida` |
| 4 | `Borrador_Cancelar_PasaACancelada` | Permitida `Borrador → Cancelada` |
| 5 | `PendienteAprobacion_Cancelar_PasaACancelada` | Permitida `PendienteAprobacion → Cancelada` |
| 6 | `Aprobada_Cancelar_PasaACancelada` | Permitida `Aprobada → Cancelada` |
| 7 | `Borrador_Aprobar_LanzaInvalidOperationException_YEstaNoCambia` | Prohibida `Borrador → Aprobada` |
| 8 | `Borrador_Recibir_LanzaInvalidOperationException_YElEstadoNoCambia` | Prohibida `Borrador → Recibida` |
| 9 | `PendienteAprobacion_Recibir_LanzaInvalidOperationException_YElEstadoNoCambia` | Prohibida `PendienteAprobacion → Recibida` |
| 10 | `PendienteAprobacion_EnviarAAprobacion_...` | Prohibida `PendienteAprobacion → PendienteAprobacion` (auto-transición) |
| 11 | `Aprobada_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia` | Prohibida `Aprobada → Aprobada` (auto-transición) |
| 12 | `Aprobada_EnviarAAprobacion_LanzaInvalidOperationException_YElEstadoNoCambia` | Prohibida `Aprobada → PendienteAprobacion` (retroceso) |
| 13 | `Recibida_Recibir_LanzaInvalidOperationException_YEstaNoCambia` | **Estado terminal** `Recibida` |
| 14 | `Recibida_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia` | Terminal `Recibida` (otra salida) |
| 15 | `Cancelada_Aprobar_LanzaInvalidOperationException_YElEstadoNoCambia` | **Estado terminal** `Cancelada` |
| 16 | `Cancelada_EnviarAAprobacion_...` | Terminal `Cancelada` (otra salida) |
| 17 | `Borrador_EnviarAAprobacion_SinDetalles_...` | **Condición** de la transición: sin detalles se rechaza |

### 4.2 `Bussiness.inventory.Tests/OrdenCompraIntegracionTests.cs` — 1 prueba

`Recibir_SinPermisoAprobado_Rechaza_YConPermisoAprobado_Permite`

1. **Arrange:** `ApplicationDbContext` InMemory; dos `Usuario` reales (Estándar y Administrador) con
   `Id`, `Rol` y `Activo` ajustados por reflexión (los setters son privados); una `OrdenCompra` real
   llevada a `Aprobada`; se instancian `OrdenCompraRepository`, `OrdenCompraService` y
   `SolicitudPermisoService` reales.
2. **Act/Assert cara 1:** `RecibirAsync` sin permiso lanza `UnauthorizedAccessException` y la orden
   sigue `Aprobada`.
3. **Act:** el Estándar crea la solicitud `recibir-orden`; se comprueba que **todavía no** hay fila en
   `PermisosUsuario`; el Administrador la aprueba y se comprueba que **ya** existe.
4. **Act/Assert cara 2:** `RecibirAsync` ahora termina y la orden queda en `Recibida`.

### 4.3 `Core.Domain.Tests` (12) y `Core.Application.Tests` (7)

Cubren creación, filtrado por usuario, transiciones válidas y prohibidas de `SolicitudPermiso`, el
registro del permiso al aprobar y la persistencia con EF Core InMemory (RF-GP-01..07, RD-04/06/07/09/12).

---

## 5. Defectos plantados (estado final verificado)

| Tag | Commit | Padre | Archivo y línea | Prueba que falla | Comportamiento introducido |
|---|---|---|---|---|---|
| `practica-3-defecto-1` | `3446b00` | `2d4ffcd` | `Business.Inventory/Entities/OrdenCompra.cs` L29 | `Borrador_Aprobar_LanzaInvalidOperationException_YEstaNoCambia` | Añade `Aprobada` a las salidas de `Borrador` (salto de estado permitido) |
| `practica-3-defecto-2` | `4dbfe20` | `2d4ffcd` | `Business.Inventory/Entities/OrdenCompra.cs` L32 | `Recibida_Recibir_LanzaInvalidOperationException_YEstaNoCambia` | Añade `Recibida` a las salidas de `Recibida` (rompe el estado terminal) |
| `practica-3-defecto-3` | `0040cf7` | `2d4ffcd` | `Business.Inventory/Entities/OrdenCompra.cs` L39-46 | `Borrador_EnviarAAprobacion_SinDetalles_LanzaInvalidOperationException_YElEstadoNoCambia` | Elimina la validación de detalles/proveedor en `EnviarAAprobacion` |
| `practica-3-defecto-4` | `dcee56d` | `2d4ffcd` | `Core.Application/Services/OrdenCompraService.cs` L83-88 | `Recibir_SinPermisoAprobado_Rechaza_YConPermisoAprobado_Permite` (integración) | Elimina la consulta a `PermisosUsuario` y el rechazo |

Cada tag fue comprobado ejecutando la suite completa: **1 fallo**, el resto en verde, y el
`git show --name-only` confirma que **solo** se modificó el archivo de producción indicado.

---

## 6. Estado de Git y publicación

```
main             eb496c5  merge: practica 3 final (README + docs alineados)
practica-3       2d4ffcd  docs(practica-3): alinea tabla de transiciones, añade
                          prueba de auto-transición y documenta la revisión acumulada
practica-3-defecto-1  3446b00
practica-3-defecto-2  4dbfe20
practica-3-defecto-3  0040cf7
practica-3-defecto-4  dcee56d
```

- `main` y `practica-3` tienen **árbol idéntico** salvo `contexto-sesion.md`, que vive solo en `main`
  como documento de handoff y no altera lo etiquetado.
- Tags publicados en `origin` con `git push origin --force --tags` (el tag se movió al añadirse el
  commit de documentación; los cuatro defectos se recrearon por `cherry-pick` sobre la nueva base).
- Ramas temporales `d1..d4` eliminadas después de etiquetar.

### Cómo regenerar los defectos si se añade un commit a `practica-3`

```powershell
# 1) Nuevo commit sobre p3-final y mover el tag
git checkout p3-final
git add -A; git commit -m "..."
git tag -f practica-3

# 2) Recrear cada defecto (los commits viejos son 3446b00, 4dbfe20, 0040cf7, dcee56d)
git checkout -B d1 practica-3; git cherry-pick 3446b00; git tag -f practica-3-defecto-1
git checkout -B d2 practica-3; git cherry-pick 4dbfe20; git tag -f practica-3-defecto-2
git checkout -B d3 practica-3; git cherry-pick 0040cf7; git tag -f practica-3-defecto-3
git checkout -B d4 practica-3; git cherry-pick dcee56d; git tag -f practica-3-defecto-4

# 3) Verificar y publicar
git checkout main; git merge p3-final --no-ff
git push origin main; git push origin --force --tags
```

### Comandos de verificación

```powershell
# Suite completa sobre cualquier tag
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Ver solo la prueba que debe caer en un defecto
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx `
  --filter "FullyQualifiedName~Borrador_Aprobar"

# Comprobar que un defecto solo toca producción
git show --name-only --format="" practica-3-defecto-1

# Comprobar ascendencia
git rev-parse --short practica-3-defecto-1^   # debe ser 2d4ffcd
```

---

## 7. Archivos creados / modificados en la Práctica 3

### Producción

| Archivo | Estado | Contenido |
|---|---|---|
| `Business.Inventory/Entities/OrdenCompra.cs` | modificado | Máquina de estados, `TransicionesPermitidas`, `CambiarEstado`, condición de `EnviarAAprobacion` |
| `Core.Application/DTOs/Ordenes/CrearOrdenCompraDto.cs` | nuevo | DTO de creación (proveedor + items) |
| `Core.Application/DTOs/Ordenes/DetalleOrdenDto.cs` | nuevo | DTO de línea de detalle |
| `Core.Application/Interfaces/IOrdenCompraService.cs` | nuevo | Contrato del servicio (incluye `RecibirAsync(int, Guid)`) |
| `Core.Application/Interfaces/IOrdenCompraRepository.cs` | nuevo | Contrato del repositorio |
| `Core.Application/Services/OrdenCompraService.cs` | nuevo | Casos de uso + verificación del permiso `recibir-orden` |
| `Infrastructure/Repositories/OrdenCompraRepository.cs` | nuevo | Acceso EF Core con `Include(Detalles)` |
| `Presentation.API/Controllers/OrdenesCompraController.cs` | nuevo | 5 endpoints, roles, mapeo a `400`/`403` |
| `Presentation.API/Program.cs` | modificado | Registro en DI de repositorio/servicio del negocio |
| `Core.Application/Application.csproj` | modificado | Referencia a `Business.Inventory` |

### Pruebas y documentación

| Archivo | Estado | Contenido |
|---|---|---|
| `Bussiness.inventory.Tests/Bussiness.inventory.Tests.csproj` | nuevo | xUnit 2.9.3 + EF Core InMemory |
| `Bussiness.inventory.Tests/OrdenCompraTests.cs` | nuevo | 17 pruebas de la máquina de estados |
| `Bussiness.inventory.Tests/OrdenCompraIntegracionTests.cs` | nuevo | Prueba de integración con las dos caras |
| `ProyectoFinalProg3_Inventory.slnx` | modificado | Añade el proyecto de pruebas |
| `docs/maquina-de-estados.md` | nuevo/modificado | Estados, permitidas, prohibidas, RD-07, diagrama |
| `README.md` | modificado | §Práctica 3, §Pruebas, §Cómo provocar, §Revisión acumulada, §Defectos |

---

## 8. Errores y problemas resueltos

### 8.1 Durante la Práctica 3

1. **El defecto 4 no era detectable sin HTTP.** La comprobación del permiso `recibir-orden` estaba en
   `OrdenesCompraController`, así que ningún test sin servidor podía alcanzarla. → Se movió a
   `OrdenCompraService.RecibirAsync(int id, Guid usuarioId)`, con `IApplicationDbContext` inyectado, y
   el controlador conserva el mapeo a `403`. Esto además alinea la regla con RD-04 (una sola
   implementación).
2. **Fila de la tabla de prohibidas imposible de probar.** `Aprobada → Borrador` no es alcanzable por
   la API pública (no existe método que lleve a `Borrador`). → La fila se sustituyó por
   `Aprobada → Aprobada` (auto-transición, sí comprobable) y se añadió la prueba que faltaba para
   `PendienteAprobacion → PendienteAprobacion`, de modo que **cada fila** de la tabla tiene al menos
   una prueba.
3. **Conteos del README desactualizados.** Pasó por `Passed: 34` → `36` → `37` a medida que crecía la
   suite. → Se corrigió el número y el desglose (12 + 7 + 18).
4. **Nombre de estado incorrecto en el README.** El diagrama y el recorrido decían `Solicitada` /
   `EnAprobacion`. → Corregido a `PendienteAprobacion` en el diagrama Mermaid y en los pasos.
5. **Falta de documentación del requisito de permiso en el recorrido manual.** El paso 4 (`recibir`)
   no mencionaba que hacía falta el permiso. → Se documentó el flujo solicitar → aprobar → recibir y el
   `403` en caso contrario.
6. **Defectos colgando de una base vieja.** Los tags `practica-3` y `practica-3-defecto-*` habían
   quedado desincronizados al añadirse commits. → Se movió el tag y se **recrearon** los cuatro
   defectos por `cherry-pick` sobre la nueva base, verificando padre, archivos y fallo esperado.
7. **PowerShell vs. Bash.** El entorno es PowerShell: `&&`, `tail` y `grep` no existen. → Los comandos
   se reescribieron con `;`, `Select-Object`, `Select-String`, `Out-Host`.

### 8.2 Heredados de asignaciones anteriores (ya resueltos)

8. **Credencial de Gmail filtrada en el historial** (señalada en la Práctica 1). → Se rotó la
   credencial, se limpió del historial y el README documenta la corrección.
9. **Conversión del enum `RolUsuario` en EF Core.** → Migración `20261001215443_FixRolEnumConversion`.
10. **`logout` no invalidaba el JWT.** → Entidad `TokenRevocado` + migraciones
    `AddTokensRevocados` y `UpdateTokenRevocadoUsuarioId`.
11. **Bloqueo por intentos fallidos / recuperación de contraseña.** → Migraciones
    `AgregarCamposBloqueoUsuario` y `AgregarCamposRecuperacionPassword`.
12. **`Estado` por defecto inválido en `OrdenesCompra`.** El enum empieza en `1`; un `default(0)` no es
    un estado válido. → La migración `AgregarSolicitudesPermiso` añade la columna con
    `defaultValue: 1` (`Borrador`).
13. **Correos sin SMTP en un clon limpio.** → Los tokens/códigos quedan en `Usuarios` y el README
    explica cómo leerlos por SQL para completar activación y recuperación.

---

## 9. Próximos pasos y deuda técnica

### 9.1 Pendiente inmediato (no bloquea la entrega)

1. **Revisión manual 1.5 con el docente.** El README ya tiene los cinco recorridos; conviene ensayarlos
   una vez con la base real (o al menos comprobar que los endpoints responden con los códigos
   documentados). Sin SMTP, usar las consultas SQL indicadas.
2. **Decidir qué hacer con el «impacto en inventario» de `Recibir`.** El README dice que `recibir`
   impacta el inventario, pero `RecibirAsync` solo cambia el estado: **no incrementa
   `Producto.StockActual`**. Opciones: implementar el incremento dentro de la misma transacción, o
   suavizar la frase del README. Si el docente lo comprueba, la implementación es lo esperable.

### 9.2 Deuda técnica recomendada (mejoras, no requisitos)

3. **Duplicación de la regla de permiso.** `OrdenesCompraController.Recibir` vuelve a consultar
   `PermisosUsuario` antes de llamar al servicio. Funciona, pero rompe el principio de un solo lugar
   para la regla y puede desincronizarse. Dejar solo la comprobación del servicio (el controlador ya
   traduce `UnauthorizedAccessException` a `403`).
4. **`CrearBorrador` toma `usuarioId` de la query string.** Permite que un usuario cree una orden a
   nombre de otro. Debería salir del claim del JWT, igual que en `Recibir`.
5. **`ObtenerUsuarioIdActual()` usa `Guid.Parse`.** Si falta el claim lanza `FormatException` → `500`.
   Cambiar a `Guid.TryParse` y devolver `401`.
6. **Condición muerta en `EnviarAAprobacion`:** `Detalles.Count == null` nunca puede ser cierto
   (`Count` es `int`). Simplificar a `Detalles.Count == 0`.
7. **Advertencias NU1902/NU1903:** `System.Security.Cryptography.Pkcs` 9.0.11 tiene una vulnerabilidad
   **alta** conocida (transitiva). Actualizar el paquete que la arrastra o fijar una versión parcheada.
8. **Encoding:** varios `.cs` y el README muestran caracteres mal codificados en consola
   (`Transici�n`, `contrase�a`). El contenido real es UTF-8 válido; si se edita en un editor que no
   detecta UTF-8 conviene forzar la codificación para evitar corrupción.
9. **`OrdenCompraRepository.GetByIdAsync` devuelve `OrdenCompra?`** y la interfaz declara no-nulo
   (warning CS8603). Ajustar la firma a nullable.
10. **`Business.Inventory` y `Bussiness.inventory.Tests`:** nombres con errata (`Bussiness`). No es
    funcional, pero unificarlos requeriría renombrar proyecto y carpeta.

### 9.3 Si se continúa con la siguiente práctica

- Mantener el patrón: regla en la entidad/servicio → prueba que la cubre → defecto plantado que la
  rompe en un tag hijo.
- Recordar que **cualquier commit nuevo sobre `practica-3` obliga a recrear los cuatro tags de
  defecto** (§6).
- El README es la fuente de verdad para la revisión manual: al añadir funcionalidad, actualizar
  §Pruebas (conteo y comando), §Cómo provocar y §Defectos en el mismo commit.

---

## 10. Anexo — contratos y archivos clave

| Archivo | Para qué mirarlo |
|---|---|
| `Business.Inventory/Entities/OrdenCompra.cs` | Máquina de estados (fuente de verdad) |
| `Core.Application/Services/OrdenCompraService.cs` | Casos de uso + permiso `recibir-orden` |
| `Core.Application/Interfaces/IOrdenCompraService.cs` | Firma de `RecibirAsync(int, Guid)` |
| `Infrastructure/Repositories/OrdenCompraRepository.cs` | Persistencia con `Include(Detalles)` |
| `Presentation.API/Controllers/OrdenesCompraController.cs` | Endpoints, roles y códigos HTTP |
| `Bussiness.inventory.Tests/OrdenCompraTests.cs` | 17 pruebas de la máquina de estados |
| `Bussiness.inventory.Tests/OrdenCompraIntegracionTests.cs` | Prueba de integración (las dos caras) |
| `docs/maquina-de-estados.md` | Tablas de permitidas/prohibidas que la suite debe espejar |
| `README.md` | §Pruebas, §Práctica 3, §Revisión acumulada, §Defectos |
