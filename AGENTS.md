# AGENTS.md — Contexto para agentes de código

Este archivo describe el proyecto, cómo ejecutarlo, cómo probarlo y qué **no** se puede tocar.
Cualquier agente que trabaje en este repositorio debe leerlo completo antes de escribir código.

> **Estado actual (21/11/2026):** `main` en `e557148` (PR #25 de contraseñas mergeado; antes
> PR #24 de roles/administración, #23 de registro/activación y #22 de Práctica 5, **entregada al
> docente el 20/11/2026**).
> Baseline de pruebas: **92/92** (12 + 62 + 18).
> Para retomar el trabajo, leer primero `contexto-sesion.md` (§0 = estado vigente; el resto es
> el corte histórico de la Práctica 5).

---

## 1. Propósito del proyecto

Sistema de inventario desarrollado como proyecto final de **Programación III (TDS-007, ITLA)**.
Cubre, por hitos:

| Módulo | Requisitos | Estado |
|---|---|---|
| Autenticación y sesión (registro, activación, login/logout, recuperación/cambio/restablecimiento de contraseña) | RF-CA | Implementado en backend y frontend (PR #25) |
| Gestión de permisos de usuario | RF-GP | Implementado |
| Roles y administración de usuarios (frontend) | RF-CA-04/05/06/08/20/21 | Implementado |
| Órdenes de compra con máquina de estados | RF-NEG | Implementado |
| Manejador de documentos | RF-DOC | Implementado |
| Notificaciones y cola de correos | RF-NOT | Implementado (Práctica 5, tag `practica-5`) |
| Reportes con agregación | RF-REP | Implementado (Práctica 5, tag `practica-5`) |

## 2. Lenguaje, framework y estructura

- **C# / .NET 10** (`net10.0`), Nullable e ImplicitUsings habilitados.
- **Backend:** ASP.NET Core Web API + Entity Framework Core 10 + SQL Server 2022 (contenedor).
- **Frontend:** Blazor Web App (`InteractiveServer`) en `frontend/Inventario.Web`.
- **Pruebas:** xUnit 2.9 + EF Core InMemory.
- **Autenticación:** JWT Bearer; contraseñas con hash; autorización por rol (`Estandar`, `Administrador`).

```
ProyectoFinalProg3_Inventory/
├── Core.Domain/            Entidades y enums compartidos (Usuario, SolicitudPermiso, CorreoEnCola, Documento…)
├── Business.Inventory/     Módulo de negocio (OrdenCompra, DetalleOrden, Producto, Proveedor)
├── Core.Application/       Casos de uso, DTOs e interfaces (Services/, DTOs/, Interfaces/)
├── Infrastructure/         EF Core, migraciones, SMTP, JWT, hashing, almacenamiento, worker de correos
├── Presentation.API/       Controladores REST, DI, autorización, Swagger
├── EmailWorker/            Proceso independiente que vacía la cola de correos
└── *Tests/                 Core.Domain.Tests, Core.Application.Tests, Bussiness.inventory.Tests
frontend/Inventario.Web/    Blazor Server (login, registro/activación, roles y sesión)
docs/                       Documentación y criterios de aceptación
```

Dirección de dependencias: `Presentation.API → Infrastructure → Core.Application → Core.Domain`,
y `Business.Inventory → Core.Domain`. **No se permiten dependencias en sentido contrario.**

## 3. Comandos

### Levantar el proyecto (Docker)

```powershell
Copy-Item .env.example .env
docker compose up --build
```

- API y Swagger: `http://localhost:8080` y `http://localhost:8080/swagger`.
- El contenedor `worker` procesa la cola de correos; la API **no** envía correos.
- Logs: `docker compose logs -f api` y `docker compose logs -f worker`.

### Frontend (local)

```powershell
dotnet run --project frontend/Inventario.Web --launch-profile http
```

- Disponible en `http://localhost:5299`; requiere la API arriba en `http://localhost:8080`.

### Compilar y probar

```powershell
# Backend completo
dotnet build ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Suite de pruebas (línea base: 92 pruebas en verde)
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx

# Un grupo de pruebas
dotnet test ProyectoFinalProg3_Inventory/ProyectoFinalProg3_Inventory.slnx --filter "FullyQualifiedName~Notificacion"

# Frontend
dotnet build frontend/Inventario.Web/Inventario.Web.csproj
```

> `dotnet ef` devuelve código de salida 1 por warnings NU1902/NU1903 aunque el comando tenga
> éxito. No es un fallo.

### Migraciones

```powershell
dotnet ef migrations add <Nombre> `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API

dotnet ef database update `
  --project ProyectoFinalProg3_Inventory/Infrastructure `
  --startup-project ProyectoFinalProg3_Inventory/Presentation.API
```

La API aplica migraciones al arrancar (`Database.MigrateAsync` con reintentos).

## 4. Convenciones de código

- **Idioma:** comentarios, nombres de pruebas y mensajes de error en **español**; identificadores
  en inglés/PascalCase como ya lo hace el repositorio.
- **Capas:** la regla de negocio vive en la entidad de dominio; el servicio de caso de uso
  coordina; el controlador solo valida entrada, llama al servicio y traduce el resultado a HTTP.
- **Servicios:** interfaz en `Core.Application/Interfaces`, implementación en
  `Core.Application/Services`, registro en `Presentation.API/Program.cs`.
- **DTOs:** en `Core.Application/DTOs/<Área>`; nunca exponer entidades de EF en respuestas.
- **Errores:** el patrón actual es `InvalidOperationException` → `400`,
  `UnauthorizedAccessException` → `403`, `KeyNotFoundException` → `404`, capturados en el
  controlador. Mantener ese patrón salvo que la tarea pida otro.
- **Asincronía:** métodos `...Async` y `CancellationToken` cuando la firma ya lo recibe.
- **Pruebas:** xUnit + EF InMemory, una prueba por regla, nombre
  `RF_XX_NN_DescripcionDelEscenario`. Toda funcionalidad nueva debe traer pruebas.
- **Sin dependencias nuevas** de NuGet sin justificarlo; sin sobreingeniería (no CQRS, no MediatR,
  no patrones Repository nuevos si el módulo no los usa).
- **Comentarios de requisito:** cuando se implementa una regla, citarla (`// RF-NOT-01: …`).

## 5. Restricciones (no negociables)

1. **Secretos:** nunca leer `.env`, nunca escribir credenciales, claves, tokens ni contraseñas en
   código, pruebas, documentación, logs o commits. La configuración se inyecta por variables de
   entorno y se referencia como `${VARIABLE}`. `.env` está en `.gitignore`.
2. **No versionar** `README.Docker.md`, `uploads/`, `*.env` ni artefactos de build.
3. **Migraciones ya aplicadas:** no editarlas; cualquier cambio de esquema se hace con una
   migración **nueva**.
4. **No romper lo existente:** la máquina de estados de `OrdenCompra`, las reglas de
   `DocumentoService`, los flujos de `AuthService` y las rutas/contratos actuales no se modifican
   salvo que la tarea lo pida explícitamente. Solo se agrega funcionalidad.
5. **La suite debe seguir en verde:** las 92 pruebas actuales no pueden romperse; las nuevas se
   agregan aparte.
6. **No ejecutar** `docker compose down -v` ni comandos que borren volúmenes, datos o historial.
   Tampoco `git reset --hard`, `git push --force` ni reescrituras de historia.
7. **Frontend:** la prohibición de tocarlo era exclusiva de la Práctica 5 (cerrada y etiquetada).
   Hoy el frontend incluye registro/activación y administración de usuarios; cualquier cambio
   nuevo debe pedirlo el usuario.
8. **Shell:** PowerShell en Windows. No usar `&&`, `||`, `tail` ni `grep`; usar `;`,
   `Select-String`, `Select-Object`, `Out-Host`.
9. **Alcance:** si un requisito es ambiguo, preguntar antes de inventar comportamiento.
10. **Idioma de respuesta:** español.

## 6. Flujo de trabajo esperado

1. Leer `contexto-sesion.md` §0 (estado vigente; el resto es el corte de Práctica 5) y
   `docs/criterios-notificaciones-reportes.md` (criterios de aceptación vigentes).
2. Verificar que el build y las pruebas estén en verde **antes** de cambiar código.
3. Implementar en incrementos pequeños, con su prueba y su entrada en el README.
4. Ejecutar build + suite completa al terminar cada incremento.
5. No mezclar en un mismo commit documentación de criterios con código del motor de
   notificaciones: el orden de commits es parte de la evaluación.

## 7. Definición de terminado

- `dotnet build` del backend y del frontend sin errores.
- `dotnet test` con la suite anterior en verde y las pruebas nuevas pasando.
- Criterios de `docs/criterios-notificaciones-reportes.md` verificables a mano siguiendo el
  README, sin preguntar al autor.
- README actualizado (secciones **Notificaciones** y **Reportes**).
- Ningún secreto nuevo en el working tree ni en el historial (`gitleaks` en el pre-commit).
