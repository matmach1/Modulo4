# Implementation Plan: Sistema de Reporte y Prevención de Riesgos Laborales

**Branch**: `001-reporte-riesgos-laborales` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-reporte-riesgos-laborales/spec.md`

## Summary

Aplicación web donde empleados reportan riesgos laborales (descripción, ubicación, fotos) y un
administrador de Seguridad e Higiene los gestiona hasta su cierre. Cada reporte se guarda al
instante como "Pendiente de clasificación" y se clasifica en segundo plano por tipo, categoría y
criticidad con una única llamada a Claude API, usando `kb.md` como contexto; si la IA falla o
tarda más de 10 s, queda pendiente. Backend ASP.NET Core 8 + EF Core/SQLite con sesiones del lado
del servidor, frontend React 18 + Vite 5. La integración con Claude vive aislada detrás de
`IClasificadorReportes` y todos los tests usan un clasificador simulado.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend); JavaScript (ES2022) con React 18 sobre Node 20 LTS (frontend)

**Primary Dependencies**: ASP.NET Core 8 Web API (minimal APIs), EF Core 8 + `Microsoft.EntityFrameworkCore.Sqlite`, `BCrypt.Net-Next`, SDK oficial `Anthropic` (C#); React 18, Vite 5, `react-router-dom`

**Storage**: SQLite (archivo `data/riesgos.db`) vía EF Core con migraciones; imágenes en disco (`data/imagenes/`), fuera de la carpeta pública

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory, SQLite en memoria, clasificador simulado) para backend; Vitest + Testing Library para frontend; herramienta aparte para precisión/latencia contra Claude API real

**Target Platform**: servidor Linux o Windows con .NET 8; navegadores de escritorio y móviles actuales

**Project Type**: aplicación web (backend API + frontend SPA)

**Performance Goals**: clasificación p95 < 3 s por llamada (SC-003, AC-45); respuesta de crear/editar sin esperar la IA (objetivo < 500 ms p95 en carga normal, supuesto del plan)

**Constraints**: timeout de 10 s y sin reintentos para la IA (FR-018); una única llamada por clasificación (RNF-01); solo texto al modelo (FR-015); secretos solo por variables de entorno (FR-031); sesión de 24 h por inactividad (FR-005)

**Scale/Scope**: supuesto: una organización, cientos de usuarios y algunos miles de reportes por año, un solo proceso de backend (la cola de clasificación es en memoria); 6 user stories, ~15 endpoints, ~8 pantallas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cómo lo cumple el diseño | Estado |
|-----------|--------------------------|--------|
| I. IA aislada y solo texto | Proyecto `Riesgos.Clasificacion` es el único que referencia el SDK; el resto usa `IClasificadorReportes` ([contracts/clasificador.md](contracts/clasificador.md)). `EntradaClasificacion` solo tiene descripción, ubicación y categoría sugerida. Una llamada con salida estructurada y `kb.md` en el system prompt. | ✅ |
| II. Ningún reporte se pierde por la IA | El reporte se guarda antes de clasificar; falla, timeout de 10 s o valor inválido ⇒ queda `Pendiente`; nunca un error HTTP por la IA. Validaciones antes de encolar. El borrador se regenera también al pasar a pendiente. | ✅ |
| III. Control de acceso en el backend | Políticas por rol + verificación de propiedad en el servicio; DTO de detalle armado según el rol; imágenes servidas solo por endpoint autenticado (FR-013a). Cada endpoint tiene test de acceso ([contracts/api.md](contracts/api.md)). Exactamente dos roles. | ✅ |
| IV. Test-first con modelo simulado | Máquina de estados, procesador de clasificación y permisos se escriben test-first. `ClasificadorSimulado` + cola controlable en tests ([research.md](research.md) R1). Cada AC con al menos un test que lo referencia. Precisión (AC-05) y latencia (AC-45) se miden aparte con la herramienta de evaluación (R9), porque requieren Claude API real y los tests no pueden llamarla; AC-46 se verifica con un script automatizado (T117). | ✅ |
| V. Sin secretos en el código | `API_KEY`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` por entorno; BCrypt; `.env` en `.gitignore`; chequeo AC-46 en quickstart. | ✅ |
| Alcance: nada fuera del PRD | El PRD fue enmendado (commit propio) con RF-02e (orden por criticidad), RF-42 (cerrar sesión), RF-43 (acceso a imágenes), RNF-07 (HTTPS), RNF-08 (clasificación en segundo plano) y AC-70 a AC-75. | ✅ |
| Flujo: trazabilidad RF/AC | Tabla endpoint → AC en [contracts/api.md](contracts/api.md). | ✅ |

### Chequeo contra `AGENTS.md`

La constitución define `AGENTS.md` como guía operativa y exige que se mantenga consistente.

| `AGENTS.md` | Plan | Estado |
|-------------|------|--------|
| Backend .NET 8, ASP.NET Core Web API | ASP.NET Core 8 Web API con minimal APIs (`Riesgos.Api`) | ✅ |
| SQLite vía EF Core | EF Core 8 + SQLite, migraciones | ✅ |
| Tests backend xUnit con `dotnet test` | xUnit en `backend/tests/`, `dotnet test` desde `backend/` sobre `Riesgos.sln` | ✅ |
| Frontend React 18 + Vite 5, Node 20 LTS | Igual; tests con Vitest vía `npm test` | ✅ |
| Clasificación con Claude API | SDK oficial aislado en `Riesgos.Clasificacion` | ✅ |
| `dotnet run --project src/Riesgos.Api` desde `backend/` | Ruta `backend/src/Riesgos.Api` | ✅ |
| `npm install` / `npm run dev` / `npm test` desde `frontend/` | Scripts `dev`, `build`, `test` en `frontend/package.json` (T004) | ✅ |
| API key solo desde `API_KEY` | `ClasificadorClaude` lee `API_KEY` y la pasa explícitamente al SDK (R3) | ✅ |
| Contraseñas con bcrypt/argon2 | BCrypt (`BCrypt.Net-Next`, work factor 12) | ✅ |
| Imágenes no son input del modelo | `EntradaClasificacion` solo tiene texto | ✅ |
| Sin CRM, multi-canal real, RBAC configurable, multi-tenant ni envío real de mails | Nada de eso en el diseño; el borrador de mail solo se muestra | ✅ |
| Variables de entorno documentadas | `API_KEY`, `ADMIN_EMAIL`, `ADMIN_PASSWORD` y `CLAUDE_MODEL` documentadas en `AGENTS.md` | ✅ |

**Resultado del gate (pre y post diseño)**: pasa. Las tareas de cerrar sesión y de orden por
criticidad, que dependían de enmendar el PRD, quedaron desbloqueadas con la enmienda (T006).

Notas de revisión post-diseño:
- La creación por el administrador (FR-009a) **sí** está cubierta por RF-01 ("cualquier usuario
  autenticado"); no requiere cambio del PRD.
- La visibilidad del detalle para el empleado (FR-011) restringe datos y deriva de RF-27 y RF-31;
  no es una funcionalidad nueva.
- AC-40 y AC-41 dicen "reporte recién creado … su tipo es uno de …". Con clasificación en segundo
  plano, el reporte nace `Pendiente`; los tests de esos AC procesan la cola antes de consultar el
  detalle. Es compatible con AC-56/AC-58 (201 + Pendiente), pero conviene aclarar la redacción de
  AC-40/AC-41 al actualizar el PRD.

## Project Structure

### Documentation (this feature)

```text
specs/001-reporte-riesgos-laborales/
├── plan.md              # Este archivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── api.md           # Endpoints REST, esquemas y trazabilidad a AC
│   └── clasificador.md  # IClasificadorReportes, JSON Schema, procesador
├── checklists/          # requirements, security, ai-classification, workflow
└── tasks.md             # Phase 2 (/speckit-tasks, no lo crea este comando)
```

### Source Code (repository root)

```text
backend/
├── Riesgos.sln
├── src/
│   ├── Riesgos.Api/                 # Host ASP.NET Core: Program.cs, DI, seed del admin
│   │   ├── Endpoints/               # AuthEndpoints, ReportesEndpoints, AdminEndpoints
│   │   ├── Auth/                    # Handler de sesión por cookie, políticas por rol
│   │   └── Dtos/                    # Requests/responses, armado del detalle según rol
│   ├── Riesgos.Core/                # Dominio y lógica, sin dependencia del SDK de Claude
│   │   ├── Dominio/                 # Usuario, Sesion, Reporte (máquina de estados), enums…
│   │   ├── Servicios/               # Cuentas, Reportes, Imágenes, Administración, Borrador
│   │   ├── Clasificacion/           # IClasificadorReportes, IColaClasificacion, Procesador
│   │   └── Persistencia/            # RiesgosDbContext, configuraciones, Migrations/
│   └── Riesgos.Clasificacion/       # ÚNICO proyecto con el SDK Anthropic
│       ├── ClasificadorClaude.cs
│       └── kb.md                    # Base de conocimiento (contexto del modelo)
├── tests/
│   ├── Riesgos.Core.Tests/          # Unit: estados, procesador, validaciones, borrador
│   └── Riesgos.Api.Tests/           # Integración por AC: WebApplicationFactory + simulado
└── tools/
    └── Riesgos.Evaluacion/          # Precisión y p95 contra Claude API real (fuera de dotnet test)

frontend/
├── package.json
├── vite.config.js                   # Proxy /api → backend
└── src/
    ├── api/                         # Cliente fetch con credentials: 'include'
    ├── auth/                        # Contexto de sesión, rutas protegidas
    ├── pages/                       # Login, Registro, MisReportes, NuevoReporte, Detalle,
    │                                # Panel, ListadoAdmin, CambiarPassword
    ├── components/
    └── __tests__/

datasets/                            # validacion-v{N}.csv (cuando lo entregue Seguridad e Higiene)
```

**Structure Decision**: aplicación web con `backend/` y `frontend/`, tal como indica `AGENTS.md`
(`dotnet run --project src/Riesgos.Api` desde `backend/`). El backend se divide en tres proyectos
para que el Principio I se cumpla por construcción: `Riesgos.Core` no puede referenciar el SDK de
Claude porque no lo tiene como dependencia, y solo `Riesgos.Clasificacion` lo hace. Las
referencias son `Api → Core, Clasificacion` y `Clasificacion → Core`.

## Complexity Tracking

| Violación | Por qué hace falta | Alternativa más simple descartada porque |
|-----------|--------------------|------------------------------------------|
| Tres proyectos de backend en vez de uno | Hace que el aislamiento de la IA (Principio I) lo garantice el compilador. | Un solo proyecto con carpetas depende de disciplina en las revisiones. |

## Riesgos y dependencias abiertas

- **Dataset de validación** (SC-002): no existe; bloquea dar por cumplidos AC-05 y AC-45.
- **Contenido de `kb.md`**: el plan crea un borrador desde las definiciones del PRD; debe
  validarlo Seguridad e Higiene.
- **Latencia con el modelo por defecto** (R3): medir p95 con el dataset; si supera 3 s, el equipo
  decide cambiar `CLAUDE_MODEL`.
- **Cola en memoria**: válida para un solo proceso de backend. Escalar a varias instancias
  requeriría una cola compartida (fuera del alcance de v1).
