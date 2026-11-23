# Bitácora — Asignación 4: Historial de auditoría

**Fecha:** 23/11/2026 · **Rama de trabajo:** `feat/registro-auditoria` · **Entrega:** tag `asignacion-4`

Esta bitácora documenta el trabajo realizado con el agente de código de OpenCode, incluidos los
casos en que se equivocó y cómo se corrigieron.

---

## 1. Tareas delegadas al agente

| # | Lo que le pedí (texto de la sesión) | Lo que devolvió | Cómo lo verifiqué |
|---|---|---|---|
| 1 | «¿Cómo puedo llevar el paso 2 a cabo? Solo quiero que me digas para saber cómo debo hacerlo» | Una explicación del patrón: capturar el valor anterior antes de mutar, mutar, capturar el nuevo, `Preparar(...)`, un solo `SaveChangesAsync`; tabla de los 8 eventos y advertencias (no serializar la entidad completa, no guardar contraseñas/códigos, `EntidadId` como `string`). No escribió código. | Leí los cuatro servicios y confirmé que el patrón encajaba con el `Preparar*` ya usado en Práctica 5. |
| 2 | «¿Puedes ayudarme a implementar lo del registro de cada evento?» | Constantes en `Core.Domain/Constants`, inyección de `IAuditoriaService` en `SolicitudPermisoService`, `DocumentoService`, `AdminUsuarioService` y `AuthService`, los 8 enganches, el cambio de firma de `ForzarRestablecimientoPasswordAsync(usuarioId, adminId)` y el ajuste del controlador para tomar el claim. Además actualizó los tests que instanciaban esos servicios. | `dotnet build` y `dotnet test`: primero falló (ver §2), después 112/112. Revisé que cada registro use el admin como actor y el usuario afectado como `EntidadId`. |

**Estado final verificado:**

```powershell
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
dotnet test  ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
# Build succeeded. 0 Error(s)
# Passed: 12 (dominio) + 89 (aplicación) + 18 (negocio) = 119/119
```

---

## 2. Casos en que el agente se equivocó y cómo los corregí

### Error 1 — Firma de `Preparar` con la fecha en medio de los parámetros

El agente dejó la interfaz así:

```csharp
RegistroAuditoria Preparar(Guid usuarioId, string usuarioEmail, string accion,
    string entidad, string entidadId, DateTime fechaHoraUtc,
    string? valorAnterior = null, string? valorNuevo = null);
```

Al implementar los enganches pasé los valores en el orden lógico (actor, acción, entidad, id,
anterior, nuevo) y el compilador respondió:

```
error CS1503: Argument 6: cannot convert from 'string' to 'System.DateTime'
   AdminUsuarioService.cs(59,17)
   AdminUsuarioService.cs(95,17)
   SolicitudPermisoService.cs(68,17)
   DocumentoService.cs(72,17)
```

**Detección:** `dotnet build` de la solución, antes de tocar nada más.

**Corrección:** moví la fecha al final y la hice opcional en interfaz e implementación:

```csharp
RegistroAuditoria Preparar(Guid usuarioId, string usuarioEmail, string accion,
    string entidad, string entidadId,
    string? valorAnterior = null, string? valorNuevo = null,
    DateTime? fechaHoraUtc = null);
```

`AuditoriaService.Preparar` usa `fechaHoraUtc ?? DateTime.UtcNow`. Además, en
`AprobarAsync` y `RechazarAsync` extraje `var fecha = DateTime.UtcNow;` una sola vez y la pasé
tanto a la mutación de la entidad como al registro, para que la fecha de resolución y la de
auditoría sean exactamente la misma (RD-11).

### Error 2 — Tests con un administrador que no existía en la base de datos

Varios tests del baseline usaban `var adminId = Guid.NewGuid();` como actor. Como ahora el
servicio carga al actor para desnormalizar su email, esos GUID inexistentes provocaron 5 fallos
y, tras corregir unos, 3:

```
Failed Core.Application.Tests.SolicitudPermisoServiceTests.RF_GP_04_RechazarAsync_GuardaELMotivo
Failed Core.Application.Tests.SolicitudPermisoServiceTests.RF_GP_05_AprobarAsync_ConcedePermiso
Failed Core.Application.Tests.SolicitudPermisoServiceTests.RF_GP_04_RechazarAsync_SinMotivo_LanzaExcepcion
```

**Detección:** salida de `dotnet test` con el mensaje de la excepción de actor no válido.

**Corrección:** sembré un `Usuario` real en cada prueba y pasé `admin.Id` en lugar del GUID
aleatorio:

```csharp
var solicitante = new Usuario("felipe@test.com", "password123");
var admin = new Usuario("admin-felipe@test.com", "password123");
context.Usuarios.AddRange(solicitante, admin);
...
await servicio.AprobarAsync(solicitud.Id, admin.Id);
```

No cambié la lógica de negocio: solo los datos de prueba, para que representen un escenario
válido (un admin autenticado existe en la BD).

### Error 3 — Supuesto inicial inválido en la prueba de desactivación

La nueva prueba `RF_AUD_04_Desactivar_RegistraAuditoria` esperaba `"true"` en el valor anterior,
pero `Usuario` nace con `Activo = false`:

```
Assert.Contains() Failure: Sub-string not found
String:    "{\"Activo\":false}"
Not found: "true"
```

**Detección:** `dotnet test --filter FullyQualifiedName~RF_AUD`.

**Corrección:** dejé el escenario explícito llamando a `usuario.Reactivar()` antes de sembrarlo,
con un comentario que explica por qué (`El usuario nace inactivo; lo activamos para auditar el
cambio`). La prueba pasó sin tocar el servicio.

---

## 3. Paralelo: registro de auditoría ↔ commit de git

Un registro de auditoría es el `git log` de los datos de la aplicación. Un commit es el
`git log` del código. Campo por campo:

| Registro de auditoría (tabla `Auditoria`) | Commit de git (`git show`) | Comentario |
|---|---|---|
| `Id` (`long` autoincremental) | Hash SHA-1 (40 hexadecimales) | Ambos identifican una entrada de forma única. El hash es criptográfico y apunta además a un árbol completo; el `Id` es secuencial y propio de la base de datos. |
| `UsuarioId` + `UsuarioEmail` | Autor: `name` y `email` | Coinciden en «quién». El registro guarda también el ID interno del usuario; git solo conoce nombre y correo declarados en la configuración, sin ID de aplicación. |
| `Accion` (`Aprobar`, `Crear`, `CambiarRol`…) | Mensaje corto (*subject*) del commit | «Qué hizo». La auditoría usa un vocabulario controlado para poder filtrar; el mensaje de git es texto libre y no validado. |
| `Entidad` + `EntidadId` | Rutas y blobs modificados | «Sobre qué». Git no conoce entidades de dominio: lo más parecido son las rutas de archivo y los hashes de blob. Aquí `EntidadId` identifica al usuario/solicitud/documento afectado. |
| `FechaHoraUtc` | `AuthorDate` y `CommitDate` | «Cuándo». La auditoría guarda una sola marca UTC; git guarda dos fechas con zona horaria (autor y commitente), que pueden diferir. |
| `ValorAnterior` + `ValorNuevo` (JSON) | `diff` / *patch* | «Qué cambió». El diff de git es línea por línea y cubre todo el archivo; la auditoría guarda un *snapshot* JSON de solo los campos auditables, no la entidad completa, para no filtrar secretos. |
| — (no existe) | Cuerpo del mensaje (*body*) | Git permite una descripción larga y libre; la auditoría es estructurada y no tiene campo de texto libre adicional. |
| — (no existe) | Padre(s), árbol, firmas | Git encadena commits mediante hashes: reescribir la historia cambia los hashes. La auditoría no encadena hashes: su inmutabilidad se garantiza porque la aplicación no expone ninguna operación de edición ni borrado (RF-AUD-06). |

**Qué guarda la auditoría que no guarda git:** el identificador de la entidad de negocio, el
email desnormalizado del actor (sobrevive aunque el usuario cambie o se borre), el valor
anterior y nuevo en forma semántica y filtrable, y una única marca UTC homogenénea.

**Qué guarda git que no guarda la auditoría:** la estructura completa del árbol de archivos,
el diff textual, los padres de cada commit (historial ramificado), las firmas y las dos fechas
con zona horaria.

---

## 4. Notas de entrega

- No se versionan secretos: la documentación usa solo marcadores `<password>` y `<codigo>`.
- `README.Docker.md` sigue sin versionarse, según las restricciones del repositorio.
- Para entregar, desde la rama de trabajo:

```powershell
git tag asignacion-4
git push origin --tags
```

El punto evaluado es el tag. No se hizo commit ni tag durante esta sesión: los cambios quedaron
en el *working tree* para revisión del usuario.
