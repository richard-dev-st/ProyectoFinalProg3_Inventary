# Bitácora de Sesión con el Agente - Asignación 1

## 1. Tareas Delegadas al Agente
Se le solicitó al agente asistencia para validar la estructura del repositorio, verificar las reglas del archivo `.gitignore` para el entorno de .NET/C#, estructurar la plantilla de Pull Requests y definir la documentación del proyecto.

## 2. Prompts y Respuestas
* **Solicitud:** "Genera la plantilla del PULL_REQUEST_TEMPLATE.md con las secciones exigidas en clase."
* **Resultado del Agente:** El agente proporcionó la plantilla estandarizada en Markdown con los encabezados `Qué cambia`, `Por qué`, `Cómo probarlo` y `Qué NO incluye`.

## 3. Evidencia de Error del Agente y Corrección
* **Error detectado:** Durante la revisión del comportamiento y flujo de la entidad `OrdenDeCompra`, el agente sugirió inicialmente manejar los estados mediante cadenas de texto libres (`string Estado = "Borrador"`) o simples condicionales `if/else`.
* **Cómo se detectó:** Se identificó que manejar estados con cadenas de texto o condicionales dispersos violaba el principio Open/Closed (SOLID) y fragmentaba las reglas de transición del dominio (Reglas RD-02/RD-04).
* **Corrección aplicada:** Se desestimó la sugerencia inicial y se planea implementar el **Patrón de Diseño State** (Patrón del GoF), encapsulando cada estado (`EstadoBorrador`, `EstadoAprobada`, `EstadoCancelada`, etc.) en clases concretas que implementan una interfaz común (`IEstadoOrden`), garantizando que la entidad del dominio delegue el comportamiento de transición de forma orientada a objetos.