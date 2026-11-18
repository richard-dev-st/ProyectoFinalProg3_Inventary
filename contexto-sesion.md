# Contexto de sesión — Práctica 5 (notificaciones, cola de correos y reportes)

**Proyecto:** `ProyectoFinalProg3_Inventary` — Sistema de inventario (Programación III, ITLA)
**Autor:** Richard José Hernández Valdez
**Repositorio:** https://github.com/richard-dev-st/ProyectoFinalProg3_Inventary
**Fecha del corte:** 18/11/2026
**Rama actual:** `feat/practica-5` (local, sin upstream)
**HEAD:** `0da3680 docs(readme): secciones de notificaciones, cola de correos y reportes de la practica 5`
**`main`:** `fa116f2 docs(contexto): actualiza el handoff para la practica 5 y el merge del frontend` (pusheado)
**Tag `practica-4`:** objeto `581c938` → commit `901a47f`
**Solución backend:** `ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` (.NET 10)
**Frontend:** `frontend/Inventario.Web` (Blazor Web App, .NET 10)

> Este documento sustituye al handoff de la Práctica 4. La Práctica 4 está **cerrada y
> etiquetada** (`practica-4`); su estado se resume en §4 porque la revisión acumulada de la
> Práctica 5 vuelve a evaluar los mismos seis frentes.

---

## 1. Resumen ejecutivo

| Bloque | Estado |
|---|---|
| Práctica 4 (documentos, contenedor, API externa, revisión) | ✅ Cerrada, `practica-4` en `901a47f` |
| Frontend Blazor — login y sesión | ✅ Completo y mergeado a `main` (`51b3eb4`, pusheado) |
| Documentación de Práctica 5 (1.1) | ✅ `AGENTS.md` + criterios en `a90538b`, anterior a todo código |
| Frontend — pantallas de negocio | ⬜ No iniciado |
| **Práctica 5** | 🟡 En curso |

Estado por punto de la Práctica 5:

| Punto | Descripción | Estado |
|---|---|---|
| 1.1 | `AGENTS.md` + `docs/criterios-notificaciones-reportes.md` | ✅ Hecho y commiteado (`a90538b`) |
| 1.2 | Notificaciones RF-NOT-01..07 | ✅ Entidad, servicio, controlador, preferencia y enganches (`7f67c68`) |
| 1.3 | Cola de correos RF-NOT-08..13 | ✅ Reintentos (máx. 3), `Fallido`, `GET /api/correos`, idempotencia (`59bd20c`) |
| 1.4 | Reportes con agregación RF-REP-01..05 | ✅ `/api/reportes/solicitudes` y `/ordenes` con rol y fechas (`f52a070`) |
| 1.5 | README: secciones **Notificaciones** y **Reportes** | ✅ Escritas y commiteadas (`0da3680`) |
| 1.6 | Revisión acumulada de los seis frentes | 🟡 Funcionaban en Práctica 4; hay que repetirla |
| — | Tag `practica-5` + push + Moodle | ⬜ 0 % |

Verificación al cierre de esta sesión (18/11/2026):

```
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx   → OK
dotnet build frontend/Inventario.Web/Inventario.Web.csproj                   → 0 errores, 0 warnings
dotnet test  ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx   → 77/77 (12 + 47 + 18)
```

> El build del backend mantiene warnings heredados (CS8618 en `Usuario`, CS0472 en `OrdenCompra`,
> CS8613 en `AuthService`, CS8603 en `OrdenCompraRepository`). No bloquean.

---

## 2. Estado de Git y entorno

### 2.1 Ramas y commits

```
main                  fa116f2  docs(contexto): actualiza el handoff para la practica 5 y el merge del frontend
feat/practica-5       0da3680  docs(readme): secciones de notificaciones, cola de correos y reportes de la practica 5
feat/frontend-login   7eaee64  feat(frontend-login): login, sesion persistente y proteccion de rutas
```

- `main` (`fa116f2`) ya está pusheado; incluye el merge del frontend (`51b3eb4`) y el handoff.
- `feat/practica-5` salió de `main` en `fa116f2` y es **local** (sin upstream todavía).
- Working tree: solo `README.Docker.md` como `??` (lo generó Visual Studio). **No se debe
  versionar.**

### 2.2 Tags existentes

```
asignacion-1, asignacion-2
practica-1
practica-2, practica-2-defecto-1..6
practica-3, practica-3-defecto-1..4
practica-4
```

`practica-5` aún no existe.

### 2.3 Regla de oro de esta práctica

El commit que agrega `AGENTS.md` y `docs/criterios-notificaciones-reportes.md` **tiene que ser
anterior** al primer commit del motor de notificaciones. Se evalúa en `git log`, no en la
intención. Orden recomendado:

```
1. ✅ merge de feat/frontend-login a main (`51b3eb4`, pusheado en `fa116f2`)
2. ✅ rama feat/practica-5 creada desde `main`
3. ✅ commit SOLO de documentación (`a90538b`): AGENTS.md + criterios
4. ✅ primer commit de código (`7f67c68`): notificaciones RF-NOT-01..07
```

> Decisión ya fijada en los criterios: evento de negocio = `enviar-aprobacion` de `OrdenCompra`;
> máximo de intentos de la cola = **3**; endpoints `/api/notificaciones`, `/api/correos` y
> `/api/reportes/*`.

### 2.4 Entorno local

- Windows + PowerShell 7. **No hay `&&`, `tail` ni `grep`**: usar `;`, `Select-String`,
  `Select-Object`, `Out-Host`.
- Backend en contenedor: `docker compose up -d` → API `http://localhost:8080/swagger`.
- Frontend en local: `dotnet run --project frontend/Inventario.Web --launch-profile http`
  → `http://localhost:5299`.
- Base local histórica: SQL Server `localhost`, base `InventarioBP3` (solo si se corre la API
  fuera de Docker; dentro del contenedor la base es `InventarioBP4`).
- `dotnet ef` devuelve código 1 por warnings NU1902/NU1903 aunque el comando tenga éxito.
- Docker Desktop debe estar arrancado; si `docker compose ps` falla con
  `npipe:////./pipe/dockerDesktopLinuxEngine`, el motor todavía no está listo.

### 2.5 Protecciones de repositorio ya verificadas

- `.gitignore` excluye `*.env` y `uploads/`.
- `.env` **no existe** ni en el working tree ni en el historial (`git log --all -- .env` vacío).
- `.env.example` **no** cae en el patrón `*.env` (verificado con `git check-ignore`).
- Hook de pre-commit con `gitleaks` activo.
- **Riesgo de esta práctica:** los criterios y el README deben hablar de credenciales SMTP sin
  escribirlas. Nunca pegar una clave real en `AGENTS.md`, README, criterios ni commits.

---

## 3. Arquitectura actual

### 3.1 Backend (sin cambios estructurales desde Práctica 4)

```
Presentation.API  (ASP.NET Core Web API, JWT, controladores)
      │
      ▼
Infrastructure    (EF Core + SQL Server, DbContext, migraciones, SMTP, almacenamiento)
      │
      ├──────────────► Core.Application  (servicios de caso de uso, DTOs, interfaces)
      │                        │
      ▼                        ▼
Business.Inventory  ◄──── Core.Domain  (entidades y enums compartidos)
```

| Proyecto | Responsabilidad |
|---|---|
| `Core.Domain` | `Usuario`, `SolicitudPermiso`, `PermisoUsuario`, **`CorreoEnCola`**, `TokenRevocado`, `Documento`; enums `RolUsuario`, `EstadoSolicitudPermiso`, `EstadoOrden` |
| `Business.Inventory` | `OrdenCompra`, `DetalleOrden`, `Producto`, `Proveedor`; máquina de estados de `OrdenCompra` |
| `Core.Application` | `AuthService`, `SolicitudPermisoService`, `OrdenCompraService`, `AdminUsuarioService`, `DocumentoService`; DTOs e interfaces |
| `Infrastructure` | `ApplicationDbContext`, migraciones, `PasswordService`, `JwtService`, `EmailService`, `ProcesadorCorreosWorker`, `AlmacenDocumentosLocal`, `CurrencyApiTasaCambioService` |
| `Presentation.API` | Controladores REST, autorización por rol, mapeo de excepciones, DI, JWT |
| `EmailWorker` | Worker independiente que vacía `CorreosEnCola`. **No lo arranca la API.** |
| Tests | `Core.Domain.Tests` (12), `Core.Application.Tests` (31), `Bussiness.inventory.Tests` (19) = **62** |

### 3.2 Frontend (nuevo)

```
frontend/Inventario.Web
├── Components/
│   ├── App.razor                 ← render mode global InteractiveServer, prerender: false
│   ├── Routes.razor              ← gate de sesión + AuthorizeRouteView
│   ├── Layout/
│   │   ├── MainLayout.razor      ← sidebar + @Body
│   │   ├── NavMenu.razor         ← solo Home (Counter/Weather eliminados)
│   │   └── RedirectToLogin.razor ← redirige con returnUrl
│   └── Pages/
│       ├── Login.razor           ← formulario + validación + returnUrl
│       ├── Home.razor            ← [Authorize], muestra email/rol, logout
│       └── Error.razor / NotFound.razor
├── Models/                       ← LoginRequest, LoginResponse, UsuarioSesion
├── Services/
│   ├── AuthService.cs            ← login / me / logout contra la API
│   ├── SesionActual.cs           ← estado + ProtectedSessionStorage
│   └── SesionAuthStateProvider.cs← traduce la sesión a ClaimsPrincipal
├── Program.cs                    ← DI, HttpClient tipado, autorización
└── appsettings.json              ← "ApiBaseUrl": "http://localhost:8080"
```

---

## 4. Lo que ya existe y sirve para la Práctica 5

### 4.1 Entidad `CorreoEnCola` (completada en `59bd20c`)

`Core.Domain/Entities/CorreoEnCola.cs` ya tiene:

```csharp
public string Estado { get; private set; }   // Pendiente | Enviado | Fallido
public int Intentos { get; private set; }
public string? UltimoError { get; private set; }
public DateTime? FechaUltimoIntento { get; private set; }

public void RegistrarIntento();
public void MarcarComoEnviado();              // idempotente (RF-NOT-12)
public void RegistrarFallo(string error, int maximoIntentos); // pasa a Fallido al agotar
public bool PuedeProcesarse(int maximoIntentos);
```

Migración asociada: `AgregarReintentosColaCorreos`.

### 4.2 Procesamiento de la cola (corregido en `59bd20c`)

- `Infrastructure/Services/ProcesadorCorreos.cs`: lógica probada sin host. Toma hasta 10
  `Pendiente` con `Intentos < 3`, registra el intento **antes** de enviar, guarda tras cada
  correo, reintenta hasta 3 y deja `Fallido` con `UltimoError` (`MaximoIntentos = 3`).
- `ProcesadorCorreosWorker` solo lo invoca cada 10 s y maneja la cancelación.
- `GET /api/correos` (`CorreosController`, solo Administrador) expone estado, intentos, fechas y
  último error (RF-NOT-11).
- Un correo `Enviado` o `Fallido` no vuelve a seleccionarse (RF-NOT-12).

### 4.3 `EmailService` (sin cambios, con fallback de revisión)

`Infrastructure/Services/EmailService.cs`:

- Lee `EmailSettings:SmtpServer`, `SenderEmail`, `SenderPassword`, `SmtpPort`, `UseSsl`.
- Si alguno es valor de ejemplo (`tu_correo@gmail.com`, `valor-de-ejemplo`, etc.), **no envía**:
  escribe `[FALLBACK CORREO]` en el log del worker y devuelve **`true`**.

**Consecuencia para la rúbrica:** con el `.env.example` tal cual, *ningún* correo falla nunca.
Para demostrar RF-NOT-10 hay que documentar en el README cómo provocar un fallo real:
usar un `SMTP_SERVER` que **no** sea valor de ejemplo y que no resuelva (p. ej.
`smtp.invalido.local`), con credenciales no-ejemplo, para que `SmtpClient` lance y
`EnviarCorreoAsync` devuelva `false`.

### 4.4 Enganches de notificaciones (implementados en `7f67c68`)

`SolicitudPermisoService` ya notifica: al crear (Administradores activos), al aprobar y al
rechazar (solicitante, con motivo). `OrdenCompraService.EnviarAprobacionAsync` notifica a los
Administradores (evento RF-NOT-03). `NotificacionService` crea la notificación interna y encola
el correo solo si `Usuario.RecibeCorreos` es `true`.

### 4.5 Inventario de endpoints actuales

| Módulo | Ruta base | Operaciones |
|---|---|---|
| Auth | `api/auth` | `Login`, `me`, `logout`, `registrar`, `activar`, recuperación |
| Admin usuarios | `api/admin/usuarios` | listado y gestión (solo Administrador) |
| Solicitudes | `api/solicitudes` | `POST`, `GET pendientes`, `GET mias`, `POST {id}/aprobar`, `POST {id}/rechazar`, `GET /api/mis-permisos` |
| Órdenes | `api/ordenescompra` | `crear-borrador`, `{id}/enviar-aprobacion`, `{id}/aprobar`, `{id}/recibir`, `{id}/cancelar`, `{id}/total-en-dop` |
| Documentos | `api/documentos` | `subir`, `GET`, `{id}/descargar`, `DELETE {id}` |
| Inventario | `api/inventario/exportar` | CSV de productos |
| Permisos | `api/solicitudes` | ver arriba |
| Notificaciones | `api/notificaciones` | `GET` propias (`?soloNoLeidas`), `POST {id}/leer`, `GET/PUT preferencia` |
| Correos | `api/correos` | `GET` cola (solo Administrador, `?estado=`) |
| Reportes | `api/reportes` | `GET solicitudes`, `GET ordenes` (`?desde=&hasta=`) |

### 4.6 Estado de los huecos (actualizado tras `f52a070`)

Ya existen:

- Entidad `Notificacion`, `DbSet` y migración `AgregarNotificacionesYPreferenciaCorreo`.
- `Usuario.RecibeCorreos` (columna nueva, por defecto `true`).
- `INotificacionService` / `NotificacionService` y `api/notificaciones` (`GET`, `POST {id}/leer`,
  `GET/PUT preferencia`).
- Enganches RF-NOT-01/02 en `SolicitudPermisoService` y RF-NOT-03 en
  `OrdenCompraService.EnviarAprobacionAsync`.
- Reintentos limitados (3), estado `Fallido`, `UltimoError` e idempotencia en `CorreoEnCola` y
  `ProcesadorCorreos` (migración `AgregarReintentosColaCorreos`).
- `GET /api/correos` solo para Administrador (`CorreosController`).
- `IReporteService` / `ReporteService` y `api/reportes/solicitudes` + `api/reportes/ordenes` con
  `GroupBy`/`Count`/`Sum`, filtro por rol y rango inclusivo (`ReportesController`).

Siguen pendientes:

- Revisión acumulada de los seis frentes y tag `practica-5`.

### 4.7 Forma de las respuestas de reportes

`GET /api/reportes/solicitudes?desde=2026-01-01&hasta=2026-12-31`:

```json
{ "desde": "...", "hasta": "...", "total": 10,
  "grupos": [ { "estado": "Pendiente", "cantidad": 4 } ] }
```

`GET /api/reportes/ordenes?desde=2026-01-01&hasta=2026-12-31`:

```json
{ "desde": "...", "hasta": "...", "totalOrdenes": 8, "montoTotalUsd": 1250.00,
  "grupos": [ { "proveedorId": 1, "proveedor": "Proveedor Uno",
                "estado": "Aprobada", "cantidadOrdenes": 3, "montoTotalUsd": 700.00 } ] }
```

Administrador ve todo; un Estándar solo sus solicitudes u órdenes. Un rango con `hasta < desde`
responde `400`.

---

## 5. Frontend Blazor — lo que quedó funcionando

### 5.1 Flujo de sesión

```
Login.razor
  └─ POST /api/auth/login
       └─ SesionActual.IniciarAsync(token, email, rol, expiracion)
            ├─ memoria del circuito
            └─ ProtectedSessionStorage  (claves: sesion.token, sesion.email, sesion.rol, sesion.expiracion)
  └─ Nav.NavigateTo(returnUrl ?? "/")

Home.razor  [Authorize]
  └─ GET /api/auth/me  con Authorization: Bearer <token>
  └─ muestra email + rol, botón Cerrar sesión

Logout
  └─ POST /api/auth/logout (si hay token)
  └─ SesionActual.CerrarAsync()  → borra memoria y sessionStorage
  └─ SesionActual.Cambio → SesionAuthStateProvider.NotifyAuthenticationStateChanged
```

### 5.2 Autorización

- `SesionAuthStateProvider : AuthenticationStateProvider` construye un `ClaimsIdentity` con
  `ClaimTypes.Name` (email) y `ClaimTypes.Role` (rol), `authenticationType = "SesionInventario"`.
- `Program.cs`: `AddScoped<AuthenticationStateProvider, SesionAuthStateProvider>()`,
  `AddCascadingAuthenticationState()`, `AddAuthorizationCore()`.
- `Routes.razor` envuelve el `<Router>` en un gate (`cargada`) y usa
  **`AuthorizeRouteView`** con `<NotAuthorized><RedirectToLogin /></NotAuthorized>`.
- `RedirectToLogin` navega a `/login?returnUrl=<ruta>`.
- `Home.razor` usa `@attribute [Authorize]`.

### 5.3 Trampas ya resueltas (no repetirlas)

1. **`[Authorize]` en una página añade metadatos al endpoint HTTP.** Sin un esquema de
   autenticación, el middleware intenta un *challenge* y falla con
   `Unable to find the required 'IAuthenticationService'`. Solución aplicada:
   `.WithMetadata(new AllowAnonymousAttribute())` en `MapRazorComponents<App>()`; la
   autorización real la hace `AuthorizeRouteView` en el circuito.
2. **No inyectar servicios scoped en un `DelegatingHandler`.** `IHttpClientFactory` construye
   los handlers desde el proveedor raíz, así que recibirían **otra** `SesionActual`. Se eliminó
   `TokenHandler` y el `Bearer` se pone por petición en `AuthService`.
3. **La sesión se lee en `OnAfterRenderAsync`**, no en `OnInitializedAsync`, porque
   `ProtectedSessionStorage` usa JS Interop.
4. **El gate va antes del `Router`**, no en `MainLayout`, para que la identidad esté lista
   cuando `AuthorizeRouteView` decide.

### 5.4 Pendiente en el frontend

- Pantallas de negocio (ninguna existe).
- `D.1`/`D.2`: `[Authorize(Roles = "Administrador")]` y `<AuthorizeView Roles="Administrador">`
  cuando exista la primera pantalla de administración.
- Registro, activación y recuperación de contraseña.

---

## 6. Práctica 5 — plan de trabajo

### 6.1 Punto 1.1 — `AGENTS.md` + criterios (hacer y commitear PRIMERO)

**`AGENTS.md`** (raíz) debe declarar:

| Sección | Contenido mínimo |
|---|---|
| Propósito | sistema de inventario con auth, permisos, documentos y órdenes |
| Stack | C# / .NET 10, ASP.NET Core Web API + EF Core + SQL Server; Blazor Server para el frontend |
| Cómo correr | `docker compose up -d`; frontend con `dotnet run --project frontend/Inventario.Web --launch-profile http` |
| Cómo probar | `dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` |
| Convenciones | servicios en `Core.Application/Services`, controladores finos, DTOs, reglas de negocio en el dominio, pruebas con EF InMemory, nombres `RF_XX_N` |
| Restricciones | **no** leer `.env` ni escribir credenciales; **no** modificar migraciones ya aplicadas (solo añadir); **no** cambiar la máquina de estados de `OrdenCompra` ni las reglas de `DocumentoService`; **no** romper los 62 tests; **no** ejecutar `docker compose down -v`; responder en español; PowerShell sin `&&` |

**`docs/criterios-notificaciones-reportes.md`** — cada criterio comprobable, en formato
dado/cuando/entonces, citando `RF-NOT-xx` / `RF-REP-xx`. Debe cubrir las dos funcionalidades y
poder verificarse sin preguntar nada. Ejemplo del nivel exigido:

> **RF-NOT-01** — Dado un usuario Estándar autenticado, cuando crea una solicitud de permiso,
> entonces cada usuario con rol Administrador y `Activo = true` tiene una notificación nueva sin
> leer, y ningún usuario con rol Estándar la tiene.

### 6.2 Punto 1.2 — Notificaciones (RF-NOT-01..07)

Piezas mínimas:

1. **Entidad `Notificacion`** (`Core.Domain`): `Id`, `UsuarioId`, `Mensaje`, `Leida`,
   `FechaCreacion`, `Tipo` y opcional `ReferenciaId`.
2. **`INotificacionService` + `NotificacionService`**: crear, listar propias, marcar leída.
   La autorización va **en el servicio** (RD-06): un usuario no puede listar ni marcar las de otro.
3. **Controlador** `api/notificaciones`: `GET`, `POST {id}/leer`, y
   `GET`/`PUT` de preferencia de correo.
4. **Enganches** en `SolicitudPermisoService` (crear/aprobar/rechazar) creando la notificación
   interna y, si el destinatario tiene el correo habilitado, encolando `CorreoEnCola`.
5. **RF-NOT-03**: elegir un evento del negocio. La opción natural es la máquina de estados de
   `OrdenCompra` (`enviar-aprobacion` → notificar Administradores; `aprobar` → notificar al
   creador). Documentarlo en el README.
6. **RF-NOT-07**: flag por usuario (`Usuario.RecibeCorreos`, por defecto `true`). Con el correo
   desactivado: se crea la notificación interna y **no** se encola ningún `CorreoEnCola`.

### 6.3 Punto 1.3 — Cola de correos (RF-NOT-08..13)

Cambios sobre lo existente:

1. **Esquema**: agregar `Intentos`, `UltimoError`, `FechaUltimoIntento` a `CorreoEnCola`
   (+ migración nueva).
2. **Reintentos**: máximo declarado en el README (sugerido **3**). Al llegar al máximo →
   `Estado = "Fallido"` y `UltimoError` con el mensaje de la última excepción.
3. **Idempotencia**: marcar el correo como `EnProceso` (o usar transacción + `RowVersion`)
   **antes** de enviar, para que dos ejecuciones del procesador no lo envíen dos veces.
4. **Consulta de la cola**: `GET /api/correos` solo para Administrador (RF-NOT-11); un Estándar
   recibe `403`.
5. **Credenciales**: ya salen de configuración/variables de entorno. Verificar con
   `git log -S "<clave>" --all` vacío y `gitleaks` en verde.

### 6.4 Punto 1.4 — Reportes (RF-REP-01..05)

1. **Core**: `GET /api/reportes/solicitudes?desde=...&hasta=...` agrupado por `Estado`
   (y opcionalmente por mes) con `GROUP BY` en EF. Devuelve conteos, no la lista.
2. **Negocio**: `GET /api/reportes/ordenes?desde=...&hasta=...` con una pregunta real, por
   ejemplo **monto total y cantidad de órdenes por estado y proveedor** en el período.
   No un conteo de filas.
3. **Rol**: un Estándar solo ve lo suyo (sus solicitudes o sus documentos) o recibe `403` en el
   reporte global. No debe poder inferir datos de otros.
4. **Fechas**: los dos rangos deben producir resultados distintos sobre los mismos datos.
5. **Agregación en el motor**: `GroupBy` + `Sum`/`Count` en EF, no traer todo y sumar en C#.

### 6.5 Punto 1.5 — Revisión acumulada

Repetir desde un clon limpio, con `.env.example` copiado a `.env` y el comando del README:

1. Registro con activación por correo (el correo sale por la cola).
2. Inicio y cierre de sesión.
3. Estándar rechazado en una operación de Administrador.
4. Recuperación de contraseña.
5. Permisos: crear, aprobar, rechazar y transiciones inválidas.
6. Negocio: operación válida y transición prohibida.
7. Documento que sobrevive a `docker compose down` (sin `-v`).

### 6.6 README — secciones obligatorias

- **Notificaciones**: cómo provocar cada evento (solicitud creada, resuelta, evento del
  negocio), cómo ejecutar el procesador, **cómo provocar un fallo de envío** y **cuál es el
  máximo de intentos**.
- **Reportes**: qué pregunta responde el reporte del negocio y cómo consultar ambos con rango
  de fechas.

---

## 7. Riesgos y decisiones abiertas

1. ~~El fallback de correo devuelve `true`.~~ **Resuelto:** el README explica cómo forzar un fallo
   real con `SMTP_SERVER=127.0.0.1`, `SMTP_PORT=1` y credenciales no-ejemplo.
2. ~~Reintento infinito en `ProcesadorCorreosWorker`.~~ **Resuelto:** `ProcesadorCorreos` limita a
   `MaximoIntentos = 3` y deja `Fallido` con `UltimoError` (`59bd20c`).
3. ~~Idempotencia del procesador.~~ **Resuelto:** solo toma `Pendiente` con `intentos < 3` y
   `MarcarComoEnviado()` no hace nada si ya está `Enviado`; probado en RF-NOT-12.
4. **Orden de commits:** los criterios (`a90538b`) preceden a todo el código; no reescribir el
   historial de la rama.
5. **Secretos:** `AGENTS.md`, README y criterios no deben contener credenciales reales.
6. **Pendiente real:** la revisión acumulada de los seis frentes desde un clon limpio y el tag
   `practica-5`.
6. **Frontend**: no se toca en esta práctica; si se mergea, hacerlo **antes** del tag para que
   `practica-5` incluya todo.
7. **Migraciones**: `CorreoEnCola` y `Usuario` cambian; crear migraciones nuevas, nunca editar
   las existentes.

---

## 8. Orden recomendado de trabajo

| # | Paso | Estimación |
|---|---|---|
| 1 | ✅ Merge `feat/frontend-login` → `main` (`51b3eb4`) y rama `feat/practica-5` | 10 min |
| 2 | ✅ `AGENTS.md` + `docs/criterios-notificaciones-reportes.md` + commit **solo docs** (`a90538b`) | 1-2 h |
| 3 | ✅ Entidad `Notificacion` + migración + `NotificacionService` + controlador (`7f67c68`) | 2-3 h |
| 4 | ✅ Enganches en `SolicitudPermisoService` + evento de `OrdenCompra` (`7f67c68`) | 1-2 h |
| 5 | ✅ Preferencia de correo (`RecibeCorreos`) + encolado condicional (`7f67c68`) | 1 h |
| 6 | ✅ Cola: `Intentos`, `UltimoError`, estado `Fallido`, idempotencia, `GET /api/correos` (`59bd20c`) | 2-3 h |
| 7 | ✅ Reportes Core + negocio con `GroupBy`, rol y fechas (`f52a070`) | 2-3 h |
| 8 | ✅ README: Notificaciones, cola de correos y Reportes (`0da3680`) | 1-2 h |
| 9 | Revisión acumulada desde clon limpio | 1 h |
| 10 | Tag `practica-5`, push, Moodle | 20 min |

---

## 9. Anexo — comandos y referencias

### 9.1 Comandos útiles

```powershell
# Build backend
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Suite completa (62 pruebas)
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Solo un grupo
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx `
  --filter "FullyQualifiedName~NotificacionServiceTests"

# Frontend
dotnet build frontend/Inventario.Web/Inventario.Web.csproj
dotnet run --project frontend/Inventario.Web --launch-profile http

# Stack completo
docker compose up -d
docker compose ps
docker compose logs -f worker
docker compose logs -f api

# Migraciones (dotnet ef devuelve 1 por NU1902/NU1903 aunque tenga éxito)
dotnet ef migrations add <Nombre> `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

dotnet ef database update `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

# Comprobar secretos
git log --all --oneline -- .env
git log -S "<posible-clave>" --all --oneline
git check-ignore -v .env
```

### 9.2 Archivos a mirar primero

| Archivo | Para qué |
|---|---|
| `Core.Domain/Entities/CorreoEnCola.cs` | base de la cola; aquí entran `Intentos` y `UltimoError` |
| `Infrastructure/Services/ProcesadorCorreosWorker.cs` | reintentos, estado fallido e idempotencia |
| `Infrastructure/Services/EmailService.cs` | fallback y cómo forzar un fallo real |
| `Core.Application/Services/SolicitudPermisoService.cs` | enganches RF-NOT-01 y RF-NOT-02 |
| `Core.Domain/Entities/Usuario.cs` | preferencia `RecibeCorreos` (RF-NOT-07) |
| `Business.Inventory/Entities/OrdenCompra.cs` | evento de negocio RF-NOT-03 |
| `Presentation.API/Program.cs` | DI, autorización, migración automática |
| `frontend/Inventario.Web/Services/SesionActual.cs` | sesión persistente del frontend |
| `frontend/Inventario.Web/Components/Routes.razor` | gate + `AuthorizeRouteView` |
| `README.md` | fuente de verdad de la revisión manual |
