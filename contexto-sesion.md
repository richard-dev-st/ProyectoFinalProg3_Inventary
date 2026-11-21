# Contexto de sesión — Handoff activo (actualizado 21/11/2026)

**Proyecto:** `ProyectoFinalProg3_Inventary` — Sistema de inventario (Programación III, ITLA)
**Autor:** Richard José Hernández Valdez
**Repositorio:** https://github.com/richard-dev-st/ProyectoFinalProg3_Inventary
**Rama actual:** `main`, alineada con `origin/main` en `3746eb5` (merge del PR #24)
**Estado vigente:** ver **§0**. Las secciones §1–§10 son el corte histórico de la Práctica 5
(19–20/11/2026); donde digan otra cosa (79/62 pruebas, frontend «no iniciado», etc.), prevalece §0.
**Historial de la Práctica 5:** último commit de código `82ad402`; README `f270878`; tag `practica-5` = `ddefd7f` (pusheado)
**Hitos posteriores:** PR #22 (Práctica 5) → `97396e2`; PR #23 (registro/activación) → `4df728f`; PR #24 (roles/administración) → `3746eb5`
**Tag `practica-4`:** objeto `581c938` → commit `901a47f`
**Solución backend:** `ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx` (.NET 10)
**Frontend:** `frontend/Inventario.Web` (Blazor Web App, .NET 10)

> Este documento es el handoff que se usa al cambiar de agente. **Si algo cambia, actualízalo en el
> mismo commit.** La Práctica 4 y la Práctica 5 están cerradas y etiquetadas; sus detalles se
> conservan como referencia histórica en §4 y §10.

---

## 0. Estado vigente — leer primero

| Bloque | Estado |
|---|---|
| Práctica 4 (`practica-4`) | ✅ Cerrada, etiquetada y pusheada |
| Práctica 5 (`practica-5`) | ✅ Cerrada, etiquetada, pusheada y **entregada al docente el 20/11/2026** |
| Backend (auth, permisos, órdenes, documentos, notificaciones, reportes) | ✅ Implementado |
| Frontend — login y sesión persistente | ✅ Implementado |
| Frontend — registro, activación y reenvío de enlace | ✅ Implementado (PR #23) |
| Frontend — roles y administración de usuarios | ✅ Implementado (PR #24) |
| UI de recuperación de contraseña | ⬜ Pendiente (el backend ya existe) |
| Constantes/policies de rol centralizadas | ⬜ Pendiente (los roles siguen en literales `[Authorize(Roles = "Administrador")]`) |
| Pruebas de `AdminUsuarioService` / endpoints de admin | ⬜ Pendientes (cobertura solo indirecta) |

### Verificación actual

```powershell
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx   → OK (warnings heredados)
dotnet build frontend/Inventario.Web/Inventario.Web.csproj                   → 0 errores, 0 warnings
dotnet test  ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx   → 86/86 (12 + 56 + 18)
```

### Rutas del frontend que ya existen

`/login`, `/registrar`, `/activar?token=…`, `/reenviar-activacion`, `/admin/usuarios`,
`/acceso-denegado`, `/` (Home con email/rol y logout). El menú muestra «Iniciar sesión» y
«Registrarse» sin autenticar, y «Usuarios» solo con `<AuthorizeView Roles="Administrador">`.

### Servicios del frontend

`AuthService` (login, `me`, logout, registro, activación, reenvío), `AdminUsuariosServices`
(listado, cambio de rol y estado), `SesionActual`, `SesionAuthStateProvider`. El `Bearer` se pone
**por petición**, nunca en un `DelegatingHandler` scoped (recibiría otra `SesionActual`).

### Trampas ya resueltas que no hay que repetir

- El enum `RolUsuario` de la API admin se serializa como **número** (1/2); en login y `/me` el rol
  es **string**. No mezclar ambos.
- Las mutaciones de admin devuelven `204` sin cuerpo: no hacer `ReadFromJsonAsync`.
- Un Estándar recibe `403` con cuerpo vacío; un token inactivo/revocado, `401` sin cuerpo.
- Cambiar el rol en BD exige **cerrar sesión y volver a entrar** para regenerar el claim.
- No hay administrador sembrado: promover un usuario activo con
  `UPDATE Usuarios SET Rol = 'Administrador', Activo = 1 WHERE Email = '…';` (en `InventarioBP4`).
- `README.Docker.md` está sin versionar: no agregarlo.

### Cómo retomar

1. Leer §0 y el README (secciones de registro/activación y roles/administración).
2. Verificar build + `dotnet test` antes de tocar código.
3. Trabajar en rama nueva desde `main`; commit de docs aparte si cambia este handoff.

---

## 1. Resumen ejecutivo

| Bloque | Estado |
|---|---|
| Práctica 4 (documentos, contenedor, API externa, revisión) | ✅ Cerrada, `practica-4` en `901a47f` |
| Frontend Blazor — login y sesión | ✅ Completo y mergeado a `main` (`51b3eb4`, pusheado) |
| Documentación de Práctica 5 (1.1) | ✅ `AGENTS.md` + criterios en `a90538b`, anterior a todo código |
| Frontend — registro/activación | ✅ Mergeado a `main` (PR #23, `4df728f`) |
| Frontend — roles y administración | ✅ Mergeado a `main` (PR #24, `3746eb5`) |
| **Práctica 5** | ✅ Cerrada, etiquetada (`practica-5`) y entregada al docente el 20/11/2026; detalle histórico abajo |

Estado por punto de la Práctica 5:

| Punto | Descripción | Estado |
|---|---|---|
| 1.1 | `AGENTS.md` + `docs/criterios-notificaciones-reportes.md` | ✅ Hecho y commiteado (`a90538b`) |
| 1.2 | Notificaciones RF-NOT-01..07 | ✅ Entidad, servicio, controlador, preferencia y enganches (`7f67c68`) |
| 1.3 | Cola de correos RF-NOT-08..13 | ✅ Reintentos (máx. 3), `Fallido`, `GET /api/correos`, idempotencia (`59bd20c`) |
| 1.4 | Reportes con agregación RF-REP-01..05 | ✅ `/api/reportes/solicitudes` y `/ordenes` con rol y fechas (`f52a070`) |
| 1.5 | README: secciones **Notificaciones** y **Reportes** | ✅ Escritas y commiteadas (`0da3680`) |
| 1.6 | Revisión acumulada de los seis frentes | ✅ Ejecutada el 19/11/2026 (178 comprobaciones, 0 fallos) |
| — | Tag `practica-5` + push | ✅ `practica-5` → `ddefd7f`, rama y tag pusheados el 20/11/2026 |

Verificación histórica al cierre de la Práctica 5 (19/11/2026):

```
dotnet test → 79/79 (12 + 49 + 18)
```

**Verificación vigente (21/11/2026): 86/86 (12 + 56 + 18).** Ver §0.

> El build del backend mantiene warnings heredados (CS8618 en `Usuario`, CS0472 en `OrdenCompra`,
> CS8613 en `AuthService`, CS8603 en `OrdenCompraRepository`). No bloquean.

---

## 2. Estado de Git y entorno

### 2.1 Ramas y commits

```
main (y origin/main)  3746eb5  Merge pull request #24 from .../feat/frontend-roles-administracion-usuarios
feat/frontend-roles-administracion-usuarios  abe77c9 + c203c91 (merge de origin/main)
origen del PR #23     26b3e64  registro, activacion y reenvio de enlace
hitos historicos      fa116f2, 82ad402, 7eaee64 (detalle abajo)
```

> `feat/practica-5` tendrá encima los commits de `contexto-sesion.md`; el último **de código**
> es `82ad402`.

Hitos de la Práctica 5 en orden:

```
a90538b  docs(practica-5): criterios de aceptacion y contexto para agentes
7f67c68  feat(notificaciones): RF-NOT-01..07
59bd20c  feat(correos): cola con reintentos e idempotencia RF-NOT-08..12
f52a070  feat(reportes): agregados RF-REP-01..05
0da3680  docs(readme): secciones de notificaciones, cola de correos y reportes
f270878  docs(readme): ejemplos de PowerShell robustos
1fefc5b  docs(contexto): estado del README y de la rama
82ad402  fix(practica-5): bordes detectados en la revision acumulada
```

- `main` está en `3746eb5` e incluye los PR #22, #23 y #24 además del merge del frontend (`51b3eb4`).
- `feat/practica-5` salió de `fa116f2` y quedó pusheada; su contenido ya está en `main` vía PR #22.
- `feat/frontend-registro-activacion` y `feat/frontend-roles-administracion-usuarios` ya están mergeadas.
- El tag `practica-5` apunta a `ddefd7f` y ya está en `origin`. Es inmutable: los commits de
  documentación posteriores (como este) van solo a la rama.
- Working tree: solo `README.Docker.md` como `??` (lo generó Visual Studio). **No se debe
  versionar.**

### 2.2 Tags existentes

```
asignacion-1, asignacion-2
practica-1
practica-2, practica-2-defecto-1..6
practica-3, practica-3-defecto-1..4
practica-4
practica-5
```

`practica-5` es anotado, apunta a `ddefd7f` y está en `origin`.

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
| Tests | `Core.Domain.Tests` (12), `Core.Application.Tests` (56), `Bussiness.inventory.Tests` (18) = **86** (62 heredadas + 17 de Práctica 5 + 7 de registro/activación) |

### 3.2 Frontend (estado vigente)

```
frontend/Inventario.Web
├── Components/
│   ├── App.razor                 ← render mode global InteractiveServer, prerender: false
│   ├── Routes.razor              ← gate de sesión + AuthorizeRouteView + <AccesoDenegado />
│   ├── Layout/
│   │   ├── MainLayout.razor      ← sidebar + @Body
│   │   ├── NavMenu.razor         ← Home, Iniciar sesión, Registrarse, Usuarios (Admin)
│   │   └── RedirectToLogin.razor ← redirige con returnUrl
│   └── Pages/
│       ├── Login.razor / Home.razor
│       ├── Registrar.razor / Activar.razor / ReenviarActivacion.razor
│       ├── AdminUsuarios.razor   ← [Authorize(Roles = "Administrador")]
│       ├── AccesoDenegado.razor
│       └── Error.razor / NotFound.razor
├── Models/                       ← sesión, registro, reenvío, RolUsuario, UsuarioAdmin, requests admin
├── Services/
│   ├── AuthService.cs            ← login / me / logout / registro / activación / reenvío
│   ├── AdminUsuariosServices.cs  ← listado, cambio de rol y estado
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
| Auth | `api/auth` | `Login`, `me`, `logout`, `registrar`, `activar`, `reenviar-activacion`, recuperación |
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
  `GroupBy`/`Count`/`Sum`, filtro por rol y rango inclusivo (`ReportesController`). `hasta` con
  fecha sin hora incluye el día completo (`82ad402`).
- `UltimoError` guarda el mensaje real de la excepción SMTP (`82ad402`).

No queda nada pendiente de código **de la Práctica 5**: el tag `practica-5` (`ddefd7f`) está creado y pusheado. Las tareas posteriores están en §0.

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
- `Routes.razor` envuelve el `<Router>` en un gate (`cargada`) y usa **`AuthorizeRouteView`**.
  El `<NotAuthorized>` distingue dos casos: si el usuario **no está autenticado** usa
  `<RedirectToLogin />`, y si está autenticado pero no tiene el rol usa `<AccesoDenegado />`.
- `RedirectToLogin` navega a `/login?returnUrl=<ruta>`.
- `AdminUsuarios.razor` lleva `[Authorize(Roles = "Administrador")]`; el menú usa
  `<AuthorizeView Roles="Administrador">` para mostrar «Usuarios». El Bearer se añade por petición
  (no en un `DelegatingHandler`, porque recibiría otra instancia de `SesionActual`).
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

### 5.4 Pendiente en el frontend (estado vigente)

- ✅ `D.1`/`D.2`: ya hay `[Authorize(Roles = "Administrador")]` en `/admin/usuarios` y
  `<AuthorizeView Roles="Administrador">` en el menú (PR #24).
- ✅ Registro, activación y reenvío de enlace (PR #23).
- ⬜ Recuperación de contraseña: solo existe el backend; no hay UI.
- ⬜ Pantallas de negocio (órdenes, documentos, etc.): no hay ninguna todavía.

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
| Restricciones | **no** leer `.env` ni escribir credenciales; **no** modificar migraciones ya aplicadas (solo añadir); **no** cambiar la máquina de estados de `OrdenCompra` ni las reglas de `DocumentoService`; **no** romper la línea base de tests (62 entonces, **86 desde el 21/11/2026**); **no** ejecutar `docker compose down -v`; responder en español; PowerShell sin `&&` |

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

### 6.5 Punto 1.5 — Revisión acumulada (ejecutada el 19/11/2026)

Repetida sobre el stack Docker real (`api` + `db` + `worker`) con usuarios desechables
`rev5.*` / `rev5r*@inventario.test`. Resultado: **178 comprobaciones, 0 fallos**. Detalle en §10.

1. ✅ Registro con activación por correo (el correo sale por la cola).
2. ✅ Inicio y cierre de sesión, token revocado y usuario desactivado.
3. ✅ Estándar rechazado (`403`) en las operaciones de Administrador.
4. ✅ Recuperación de contraseña, código inválido y cambio de contraseña.
5. ✅ Permisos: crear, aprobar, rechazar, autoaprobación y transiciones inválidas.
6. ✅ Negocio: borrador, enviar, aprobar, recibir y transiciones prohibidas.
7. ✅ Documento que sobrevive a `docker compose down` (sin `-v`).

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
6. **Entrega:** tag `practica-5` creado y pusheado (`ddefd7f`). **Entregada al docente el
   20/11/2026.** No queda nada por subir.
   La revisión acumulada está en §10.
7. **Frontend**: no se tocó **en la Práctica 5** (cerrada y etiquetada). Los PR #23 y #24 sí
   trabajaron el frontend después, ya con `main`; la restricción no aplica a tareas nuevas sin
   autorización explícita del usuario.
8. **Migraciones**: `CorreoEnCola` y `Usuario` cambian; crear migraciones nuevas, nunca editar
   las existentes.
9. **Reconstrucción de imágenes:** si Docker Desktop sigue con la caché rota o sin red al
   registro, `docker builder prune -af` y volver a `docker compose up -d --build`. La revisión
   usó imágenes delgadas solo como puente; el `Dockerfile` del repositorio es el bueno.

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
| 9 | ✅ Revisión acumulada de los seis frentes (178 comprobaciones, 0 fallos) + arreglos (`82ad402`) | 1 h |
| 10 | ✅ Tag `practica-5` (`ddefd7f`), rama y tag pusheados; entregada al docente el 20/11/2026 | 20 min |

---

## 9. Anexo — comandos y referencias

### 9.1 Comandos útiles

```powershell
# Build backend
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Suite completa (86 pruebas; 62 en el corte de la Práctica 5)
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
| `frontend/Inventario.Web/Services/AdminUsuariosServices.cs` | listado/cambio de rol y estado (Bearer por petición) |
| `frontend/Inventario.Web/Components/Routes.razor` | gate + `AuthorizeRouteView` + `AccesoDenegado` |
| `frontend/Inventario.Web/Components/Pages/AdminUsuarios.razor` | pantalla `/admin/usuarios` |
| `frontend/Inventario.Web/Components/Layout/NavMenu.razor` | menú combinado (login, registro, usuarios) |
| `README.md` | fuente de verdad de la revisión manual |

---

## 10. Revisión acumulada de los seis frentes (19/11/2026)

Ejecutada sobre el stack Docker real (`api` en `http://localhost:8080`, `db` y `worker`), con
usuarios desechables creados por la propia revisión (`rev5.std1`, `rev5.std2`, `rev5.adm1`,
`rev5.adm2`, `rev5.inactivo` y una corrida nueva `rev5r#####.*`). Las contraseñas de esas
cuentas desechables se usaron solo durante la revisión y **no se registran aquí**; la de `std1`
cambió al probar la recuperación, como debe ser.

### 10.1 Resultado por tanda

| Tanda | Frentes | Comprobaciones | Fallos |
|---|---|---|---|
| `review-a2.ps1` | 1 registro/activación · 2 login/logout · 3 rol y usuario desactivado · 4 recuperación | 39 | 0 |
| `review-b.ps1` | 5 permisos · 6 órdenes de compra | 32 | 0 |
| `review-c.ps1` | RF-NOT-01..07 y RF-NOT-11/12 (notificaciones y cola) | 44 | 0 |
| `review-d.ps1` | RF-REP-01..05 y RF-NOT-10 (fallo forzado del SMTP) | 44 | 0 |
| `review-e.ps1` | Documentos y persistencia tras `docker compose down` (sin `-v`) | 19 | 0 |
| **Total** | | **178** | **0** |

Los guiones viven fuera del repositorio (`%TEMP%\opencode\review-*.ps1`) y se apoyan en
`review-common.ps1` (`Call`, `Check`, `CheckTrue`, `Sql`, `Login`, `Resumen`).

### 10.2 Qué quedó verificado

- **Registro y activación:** el correo de activación se encola; el token activa una sola vez
  (segundo intento `400`); una cuenta sin activar no entra (`400`); la contraseña incorrecta
  también responde `400` (patrón del proyecto, no `401`).
- **Sesión:** `me` identifica al usuario; `logout` revoca solo ese token; cerrar una sesión no
  invalida las demás del mismo usuario; un usuario desactivado pierde la sesión abierta y no
  puede volver a entrar.
- **Roles:** un Estándar recibe `403` en `/api/admin/usuarios`, `/api/correos` y en las rutas de
  Administrador; sin token, `401`.
- **Recuperación:** código inválido `400`, código válido `200`, token anterior `401` (con 3 s de
  margen), contraseña anterior rechazada, código no reutilizable.
- **Permisos:** la solicitud nace `Pendiente`; solo el Administrador lista pendientes; aprobar
  aplica el permiso (el `exportar` pasa de `403` a `200`) y guarda resolución y fecha; aprobar
  dos veces `400`; rechazar sin motivo `400`; el solicitante no aprueba lo propio `400`.
- **Negocio:** borrador con total calculado (`100.00`), `enviar-aprobacion` → `2`, `aprobar` →
  `3`, `recibir` → `4`; cancelar o reenviar una orden `Recibida` responde `400`; `total-en-dop`
  responde de forma controlada con la clave de ejemplo.
- **Notificaciones:** crear solicitud notifica exactamente a los Administradores activos y no al
  solicitante ni a un Administrador inactivo; aprobar/rechazar notifica al solicitante con el
  motivo; `enviar-aprobacion` notifica a los Administradores; listados aislados por usuario;
  marcar propia `204` e idempotente; marcar ajena `404` y no cambia nada; `?soloNoLeidas`
  funciona.
- **Cola:** un correo por destinatario y evento; `PUT preferencia=false` deja de encolar correo
  pero conserva la notificación interna, y persiste tras un nuevo login; `GET /api/correos` solo
  Administrador; el worker marca `Enviado` con `fechaEnvio`, `fechaUltimoIntento` e `intentos`;
  reiniciar el worker no reenvía ni duplica.
- **Fallo forzado (RF-NOT-10):** con `SMTP_SERVER=127.0.0.1`, `SMTP_PORT=1` y credenciales no
  de ejemplo, el correo llega a `Fallido` con **3 intentos**, `UltimoError` con el mensaje real
  (`Failure sending mail.`) y `fechaUltimoIntento`; después no se reintenta ni desaparece.
- **Reportes:** totales y grupos coinciden con `COUNT`/`SUM` de SQL; hay menos filas que
  registros (agregación real); el Estándar solo ve lo suyo y no puede espiar con `usuarioId`;
  el Administrador ve el global; `desde`/`hasta` son inclusivos, dos rangos difieren, el rango
  invertido responde `400` y `hasta=AAAA-MM-DD` incluye el día completo.
- **Documentos:** subida, listado propio, descarga con contenido idéntico, `403` para otro
  usuario, archivo vacío `400`, id inexistente `404`; el documento **sobrevive** a
  `docker compose down` seguido de `up -d` y se puede descargar igual.

### 10.3 Defectos encontrados y corregidos en `82ad402`

1. **RF-REP-05 — `hasta` cortaba a medianoche.** `hasta=2026-12-31` dejaba fuera los registros
   de ese día. Ahora una fecha sin hora incluye el día completo; una marca con hora respeta el
   instante. Con prueba `RF_REP_05_HastaFecha_IncluyeTodoElDia`.
2. **RF-NOT-10 — `UltimoError` era genérico.** Guardaba «El servicio de correo no pudo enviar el
   mensaje.» en vez del mensaje del SMTP. `IEmailService` expone `UltimoError` y
   `ProcesadorCorreos` lo persiste, con respaldo al texto genérico. Con prueba
   `RF_NOT_10_ServicioDevuelveFalse_GuardaMensajeDelError`.

No se encontró ningún defecto abierto en los seis frentes.

### 10.4 Notas de la corrida

- El «token sigue válido tras restablecer» que apareció al principio era un artefacto del guion:
  emitía el token y restablecía en el mismo segundo, dentro del margen anti-desfase de 2 s de
  `OnTokenValidated`. Con 3–4 s de espera responde `401`, como exige el criterio.
- La caché de Docker Desktop se corrompió (`failed to compute cache key`) y luego el registro
  dejó de entregar `mcr.microsoft.com/dotnet/sdk:10.0` por timeouts. Para no bloquear la
  revisión se publicó en local y se armaron imágenes delgadas sobre `aspnet:10.0` (que sí estaba
  en el store). **El repositorio no cambió por esto**: `Dockerfile`, `Dockerfile.worker` y
  `docker-compose.yml` siguen iguales.
- Los registros históricos de `CorreosEnCola` anteriores a la migración de reintentos no tienen
  `FechaUltimoIntento`; la revisión solo exige esos campos a los correos nuevos.
