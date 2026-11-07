# Contexto de sesión — Práctica 4 (documentos, contenedor y API externa)

**Proyecto:** `ProyectoFinalProg3_Inventary` — Sistema de inventario (Programación III, ITLA)
**Autor:** Richard José Hernández Valdez
**Repositorio:** https://github.com/richard-dev-st/ProyectoFinalProg3_Inventary
**Fecha del corte:** 07/11/2026
**Rama de trabajo:** `feat/manejo-documentos` (sin upstream, sin publicar)
**HEAD de la rama:** `823938b feat(documentos): implementa el manejador de documentos del Core`
**Base:** `main` = `412eb0e`
**Solución única:** `ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` (.NET 10)

> Este documento sustituye al handoff de la Práctica 3. La Práctica 3 queda cerrada y
> etiquetada (`practica-3`); su estado se resume en §2.3 porque la revisión acumulada de la
> Práctica 4 vuelve a evaluar esos frentes.

---

## 1. Resumen ejecutivo

La **Práctica 4 está al ~25 %**. Lo único terminado es el punto **1.1 (Manejador de documentos)**,
que está completo, probado y **commiteado** en `feat/manejo-documentos` (`823938b`).

Estado por punto del enunciado:

| Punto | Descripción | Estado |
|---|---|---|
| 1.1 | Manejador de documentos (RF-DOC-01..04, RD-06, RD-07) | ✅ **Completo y commiteado** |
| 1.2 | Contenedor con un solo comando (Dockerfile, `.dockerignore`, compose, migración automática) | ⬜ 0 % |
| 1.3 | Volúmenes persistentes (documentos + BD) | ⬜ 0 % |
| 1.4 | API externa real con clave como secreto | ⬜ 0 % |
| 1.5 | Revisión acumulada de los seis frentes desde el contenedor | ⬜ 0 % |
| — | Tag `practica-4` + push + entrega en Moodle | ⬜ 0 % |

Verificación al cierre de esta sesión:

```
Build succeeded   (sin warnings del módulo de documentos; persisten 3 CS8604 heredados)

Passed! - Failed: 0, Passed: 12 - Core.Domain.Tests.dll
Passed! - Failed: 0, Passed: 19 - Core.Application.Tests.dll   (7 previos + 12 nuevos)
Passed! - Failed: 0, Passed: 18 - Bussiness.inventory.Tests.dll
                                            ── 49 en total
```

El commit `823938b` pasó el hook `gitleaks` con `no leaks found`, relevante para el punto 1.4.

---

## 2. Estado de Git y entorno

### 2.1 Ramas y commits

```
main                    412eb0e  docs: actualiza hashes de tags y notas de verificacion en contexto-sesion.md
feat/manejo-documentos  823938b  feat(documentos): implementa el manejador de documentos del Core
```

- La rama `feat/manejo-documentos` **salió de `main` y no tiene upstream**: no está publicada.
- `main` **no tiene** los cambios de documentos todavía; el merge está pendiente.
- Working tree: solo `contexto-sesion.md` aparece como `M` (por fin de línea); este documento
  se está reescribiendo ahora.

### 2.2 Tags existentes

```
asignacion-1, asignacion-2
practica-1
practica-2, practica-2-defecto-1..6
practica-3, practica-3-defecto-1..4
```

`practica-4` aún no existe.

### 2.3 Práctica 3 (contexto histórico)

- Tag `practica-3` = `1c45d3a`; defectos `46ce800`, `f9055bb`, `d779d66`, `44fe284`.
- `main` = `412eb0e`.
- 37 pruebas en verde sobre el tag.
- Máquina de estados de `OrdenCompra` con 5 estados, 5 endpoints y prueba de integración
  negocio ↔ permisos. Documentación en `docs/maquina-de-estados.md` y README.
- Todo publicado en `origin`, incluidos los tags.

### 2.4 Entorno local

- Windows + PowerShell 7. **No hay `&&`, `tail` ni `grep`**: usar `;`, `Select-String`,
  `Select-Object`, `Out-Host`.
- Base local: SQL Server `localhost`, base `InventarioBP3`, autenticación integrada de Windows.
- Migración de documentos **ya aplicada** en esa base; `has-pending-model-changes` sin cambios.
- API local: `http://localhost:5176` (perfil `http` de `launchSettings.json`).
- `dotnet ef` devuelve código 1 por warnings NU1902/NU1903 aunque el comando tenga éxito.

### 2.5 Protecciones de repositorio ya verificadas

- `.gitignore` excluye `*.env` (L438) y `uploads/` (L447).
- `.env` **no existe** ni en el working tree ni en el historial (`git log --all -- .env` vacío).
- `.env.example` **no** cae en el patrón `*.env` (verificado con `git check-ignore`), así que se
  puede versionar.
- Hook de pre-commit con `gitleaks` activo: se ejecuta en cada commit.

---

## 3. Arquitectura actual

### 3.1 Capas y dependencias

```
Presentation.API  (ASP.NET Core Web API, JWT, controladores)
      │
      ▼
Infrastructure    (EF Core + SQL Server, DbContext, migraciones, almacenamiento de archivos)
      │
      ├──────────────► Core.Application  (servicios de caso de uso, DTOs, interfaces)
      │                        │
      ▼                        ▼
Business.Inventory  ◄──── Core.Domain  (entidades y enums compartidos)
```

| Proyecto | Responsabilidad | Referencia a |
|---|---|---|
| `Core.Domain` | Entidades transversales: `Usuario`, `SolicitudPermiso`, `PermisoUsuario`, `CorreoEnCola`, `TokenRevocado`, **`Documento`**; enums `RolUsuario`, `EstadoSolicitudPermiso`, `EstadoOrden`. Sin dependencias. | — |
| `Business.Inventory` | Entidades del negocio: `OrdenCompra`, `DetalleOrden`, `Producto`, `Proveedor`; máquina de estados de `OrdenCompra`. | `Core.Domain` |
| `Core.Application` | Casos de uso (`AuthService`, `SolicitudPermisoService`, `OrdenCompraService`, `AdminUsuarioService`, **`DocumentoService`**), DTOs e interfaces (`IApplicationDbContext`, `IDocumentoService`, `IAlmacenDocumentos`, etc.). | `Business.Inventory`, `Core.Domain` |
| `Infrastructure` | `ApplicationDbContext`, repositorios, migraciones y servicios (`PasswordService`, `JwtService`, `EmailService`, `ProcesadorCorreosWorker`, **`AlmacenDocumentosLocal`**). | `Core.Application`, `Business.Inventory`, `Core.Domain` |
| `Presentation.API` | Controladores REST, autorización por rol, mapeo de excepciones a códigos HTTP, DI y JWT. | `Infrastructure` |
| `EmailWorker` | Worker independiente que vacía `CorreosEnCola` contra SMTP. **No lo arranca la API.** | `Infrastructure` |
| `Core.Domain.Tests` | 12 pruebas de `SolicitudPermiso`. | `Core.Domain` |
| `Core.Application.Tests` | 7 pruebas de `SolicitudPermisoService` + **12 de `DocumentoService`** con EF Core InMemory. | `Core.Application`, `Infrastructure` |
| `Bussiness.inventory.Tests` | 18 pruebas del módulo de negocio. | `Business.Inventory`, `Infrastructure` |

> Errata histórica: el proyecto de pruebas se llama `Bussiness.inventory.Tests` (doble `s`).
> No se renombró para no romper la solución ni el historial.

### 3.2 Modelo de datos (EF Core)

Tablas y `DbSet` actuales:

- `Usuarios`, `CorreosEnCola`, `TokensRevocados` (Core).
- `OrdenesCompra`, `DetallesOrden`, `Productos`, `Proveedores` (negocio).
- `SolicitudesPermiso`, `PermisosUsuario` (gestión de permisos).
- **`Documentos`** (nuevo, Core).

Migraciones:

```
20260926215935_MigracionInicial
20260930003057_AgregarCamposBloqueoUsuario
20260930201107_AddTokensRevocados
20260930210838_UpdateTokenRevocadoUsuarioId
20261001215443_FixRolEnumConversion
20261002160247_AgregarCamposRecuperacionPassword
20261017222054_AgregarSolicitudesPermiso
20261107151809_AgregarManejadorDocumentos   ← nueva, aplicada en local
```

### 3.3 Módulo de documentos — piezas

```
DocumentosController  ──►  IDocumentoService  ──►  DocumentoService
                                                     │        │
                                              IApplicationDbContext
                                                     │        │
                                                     ▼        ▼
                                          ApplicationDbContext  IAlmacenDocumentos
                                                                     │
                                                                     ▼
                                                          AlmacenDocumentosLocal (sistema de archivos)
```

| Pieza | Archivo |
|---|---|
| Entidad | `Core.Domain/Entities/Documento.cs` |
| Contratos | `Core.Application/Interfaces/IDocumentoService.cs`, `IAlmacenDocumentos.cs` |
| DTOs | `Core.Application/DTOs/Documentos/{DocumentoDto,DocumentoDescargaDto,ArchivoEntrada}.cs` |
| Caso de uso | `Core.Application/Services/DocumentoService.cs` |
| Almacenamiento | `Infrastructure/Services/AlmacenDocumentosLocal.cs` |
| API | `Presentation.API/Controllers/DocumentosController.cs` |
| Persistencia | `Infrastructure/Persistence/ApplicationDbContext.cs` (L140-152) |
| Pruebas | `Core.Application.Tests/DocumentoServiceTests.cs` |

---

## 4. Manejador de documentos (punto 1.1)

### 4.1 Entidad `Documento`

`Core.Domain/Entities/Documento.cs` — setters privados, constructor de negocio público,
constructor vacío **privado** para EF.

| Propiedad | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | generado en el constructor |
| `NombreOriginal` | `string` | nombre visible; se sanea con `Path.GetFileName` |
| `NombreAlmacenado` | `string` | GUID + extensión; nombre físico |
| `TipoContenido` | `string` | `Content-Type`; fallback `application/octet-stream` |
| `TamanioBytes` | `long` | tamaño declarado por el cliente |
| `FechaSubida` | `DateTime` | `DateTime.UtcNow` |
| `SubidoPorUsuarioId` | `Guid` | **FK real** a `Usuarios` |
| `SubidoPorUsuario` | `Usuario?` | navegación |
| `EsEliminado` | `bool` | borrado lógico |
| `FechaEliminacion` | `DateTime?` | se fija en `Eliminar()` |

`Eliminar()` es idempotente: si ya está eliminado, no hace nada.

### 4.2 Configuración EF

`ApplicationDbContext.OnModelCreating` (L140-152):

- Clave `Id`.
- `NombreOriginal` y `NombreAlmacenado`: requeridos, `nvarchar(255)`.
- `TipoContenido`: requerido, `nvarchar(100)`.
- Índice en `SubidoPorUsuarioId`.
- FK `SubidoPorUsuarioId → Usuarios.Id` con **`DeleteBehavior.Restrict`** (no se puede borrar
  un usuario que tenga documentos; a diferencia de `OrdenCompra.CreadoPorUsuarioId`, que no
  tiene FK).

Migración `20261107151809_AgregarManejadorDocumentos`: crea `Documentos`, la FK
`FK_Documentos_Usuarios_SubidoPorUsuarioId` (Restrict) y el índice
`IX_Documentos_SubidoPorUsuarioId`.

### 4.3 Almacenamiento físico

`IAlmacenDocumentos`:

```csharp
Task GuardarAsync(Stream contenido, string nombreAlmacenado, CancellationToken ct = default);
Task<Stream> AbrirAsync(string nombreAlmacenado, CancellationToken ct = default);
bool Existe(string nombreAlmacenado);
Task EliminarAsync(string nombreAlmacenado, CancellationToken ct = default);
```

`AlmacenDocumentosLocal`:

- Raíz desde configuración `Documentos:RutaBase` (por defecto `uploads`), resuelta con
  `Path.GetFullPath`; crea el directorio en el constructor.
- `Ruta()` es **privada** y valida que el resultado quede dentro de la raíz (bloquea
  *path traversal*).
- `AbrirAsync` devuelve `File.OpenRead` (stream, sin cargar el archivo en memoria).
- `EliminarAsync` ignora archivos inexistentes.

### 4.4 `DocumentoService`

Contrato (`IDocumentoService`):

```csharp
Task<DocumentoDto> SubirDocumentoAsync(ArchivoEntrada archivo, Guid usuarioId);
Task<IEnumerable<DocumentoDto>> ListarDocumentosAsync(Guid usuarioId);
Task<DocumentoDescargaDto> DescargarDocumentoAsync(Guid id, Guid usuarioId);
Task EliminarDocumentoAsync(Guid id, Guid usuarioId);
```

Reglas implementadas:

| Operación | Reglas |
|---|---|
| **Subir** | archivo no nulo, nombre no vacío, longitud > 0, ≤ 10 MB, extensión en `{.pdf,.png,.jpg,.jpeg,.docx,.xlsx,.txt}`, usuario existente. Nombre físico `Guid:N + extensión`. Si falla `SaveChangesAsync`, borra el archivo recién guardado (compensación). `TipoContenido` vacío → `application/octet-stream`. |
| **Listar** | usuario existente; filtra `!EsEliminado`; si no es Administrador, filtra `SubidoPorUsuarioId == usuarioId`; ordena por `FechaSubida` descendente; `Include(SubidoPorUsuario)` para exponer `SubidoPorEmail`. |
| **Descargar** | documento existente y no eliminado; **dueño o Administrador**; archivo físico existente; devuelve stream + nombre original + tipo. |
| **Eliminar** | documento existente y no eliminado; **dueño o Administrador**; llama `Eliminar()` y persiste (borrado lógico). El archivo físico **se conserva** (RF-DOC-04). |

Decisiones clave:

1. **La autorización vive en el servicio**, no en el controlador (RD-06: invocar la operación
   directamente también se rechaza). Los tests llaman al servicio sin HTTP y comprueban el
   `UnauthorizedAccessException`.
2. **`[Authorize]` sin roles** en descarga y eliminación: la regla es «dueño **o** Administrador»,
   no «solo Administrador». Poner `Roles = "Administrador"` habría bloqueado al dueño Estándar.
3. **Borrado lógico conserva el archivo físico**: cumple RF-DOC-04 y permite auditar.
4. **FK real a `Usuario` con `Restrict`**, para que «quién lo subió» sea integridad referencial y
   no un `Guid` suelto.

### 4.5 API

`DocumentosController` — todas las rutas requieren JWT (`[Authorize]`).

| Método | Ruta | Entrada | Respuesta |
|---|---|---|---|
| POST | `/api/documentos/subir` | `multipart/form-data`, campo `archivo` | `200 DocumentoDto` |
| GET | `/api/documentos` | — | `200 DocumentoDto[]` |
| GET | `/api/documentos/{id}/descargar` | ruta `{id:guid}` | `200` archivo (`Content-Disposition: attachment`) |
| DELETE | `/api/documentos/{id}` | ruta `{id}` | `200` vacío |

Mapeo de errores:

| Situación | Código |
|---|---|
| Sin token / token inválido | `401` |
| Usuario sin derecho (ajeno) | `403` |
| Archivo vacío, extensión no permitida, > 10 MB, nombre vacío | `400` |
| Documento inexistente, eliminado o archivo físico ausente | `404` |
| `IOException` al abrir el archivo (carrera) | `404` |

`ObtenerUsuarioIdActual()` lee `ClaimTypes.NameIdentifier` o `sub` y usa `Guid.TryParse`; si no
hay claim válido lanza `UnauthorizedAccessException` → `403`.

### 4.6 DI y configuración

`Presentation.API/Program.cs` L146-147:

```csharp
builder.Services.AddScoped<IDocumentoService, DocumentoService>();
builder.Services.AddScoped<IAlmacenDocumentos, AlmacenDocumentosLocal>();
```

`Presentation.API/appsettings.json` L24-25:

```json
"Documentos": {
  "RutaBase": "uploads"
}
```

En contenedor habrá que sobrescribir con `Documentos__RutaBase=/app/uploads` para que coincida
con el volumen.

---

## 5. Cumplimiento del punto 1.1

| Requisito | Evidencia | Veredicto |
|---|---|---|
| **RF-DOC-01** — subir registrando nombre, tipo, tamaño, fecha y quién | `Documento` + migración + `SubirDocumentoAsync`; el DTO devuelve `SubidoPorEmail` | ✅ |
| **RF-DOC-02** — listar solo lo permitido; sin eliminados ni ajenos salvo Admin | `ListarDocumentosAsync` con doble filtro y prueba dedicada | ✅ |
| **RF-DOC-03** — descargar solo dueño o Admin; otro recibe rechazo | chequeo en el servicio → `UnauthorizedAccessException` → `403`; 3 pruebas | ✅ |
| **RF-DOC-04** — eliminación lógica, sigue en almacenamiento | `Eliminar()` + `!EsEliminado` en consultas; prueba verifica registro y `FechaEliminacion` | ✅ |
| **RD-06** — autorización server-side; invocación directa rechazada | regla en `DocumentoService`; pruebas llaman al servicio con otro usuario | ✅ |
| **RD-07** — archivo ausente/vacío → rechazo controlado | vacío → `400`; extensión/tamaño → `400`; ausente → `404`; `IOException` → `404` | ✅ |
| Sesión válida en las 4 operaciones | `[Authorize]` en los 4 endpoints | ✅ |
| Registro consultable con todos los datos | `GET /api/documentos` devuelve nombre, tipo, tamaño, fecha, autor (id + email) | ✅ |

**Punto 1.1 cerrado.**

---

## 6. Suite de pruebas (49 en total)

### 6.1 Nuevas: `Core.Application.Tests/DocumentoServiceTests.cs` (12)

| # | Prueba | Cubre |
|---|---|---|
| 1 | `RF_DOC_01_Subir_RegistraTodosLosDatos` | nombre, tipo, tamaño, fecha, autor, archivo en almacén |
| 2 | `RF_DOC_01_Subir_SinTipoContenido_UsaValorPorDefecto` | fallback `application/octet-stream` |
| 3 | `RF_DOC_02_Listar_ExcluyeEliminadosYAjenos` | doble filtro |
| 4 | `RF_DOC_02_Listar_AdministradorVeTodos` | excepción del Admin |
| 5 | `RF_DOC_03_Descargar_DuenioPuede` | descarga y contenido |
| 6 | `RF_DOC_03_Descargar_OtroUsuarioEsRechazado` | **RD-06 directo** |
| 7 | `RF_DOC_03_Descargar_AdministradorPuede` | Admin descarga ajeno |
| 8 | `RF_DOC_04_Eliminar_EsLogicaYDesapareceDelListado` | flag, fecha y listado |
| 9 | `RF_DOC_04_Eliminar_OtroUsuarioEsRechazado` | **RD-06 directo** |
| 10 | `RD_07_Descargar_ArchivoAusente_LanzaRechazoControlado` | archivo físico ausente |
| 11 | `RD_07_Subir_ArchivoVacio_LanzaRechazoControlado` | longitud 0 |
| 12 | `RD_07_Subir_ExtensionNoPermitida_LanzaRechazoControlado` | `.exe` |

Infraestructura de prueba: `AlmacenEnMemoria` (diccionario `nombre → byte[]`) y
`ApplicationDbContext` con `UseInMemoryDatabase(Guid.NewGuid().ToString())`. No se levanta la API.

### 6.2 Totales

```
Core.Domain.Tests          12
Core.Application.Tests     19   (7 de permisos + 12 de documentos)
Bussiness.inventory.Tests  18
                          ────
                           49
```

Comando único:

```powershell
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
```

---

## 7. Archivos del commit `823938b`

17 archivos, 1346 inserciones.

### Nuevos

| Archivo | Contenido |
|---|---|
| `Core.Domain/Entities/Documento.cs` | entidad con borrado lógico |
| `Core.Application/Interfaces/IAlmacenDocumentos.cs` | abstracción de almacenamiento |
| `Core.Application/Interfaces/IDocumentoService.cs` | contrato de las 4 operaciones |
| `Core.Application/Services/DocumentoService.cs` | validaciones, autorización, compensación |
| `Core.Application/DTOs/Documentos/DocumentoDto.cs` | DTO de listado/respuesta |
| `Core.Application/DTOs/Documentos/DocumentoDescargaDto.cs` | stream + nombre + tipo |
| `Core.Application/DTOs/Documentos/ArchivoEntrada.cs` | entrada de subida |
| `Infrastructure/Services/AlmacenDocumentosLocal.cs` | almacenamiento en disco |
| `Infrastructure/Migrations/20261107151809_AgregarManejadorDocumentos.cs` | tabla + FK + índice |
| `Infrastructure/Migrations/20261107151809_AgregarManejadorDocumentos.Designer.cs` | snapshot de la migración |
| `Presentation.API/Controllers/DocumentosController.cs` | 4 endpoints |
| `Core.Application.Tests/DocumentoServiceTests.cs` | 12 pruebas |

### Modificados

| Archivo | Cambio |
|---|---|
| `Core.Application/Interfaces/IApplicationDbContext.cs` | `DbSet<Documento> Documentos` |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | `DbSet` + configuración EF |
| `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` | snapshot actualizado |
| `Presentation.API/Program.cs` | registro de `IDocumentoService` e `IAlmacenDocumentos` |
| `Presentation.API/appsettings.json` | sección `Documentos:RutaBase` |

---

## 8. Errores resueltos en esta sesión

### 8.1 Errores de compilación

1. **CS0029 (`DocumentoService` L38) — tipo de retorno incorrecto.**
   `DescargarDocumentoAsync` devolvía `DocumentoDescargaDto`, pero la interfaz y la firma
   declaraban `DocumentoDto`. → Se cambió el tipo en `IDocumentoService` y en la implementación.

2. **CS1503 (`DocumentoService` L130) — `Guid` vs `Usuario?`.**
   `DocumentoDto` declaraba `Usuario? SubidoPorUsuarioId` (la navegación) y `Mapear` le pasaba un
   `Guid`. → El DTO pasó a `Guid SubidoPorUsuarioId`; la navegación queda solo en la entidad.

3. **CS1061 (`DocumentosController` L106) — método inexistente.**
   Tras renombrar `EliminarDocumentosAsync` → `EliminarDocumentoAsync`, el controlador seguía
   llamando al plural y el build quedaba en `FAILED`. → Se corrigió la llamada.

4. **CS0169 (`DocumentosController`) — `_context` sin usar.**
   El controlador inyectaba `IApplicationDbContext` y no lo usaba. → Se eliminó el campo y el
   parámetro del constructor.

5. **CS0168 (`Descargar`) — `ex` declarado y no usado.**
   `catch (IOException ex)` sin usar `ex`. → `catch (IOException)`.

### 8.2 Errores de comportamiento (los que rompían la rúbrica)

6. **`DELETE` sin `await` → siempre respondía `200`.**
   `var documentos = _service.EliminarDocumentosAsync(id, usuarioId); return Ok();`
   El servicio se detenía en su primer `await`, así que un Estándar podía «eliminar» un documento
   ajeno y recibir `200`; los `catch` nunca se ejecutaban y la excepción quedaba como tarea no
   observada. → Se añadió `await`.

7. **`Subir` sin `try/catch` → `500` en rechazos esperados.**
   Extensión no permitida, archivo > 10 MB o nombre vacío lanzaban `InvalidOperationException`
   sin manejar. → `catch (UnauthorizedAccessException)` → `403`; `catch (InvalidOperationException)`
   → `400`.

8. **`Descargar` sin `catch (IOException)` → `500` por carrera.**
   Entre `_almacen.Existe(...)` y `AbrirAsync(...)` el archivo podía desaparecer. → Se añadió el
   `catch` que devuelve `404` con mensaje comprensible (RD-07).

9. **`[Authorize(Roles = "Administrador")]` en `Descargar` + parámetro `Guid id` ausente.**
   El esqueleto inicial impedía que el dueño Estándar descargara lo suyo y no recibía el `id` de
   la ruta. → `[Authorize]` + `Descargar(Guid id)`; la regla dueño/Admin queda en el servicio.

10. **`TipoContenido` podía guardarse vacío.**
    El valor venía del cliente sin validar y la columna `IsRequired` acepta cadena vacía. → Fallback
    a `application/octet-stream` en `SubirDocumentoAsync`.

11. **Consulta redundante en `Subir`.**
    Se hacía `AnyAsync` para validar el usuario y luego `FindAsync` para el correo. → Un solo
    `FindAsync`.

12. **Miembros sobreexpuestos.**
    `EsAdministradorAsync` era `public`, `Documento()` era `public`, `AlmacenDocumentosLocal.Ruta`
    era `public`. → Todos `private`.

### 8.3 Falsas alarmas

13. **Acentos «corruptos» en consola.**
    Los `.cs` mostraban `est"`, `recibi"`. Se comprobó el contenido real: es UTF-8 correcto
    (`U+00E1`, etc.); el problema es la página de códigos de la consola de Windows. No se tocó
    ningún archivo por esto.

---

## 9. Próximos pasos

### 9.1 Punto 1.2 — Contenedor con un solo comando

**Archivos a crear:**

1. **`Dockerfile`** multi-etapa en la raíz:
   - build: `mcr.microsoft.com/dotnet/sdk:10.0` → `dotnet restore` + `dotnet publish` de
     `ProyectoFinalProg3_Inventory/Presentation.API/Presentation.API.csproj -c Release -o /app/publish`.
   - runtime: `mcr.microsoft.com/dotnet/aspnet:10.0` → copiar publish, `ASPNETCORE_URLS=http://+:8080`,
     `EXPOSE 8080`, `ENTRYPOINT ["dotnet","Presentation.API.dll"]`.
   - La imagen del worker (`EmailWorker`) puede reutilizar la misma imagen con otro entrypoint o
     tener su propio stage.

2. **`.dockerignore`**: `**/bin`, `**/obj`, `.git`, `.vs`, `.idea`, `uploads`, `.env`,
   `TestResults`, `*.user`, `contexto-sesion.md` (opcional).

3. **`docker-compose.yml`** con tres servicios:
   - `db`: `mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`,
     `MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}`, volumen `sqlserver_data:/var/opt/mssql`,
     **sin `ports:`**.
   - `api`: `build: .`, único servicio con `ports:` (propuesta `8080:8080`), `depends_on: db`,
     volumen `documents_data:/app/uploads`, variables de entorno.
   - `worker`: `EmailWorker` con la misma cadena de conexión y credenciales SMTP;
     `depends_on: db`.

4. **Migración automática al arrancar** en `Program.cs` (hoy **no existe**):
   ```csharp
   using var scope = app.Services.CreateScope();
   var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
   // SQL Server tarda en aceptar conexiones: reintentar con espera progresiva
   await db.Database.MigrateAsync();
   ```
   Sin esto, `docker compose up --build` deja la BD vacía y falla «sin pasos manuales».

5. **README sección «Contenedor»**: comando exacto, URL (`http://localhost:PUERTO`) y cómo
   detener (`docker compose down`).

**Variables que la composición debe inyectar** (las actuales de `appsettings.json` no sirven en
contenedor):

```
ConnectionStrings__DefaultConnection = Server=db,1433;Database=InventarioBP4;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True
JwtSettings__SecretKey              = ${JWT_SECRET_KEY}      # hoy vacío en appsettings.json
Documentos__RutaBase                = /app/uploads
EmailSettings__SenderEmail          = ${SMTP_USER}
EmailSettings__SenderPassword       = ${SMTP_PASSWORD}
ApiExterna__ApiKey                  = ${EXCHANGE_RATE_API_KEY}   # punto 1.4
```

**Riesgos conocidos:**

- `JwtSettings:SecretKey` está vacío en `appsettings.json`; sin la variable, el login falla.
- La cadena actual usa `localhost` + autenticación Windows; es inválida en Linux.
- `EmailWorker` tiene su propio `appsettings.json` y su propia cadena de conexión.
- El worker debe esperar a que SQL Server esté listo (o tolerar fallos y reintentar).

### 9.2 Punto 1.3 — Volúmenes persistentes

- `documents_data` montado en `/app/uploads` (debe coincidir con `Documentos__RutaBase`).
- `sqlserver_data` montado en `/var/opt/mssql`.
- Prueba de aceptación: subir documento → `docker compose down` (sin `-v`) → `docker compose up`
  → el documento sigue en el listado y se descarga; el usuario sigue iniciando sesión.

### 9.3 Punto 1.4 — API externa con clave

API elegida: **tasa de cambio**. Debe **exigir clave**; Frankfurter (libre) no sirve.
Candidatas: ExchangeRate-API, Fixer, CurrencyLayer, FreeCurrencyAPI.

**Falta:**

1. `ITasaCambioService` + implementación con `HttpClient` tipado (`AddHttpClient`), timeout ~5 s,
   clave desde `ApiExterna:ApiKey`.
2. **Operación real del proyecto que use el dato** (no un endpoint que reenvía): por ejemplo
   `POST /api/OrdenesCompra/{id}/cotizar?moneda=USD`, que toma el total de la orden, consulta la
   tasa y devuelve total original, tasa, total convertido, fecha y fuente.
3. Rechazo controlado si falta la clave o la API no responde → `503`/`400` con mensaje; el resto
   del proyecto sigue funcionando.
4. `.env.example` con **todas** las variables y valores de ejemplo.
5. README sección «API externa»: qué API, qué operación la usa, nombre de la variable y un ejemplo
   real de respuesta (sin clave).

**Verificación de secreto:** `git log -S "<clave>" --all` vacío, `.env` ignorado y fuera del
historial, y `gitleaks` en verde (el hook ya corre).

### 9.4 Punto 1.5 — Revisión acumulada (seis frentes)

Los seis frentes ya funcionan en local; falta que funcionen **desde el contenedor** siguiendo el
README al pie de la letra:

1. Registro con activación por correo.
2. Inicio y cierre de sesión.
3. Estándar rechazado en operación de Administrador.
4. Recuperación de contraseña.
5. Gestión de permisos: crear, aprobar, rechazar y transiciones inválidas.
6. Módulo de negocio: operación válida y transición prohibida de la máquina de estados.

**Problema oculto — el correo:** el `.env.example` llevará SMTP de ejemplo, así que los correos de
activación y recuperación **no llegan**. Y no se puede leer la BD por SQL porque el puerto de la
base no se publica (requisito del enunciado).

**Solución recomendada:** *fallback* en `EmailService` — si no hay SMTP configurado, escribir el
enlace/código en el log (`docker compose logs -f api` o `worker`) y documentarlo en el README.
Así el revisor completa los frentes 1 y 4 sin credenciales reales.

### 9.5 Entrega

1. Merge de `feat/manejo-documentos` a `main` (`--no-ff`).
2. Prueba desde clon limpio: copiar `.env.example` a `.env` → `docker compose up --build`.
3. Recorrer los seis frentes y la subida/descarga/eliminación de documentos.
4. `git tag practica-4` sobre el commit final de `main`.
5. `git push origin main` y `git push origin --tags`.
6. Moodle: URL del repositorio + nombre del tag `practica-4`.

### 9.6 Deuda técnica menor (no bloquea la entrega)

1. **Endurecer `AlmacenDocumentosLocal.Ruta`**: `_rutaBase` no termina en separador, así que
   `StartsWith(_rutaBase)` aceptaría `/app/uploadsX`. Hoy no es explotable (el nombre almacenado
   es un GUID generado por el servidor), pero se corrige comparando contra
   `_rutaBase + Path.DirectorySeparatorChar`.
2. **Warnings CS8604 heredados** en `AdminUsuariosController` L92, `SolicitudesPermisosController`
   L118 y `OrdenesCompraController` L117 (`Guid.Parse` sobre un claim que puede ser nulo).
   Cambiar a `Guid.TryParse`.
3. **Warnings NU1902/NU1903**: `System.Security.Cryptography.Pkcs` 9.0.11 con vulnerabilidad alta
   transitiva. Actualizar el paquete que la arrastra.
4. **`DocumentoDto.EsEliminado`** siempre es `false` en las respuestas (el listado filtra los
   eliminados). Es inocuo; se puede quitar o dejar como documentación del contrato.
5. **Duplicación de la regla de permiso** en `OrdenesCompraController.Recibir` (heredada de la
   Práctica 3): el controlador vuelve a consultar `PermisosUsuario` antes de llamar al servicio.
6. **`CrearBorrador` toma `usuarioId` de la query string** (heredado): permite crear órdenes a
   nombre de otro. Debería salir del claim.

---

## 10. Orden recomendado de trabajo

| # | Paso | Estimación |
|---|---|---|
| 1 | Dockerfile + `.dockerignore` + compose (api + db + worker) | 2-3 h |
| 2 | Migración automática al arrancar con reintentos | 30 min |
| 3 | Variables de entorno + `.env.example` + `.gitignore` | 30 min |
| 4 | Volúmenes y prueba `down`/`up` | 30 min |
| 5 | Fallback de correo a log para la revisión 1.5 | 1 h |
| 6 | API externa (servicio + operación real + rechazo controlado) | 2 h |
| 7 | README: Contenedor, API externa y los seis frentes | 1-2 h |
| 8 | Clon limpio, merge, tag `practica-4`, push, Moodle | 30 min |

---

## 11. Anexo — comandos y referencias

### 11.1 Comandos útiles

```powershell
# Build de la solución
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Suite completa (49 pruebas)
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Solo documentos
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx `
  --filter "FullyQualifiedName~DocumentoServiceTests"

# Migraciones (dotnet ef devuelve 1 por NU1902/NU1903 aunque tenga éxito)
dotnet ef migrations add <Nombre> `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

dotnet ef database update `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

dotnet ef migrations has-pending-model-changes `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

# Comprobar que .env está ignorado y no está en el historial
git check-ignore -v .env
git log --all --oneline -- .env
```

### 11.2 Contratos clave

```csharp
// IDocumentoService
Task<DocumentoDto> SubirDocumentoAsync(ArchivoEntrada archivo, Guid usuarioId);
Task<IEnumerable<DocumentoDto>> ListarDocumentosAsync(Guid usuarioId);
Task<DocumentoDescargaDto> DescargarDocumentoAsync(Guid id, Guid usuarioId);
Task EliminarDocumentoAsync(Guid id, Guid usuarioId);

// IAlmacenDocumentos
Task GuardarAsync(Stream contenido, string nombreAlmacenado, CancellationToken ct = default);
Task<Stream> AbrirAsync(string nombreAlmacenado, CancellationToken ct = default);
bool Existe(string nombreAlmacenado);
Task EliminarAsync(string nombreAlmacenado, CancellationToken ct = default);
```

### 11.3 Archivos a mirar primero

| Archivo | Para qué |
|---|---|
| `Core.Domain/Entities/Documento.cs` | modelo y borrado lógico |
| `Core.Application/Services/DocumentoService.cs` | reglas de negocio y autorización (RD-06) |
| `Presentation.API/Controllers/DocumentosController.cs` | endpoints y mapeo de códigos HTTP |
| `Infrastructure/Services/AlmacenDocumentosLocal.cs` | almacenamiento en disco y path traversal |
| `Core.Application.Tests/DocumentoServiceTests.cs` | evidencia de RF-DOC-01..04, RD-06 y RD-07 |
| `Presentation.API/Program.cs` | DI y punto donde añadir `MigrateAsync()` |
| `Presentation.API/appsettings.json` | valores que hay que sobrescribir en contenedor |
| `EmailWorker/Program.cs` | worker de correos que debe entrar en compose |
| `README.md` | fuente de verdad para la revisión manual |
