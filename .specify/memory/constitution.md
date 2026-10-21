# Sistema de Reporte de Riesgos Laborales Constitution

## Core Principles

### I. IA aislada y solo texto

- Toda la integración con Claude API MUST vivir en un módulo de clasificación dedicado, expuesto
  detrás de una interfaz (p. ej. `IClasificadorReportes`).
- Controladores, servicios de negocio y persistencia MUST NOT invocar el SDK ni construir prompts;
  solo consumen la interfaz.
- El clasificador MUST recibir únicamente texto (descripción, ubicación y demás campos de texto) y
  MUST NOT recibir contenido de imagen (RF-21).
- Tipo, categoría y criticidad MUST obtenerse en una única llamada al modelo (RNF-01), usando
  `kb.md` como contexto.

**Rationale**: aislar la IA permite reemplazarla por un cliente simulado en tests (AC-23, AC-52,
AC-56, AC-67) y evita que cambios de prompt o de modelo afecten la lógica de negocio.

### II. Ningún reporte se pierde por la IA

- Si Claude API falla, no responde dentro de 10 s o devuelve un valor fuera de los tipos,
  categorías o criticidades válidos, el reporte MUST guardarse igual con tipo, categoría y
  criticidad "Pendiente de clasificación" (RF-35, RF-37).
- Una falla del modelo MUST NOT producir un error HTTP al empleado al crear o editar un reporte
  válido.
- El borrador de mail MUST regenerarse también al pasar a "Pendiente de clasificación" (RF-36).
- Las validaciones de entrada (campos obligatorios, largos de RF-41) MUST ejecutarse antes de
  llamar al modelo.

**Rationale**: el valor del sistema es que el riesgo quede registrado; la clasificación es una
ayuda. Ante la duda, decide un humano: el administrador ve los pendientes en su panel (RF-38).

### III. Control de acceso en el backend

- Cada endpoint MUST verificar autenticación y rol en el backend; ocultar elementos en el frontend
  no cuenta como control de acceso.
- Un empleado MUST poder ver y editar únicamente sus propios reportes; el acceso a reportes ajenos
  MUST responder HTTP 403 (AC-04, AC-36).
- Listado global, panel, cambio de estado, acciones correctivas y borrador de mail MUST ser
  exclusivos del administrador (AC-03, AC-16, AC-37, AC-38, AC-39, AC-42).
- Existen exactamente dos roles: empleado y administrador.

**Rationale**: el control de acceso roto es el riesgo #1 de OWASP, y el PRD lo verifica con AC
explícitos.

### IV. Test-First con el modelo simulado (NO NEGOCIABLE)

- La lógica de clasificación, el flujo de estados (Abierto → En Progreso → Cerrado) y los permisos
  MUST desarrollarse test-first: el test se escribe a partir del AC, falla, y recién después se
  implementa.
- Los tests automatizados MUST usar un cliente del modelo simulado; MUST NOT llamar a Claude API
  real.
- Cada AC del PRD MUST tener al menos un test que lo verifique y lo referencie por su ID.
- La precisión del modelo (RNF-02, RNF-03) se mide aparte, contra el dataset de validación
  versionado.

**Rationale**: los AC del PRD ya son verificables; escribirlos primero como tests evita
regresiones en reglas críticas y hace los tests deterministas y sin costo.

### V. Sin secretos en el código

- La API key de Claude MUST leerse de la variable de entorno `API_KEY` (RNF-04).
- Las credenciales del administrador inicial MUST provenir de variables de entorno (RF-30).
- Las contraseñas MUST almacenarse con bcrypt o argon2, nunca en texto plano (RNF-05).
- Ningún secreto, credencial ni archivo `.env` con valores reales MUST versionarse (AC-46).

**Rationale**: un secreto commiteado queda expuesto en el historial aunque se borre después.

## Alcance y Restricciones Técnicas

- Stack fijo: .NET 8 (ASP.NET Core Web API), EF Core + SQLite, xUnit; React 18 + Vite 5 sobre
  Node 20 LTS; Claude API para clasificación.
- No se agregan features fuera del PRD vigente. Todo lo listado en "Fuera de Alcance" del PRD
  (CRM, multi-canal real, RBAC configurable o más de dos roles, multi-tenant, envío real de
  mails, reintento de clasificación, entre otros) MUST NOT implementarse sin antes modificar el
  PRD.
- Las categorías y tipos de clasificación son los de RF-04 y RF-25; MUST NOT ampliarse.

## Flujo de Desarrollo y Quality Gates

- Cada cambio MUST trazarse a uno o más RF/AC del PRD.
- `dotnet test` y `npm test` MUST pasar antes de integrar a `main`.
- Los commits siguen Conventional Commits.
- Toda revisión MUST verificar que no haya llamadas al modelo fuera del módulo de clasificación,
  que cada endpoint nuevo tenga test de control de acceso y que no haya secretos en el diff.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. `AGENTS.md` es la guía
  operativa de runtime y MUST mantenerse consistente con ella.
- Las enmiendas MUST documentarse en un commit propio, con justificación y, si cambian el alcance,
  con la actualización correspondiente del PRD.
- Versionado semántico: MAJOR para eliminar o redefinir principios, MINOR para agregar principios
  o secciones, PATCH para aclaraciones de redacción.
- Los planes (`/speckit-plan`) MUST incluir un chequeo explícito contra estos principios; toda
  excepción MUST justificarse por escrito.

**Version**: 1.0.0 | **Ratified**: 2026-10-21 | **Last Amended**: 2026-10-21
