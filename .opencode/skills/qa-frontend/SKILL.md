---
name: QA Frontend
description: Procedimiento para probar el frontend Blazor del inventario sin modificar el proyecto y registrar los hallazgos en errores-frontend.md
metadata:
  opencode/autoinvoke: false
---

# QA del frontend — solo pruebas

## Objetivo

Verificar el frontend `frontend/Inventario.Web` (Blazor Web App, .NET 10) de extremo a extremo y
documentar cada error, advertencia o bloqueo en `errores-frontend.md`.

**Regla de oro:** no modificar el proyecto. La única escritura permitida es el informe
`errores-frontend.md` en la raíz. No se edita código, configuración, migraciones, README ni ningún
otro archivo. No se ejecuta `git add`, `commit`, `push`, `merge`, `reset` ni `checkout`.

## 0. Contexto previo

1. Lee `AGENTS.md` y la sección §0 de `contexto-sesion.md` para conocer rutas y estado.
2. Revisa el estado sin cambiarlo: `git status --short` y `git log -1 --oneline`.

## 1. Comprobar el entorno

Sin autenticación, usa peticiones de solo lectura:

```powershell
Invoke-WebRequest http://localhost:8080/api/ordenescompra -SkipHttpErrorCheck   # 401 = API viva
Invoke-WebRequest http://localhost:5299 -SkipHttpErrorCheck                    # 200 = frontend vivo
Get-NetTCPConnection -LocalPort 8080,5299 -State Listen
```

- Si la API o el frontend no responden, **no los levantes por tu cuenta**: regístralo como
  bloqueado y pide al usuario que los inicie (Docker para la API, `dotnet run` para el frontend).
- No leas `.env` ni ningún archivo de secretos. No inventes credenciales.

## 2. Compilación

```powershell
dotnet build frontend/Inventario.Web/Inventario.Web.csproj --nologo
```

Registra cada error y warning con archivo y línea. Un build en verde es requisito para seguir.

## 3. Pruebas de rutas y navegación

Usa las herramientas de navegador si están disponibles; si no, al menos `Invoke-WebRequest`.
Para cada ruta revisa: código de respuesta, errores en consola, peticiones de red 4xx/5xx,
enlaces rotos y que la página renderice.

Rutas esperadas:

- Públicas: `/login`, `/registrar`, `/activar`, `/reenviar-activacion`, `/recuperar-password`,
  `/restablecer-password`.
- Privadas: `/`, `/ordenes`, `/proveedores`, `/productos`, `/solicitudes`,
  `/solicitudes/pendientes`, `/mis-permisos`, `/cambiar-password`, `/admin/usuarios`.
- Casos de acceso: sin sesión debe redirigir a login; Estándar en `/admin/usuarios` debe caer en
  `/acceso-denegado` o recibir `403`.

## 4. Flujos funcionales

Si el usuario te da una cuenta de prueba (Administrador y Estándar), recórrelos por la interfaz:

### Autenticación
- Login correcto e incorrecto; logout; persistencia de sesión al recargar.
- Registro, activación y reenvío; recuperación y cambio de contraseña.

### Órdenes de compra (`/ordenes`)
- Listado con **nombre del proveedor**, estado, total y fecha.
- Crear borrador: el desplegable de producto **solo muestra los productos del proveedor elegido**.
- Al cambiar de proveedor se limpian las líneas.
- **Ver detalle**: proveedor y líneas con producto, código, cantidad, precio y subtotal.
- **Editar** un borrador y guardar: el total se recalcula; tras enviar a aprobación el botón
  Editar desaparece.
- Enviar a aprobación; aprobar y cancelar con Administrador; recibir con permiso
  `recibir-orden` (y comprobar el mensaje sin permiso).
- Total en DOP.

### Datos maestros
- `/proveedores` y `/productos`: listado para cualquier sesión; formulario de alta solo para
  Administrador.
- Alta válida y validaciones: nombre vacío, correo inválido, precio/stock negativos, código
  duplicado, proveedor inexistente.

### Permisos
- Crear solicitud, ver pendientes como Administrador, aprobar/rechazar y comprobar «Mis permisos».
- Intentar exportar o recibir sin el permiso correspondiente.

Si no hay credenciales, prueba solo lo que no requiera sesión (401/403 esperados) y registra el
resto como **bloqueado**; no lo des por bueno.

## 5. Pruebas de API de apoyo

Verifica contratos que alimentan la UI (solo lectura):

- Sin token: `401` en `GET /api/ordenescompra`, `GET /api/ordenescompra/1`,
  `GET /api/catalogo/proveedores`.
- Con token Estándar: `403` al abrir una orden de otro usuario o al llamar endpoints de admin.
- Con datos inválidos: `400` con mensaje claro (por ejemplo, producto de otro proveedor).

## 6. Reglas de datos

- No borres datos existentes.
- Si necesitas crear datos para probar, usa el prefijo `QA-` en nombres y códigos, y anótalos en el
  informe para que el usuario los identifique.
- No ejecutes `docker compose down -v` ni comandos que borren volúmenes.

## 7. Informe `errores-frontend.md`

Al terminar, escribe el informe completo en `errores-frontend.md` (raíz del repositorio) con esta
plantilla. Si el archivo ya existe, reemplázalo; el informe siempre refleja la última corrida.

```markdown
# Informe de QA — Frontend

- **Fecha:** AAAA-MM-DD HH:MM
- **Rama:** `<rama>` · **Commit:** `<hash corto>`
- **Entorno:** API `http://localhost:8080` · Frontend `http://localhost:5299`
- **Cuentas usadas:** Administrador sí/no · Estándar sí/no
- **Build frontend:** OK / con errores (N)

## Resumen

| Severidad | Cantidad |
|---|---|
| Bloqueante | 0 |
| Alta | 0 |
| Media | 0 |
| Baja | 0 |
| Bloqueado / no verificable | 0 |

## Errores

| # | Severidad | Ruta / Archivo | Esperado | Ocurrió | Pasos para reproducir | Evidencia |
|---|---|---|---|---|---|---|
| 1 | Alta | `/ordenes` | ... | ... | 1. ... 2. ... | mensaje / captura / código |

## Advertencias

- ...

## Bloqueado / no verificable

- Motivo por el que no se pudo probar y qué necesita el usuario para desbloquearlo.

## Recomendaciones

- ...
```

### Severidades

- **Bloqueante:** impide usar el flujo principal (login, crear/aprobar/recibir una orden).
- **Alta:** el flujo termina, pero con datos o comportamiento incorrectos.
- **Media:** problema de usabilidad o validación que no rompe el flujo.
- **Baja:** detalle visual, texto o mejora menor.
- **Bloqueado:** no se pudo verificar por falta de credenciales, servicios caídos o datos.

## 8. Cierre

Responde con un resumen breve: total de errores por severidad, qué quedó bloqueado y la ruta del
informe. No propongas cambios de código en el informe; solo describe el síntoma y cómo reproducirlo.
