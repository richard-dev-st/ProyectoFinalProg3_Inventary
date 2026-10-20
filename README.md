# ProyectoFinalProg3_Inventary
# Descripcion del proyecto
Este es el repositorio oficial del proyecto final de programación 3 basado en un sistema de inventario para un negocio pequeño desarrollado por Richard Hernández.

## Diagrama de componentes hecho en mermaid

```mermaid
flowchart LR
    subgraph CORE ["CORE (Servicios Transversales)"]
        ACC["Control de acceso<br/><i>[C# Service]</i> Autentica, asigna rol"]
        PERM["Gestión de permisos<br/><i>[C# Service]</i> Solicitud -> Aprobacion"]
        NOTI["Notificaciones<br/><i>[C# Service]</i> Avisos internos por evento"]
        DOC["Manejador de documentos<br/><i>[C# Service]</i> Subir, listar, borrado logico"]
        REP["Reportes<br/><i>[C# Service]</i> Agregacion, filtros por rol"]
        AUD["Auditoría<br/><i>[C# Service]</i> Quien hizo que y cuando"]
    end

    subgraph NEGOCIO ["MÓDULO DE NEGOCIO"]
        INV["Módulo de Inventario<br/><i>Entidades: Producto, Proveedor, OrdenCompra<br/>Estado: Borrador → Solicitada → Recibida / Cancelada</i>"]
    end

    %% Relaciones del Módulo hacia el Core
    INV -->|"Consulta quién es y qué rol tiene"| ACC
    INV -->|"Dispara avisos por alertas de stock y órdenes"| NOTI
    INV -->|"Guarda adjuntos de proveedores u órdenes"| DOC
    INV -->|"Alimenta reportes de compras y stock"| REP
    INV -.-|"Registra cambios de estado y movimientos"| AUD

    %% Relaciones internas del Core
    PERM -->|"Avisa al solicitante"| NOTI
    PERM -.-|"Registra transacciones de permisos"| AUD
    ACC -.-|"Registra inicio de sesión y registro"| AUD
```

### 4 Entidades base y relaciones
Cree las siguientes clases a continuacion:

1. **DetalleOrden** — Atributos: `Id`, `Cantidad`, `Precio`, `Subtotal`. Relaciones: `OrdenCompra` y `Producto`.
2. **OrdenCompra** — Atributos: `Id`, `FechaCreacion`, `Total`, `CreadoPorUsuarioId`, `ProveedorId`. Relaciones: `Proveedor` y `Detalles`.
3. **Producto** — Atributos: `Id`, `Nombre`, `Codigo`, `Precio`, `StockActual`, `StockMinimo`, `ProveedorId`. Relaciones: `Proveedor` y `DetallesOrden`.
4. **Proveedor** — Atributos: `Id`, `Nombre`, `Cedula`, `Telefono`, `Email`. Relaciones: `Productos` y `OrdenesCompra`.

#### Bitacora con el Agente - Punto 3
Crítica de la bitácora con el agente.

Le pedí que criticara el diagrama de componentes de acuerdo a las seis señales y me confirmó lo que supuse, la frontera de la dependencia Core- Negocio es unidireccional y correcta, pero me señaló que el componente Reportes no tenía conexiones en el diagrama visual, por lo que este agregó la relación de alimentación de reportes.

Error puntual que noté:
El agente me dio el diagrama corregido pero cometió un error, en la parte de los componentes del core no estaba el mensaje característico que decía de que se encargaba cada componente. tuve que corregirlo finalmente.

## Requisitos previos
- .NET SDK 10.0
- SQL Server (LocalDB o Express)
- EF Core CLI para aplicar las migraciones: `dotnet tool install --global dotnet-ef`
- Gmail con contraseña de aplicación (para el envío de correos)
- gitleaks (opcional, hook de seguridad): `winget install gitleaks.gitleaks`

## Variables de entorno
Ningún valor real debe guardarse en el repositorio; se inyectan por variables de entorno.

| Variable | Propósito | ¿Cuándo es obligatoria? |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a SQL Server | Siempre |
| `JwtSettings__SecretKey` | Clave de firma de los JWT (mín. 32 caracteres) | Siempre (la API no arranca sin ella) |
| `EmailSettings__SenderEmail` | Correo remitente (SMTP) | Para enviar correos reales |
| `EmailSettings__SenderPassword` | App Password de Gmail | Para enviar correos reales |
| `EmailSettings__UseSsl` | `true`/`false`; activa SSL del SMTP | Opcional (por defecto `true`) |

## Instrucciones de ejecución

1. Coloca las variables de entorno (ejemplo en PowerShell):
   ```powershell
   $env:JwtSettings__SecretKey = "una_clave_secreta_de_al_menos_32_caracteres_aqui"
   $env:EmailSettings__SenderEmail = "tucorreo@gmail.com"
   $env:EmailSettings__SenderPassword = "TU_APP_PASSWORD"
   ```

2. Aplica las migraciones y ejecuta la API web (desde la raíz del repositorio):
   ```powershell
   dotnet ef database update --project ProyectoFinalProg3_Inventory/Infrastructure --startup-project ProyectoFinalProg3_Inventory/Presentation.API
   dotnet run --project ProyectoFinalProg3_Inventory/Presentation.API
   ```
   Swagger: http://localhost:5176/swagger

3. Ejecuta el worker de correos en otro proceso (procesa la cola `CorreosEnCola`):
   ```powershell
   cd ProyectoFinalProg3_Inventory/EmailWorker
   dotnet run
   ```
   Ejecuta el worker desde su carpeta: `dotnet run` toma el directorio actual para leer `appsettings.json`.

### ¿Cómo crear un Administrador?
El registro crea usuarios con rol Estandar. Para el primer administrador, promueve un usuario ya registrado en la BD:

```sql
UPDATE Usuarios SET Rol = 'Administrador' WHERE Email = 'tucorreo@gmail.com';
```

Luego vuelve a iniciar sesión para obtener un token con el rol incluido.

## Criterios implementados y cómo verificarlos

| Criterio | Endpoint / acción | Evidencia esperada |
|---|---|---|
| RF-CA-03 | `POST /api/Auth/Login` con usuario inexistente | Mensaje genérico, no revela si el correo existe |
| RF-CA-05/08 | `GET/PUT /api/admin/usuarios`, `PUT .../{id}/rol` con token Administrador | 200; con rol Estandar → 403 |
| RF-CA-09 | `POST /api/Auth/solicitar-recuperacion` | Respuesta genérica exista o no el correo |
| RF-CA-10/11/12 | `POST /api/Auth/restablecer-password` con código | Cambia la contraseña; invalida sesiones previas |
| RF-CA-13 | `POST /api/Auth/{id}/forzar-restablecimiento` (Administrador) | 200; sin rol Administrador → 401/403 |
| RF-CA-14 | `POST /api/Auth/registrar` con clave débil | 400 (política de contraseña) |
| RF-CA-18 | `POST /api/Auth/logout` y reusar el token | El token revocado es rechazado |
| RF-CA-19 | 5 logins fallidos seguidos | Bloqueo 15 min |
| RF-CA-20 | `PUT /api/admin/usuarios/{id}/estado` desactivar | Sesión abierta deja de ser válida; no se puede autodesactivar |
| RF-CA-22 | `POST /api/Auth/cambiar-password` con sesión | Cambia la clave y expira el token actual |
| RF-NEG-03/04/05 | Transiciones en `OrdenCompra` | Prohibidas lanzan error; `Recibida`/`Cancelada` son terminales |
| RD-04 | `Business.Inventory/Entities/OrdenCompra.cs` | Transiciones declaradas en un solo lugar |
| RD-06 | `[Authorize(Roles = "Administrador")]` en acciones destructivas | 403 sin el rol |


### Corrección Puntual aplicada - Problema de Credencial filtrada:

El Problema puntual identificado por el maestro cuando realizó la corrección de la práctica 1 fue resuelto, el problema era el siguiente: En el historial de commits había datos de una credencial de Gmail visible que no debía estar ahí. Finalmente, este error fue corregido.

## Gestión de permisos (Asignación 2)

Segunda pieza del Core: un usuario **Estándar** crea solicitudes de permiso y un **Administrador** las resuelve. Las transiciones válidas son `Pendiente → Aprobada → Aplicada` y `Pendiente → Rechazada`; ninguna otra está permitida. Al aprobar, el sistema **concede el permiso automáticamente** (queda registrado en `PermisosUsuario`) y el solicitante ya puede ejecutar la acción que pidió, sin ningún paso manual. Las solicitudes se guardan en SQL Server, por lo que **sobreviven a reiniciar la aplicación** (RD-09).

### Preparación
- Un usuario **Estándar** (el registro crea usuarios con rol `Estandar`).
- Un usuario **Administrador** (ver «¿Cómo crear un Administrador?»).
- Ejemplos usados abajo: Estándar `florhernandez@gmail.com`, Administrador `richardstorm71@gmail.com`.
- En Swagger: `POST /api/Auth/Login` → copia el campo `token` → botón **Authorize** → pega **solo el token** (sin `Bearer`).

### Cómo provocar cada criterio

#### RF-GP-01 — Un usuario Estándar crea una solicitud (nace en `Pendiente`)
1. Autentícate con el token del **Estándar**.
2. `POST /api/solicitudes`:
   ```json
   { "permiso": "exportar-inventario", "descripcion": "Prueba manual" }
   ```
   Respuesta **200** con `"estado": "Pendiente"`. Guarda el `id`.

#### RF-GP-02 — El Administrador consulta las pendientes; un Estándar no ve las de otros
1. Con el token **Estándar**: `GET /api/solicitudes/pendientes` → **403**.
2. `GET /api/solicitudes/mias` → **solo** las solicitudes de ese usuario.
3. Con el token **Administrador**: `GET /api/solicitudes/pendientes` → **200** con la lista (incluye la solicitud recién creada).

#### RF-GP-03 — El Administrador aprueba (queda quién y cuándo)
1. `POST /api/solicitudes/{id}/aprobar` con el token **Administrador** (pega el `id`).
2. Respuesta **200** con `"estado": "Aplicada"`, `"resueltoPorUsuarioId"` (el admin) y `"fechaResolucion"`.

#### RF-GP-04 — El Administrador rechaza indicando el motivo
1. Crea otra solicitud como **Estándar** y guarda su `id`.
2. `POST /api/solicitudes/{id}/rechazar` con `{ "motivo": "" }` → **400** (`El motivo de rechazo es obligatorio.`).
3. Repite con `{ "motivo": "No cumple los requisitos" }` → **200** con `"estado": "Rechazada"` y `"motivoRechazo"`.

#### RF-GP-05 — Al aprobar se aplica el permiso automáticamente
1. La solicitud de RF-GP-01 pide `exportar-inventario`. Con el token **Estándar**:
   - `GET /api/mis-permisos` → `[]`
   - `GET /api/inventario/exportar` → **403**
2. Aprueba esa solicitud (RF-GP-03) con el token **Administrador**.
3. De vuelta con el token **Estándar**:
   - `GET /api/mis-permisos` → ahora incluye `exportar-inventario`.
   - `GET /api/inventario/exportar` → **200** (descarga el CSV del inventario).
   No hizo falta ninguna intervención manual entre aprobar y poder exportar.

#### RF-GP-06 — Solo se permiten las transiciones de la tabla
1. Sobre una solicitud ya **Aplicada**, vuelve a ejecutar `POST /api/solicitudes/{id}/aprobar` → **400** (`Transición inválida: Aplicada -> Aprobada.`) y el estado sigue `Aplicada`.
2. Sobre una solicitud **Rechazada**, `POST /api/solicitudes/{id}/aprobar` → **400** y el estado sigue `Rechazada`.

#### RF-GP-07 — Nadie resuelve su propia solicitud
1. Autentícate con el token **Administrador**.
2. `POST /api/solicitudes` → crea una solicitud (el admin queda como solicitante). Guarda el `id`.
3. `POST /api/solicitudes/{id}/aprobar` con el mismo admin → **400** (`El solicitante no puede aprobar su propia solicitud.`). Lo mismo al intentar rechazarla.

#### RD-09 — Las solicitudes sobreviven a reiniciar
1. Anota una solicitud (`GET /api/solicitudes/mias`).
2. Detén la API (Ctrl+C) y vuelve a `dotnet run`.
3. Vuelve a iniciar sesión y repite `GET /api/solicitudes/mias` → la solicitud sigue con el mismo `id` y estado.

### Tabla resumen
| Criterio | Request | Esperado |
|---|---|---|
| RF-GP-01 | `POST /api/solicitudes` | 200, `Pendiente` |
| RF-GP-02 | `GET /api/solicitudes/pendientes` (Estándar) | 403 |
| RF-GP-02 | `GET /api/solicitudes/pendientes` (Admin) | 200, lista |
| RF-GP-02 | `GET /api/solicitudes/mias` | solo las propias |
| RF-GP-03 | `POST /{id}/aprobar` | 200, `Aplicada` + quién/cuándo |
| RF-GP-04 | `POST /{id}/rechazar` `{"motivo":""}` | 400 |
| RF-GP-04 | `POST /{id}/rechazar` `{"motivo":"..."}` | 200, `Rechazada` + motivo |
| RF-GP-05 | `GET /api/mis-permisos` + `GET /api/inventario/exportar` | `[]`/403 → con permiso/200 |
| RF-GP-06 | `POST /{id}/aprobar` (ya resuelta) | 400, estado igual |
| RF-GP-07 | `POST /{id}/aprobar` (propia del admin) | 400 |
| RD-09 | Reiniciar la API y `GET /api/solicitudes/mias` | la solicitud persiste |

## Pruebas

La suite cubre el dominio y la capa de aplicación de Gestión de permisos **sin levantar la aplicación** (RD-12):

- `ProyectoFinalProg3_Inventory/Core.Domain.Tests`: 12 pruebas del dominio (entidad `SolicitudPermiso`, incluidas las transiciones prohibidas).
- `ProyectoFinalProg3_Inventory/Core.Application.Tests`: 7 pruebas de `SolicitudPermisoService` con EF Core InMemory.

Desde la raíz del repositorio, un único comando ejecuta toda la suite:

```powershell
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx
```

Salida esperada: `Passed: 19` (12 de dominio + 7 de aplicación). Las pruebas cubren la creación (RF-GP-01), el filtrado por usuario (RF-GP-02), las transiciones válidas verificando los datos registrados —quién/cuándo y el motivo— (RF-GP-03/04), la concesión del permiso (RF-GP-05) y las transiciones prohibidas verificando que el estado no cambia (RF-GP-06/07).

## Práctica 2 — Defectos plantados (Variante A)

El tag `practica-2` corresponde a la suite en verde. Cada defecto se plantó en un commit independiente (hijo directo de `practica-2`) y se etiquetó como `practica-2-defecto-N`. Esta tabla declara qué prueba detecta cada defecto.

| # | Requisito | Archivo y cambio plantado | Prueba que lo detecta |
|---|---|---|---|
| 1 | RF-GP-06 | `Core.Domain/Entities/SolicitudPermiso.cs` — en `Aprobar`, se sustituye la validación «estado Pendiente» por «estado no Rechazada». | `RF_GP_06_Aprobar_SolicitudYaAplicada_LanzaYElEstadoNoCambia` |
| 2 | RF-GP-06 | `SolicitudPermiso.cs` — `TransicionesPermitidas`: se agrega `Rechazada → Aplicada`. | `RF_GP_06_CambiarEstado_DeRechazadaAAplicada_LanzaYNoCambia` |
| 3 | RF-GP-07 | `SolicitudPermiso.cs` — en `Aprobar` y `Rechazar` se elimina la verificación de auto-resolución. | `RF_GP_07_Aprobar_SolicitudCreadaPorElMismoAdministrador_LanzaYElEstadoNoCambia` y `RF_GP_07_Rechazar_SolicitudCreadaPorElMismoAdministrador_LanzaYElEstadoNoCambia` |
| 4 | RF-GP-02 | `Core.Application/Services/SolicitudPermisoService.cs` — en `MiasAsync`, `==` pasa a `!=`. | `RF_GP_02_MiasAsync_SoloDevuelveLasDelUsuario` |
| 5 | RF-GP-06 | `SolicitudPermiso.cs` — `TransicionesPermitidas`: se agrega `Aprobada → Pendiente`. | `RF_GP_06_CambiarEstado_DeAprobadaAPendiente_LanzaYNoCambia` |
| 6 | RF-GP-04 | `SolicitudPermiso.cs` — en `Rechazar` se elimina `MotivoRechazo = motivo;`. | `RF_GP_04_Rechazar_SolicitudPendienteConMotivo_LaDejaRechazadaYRegistraElMotivo` |

