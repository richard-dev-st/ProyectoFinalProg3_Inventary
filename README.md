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