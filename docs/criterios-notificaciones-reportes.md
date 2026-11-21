# Criterios de aceptación — Notificaciones y reportes (Práctica 5)

> Escritos **antes** de implementar el motor de notificaciones y los reportes. Cada criterio es
> una condición comprobable por un tercero siguiendo solo este documento y el README del
> repositorio, sin preguntar al autor. Si un criterio no se puede comprobar, se considera no
> cumplido.

## 0. Entorno y reglas de verificación

1. Clonar el repositorio y hacer `git checkout practica-5`.
2. `Copy-Item .env.example .env` y `docker compose up --build` (sección **Contenedor** del README).
3. Tener al menos **dos usuarios Administrador activos** y **dos usuarios Estándar activos**,
   creados según el README.
4. Obtener un JWT por usuario con `POST /api/auth/login` y usarlo como
   `Authorization: Bearer <token>`.
5. Salvo que el criterio diga otra cosa, **toda** petición se hace con sesión válida (RD-06).
6. Las fechas de los reportes se expresan en ISO 8601 (`AAAA-MM-DD`), en UTC, y los extremos
   `desde` y `hasta` son **inclusivos**.
7. **Máximo de intentos de envío de la cola: 3.** Este valor debe estar declarado también en la
   sección **Notificaciones** del README.

### Superficie de API que usan los criterios

| Método | Ruta | Quién | Para qué |
|---|---|---|---|
| `GET` | `/api/notificaciones` | autenticado | notificaciones propias (admite `?soloNoLeidas=true`) |
| `POST` | `/api/notificaciones/{id}/leer` | autenticado | marcar propia como leída |
| `GET` | `/api/notificaciones/preferencia` | autenticado | consultar `{ "recibeCorreos": bool }` |
| `PUT` | `/api/notificaciones/preferencia` | autenticado | cambiar la preferencia de correo |
| `GET` | `/api/correos` | Administrador | estado de la cola (admite `?estado=`) |
| `GET` | `/api/reportes/solicitudes` | autenticado | agregado de solicitudes por estado |
| `GET` | `/api/reportes/ordenes` | autenticado | agregado de órdenes por proveedor y estado |

---

## 1. Notificaciones (RF-NOT-01 a RF-NOT-07)

### RF-NOT-01 — Crear una solicitud notifica solo a los Administradores

- **Dado** dos Administradores activos (A1, A2), un Administrador **inactivo** (A3) y un Estándar
  autenticado (E), todos sin notificaciones previas.
- **Cuando** E ejecuta `POST /api/solicitudes` con un permiso y una descripción válidos.
- **Entonces**
  - A1 y A2 tienen exactamente **1** notificación nueva con `leida = false` cuyo mensaje menciona
    el permiso solicitado y el solicitante.
  - A3 (inactivo) y E tienen **0** notificaciones nuevas.
  - Ningún Estándar recibe la notificación.
  - La solicitud queda en estado `Pendiente` (no se altera el flujo de RF-GP-01).

**Verificación:** `GET /api/notificaciones` con el token de A1, A2, A3 y E. Se repite con una
segunda solicitud y cada Administrador activo debe tener 2, no 4.

### RF-NOT-02 — Resolver una solicitud notifica al solicitante

- **Dado** una solicitud pendiente creada por E y un Administrador autenticado.
- **Cuando** el Administrador la aprueba (`POST /api/solicitudes/{id}/aprobar`).
- **Entonces** E recibe **1** notificación nueva, `leida = false`, que indica que fue **aprobada**;
  ningún otro usuario recibe una notificación por esa resolución.
- **Y cuando** en otra solicitud el Administrador la rechaza con
  `POST /api/solicitudes/{id}/rechazar` y motivo `"No hay cupo en el turno"`.
- **Entonces** E recibe **1** notificación que indica que fue **rechazada** y cuyo texto contiene
  el motivo `"No hay cupo en el turno"`.
- En ambos casos la transición de estado de la solicitud sigue siendo la de RF-GP (la
  notificación no la bloquea ni la duplica).

**Verificación:** `GET /api/notificaciones` con el token de E después de cada resolución; se
compara el texto con el estado y el motivo.

### RF-NOT-03 — Un evento del módulo de negocio dispara notificación

Evento elegido: **enviar una orden de compra a aprobación**.

- **Dado** una orden de compra en estado `Borrador`, con al menos un detalle y proveedor asignado,
  y dos Administradores activos.
- **Cuando** se ejecuta `POST /api/ordenescompra/{id}/enviar-aprobacion`.
- **Entonces** cada Administrador activo recibe **1** notificación nueva, `leida = false`, cuyo
  mensaje identifica la orden (su `Id` o su total) y el evento.
- **Y** el README, en la sección **Notificaciones**, describe los pasos exactos para provocarlo; un
  tercero puede reproducirlo **sin leer el código**.
- La transición `Borrador → PendienteAprobacion` sigue funcionando como antes (RF-NEG).

**Verificación:** seguir la sección **Notificaciones** del README al pie de la letra y consultar
`GET /api/notificaciones` con cada Administrador.

### RF-NOT-04 — Consultar solo las notificaciones propias, con leídas y no leídas

- **Dado** dos usuarios autenticados, U1 y U2, cada uno con notificaciones leídas y no leídas.
- **Cuando** U1 ejecuta `GET /api/notificaciones`.
- **Entonces** la respuesta contiene **solo** notificaciones de U1; ninguna de U2 aparece. Cada
  elemento incluye `id`, `mensaje`, `leida`, `fechaCreacion` y un `tipo` no vacío.
- **Y cuando** U1 usa `GET /api/notificaciones?soloNoLeidas=true`, **entonces** solo recibe las de
  U1 con `leida = false`.
- **Y cuando** se llama sin token, **entonces** la respuesta es `401`.

**Verificación:** comparar los `id` devueltos para U1 y U2: las intersecciones deben ser vacías.

### RF-NOT-05 — Marcar como leída solo la propia

- **Dado** una notificación `N` de U2, con `leida = false`, y U1 autenticado.
- **Cuando** U1 ejecuta `POST /api/notificaciones/{N}/leer`.
- **Entonces** la operación se rechaza con `403` o `404` y `N` sigue con `leida = false` para U2.
- **Y cuando** U2 marca la suya, **entonces** responde `200`/`204` y `GET /api/notificaciones`
  muestra `leida = true`.
- Marcar dos veces la misma notificación propia es idempotente: sigue leída y no produce error.

**Verificación:** consultar con el token de U2 antes y después del intento de U1.

### RF-NOT-06 — Doble canal: interno y correo

- **Dado** un Administrador A con `recibeCorreos = true` y la cola vacía.
- **Cuando** se genera una notificación para A (por RF-NOT-01 o por RF-NOT-03).
- **Entonces**
  - A la ve en `GET /api/notificaciones` (canal interno), y
  - existe **exactamente un** registro `Pendiente` en `/api/correos` dirigido al email de A, cuyo
    asunto o cuerpo identifica el mismo evento (canal correo).
- No se crean dos correos para la misma notificación ni se envía el correo dentro de la petición
  (eso lo cubre RF-NOT-08).

**Verificación:** `GET /api/correos?estado=Pendiente` con token de Administrador, filtrando por el
email de A.

### RF-NOT-07 — El destinatario puede desactivar el canal de correo

- **Dado** un Administrador A1 con correo activo y A2 que ejecutó
  `PUT /api/notificaciones/preferencia` con `{ "recibeCorreos": false }`.
- **Cuando** se crea una solicitud de permiso (evento que notifica a todos los Administradores).
- **Entonces**
  - A2 recibe la notificación **interna** (aparece en su `GET /api/notificaciones`), y
  - **no** se encola ningún correo nuevo para el email de A2.
  - A1, con el correo activo, sí tiene un correo `Pendiente`.
- **Y** `GET /api/notificaciones/preferencia` devuelve `{ "recibeCorreos": false }` para A2, incluso
  después de cerrar y volver a iniciar sesión (la preferencia persiste).
- Al reactivarla (`true`), el siguiente evento vuelve a encolar correo para A2.

**Verificación:** contar correos por destinatario en `/api/correos` antes y después del evento.

---

## 2. Cola de correos (RF-NOT-08 a RF-NOT-13)

### RF-NOT-08 — El correo se encola, no se envía dentro de la operación

- **Dado** un `.env` cuyo servidor SMTP no responde (por ejemplo `SMTP_SERVER=127.0.0.1` y
  `SMTP_PORT=1`, o un host inexistente; valores **no** de ejemplo) y el stack levantado.
- **Cuando** se crea una solicitud de permiso (operación que origina correos).
- **Entonces**
  - la petición HTTP termina con éxito (`200`/`201`), **sin** `500` ni timeout,
  - la solicitud queda persistida,
  - los correos quedan en la cola (`Pendiente` o ya `Fallido`), y
  - el intento de conexión SMTP aparece solo en los logs del contenedor `worker`, nunca en los de
    `api`.

**Verificación:** `docker compose logs api` no muestra intentos SMTP; `docker compose logs worker`
sí. La API sigue respondiendo `GET /swagger` después de la operación.

### RF-NOT-09 — Un proceso independiente envía los pendientes

- **Dado** al menos un correo `Pendiente` y el contenedor `worker` corriendo.
- **Cuando** transcurre un ciclo del procesador (≤ 10 s) **sin** repetir la operación que creó el
  correo.
- **Entonces** el correo cambia de estado por acción del worker (`Enviado` con `fechaEnvio`, o
  `Fallido` si el SMTP falla) y `fechaUltimoIntento` queda registrada.
- El envío ocurre aunque la API se haya reiniciado después de encolar.

**Verificación:** `docker compose logs -f worker` muestra el procesamiento y
`GET /api/correos` refleja el cambio de estado.

### RF-NOT-10 — Reintentos limitados y estado fallido

- **Dado** un SMTP que falla siempre (host o puerto inválidos, credenciales no válidas) y un correo
  `Pendiente`.
- **Cuando** el worker lo procesa repetidamente.
- **Entonces**
  - `intentos` llega como máximo a **3**,
  - el estado final es `Fallido`,
  - `ultimoError` contiene el mensaje de la última excepción (no vacío),
  - `fechaUltimoIntento` queda registrada,
  - tras alcanzar `Fallido` **no se vuelve a intentar** en ciclos posteriores (el contador y la
    fecha no cambian al esperar dos ciclos más),
  - el correo **no desaparece** de la cola.
- El README declara el máximo de intentos (**3**) y cómo provocar el fallo.

**Verificación:** provocar el fallo según el README, esperar al menos tres ciclos y consultar el
correo como Administrador; luego esperar dos ciclos más y comprobar que `intentos` sigue en 3.

### RF-NOT-11 — La cola es consultable por un Administrador

- **Dado** correos en los tres estados posibles (`Pendiente`, `Enviado`, `Fallido`).
- **Cuando** un Administrador ejecuta `GET /api/correos`.
- **Entonces** obtiene `200` y por cada correo: `id`, `destinatario`, `asunto`, `estado`,
  `intentos`, `fechaCreacion`, `fechaEnvio` (nulo si no se envió), `fechaUltimoIntento` (nulo si
  no se intentó) y `ultimoError` (nulo si no falló).
- `GET /api/correos?estado=Fallido` devuelve solo los fallidos.
- **Y cuando** un Estándar consulta el endpoint, **entonces** recibe `403`.
- **Y cuando** se consulta sin token, **entonces** recibe `401`.

**Verificación:** tres llamadas con token de Administrador, token de Estándar y sin token.

### RF-NOT-12 — Un correo enviado no se reenvía

- **Dado** un correo ya `Enviado` (o un correo `Pendiente` que se enviará en el primer ciclo).
- **Cuando** se ejecuta el procesador dos veces seguidas (`docker compose restart worker` y esperar
  un ciclo, repetir).
- **Entonces**
  - no se crea un segundo registro para el mismo destinatario y asunto,
  - `fechaEnvio` del correo no cambia con la segunda ejecución,
  - `intentos` no aumenta por la segunda ejecución,
  - el envío se registra **una sola vez** en el log del worker (o una sola línea
    `[FALLBACK CORREO]` si se usa el fallback documentado).

**Verificación:** anotar `fechaEnvio` e `intentos` antes y después del segundo ciclo; contar las
líneas del log correspondientes a ese correo.

### RF-NOT-13 — Credenciales SMTP solo por variables de entorno

- **Dado** el repositorio clonado y `.env.example` copiado a `.env`.
- **Cuando** se revisa el código, la configuración y el historial de git.
- **Entonces**
  - `appsettings.json` no contiene contraseñas ni claves SMTP,
  - `docker compose config` muestra que `EmailSettings__*` se alimenta de variables de `.env`,
  - `git grep` no encuentra la contraseña SMTP ni la clave de la API externa en archivos
    versionados,
  - `git log --all -S "<valor>"` está vacío para esos valores,
  - el pre-commit de `gitleaks` pasa sin hallazgos.

**Verificación:** ejecutar los comandos anteriores usando el valor real solo en la terminal, sin
pegarlo en ningún archivo del repositorio.

---

## 3. Reportes con agregación (RF-REP-01 a RF-REP-05)

### RF-REP-01 — Reporte del Core: solicitudes por estado y período

- **Dado** al menos 10 solicitudes de permiso en distintos estados y fechas.
- **Cuando** un Administrador ejecuta
  `GET /api/reportes/solicitudes?desde=2026-01-01&hasta=2026-12-31`.
- **Entonces**
  - responde `200` con una colección de grupos `{ estado, cantidad }` y un total,
  - hay **menos filas que solicitudes** (por ejemplo, 10 solicitudes en 3 estados → 3 filas),
  - la suma de `cantidad` es igual al número de solicitudes del rango,
  - la respuesta **no** incluye el listado de solicitudes ni identificadores individuales.

**Verificación:** comparar el número de filas con el número de solicitudes creadas y sumar los
grupos.

### RF-REP-02 — Reporte del negocio: compras por proveedor y estado

Pregunta que responde: **¿cuánto se está comprando a cada proveedor y en qué estado están esas
órdenes dentro del período?** Permite ver en qué proveedor se concentra el gasto y cuánto está
aprobado, pendiente o recibido.

- **Dado** órdenes de compra de al menos dos proveedores y al menos dos estados.
- **Cuando** un Administrador ejecuta
  `GET /api/reportes/ordenes?desde=2026-01-01&hasta=2026-12-31`.
- **Entonces** devuelve, por cada combinación `(proveedor, estado)` presente en el rango:
  - `cantidadOrdenes`,
  - `montoTotalUsd` (suma de los totales de esas órdenes),
  - y los totales generales del período.
- El README, sección **Reportes**, formula la pregunta y explica cómo consultarlo.
- No es un conteo de filas ni un listado: agrupa y suma.

**Verificación:** comparar cada `montoTotalUsd` con la suma manual de las órdenes de ese proveedor
y estado; la suma de grupos debe igualar el total general.

### RF-REP-03 — Agregación real en ambos reportes

- **Dado** un conjunto con muchas filas y pocos grupos (por ejemplo, 12 solicitudes en 2 estados y
  12 órdenes de 3 proveedores en 4 estados).
- **Cuando** se consultan ambos reportes.
- **Entonces** el número de filas devueltas es menor que el número de registros que resumen, y los
  totales coinciden con los datos de origen.
- La agregación se hace en el motor de datos (EF `GroupBy`/`Sum`/`Count`, visible como `GROUP BY`
  en el SQL) o en una función propia del backend; **no** se trae el listado completo para sumarlo
  en el cliente.

**Verificación:** contar registros de origen y filas de salida; revisar el SQL generado en el log
de EF o el código del servicio.

### RF-REP-04 — Los reportes respetan el rol

- **Dado** dos Estándar, E1 y E2, cada uno con solicitudes y órdenes propias, y un Administrador con
  todas.
- **Cuando** E1 consulta `GET /api/reportes/solicitudes` y `GET /api/reportes/ordenes`.
- **Entonces**
  - los totales de E1 incluyen **solo** sus registros,
  - crear registros nuevos para E2 **no cambia** ningún número de E1,
  - E1 no recibe filas cuyos totales incluyan datos de E2,
  - un parámetro como `usuarioId` no permite a E1 consultar los datos de E2 (se ignora o se
    rechaza).
- El Administrador sí obtiene la vista global.

**Verificación:** comparar los totales de E1 con el conteo de sus propios registros y repetir tras
crear datos para E2.

### RF-REP-05 — Ambos reportes filtran por rango de fechas

- **Dado** solicitudes y órdenes distribuidas en enero, junio y diciembre de 2026.
- **Cuando** se consulta con `desde=2026-01-01&hasta=2026-03-31` y luego con
  `desde=2026-10-01&hasta=2026-12-31`, en ambos reportes.
- **Entonces**
  - los dos rangos producen **resultados distintos** sobre los mismos datos,
  - cada total coincide con los registros cuya fecha cae dentro del rango,
  - un registro con fecha exactamente igual a `desde` o a `hasta` **se incluye**,
  - `hasta` anterior a `desde` responde `400`.

**Verificación:** crear registros en fechas conocidas, consultar ambos rangos y comparar totales;
probar los extremos exactos y el rango invertido.

---

## 4. Criterios transversales

- **CT-01 — Sesión obligatoria:** los seis endpoints nuevos responden `401` sin token (RD-06).
- **CT-02 — Autorización por rol:** `/api/correos` es solo para Administrador (`403` para
  Estándar); los reportes nunca entregan datos de otros usuarios a un Estándar (RF-REP-04).
- **CT-03 — No romper lo anterior:** la suite existente (62 pruebas) sigue en verde y los seis
  frentes de la revisión acumulada siguen funcionando.
- **CT-04 — Pruebas nuevas:** cada regla nueva (RF-NOT y RF-REP) tiene al menos una prueba
  automatizada que falla si la regla se rompe.
- **CT-05 — Secretos:** ningún valor de `.env` aparece en archivos versionados ni en el historial
  (RF-NOT-13, RD-10).
- **CT-06 — README:** las secciones **Notificaciones** y **Reportes** permiten reproducir todos
  los criterios sin leer el código: eventos, procesador, fallo de envío, máximo de intentos,
  pregunta del reporte de negocio y ejemplos de rangos de fechas.

---

## 5. Matriz de trazabilidad

| Requisito | Endpoint / mecanismo | Prueba automatizada esperada |
|---|---|---|
| RF-NOT-01 | `POST /api/solicitudes` → notificaciones a Administradores | `RF_NOT_01_*` |
| RF-NOT-02 | `POST /api/solicitudes/{id}/aprobar` / `rechazar` | `RF_NOT_02_*` |
| RF-NOT-03 | `POST /api/ordenescompra/{id}/enviar-aprobacion` | `RF_NOT_03_*` |
| RF-NOT-04 | `GET /api/notificaciones` | `RF_NOT_04_*` |
| RF-NOT-05 | `POST /api/notificaciones/{id}/leer` | `RF_NOT_05_*` |
| RF-NOT-06 | Notificación interna + `CorreoEnCola` | `RF_NOT_06_*` |
| RF-NOT-07 | `PUT /api/notificaciones/preferencia` | `RF_NOT_07_*` |
| RF-NOT-08 | Encolado sin envío en la operación | `RF_NOT_08_*` |
| RF-NOT-09 | `ProcesadorCorreosWorker` | `RF_NOT_09_*` |
| RF-NOT-10 | Reintentos y `Fallido` | `RF_NOT_10_*` |
| RF-NOT-11 | `GET /api/correos` (Administrador) | `RF_NOT_11_*` |
| RF-NOT-12 | Idempotencia del envío | `RF_NOT_12_*` |
| RF-NOT-13 | Variables de entorno / historial | Revisión manual con `git grep` y `gitleaks` |
| RF-REP-01 | `GET /api/reportes/solicitudes` | `RF_REP_01_*` |
| RF-REP-02 | `GET /api/reportes/ordenes` | `RF_REP_02_*` |
| RF-REP-03 | `GroupBy` + totales en ambos | `RF_REP_03_*` |
| RF-REP-04 | Filtro por rol en ambos | `RF_REP_04_*` |
| RF-REP-05 | `desde` / `hasta` en ambos | `RF_REP_05_*` |
