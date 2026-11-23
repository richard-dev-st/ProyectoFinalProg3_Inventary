# Bitácora — Asignación 3 (auditoría de la Práctica 5)

**Proyecto:** `ProyectoFinalProg3_Inventary` — Sistema de inventario (Programación III, ITLA)
**Autor:** Richard José Hernández Valdez
**Repositorio:** https://github.com/richard-dev-st/ProyectoFinalProg3_Inventary
**Rama de la entrega:** `feat/asignacion-3`
**Base de la auditoría:** `main` en `b1b9582` (incluye la Práctica 5 mergeada en el PR #22 y los
PR #23–#25)
**Tag de entrega:** `asignacion-3`
**Fecha:** 21–23/11/2026

---

## 1. Objetivo y alcance

Auditar la Práctica 5 (notificaciones, cola de correos y reportes) contra:

1. los criterios de `docs/criterios-notificaciones-reportes.md` (RF-NOT y RF-REP);
2. los casos que la revisión acumulada no ejercitó: entrada vacía/fuera de rango, ningún
   Administrador activo, dos solicitudes simultáneas, rango de fechas sin datos y destinatario de
   correo inválido;
3. las reglas de diseño RD-02, RD-03, RD-06, RD-07, RD-08, RD-10 y RD-11 aplicadas al código de
   la Práctica 5.

Cada defecto corregido tiene **un commit propio** (sin mezclar hallazgos), una prueba de regresión
que **falla en el commit anterior** y pasa en el tag, y una entrada en esta bitácora. Se declara
también, con justificación, un hallazgo de severidad baja que **no** se corrigió.

> **Nota de trazabilidad:** el trabajo se empezó sobre `feat/practica-5`, pero el repositorio avanzó
> a `main` (`b1b9582`) con los PR #22–#25. Los cuatro commits se reaplicaron sobre `main` con
> `git cherry-pick`; los hashes de abajo son los definitivos de `feat/asignacion-3`.

---

## 2. Resumen de hallazgos

| # | Tema | Criterio / regla | Severidad | Commit | Estado |
|---|---|---|---|---|---|
| 1 | `hasta=9999-12-31` desborda `DateTime` y responde 500 | RF-REP-05 + RD-07 | **Alta** | `0126647` | Corregido |
| 2 | La notificación y el evento de negocio no son atómicos | RF-NOT-01/02/03 | **Alta** | `ffe4be7` | Corregido |
| 3 | Permiso/descripción/motivo sin límite de longitud | RF-GP-01/04 + entrada fuera de rango | **Media** | `6cf2761` | Corregido |
| 4 | `Core.Application` depende de `Business.Inventory` | RD-03 | **Baja** | — | Declarado, no corregido |

Pruebas de regresión añadidas por los hallazgos: **6** (1 + 2 + 3). Cobertura adicional de los
casos no contemplados: **5** pruebas en el commit `558c948`. Suite final: **103/103**
(12 + 73 + 18).

---

## 3. Método y evidencia ejecutada

```powershell
# Línea base sobre main (antes de tocar código)
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
# → 92/92 (12 + 62 + 18)

# Verificación de cada regresión contra el commit anterior a su corrección
# (se revierte SOLO el archivo de producción y se conserva la prueba)
git checkout <commit-correccion>^ -- <archivo-de-produccion>
dotnet test ... --filter "FullyQualifiedName~<prueba>"
git checkout <commit-correccion> -- <archivo-de-produccion>

# Verificación final
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
# → 103/103 (12 + 73 + 18)
dotnet build frontend/Inventario.Web/Inventario.Web.csproj
# → 0 errores, 0 warnings
```

El detalle de los fallos pre-fix está en el **Anexo C**. La revisión de los seis frentes se ejecutó
sobre el stack Docker completo (API + SQL Server + worker). El registro de Microsoft
(`mcr.microsoft.com`) devolvió timeouts al bajar la imagen del SDK y el build con `docker compose up
--build` no pudo completarse (incidencia externa ya documentada en `contexto-sesion.md` §10.4). Se
usó el workaround histórico: `dotnet publish` local + imágenes delgadas sobre `aspnet:10.0` (que sí
estaba en el store local), sin modificar los `Dockerfile` ni `docker-compose.yml` del repositorio.
`docker compose up -d --no-build` levantó los tres servicios y Swagger respondió `200`. El resultado
de los seis frentes está en el **Anexo D**.

---

## 4. Hallazgo 1 — Desbordamiento de fecha en reportes

**Severidad:** Alta.

### Hallazgo

`GET /api/reportes/solicitudes?desde=2026-01-01&hasta=9999-12-31` y el equivalente de órdenes
terminan en `500`. El servicio hacía `hasta.Value.AddDays(1)` sin comprobar el límite superior de
`DateTime`; con el último día representable, `AddDays(1)` lanza
`ArgumentOutOfRangeException: The added or subtracted value results in an un-representable DateTime`.
El controlador solo captura `InvalidOperationException`, así que la excepción sube sin control. Con
`ASPNETCORE_ENVIRONMENT=Development` (valor de `docker-compose.yml`), la respuesta incluye la traza
de la pila.

### Qué se pidió

RF-REP-05 exige que `hasta` sea inclusivo y que un rango inválido responda `400`; RD-07 exige que
los errores no expongan trazas, rutas ni consultas internas. Una fecha ISO válida en el extremo del
dominio no puede tumbar el endpoint.

### Qué entregó el agente

En `82ad402` se corrigió que `hasta=AAAA-MM-DD` incluyera el día completo agregando
`hasta.Value.AddDays(1)` como límite exclusivo, con la prueba
`RF_REP_05_HastaFecha_IncluyeTodoElDia`. La prueba usó `2026-12-31`, un valor cómodo, y no el
extremo del tipo. La revisión acumulada (178 comprobaciones) tampoco probó `9999-12-31`.

### Por qué pasó

- **No hubo análisis de valores límite.** Se probó «un día normal» y no el máximo representable
  del tipo que se estaba manipulando.
- **La prueba se escribió para confirmar la implementación, no para desafiarla.** Repetía el mismo
  rango feliz que el código esperaba.
- **No se revisó qué excepciones pueden escapar del controlador.** El patrón del proyecto captura
  `InvalidOperationException`, pero nadie enumeró las excepciones que `DateTime` puede lanzar.

### Corrección

**Quién:** el agente, siguiendo la instrucción de la Asignación 3 (auditar y corregir la causa, no
el síntoma).

**Instrucción:** «si `hasta` es el último día representable no hace falta límite superior; si no,
se usa el día siguiente como límite exclusivo» y «toda prueba de rango debe incluir
`DateTime.MaxValue.Date`».

**Commit:** `0126647` — `fix(reportes): evita el desbordamiento de DateTime con hasta=9999-12-31 (hallazgo 1)`.

**Cambio:** en `ReporteService.ObtenerSolicitudesAsync` y `ObtenerOrdenesAsync` se agregó la guarda
`if (hasta.Value.Date < DateTime.MaxValue.Date)` antes de `AddDays(1)`. Cuando `hasta` es
`9999-12-31`, no se aplica límite superior (ese día ya es el máximo). Se añadió la prueba
`RF_REP_05_HastaFechaMaxima_NoDesborda`, que cubre ambos reportes.

**Por qué la corrección ataca la causa:** no se envuelve la excepción en un `try/catch` genérico ni
se devuelve un `400` artificial; se elimina la operación que desborda. Un `catch` habría ocultado
el síntoma y dejado el cálculo mal para el resto de fechas.

### Qué cambias

Se incorpora a `contexto-sesion.md` (§11) la regla de auditoría: **toda prueba de rango de fechas
incluye `DateTime.MaxValue.Date`**, y se documenta el commit y la prueba.

---

## 5. Hallazgo 2 — Evento de negocio y notificación no son atómicos

**Severidad:** Alta.

### Hallazgo

En los flujos que crean o resuelven una solicitud y en `enviar-aprobacion` de una orden, el evento
de negocio y la notificación se guardaban en **dos** llamadas a `SaveChanges`. Si la segunda falla
(caída de la base, timeout, error de disco), la solicitud queda creada/aprobada/rechazada **sin**
notificación, o el permiso queda aplicado **sin** avisar al solicitante. El usuario ve una
operación exitosa pero el destinatario nunca se entera. En el caso de `enviar-aprobacion`, la orden
pasa a `PendienteAprobacion` y los Administradores no reciben el aviso.

### Qué se pidió

RF-NOT-01/02/03 exigen que el evento notifique; la notificación es parte del resultado de la
operación, no un efecto secundario best-effort. La operación debe ser una unidad de trabajo: o se
persisten ambos, o no se persiste ninguno.

### Qué entregó el agente

`NotificacionService` ofrecía `CrearAsync` y `NotificarAdministradoresAsync`, cada uno con su propio
`SaveChanges`. `SolicitudPermisoService` y `OrdenCompraService` los llamaban **después** de su
propio `SaveChanges`. Las pruebas de la Práctica 5 verificaban el estado final con una base sana;
ninguna inyectaba un fallo entre los dos guardados.

### Por qué pasó

- **Las pruebas solo cubrían el camino feliz.** Un `SaveChanges` que falla a mitad de la operación
  no se simuló nunca.
- **La API del servicio invitaba al error.** Métodos «crear» que guardan por dentro hacen difícil
  componer una unidad de trabajo; el llamador no controla la transacción.
- **EF InMemory no reproduce fallos de escritura.** Todo guardado «funciona», así que el test no
  podía descubrir la ventana de inconsistencia.

### Corrección

**Quién:** el agente, con la instrucción de la Asignación 3.

**Instrucción:** «separa preparar de guardar: el dueño del caso de uso hace un único
`SaveChanges`» y «demuestra con un contexto que falla en el N-ésimo guardado que el evento y la
notificación se persisten juntos».

**Commit:** `ffe4be7` — `fix(notificaciones): guarda evento y notificacion en la misma unidad de trabajo (hallazgo 2)`.

**Cambio:**
- `INotificacionService`/`NotificacionService`: nuevos `PrepararAsync` y
  `PrepararAdministradoresAsync`, que agregan notificación y correo al contexto **sin** guardar.
  Los métodos antiguos se conservan para compatibilidad, pero los flujos de la Práctica 5 ya no los
  usan.
- `SolicitudPermisoService.CrearAsync/AprobarAsync/RechazarAsync` y
  `OrdenCompraService.EnviarAprobacionAsync`: preparan la notificación y hacen **un solo**
  `SaveChangesAsync` al final.
- Nueva prueba `NotificacionTransaccionTests` con un `ApplicationDbContext` que cuenta los
  guardados y lanza un `InvalidOperationException` en el N-ésimo. Comprueba que, si el guardado
  falla, no queda una solicitud sin notificación (y que con el código corregido hay un único
  guardado y todo se persiste).

**Por qué la corrección ataca la causa:** se cambia el contrato del servicio (preparar vs.
persistir), no se agrega un reintento ni un `try/catch` que «avise igual». El llamador ahora posee
la unidad de trabajo.

### Qué cambias

Se incorpora a `contexto-sesion.md` (§11) la regla: **todo flujo que emita una notificación usa
`Preparar*` y un único `SaveChanges`**, y toda prueba de evento+notificación debe inyectar un fallo
de guardado.

---

## 6. Hallazgo 3 — Entradas fuera de rango en permisos

**Severidad:** Media.

### Hallazgo

`POST /api/solicitudes` aceptaba un `permiso` de más de 100 caracteres, una `descripcion` de más de
500 y un `motivo` de rechazo de más de 500. Las columnas son `nvarchar(100)` y `nvarchar(500)`. En
EF InMemory la solicitud se guarda sin problema; en SQL Server el guardado falla (o trunca, según
configuración) y el controlador no lo traduce: el cliente recibe un `500` en vez de un `400`. Es el
caso «entrada vacía/fuera de rango» que la revisión acumulada no cubrió.

### Qué se pidió

RF-GP-01/04 exigen validar la entrada antes de persistir; el patrón del proyecto es
`InvalidOperationException` → `400`. Una entrada inválida no debe llegar a la base ni producir un
error interno.

### Qué entregó el agente

`SolicitudPermiso` validaba que `permiso` y `motivo` no estuvieran vacíos, pero **no** su longitud
máxima. `descripcion` no se validaba. Las pruebas existentes creaban textos cortos y no probaban
los límites de las columnas.

### Por qué pasó

- **EF InMemory no impone el tamaño de columna.** La prueba pasaba aunque el dato no cupiera en
  SQL Server: el entorno de pruebas era más permisivo que producción.
- **Validación pensada como «obligatorio», no como «contrato de datos».** Se cubrió el vacío, no
  el máximo.
- **No había una prueba de frontera por cada columna de texto.** El criterio no lo pedía
  explícitamente, así que se omitió.

### Corrección

**Quién:** el agente, con la instrucción de la Asignación 3.

**Instrucción:** «valida en la entidad el largo de cada texto que se persiste y agrega pruebas de
frontera» y «revisa si el mismo error se repite en otros campos».

**Commit:** `6cf2761` — `fix(permisos): valida el largo de permiso, descripcion y motivo (hallazgo 3)`.

**Cambio:** en `SolicitudPermiso` se recorta `permiso` y se valida `<= 100`; `descripcion` se
normaliza a `string.Empty` y se valida `<= 500`; `Rechazar` valida `motivo <= 500`. Se añadieron
tres pruebas: `RF_GP_01_PermisoFueraDeRango_SeRechaza`,
`RF_GP_01_DescripcionFueraDeRango_SeRechaza` y `RF_GP_04_MotivoFueraDeRango_SeRechaza`. En el
commit de cobertura (`558c948`) se añadió además `RF_GP_01_PermisoVacio_SeRechaza` (cadena vacía y
solo espacios).

**Por qué la corrección ataca la causa:** la regla vive en la entidad de dominio, que es el único
punto por el que pasan todas las altas; no se valida solo en el controlador (que podría
saltarse desde otro caso de uso).

### Qué cambias

Se incorpora a `contexto-sesion.md` (§11) la regla: **cada columna de texto con límite tiene una
prueba en el valor máximo + 1**, y se documenta que EF InMemory no sustituye la validación de
longitud.

---

## 7. Hallazgo 4 — `Core.Application` depende de `Business.Inventory` (RD-03, no corregido)

**Severidad:** Baja. Declarado y justificado, no corregido.

### Hallazgo

`Core.Application/Application.csproj` referencia `Business.Inventory` y
`IApplicationDbContext` expone `DbSet<OrdenCompra>` y `DbSet<Proveedor>` importando
`Business.Inventory.Entities`. `ReporteService` (Práctica 5) consulta `OrdenesCompra` desde
`Core.Application`. La dirección documentada en `AGENTS.md` es
`Business.Inventory → Core.Domain`; esta referencia va en sentido contrario.

### Qué se pidió

RD-03: el Core no debe depender del módulo de negocio.

### Qué entregó el agente

La dependencia es **previa a la Práctica 5** (se introdujo en `aa8d9b3` para que la capa de
aplicación coordinara órdenes). La Práctica 5 la reutilizó en `ReporteService` para el reporte de
negocio. La revisión acumulada no la marcó porque es una desviación estructural sin síntoma
funcional.

### Por qué pasó

- **La arquitectura se definió por capas, pero el flujo de órdenes se implementó en
  `Core.Application`.** El reporte de negocio de la Práctica 5 necesitaba `OrdenCompra` y reutilizó
  esa vía en lugar de crear un contrato propio del módulo.
- **No hay una verificación automática de dependencias entre proyectos.** Sin una prueba de
  arquitectura (por ejemplo, comprobar que `Core.Application` no referencia `Business.Inventory`),
  la violación pasa desapercibida.

### Por qué no se corrige

Corregirla exige separar `IApplicationDbContext` en dos contratos (`IApplicationDbContext` del Core
e `IInventoryDbContext` del negocio), mover `OrdenCompraService`, `IOrdenCompraService`,
`IOrdenCompraRepository` y el reporte de órdenes a `Business.Inventory`, y resolver la ubicación de
los DTOs y de `INotificacionService` que esos servicios consumen. `Business.Inventory` hoy solo
referencia `Core.Domain`; para mover los servicios habría que hacerlo depender de
`Core.Application` o mover los DTOs/contratos a `Core.Domain`, lo que es un rediseño transversal de
la solución, no una corrección puntual de la Práctica 5. Se declara como **deuda arquitectónica de
severidad baja** (no rompe ningún criterio funcional ni la suite) y se registra en
`contexto-sesion.md`.

### Qué cambias

Se deja constancia en `contexto-sesion.md` (§11) de la deuda RD-03 y de la condición para
cerrarla. No se toca código para no introducir una regresión en la máquina de estados ni en los
contratos actuales.

---

## 8. Anexo A — Casos no cubiertos auditados

| Caso | Resultado | Evidencia |
|---|---|---|
| Entrada vacía | Rechazada con `InvalidOperationException` → `400` | `RF_GP_01_PermisoVacio_SeRechaza` (vacío y solo espacios) |
| Entrada fuera de rango | Rechazada con `InvalidOperationException` → `400` | `RF_GP_01_PermisoFueraDeRango_SeRechaza`, `RF_GP_01_DescripcionFueraDeRango_SeRechaza`, `RF_GP_04_MotivoFueraDeRango_SeRechaza` (hallazgo 3) |
| Ningún Administrador activo | La solicitud se guarda; 0 notificaciones y 0 correos; no lanza | `RF_NOT_01_SinAdministradoresActivos_GuardaSolicitudSinNotificar` |
| Rango de fechas sin datos | Ambos reportes devuelven `total = 0`, `montoTotalUsd = 0` y grupos vacíos | `RF_REP_05_RangoSinDatos_DevuelveCero` |
| Destinatario de correo inválido | `EmailService` devuelve `false` y deja `UltimoError`; el procesador lo registra y reintenta hasta 3 | `RF_NOT_10_DestinatarioInvalido_DevuelveFalseYRegistraError` |
| Dos solicitudes simultáneas | La máquina de estados rechaza la segunda resolución **secuencial**; no hay token de concurrencia en BD, por lo que dos resoluciones exactamente simultáneas podrían leer ambas `Pendiente`. Se declara **riesgo residual de severidad baja**: los criterios ejercitan operaciones secuenciales, requiere dos Administradores en el mismo instante y corregirlo implica concurrencia optimista + migración. | Análisis de código (`SolicitudPermiso.CambiarEstado`) y criterios RF-GP |

> La prueba de concurrencia no se agregó porque EF InMemory no reproduce el aislamiento de SQL
> Server. Se documenta como limitación conocida en lugar de simular una atomicidad que la prueba no
> podría garantizar.

---

## 9. Anexo B — Auditoría RD-02, RD-03, RD-06, RD-07, RD-08, RD-10 y RD-11

| Regla | Veredicto | Evidencia |
|---|---|---|
| **RD-02** — lógica de negocio fuera del handler | ✅ Cumple | `ReportesController`, `NotificacionesController` y `CorreosController` solo leen claims/DTO y delegan; las reglas viven en `ReporteService`, `NotificacionService` y las entidades. |
| **RD-03** — Core no depende del módulo de negocio | ❌ Desviación | `Application.csproj` referencia `Business.Inventory`; `IApplicationDbContext` importa `Business.Inventory.Entities` en 3 archivos. Ver **Hallazgo 4** (baja, no corregido). |
| **RD-06** — autorización del lado del servidor | ✅ Cumple para los criterios | `[Authorize]` en notificaciones/reportes; `[Authorize(Roles = "Administrador")]` en `/api/correos`; el rol de los reportes sale del token. Nota: un cambio de rol exige nuevo login (comportamiento documentado en `contexto-sesion.md` §0); no es una violación de CT-02. |
| **RD-07** — sin trazas, rutas ni consultas en errores | ✅ Cumple tras el hallazgo 1 | Los errores de negocio se traducen a `400`/`404`; el 500 con traza del desbordamiento se eliminó en la causa. |
| **RD-08** — sin lógica en el controlador / sin credenciales | ✅ Cumple | Controladores finos; `.env` no versionado (`.env.example` sí); `gitleaks` pasó en cada commit («no leaks found»). |
| **RD-10** — secretos solo por variables de entorno | ✅ Cumple | `docker-compose.yml` inyecta `EmailSettings__*` y `JwtSettings__SecretKey` desde `${…}`; `appsettings.json` no contiene la contraseña SMTP ni la clave de la API externa. |
| **RD-11** — manejo consistente de zona horaria | ✅ Cumple | 0 usos de `DateTime.Now` y 24 de `DateTime.UtcNow` en servicios y entidades; los reportes documentan UTC y los extremos inclusivos. |

---

## 10. Anexo C — Evidencia de regresión (falla antes, pasa después)

| Hallazgo | Commit de corrección | Archivo revertido para probar | Prueba | Pre-fix | Post-fix |
|---|---|---|---|---|---|
| 1 | `0126647` | `Core.Application/Services/ReporteService.cs` | `RF_REP_05_HastaFechaMaxima_NoDesborda` | **Falla**: `ArgumentOutOfRangeException: … un-representable DateTime` | Pasa |
| 2 | `ffe4be7` | `INotificacionService.cs`, `NotificacionService.cs`, `SolicitudPermisoService.cs`, `OrdenCompraService.cs` | `NotificacionTransaccionTests` (2 pruebas) | **Fallan**: `InvalidOperationException: Fallo simulado de base de datos en el guardado 2` | Pasan |
| 3 | `6cf2761` | `Core.Domain/Entities/SolicitudPermiso.cs` | `*FueraDeRango_SeRechaza` (3 pruebas) | **Fallan**: no se lanza `InvalidOperationException` | Pasan |

Comando usado (repetible desde un clon limpio en el tag):

```powershell
git checkout <commit-correccion>^ -- <archivo-de-produccion>
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx `
  --filter "FullyQualifiedName~<prueba>"
git checkout <commit-correccion> -- <archivo-de-produccion>
```

La suite completa en el tag corre con **un solo comando**, el documentado en el README:

```powershell
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
```

---

## 11. Anexo D — Verificación final y revisión de los seis frentes

| Comprobación | Resultado |
|---|---|
| `dotnet test` (suite completa, backend) | **103/103** (12 + 73 + 18) |
| `dotnet build` del backend | OK (solo warnings heredados NU1902/NU1903) |
| `dotnet build` del frontend | 0 errores, 0 warnings |
| `gitleaks` en cada commit | «no leaks found» |
| Commits por hallazgo | 3 commits de corrección + 1 de cobertura + 1 de documentación |
| Stack Docker (API + SQL Server + worker) | Arriba; `/swagger/index.html` responde `200` |

Revisión acumulada de los seis frentes sobre el stack (`docker compose up -d --no-build`):

| Frente | Comprobaciones | Fallos |
|---|---|---|
| 1–4. Registro/activación, sesión, autorización de admin, contraseñas | 39 | 0 |
| 5–6. Permisos y módulo de negocio | 32 | 0 |
| Notificaciones (RF-NOT-01…07, 09, 11, 12) | 44 | 0 |
| Reportes (RF-REP-01…05) + RF-NOT-06/10 con fallo forzado de SMTP | 46 | 0 |
| Documentos (altas, permisos, borrado, persistencia tras `down` sin `-v`) | 19 | 0 |
| **Total** | **180** | **0** |

> El bloque de RF-NOT-10 espera hasta 180 s a que el worker agote los tres intentos. Con varios
> Administradores activos, una sola solicitud encola más de un lote (10 correos por ciclo de 10 s),
> así que una espera fija de 40 s producía un falso negativo del guion, no del producto. La espera
> se cambió por sondeo del estado del correo; la primera corrida con el guion anterior se conserva
> como evidencia de esa corrección del propio instrumento de revisión.

---

## 12. Cierre — por qué se escaparon los defectos

Los tres defectos comparten una causa de proceso: **las pruebas de la Práctica 5 se escribieron
para confirmar el camino feliz, no para romperlo.**

- El criterio de reportes pedía inclusividad de `hasta`, y se probó con una fecha normal; nadie
  preguntó qué pasa en el borde del tipo.
- El criterio de notificaciones pedía que el evento notificara, y se comprobó con una base sana;
  nadie preguntó qué pasa si el segundo guardado falla.
- El criterio de permisos pedía entradas válidas, y se probó con textos cortos; nadie contrastó el
  límite de la columna porque EF InMemory lo tolera.

**Cambios de proceso que quedan en `contexto-sesion.md` (§11):**

1. Toda prueba de rango de fechas incluye `DateTime.MaxValue.Date`.
2. Todo flujo evento + notificación usa `Preparar*` y un único `SaveChanges`; la prueba inyecta un
   fallo de guardado.
3. Cada columna de texto con límite tiene una prueba en el valor máximo + 1; no se confía en EF
   InMemory para validar longitudes.
4. La deuda RD-03 queda registrada con su condición de cierre (contratos separados y migración de
   servicios al módulo de negocio).
