---

description: "Lista de tareas para implementar el Sistema de Reporte y Prevención de Riesgos Laborales"
---

# Tasks: Sistema de Reporte y Prevención de Riesgos Laborales

**Input**: Documentos de diseño en `/specs/001-reporte-riesgos-laborales/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/api.md](contracts/api.md), [contracts/clasificador.md](contracts/clasificador.md), [quickstart.md](quickstart.md)

**Tests**: INCLUIDOS y obligatorios. La constitución (Principio IV, no negociable) exige test-first
para clasificación, estados y permisos, y al menos un test por AC del PRD que lo referencie por su
ID. Cada test lleva `[Trait("AC", "AC-xx")]` (o `[Trait("FR", "FR-xxx")]` si no tiene AC). Los
tests se escriben primero y deben FALLAR antes de implementar. Nunca llaman a Claude API real.

**Organization**: tareas agrupadas por user story para implementar y probar cada una por separado.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: user story a la que pertenece (US1…US6)
- Las tareas antes marcadas como bloqueadas por el PRD (T085–T087, T107–T110) quedaron desbloqueadas: el PRD se enmendó en T006

## Path Conventions

- Backend: `backend/src/Riesgos.Api/`, `backend/src/Riesgos.Core/`, `backend/src/Riesgos.Clasificacion/`
- Tests backend: `backend/tests/Riesgos.Core.Tests/` (unit), `backend/tests/Riesgos.Api.Tests/` (integración)
- Frontend: `frontend/src/`, tests en `frontend/src/__tests__/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: crear la solución, los proyectos y la configuración base.

- [X] T001 Crear `backend/Riesgos.sln` con los proyectos `src/Riesgos.Api` (ASP.NET Core 8 web, minimal APIs), `src/Riesgos.Core` (classlib net8.0), `src/Riesgos.Clasificacion` (classlib net8.0), `tests/Riesgos.Core.Tests` (xUnit), `tests/Riesgos.Api.Tests` (xUnit) y `tools/Riesgos.Evaluacion` (console); referencias: Api → Core + Clasificacion, Clasificacion → Core, Core.Tests → Core + Clasificacion, Api.Tests → Api, Evaluacion → Core + Clasificacion
- [X] T002 [P] Crear `backend/Directory.Build.props` con `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=12`
- [X] T003 Agregar paquetes NuGet: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` y `BCrypt.Net-Next` a `backend/src/Riesgos.Core/Riesgos.Core.csproj`; `Anthropic` **solo** a `backend/src/Riesgos.Clasificacion/Riesgos.Clasificacion.csproj`; `Microsoft.AspNetCore.Mvc.Testing` y `Microsoft.Extensions.TimeProvider.Testing` a `backend/tests/Riesgos.Api.Tests/Riesgos.Api.Tests.csproj`; `Microsoft.Extensions.TimeProvider.Testing` a `backend/tests/Riesgos.Core.Tests/Riesgos.Core.Tests.csproj`
- [X] T004 [P] Crear el frontend con Vite 5 + React 18 en `frontend/` (`package.json` con scripts `dev`, `build`, `test`; dependencias `react-router-dom`; devDependencies `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom`), y `frontend/vite.config.js` con proxy `/api` → `http://localhost:5000` y configuración de Vitest (`environment: 'jsdom'`)
- [X] T005 [P] Crear `.gitignore` en la raíz (excluir `bin/`, `obj/`, `node_modules/`, `data/`, `*.db`, `.env`) y `.env.example` solo con los nombres `API_KEY=`, `ADMIN_EMAIL=`, `ADMIN_PASSWORD=`, `CLAUDE_MODEL=` sin valores (AC-46)
- [x] T006 [P] Enmendar `PRD.md` en un commit propio: agregar RF y AC para cerrar sesión (FR-005a: invalida la sesión en el servidor, uso posterior ⇒ 401) y para el orden opcional por criticidad del listado global (FR-024: Crítica, Alta, Media, Baja, Pendiente; fecha desc. dentro de cada nivel; orden desconocido ⇒ 400), y aclarar AC-40/AC-41 para la clasificación en segundo plano (desbloquea T086–T087 y T107–T110)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: dominio base, persistencia, autenticación por sesión, módulo de clasificación (interfaces) e infraestructura de tests. Bloquea todas las user stories.

**⚠️ CRITICAL**: ninguna user story empieza hasta completar esta fase.

### Tests de base (escribir primero)

- [X] T007 [P] Test de arquitectura en `backend/tests/Riesgos.Core.Tests/ArquitecturaTests.cs`: el ensamblado `Riesgos.Core` y `Riesgos.Api` no referencian el ensamblado `Anthropic` directamente (Principio I) [Trait("FR","FR-014")]
- [X] T008 [P] Tests del validador de textos en `backend/tests/Riesgos.Core.Tests/Validacion/TextoTests.cs`: recorta espacios, un texto solo con espacios es vacío, respeta largos máximos (R8)
- [X] T009 [P] Tests del handler de sesión en `backend/tests/Riesgos.Api.Tests/Auth/SesionTests.cs`: request sin cookie a un endpoint protegido ⇒ 401 (AC-12); login con credenciales válidas ⇒ 200 + cookie `HttpOnly`/`SameSite=Strict`; login con email inexistente y con contraseña incorrecta ⇒ 401 con el mismo mensaje (R4); `UltimaActividad` se renueva; fuera de Development, un request HTTP ⇒ redirección a HTTPS y las respuestas HTTPS incluyen `Strict-Transport-Security` (FR-032, AC-74)

### Dominio y persistencia

- [X] T010 [P] Crear enums `Rol`, `EstadoReporte`, `TipoReporte`, `Categoria`, `Criticidad` (con `Pendiente` en los tres de clasificación) y el orden de criticidad (`Critica`=0 … `Pendiente`=4) en `backend/src/Riesgos.Core/Dominio/Enums.cs`, según [data-model.md](data-model.md)
- [X] T011 [P] Crear entidad `Usuario` (`Email` string(254) normalizado trim+minúsculas, `PasswordHash`, `Rol`, `FechaCreacion`) en `backend/src/Riesgos.Core/Dominio/Usuario.cs`
- [X] T012 [P] Crear entidad `Sesion` (`UsuarioId`, `TokenHash` string(64), `FechaCreacion`, `UltimaActividad`; método `EstaVigente(ahora)` = `UltimaActividad + 24 h ≥ ahora`) en `backend/src/Riesgos.Core/Dominio/Sesion.cs`
- [X] T013 [P] Implementar `Texto.Normalizar` y `Texto.Validar(valor, campo, maximo)` en `backend/src/Riesgos.Core/Validacion/Texto.cs` y `ErrorValidacion` en `backend/src/Riesgos.Core/Validacion/ErrorValidacion.cs` (hace pasar T008)
- [X] T014 Crear `RiesgosDbContext` con `Usuarios` y `Sesiones`, índice único sobre `Usuario.Email` y sobre `Sesion.TokenHash`, enums guardados como texto, en `backend/src/Riesgos.Core/Persistencia/RiesgosDbContext.cs` y `backend/src/Riesgos.Core/Persistencia/Configuraciones/UsuarioConfig.cs`, `SesionConfig.cs`
- [X] T015 Crear la migración inicial `Inicial` en `backend/src/Riesgos.Core/Persistencia/Migrations/` y aplicar migraciones al arrancar en `backend/src/Riesgos.Api/Program.cs` (cadena de conexión por defecto `Data Source=data/riesgos.db`)
- [X] T016 [P] Implementar `HashContrasena` (BCrypt work factor 12; rechazar > 72 bytes) en `backend/src/Riesgos.Core/Servicios/HashContrasena.cs`

### Autenticación y API base

- [X] T017 Implementar `ServicioSesiones` (crear token aleatorio de 256 bits, guardar SHA-256, validar vigencia con `TimeProvider`, renovar `UltimaActividad` como máximo una vez por minuto, borrar sesión, borrar las demás sesiones de un usuario) en `backend/src/Riesgos.Core/Servicios/ServicioSesiones.cs`
- [X] T018 Implementar `SesionAuthenticationHandler` (lee la cookie `riesgos_sesion`, valida con `ServicioSesiones`, emite claims `sub` y `role`) y las políticas `Empleado`/`Administrador` en `backend/src/Riesgos.Api/Auth/SesionAuthenticationHandler.cs` y `backend/src/Riesgos.Api/Auth/Politicas.cs`
- [X] T019 Implementar `POST /api/auth/login` y `GET /api/auth/yo` en `backend/src/Riesgos.Api/Endpoints/AuthEndpoints.cs` (cookie `HttpOnly`, `Secure`, `SameSite=Strict`; mismo 401 para email o contraseña incorrectos) (hace pasar T009)
- [X] T020 [P] Configurar `ProblemDetails`, mapeo `ErrorValidacion` ⇒ 400, `DbUpdateConcurrencyException` ⇒ 409, logging estructurado sin datos sensibles, y `UseHttpsRedirection` + `UseHsts` fuera de Development (FR-032) en `backend/src/Riesgos.Api/Infraestructura/Errores.cs` y `backend/src/Riesgos.Api/Program.cs` (R8, R13)
- [X] T021 Implementar el seed del administrador inicial (`ADMIN_EMAIL`/`ADMIN_PASSWORD`; si ya existe un admin no se modifica; si no existe y faltan las variables, la app no arranca con un mensaje claro) en `backend/src/Riesgos.Api/Infraestructura/SeedAdministrador.cs` (FR-006, R12)

### Módulo de clasificación (interfaces y cola)

- [X] T022 [P] Crear `IClasificadorReportes`, `EntradaClasificacion`, `ResultadoClasificacion` (`Exito`/`Falla`) y `MotivoFalla` en `backend/src/Riesgos.Core/Clasificacion/IClasificadorReportes.cs`, exactamente como en [contracts/clasificador.md](contracts/clasificador.md)
- [X] T023 [P] Crear `IColaClasificacion` (`Encolar(TrabajoClasificacion)`) y `TrabajoClasificacion(int ReporteId, int Version)` en `backend/src/Riesgos.Core/Clasificacion/IColaClasificacion.cs`, y la implementación con `Channel<TrabajoClasificacion>` en `backend/src/Riesgos.Core/Clasificacion/ColaClasificacion.cs`

### Infraestructura de tests

- [X] T024 Crear `ApiFactory` (WebApplicationFactory con SQLite en memoria compartida, `FakeTimeProvider`, `ClasificadorSimulado` y `ColaClasificacionManual` registrados, variables de admin de prueba) en `backend/tests/Riesgos.Api.Tests/Infraestructura/ApiFactory.cs`
- [X] T025 [P] Crear `ClasificadorSimulado` (configurable: devolver `Exito`, `Falla`, demorar más de 10 s; registra cada `EntradaClasificacion` recibida) en `backend/tests/Riesgos.Api.Tests/Infraestructura/ClasificadorSimulado.cs`
- [X] T026 [P] Crear `ColaClasificacionManual` (acumula trabajos; `ProcesarPendientesAsync()` los procesa con el `ProcesadorClasificacion` cuando exista, o no hace nada antes de US2) en `backend/tests/Riesgos.Api.Tests/Infraestructura/ColaClasificacionManual.cs`
- [X] T027 [P] Crear helpers `ClienteAutenticadoAsync(email, rol)` y `ComoAdminAsync()` en `backend/tests/Riesgos.Api.Tests/Infraestructura/Clientes.cs`

### Frontend base

- [X] T028 [P] Crear el cliente HTTP (`fetch` con `credentials: 'include'`, parseo de `ProblemDetails`, redirección a login ante 401) en `frontend/src/api/cliente.js`
- [X] T029 [P] Crear el contexto de sesión (`AuthProvider`, `useAuth` con `usuario`, `login`) y `RutaProtegida` (por rol) en `frontend/src/auth/AuthContext.jsx` y `frontend/src/auth/RutaProtegida.jsx`
- [X] T030 Crear el router y el layout con menú según rol en `frontend/src/main.jsx` y `frontend/src/App.jsx`, y la página de login en `frontend/src/pages/Login.jsx`
- [X] T031 [P] Test del login (envía credenciales, muestra error genérico ante 401) en `frontend/src/__tests__/Login.test.jsx`
- [X] T032 [P] Crear `frontend/src/textos.js` con la traducción de los enums de la API a los textos de la spec ("EnProgreso" ⇒ "En Progreso", "UsoEpp" ⇒ "Uso de elementos de protección personal", "Pendiente" ⇒ "Pendiente de clasificación", etc.)

**Checkpoint**: la app arranca, aplica migraciones, crea el admin, permite login y rechaza requests sin sesión. `dotnet test` y `npm test` en verde.

---

## Phase 3: User Story 1 - Empleado reporta un riesgo y sigue su estado (Priority: P1) 🎯 MVP

**Goal**: un empleado se registra, inicia sesión, crea reportes y ve solo los suyos con su estado y detalle.

**Independent Test**: registrar un usuario, crear un reporte y verificar que aparece en su lista en estado Abierto con fecha, hora y usuario de creación (la clasificación queda "Pendiente de clasificación" hasta US2).

### Tests for User Story 1 ⚠️ (escribir primero, deben fallar)

- [X] T033 [P] [US1] Tests de registro en `backend/tests/Riesgos.Api.Tests/US1/RegistroTests.cs`: AC-13 (crea y permite login), AC-31 (email duplicado ⇒ 409, también con distinta capitalización), AC-63 (registros simultáneos con el mismo email ⇒ un 201 y el resto 409, una sola cuenta), AC-64 (email inválido ⇒ 400), AC-65 (contraseña < 8 ⇒ 400), rol asignado siempre `Empleado` aunque el body pida otro (FR-001)
- [X] T034 [P] [US1] Tests de creación en `backend/tests/Riesgos.Api.Tests/US1/CrearReporteTests.cs`: AC-27 (201, estado Abierto), AC-75 (con el clasificador sin responder, responde de inmediato con clasificación `Pendiente`), AC-01 (descripción vacía ⇒ 400), AC-28 (ubicación vacía ⇒ 400), AC-67 (descripción > 2.000 ⇒ 400 y el `ClasificadorSimulado` no recibió llamadas), AC-68 (ubicación > 200 ⇒ 400), texto solo con espacios ⇒ 400, `categoriaSugerida` inválida ⇒ 400, el administrador también puede crear (FR-009a)
- [X] T035 [P] [US1] Tests de "mis reportes" en `backend/tests/Riesgos.Api.Tests/US1/MisReportesTests.cs`: AC-29 (A solo recibe los suyos), AC-12 (sin sesión ⇒ 401), orden por fecha desc.
- [X] T036 [P] [US1] Tests de detalle en `backend/tests/Riesgos.Api.Tests/US1/DetalleReporteTests.cs`: AC-50 (creador ⇒ 200), AC-17 (fecha, hora y email del creador), AC-04 (otro empleado ⇒ 403), reporte inexistente ⇒ 404, el detalle del empleado no contiene las propiedades `accionSugerida` ni `borradorMail` (FR-011)

### Implementation for User Story 1

- [X] T037 [P] [US1] Crear entidad `Reporte` con todos los campos de [data-model.md](data-model.md) (`Descripcion` string(2000), `Ubicacion` string(200), `CategoriaSugerida` nullable, `Tipo`/`Categoria`/`Criticidad` iniciando en `Pendiente`, `VersionClasificacion`, `Estado` iniciando en `Abierto`, `CreadorId`, `FechaCreacion`, campos de progreso y cierre nullable, `RowVersion` como token de concurrencia) en `backend/src/Riesgos.Core/Dominio/Reporte.cs`
- [X] T038 [US1] Configurar `Reporte` en EF Core (índices `(CreadorId, FechaCreacion)`, `(FechaCreacion, Id)`, `(Estado, Criticidad)`; `RowVersion` con `IsConcurrencyToken`) en `backend/src/Riesgos.Core/Persistencia/Configuraciones/ReporteConfig.cs` y crear la migración `Reportes`
- [X] T039 [US1] Implementar `ServicioCuentas.RegistrarAsync` (normaliza email, valida formato y largo de contraseña, hashea, captura la violación del índice único ⇒ conflicto) en `backend/src/Riesgos.Core/Servicios/ServicioCuentas.cs`
- [X] T040 [US1] Implementar `ServicioReportes.CrearAsync` (valida y recorta textos antes de cualquier otra cosa, guarda con clasificación `Pendiente`, `VersionClasificacion = 1`, encola `TrabajoClasificacion`), `ListarPropiosAsync(usuarioId, page, size)` y `ObtenerDetalleAsync(id, usuario)` con verificación de propiedad (creador o admin; ajeno ⇒ prohibido; inexistente ⇒ no encontrado) en `backend/src/Riesgos.Core/Servicios/ServicioReportes.cs`
- [X] T041 [P] [US1] Crear DTOs `RegistroRequest`, `CrearReporteRequest`, `ReporteResumen`, `ReporteDetalle`, `Pagina<T>` y el armado del detalle según rol (empleado sin `accionSugerida` ni `borradorMail`) en `backend/src/Riesgos.Api/Dtos/`
- [X] T042 [US1] Implementar `POST /api/auth/registro` en `backend/src/Riesgos.Api/Endpoints/AuthEndpoints.cs` (hace pasar T033)
- [X] T043 [US1] Implementar `POST /api/reportes`, `GET /api/reportes/mios` y `GET /api/reportes/{id}` en `backend/src/Riesgos.Api/Endpoints/ReportesEndpoints.cs` (hace pasar T034–T036)
- [X] T044 [P] [US1] Crear la página de registro en `frontend/src/pages/Registro.jsx`
- [X] T045 [P] [US1] Crear la página "Nuevo reporte" (descripción, ubicación, categoría sugerida opcional; muestra errores de validación) en `frontend/src/pages/NuevoReporte.jsx`
- [X] T046 [P] [US1] Crear la página "Mis reportes" (estado, clasificación, fecha; estado vacío "Todavía no creaste reportes") en `frontend/src/pages/MisReportes.jsx`
- [X] T047 [US1] Crear la página de detalle (datos, estado, clasificación, fecha, hora y creador; mensaje de acceso denegado ante 403) en `frontend/src/pages/DetalleReporte.jsx`
- [X] T048 [P] [US1] Tests de frontend para registro, nuevo reporte y mis reportes en `frontend/src/__tests__/US1.test.jsx`

**Checkpoint**: US1 funciona y se prueba sola (MVP sin clasificación: los reportes quedan "Pendiente de clasificación").

---

## Phase 4: User Story 2 - Clasificación automática de cada reporte (Priority: P1)

**Goal**: cada reporte se clasifica en segundo plano por tipo, categoría y criticidad con una única llamada a Claude; si falla o tarda más de 10 s, queda pendiente.

**Independent Test**: con el clasificador simulado, crear un reporte, procesar la cola y verificar tipo, categoría y criticidad válidos; simular un error y verificar que queda "Pendiente de clasificación".

### Tests for User Story 2 ⚠️ (escribir primero, deben fallar)

- [X] T049 [P] [US2] Tests unitarios del procesador en `backend/tests/Riesgos.Core.Tests/Clasificacion/ProcesadorClasificacionTests.cs`: éxito guarda los tres valores; `Falla` deja `Pendiente`; demora > 10 s (con `FakeTimeProvider`) ⇒ `Pendiente` (AC-58); trabajo con versión vieja ⇒ se descarta sin llamar al clasificador ni escribir (FR-017); resultado que llega cuando la versión ya cambió ⇒ no se guarda; la entrada enviada solo tiene descripción, ubicación y categoría sugerida (AC-23)
- [X] T050 [P] [US2] Tests del mapeo de la respuesta del modelo en `backend/tests/Riesgos.Core.Tests/Clasificacion/MapeadorRespuestaTests.cs`: JSON válido ⇒ `Exito` con los enums correctos; valor fuera de lista, mayúsculas/tildes distintas, campo faltante, JSON inválido o `stop_reason` distinto de `end_turn` ⇒ `Falla(RespuestaInvalida)` (FR-018)
- [X] T051 [P] [US2] Tests de integración en `backend/tests/Riesgos.Api.Tests/US2/ClasificacionTests.cs`: AC-40 (tras procesar la cola, el tipo es uno de los 5 válidos; categoría y criticidad válidas), AC-06 (categoría sugerida distinta ⇒ la definitiva es la automática y ambas quedan guardadas), AC-56 (error del clasificador ⇒ 201 y `Pendiente`), AC-58 (sin respuesta en 10 s ⇒ 201 y `Pendiente`), AC-23 (reporte con imágenes: la entrada registrada no contiene contenido de imagen; activar cuando exista US5)

### Implementation for User Story 2

- [X] T052 [US2] Implementar `ProcesadorClasificacion.ProcesarAsync(TrabajoClasificacion, CancellationToken)` (descarta versión vieja, aplica timeout de 10 s con `CancellationTokenSource(TimeSpan, TimeProvider)`, actualización condicional `WHERE Id = @id AND VersionClasificacion = @v`, log de resultado/motivo/latencia sin el texto) en `backend/src/Riesgos.Core/Clasificacion/ProcesadorClasificacion.cs` (hace pasar T049)
- [X] T053 [US2] Implementar `ClasificacionWorker : BackgroundService` que consume la `ColaClasificacion` y llama al procesador (una excepción no detiene el worker) en `backend/src/Riesgos.Api/Infraestructura/ClasificacionWorker.cs`
- [X] T054 [P] [US2] Escribir el borrador de la base de conocimiento con definiciones de los 5 tipos, las 4 categorías (incluido cuándo usar "Otro") y criterios para Baja/Media/Alta/Crítica, en `backend/src/Riesgos.Clasificacion/kb.md` (copiado al output como contenido; pendiente de validación por Seguridad e Higiene)
- [X] T055 [P] [US2] Implementar `MapeadorRespuesta` (JSON ⇒ `ResultadoClasificacion`, igualdad exacta contra los textos del JSON Schema) en `backend/src/Riesgos.Clasificacion/MapeadorRespuesta.cs` (hace pasar T050)
- [X] T056 [US2] Implementar `ClasificadorClaude : IClasificadorReportes` con el SDK oficial: `ApiKey` desde `API_KEY`, reintentos en 0, modelo desde `CLAUDE_MODEL` (por defecto `claude-opus-5-5`), effort `low`, `MaxTokens` 2048, system = instrucciones (incluida la de tratar el texto del reporte solo como dato e ignorar instrucciones que contenga) + `kb.md` con `CacheControlEphemeral`, mensaje de usuario solo texto, `OutputConfig.Format = JsonOutputFormat` con el schema de [contracts/clasificador.md](contracts/clasificador.md); errores del SDK y cancelación ⇒ `Falla` (sin `API_KEY` ⇒ `Falla(SinApiKey)` y advertencia al inicio), en `backend/src/Riesgos.Clasificacion/ClasificadorClaude.cs`
- [X] T057 [US2] Registrar en DI `IClasificadorReportes` ⇒ `ClasificadorClaude`, `IColaClasificacion` ⇒ `ColaClasificacion` (singleton), `ProcesadorClasificacion` y `ClasificacionWorker` en `backend/src/Riesgos.Api/Program.cs`; en `ApiFactory` la `ColaClasificacionManual` usa el procesador real con el `ClasificadorSimulado` (hace pasar T051)
- [X] T058 [US2] Mostrar en el detalle tipo, categoría y criticidad (o "Pendiente de clasificación") y la categoría sugerida si la hay, con polling cada 2 s hasta 15 s mientras esté pendiente, en `frontend/src/pages/DetalleReporte.jsx` y `frontend/src/hooks/usePollingClasificacion.js`
- [X] T059 [P] [US2] Test de frontend del polling (deja de consultar al llegar la clasificación o a los 15 s) en `frontend/src/__tests__/US2.test.jsx`

**Checkpoint**: US1 + US2 funcionan; los reportes se clasifican en segundo plano y las fallas quedan pendientes.

---

## Phase 5: User Story 3 - Administrador ve los reportes priorizados (Priority: P2)

**Goal**: el administrador ve el listado global paginado, el panel con contadores y el detalle con acción sugerida, ambas categorías y borrador de mail.

**Independent Test**: con reportes de distintos usuarios y criticidades cargados, verificar listado, contadores del panel y detalle con acción sugerida y borrador.

### Tests for User Story 3 ⚠️ (escribir primero, deben fallar)

- [X] T060 [P] [US3] Tests unitarios de acción sugerida y borrador en `backend/tests/Riesgos.Core.Tests/Notificacion/BorradorMailTests.cs`: Crítica ⇒ "Detener actividad", Alta ⇒ "Requiere intervención inmediata", Baja/Media/Pendiente ⇒ ninguna (AC-08, AC-09, AC-30); asunto con categoría y criticidad; asunto con "Pendiente de clasificación"; cuerpo con descripción, ubicación, fecha de creación y email del creador (AC-41)
- [X] T061 [P] [US3] Tests del listado global en `backend/tests/Riesgos.Api.Tests/US3/ListadoAdminTests.cs`: AC-02 (como máximo `size` de la página pedida, cada uno con estado), AC-49 (fecha desc.), página fuera de rango ⇒ lista vacía, `size` por defecto 20, `size` ≤ 0 o > 100 ⇒ 400, orden estable con fechas iguales, AC-03 y AC-42 (empleado, incluso recién registrado ⇒ 403), AC-44 (con base vacía y variables de admin, el admin inicia sesión y lista)
- [X] T062 [P] [US3] Tests del panel en `backend/tests/Riesgos.Api.Tests/US3/PanelTests.cs`: AC-26 (N Críticos en estado Abierto), AC-60 (M Abiertos pendientes), Críticos o pendientes en En Progreso o Cerrado no cuentan (FR-026), AC-38 (empleado ⇒ 403)
- [X] T063 [P] [US3] Tests del detalle para el admin en `backend/tests/Riesgos.Api.Tests/US3/DetalleAdminTests.cs`: AC-51 (cualquier reporte ⇒ 200), AC-08/AC-09/AC-30 (acción sugerida según criticidad), AC-41 (borrador tras procesar la cola), AC-43 (ambas categorías cuando difieren), AC-39 (el empleado creador no recibe `borradorMail`)
- [X] T064 [P] [US3] Test del evento de notificación en `backend/tests/Riesgos.Api.Tests/US3/EventoNotificacionTests.cs`: AC-07 (al terminar la creación existe un evento asociado al reporte, uno solo)

### Implementation for User Story 3

- [X] T065 [P] [US3] Crear entidad `EventoNotificacion` (`ReporteId` único, `FechaCreacion`, `AsuntoBorrador`, `CuerpoBorrador`, `FechaActualizacion`) en `backend/src/Riesgos.Core/Dominio/EventoNotificacion.cs`, su configuración en `backend/src/Riesgos.Core/Persistencia/Configuraciones/EventoNotificacionConfig.cs` y la migración `EventosNotificacion`
- [X] T066 [P] [US3] Implementar `AccionSugerida.Para(Criticidad)` y `GeneradorBorrador.Generar(Reporte, emailCreador)` (formato de asunto de [data-model.md](data-model.md)) en `backend/src/Riesgos.Core/Notificacion/AccionSugerida.cs` y `backend/src/Riesgos.Core/Notificacion/GeneradorBorrador.cs` (hace pasar T060)
- [X] T067 [US3] Crear el `EventoNotificacion` con su borrador en la misma transacción de `ServicioReportes.CrearAsync` en `backend/src/Riesgos.Core/Servicios/ServicioReportes.cs` (hace pasar T064)
- [X] T068 [US3] Regenerar (reemplazar) el borrador cuando el procesador guarda un resultado o una falla en `backend/src/Riesgos.Core/Clasificacion/ProcesadorClasificacion.cs` (FR-022)
- [X] T069 [US3] Implementar `ServicioAdministracion.ListarAsync(page, size)` (por defecto 20, valida 1–100, orden `FechaCreacion DESC, Id DESC`, total) y `PanelAsync()` (solo estado Abierto) en `backend/src/Riesgos.Core/Servicios/ServicioAdministracion.cs`
- [X] T070 [US3] Completar el armado del detalle para el administrador (acción sugerida, borrador, categoría sugerida y automática) en `backend/src/Riesgos.Api/Dtos/ReporteDetalleMapper.cs`
- [X] T071 [US3] Implementar `GET /api/admin/reportes` y `GET /api/admin/panel` con la política `Administrador` en `backend/src/Riesgos.Api/Endpoints/AdminEndpoints.cs` (hace pasar T061–T063)
- [X] T072 [P] [US3] Crear la página del panel (dos contadores que muestran 0 cuando no hay reportes, enlaces al listado; FR-033) en `frontend/src/pages/Panel.jsx`
- [X] T073 [P] [US3] Crear la página del listado global (paginación con total, estado y criticidad por fila, estado vacío) en `frontend/src/pages/ListadoAdmin.jsx`
- [X] T074 [US3] Agregar al detalle, solo para admin, la acción sugerida destacada, ambas categorías cuando difieren y el borrador con botón "Copiar" en `frontend/src/components/DetalleAdmin.jsx` y `frontend/src/pages/DetalleReporte.jsx`
- [X] T075 [P] [US3] Tests de frontend del panel, el listado y el borrador en `frontend/src/__tests__/US3.test.jsx`

**Checkpoint**: US1–US3 funcionan; el administrador ve y prioriza los reportes.

---

## Phase 6: User Story 4 - Administrador gestiona el reporte hasta su cierre (Priority: P2)

**Goal**: el administrador pasa un reporte de Abierto a En Progreso, registra acciones correctivas y lo cierra con acción realizada y observación.

**Independent Test**: tomar un reporte Abierto, pasarlo a En Progreso, registrar una acción y cerrarlo; verificar estados, acción con fecha y responsable, y datos de cierre.

### Tests for User Story 4 ⚠️ (escribir primero, deben fallar)

- [X] T076 [P] [US4] Tests unitarios de la máquina de estados en `backend/tests/Riesgos.Core.Tests/Dominio/ReporteEstadosTests.cs`, cubriendo cada fila de la tabla de transiciones de [data-model.md](data-model.md): Abierto ⇒ En Progreso registra fecha y admin; En Progreso ⇒ Cerrado con ambos textos registra fecha y admin; Abierto ⇒ Cerrado, En Progreso ⇒ Abierto, desde Cerrado y al mismo estado ⇒ conflicto; cerrar sin acción realizada o sin observación (o solo espacios) ⇒ validación
- [X] T077 [P] [US4] Tests de cambio de estado en `backend/tests/Riesgos.Api.Tests/US4/EstadoTests.cs`: AC-15, AC-34 (409 y sigue Abierto), AC-33 (409 y sigue Cerrado), AC-25 (400 y sigue En Progreso), AC-35 y AC-18 (Cerrado; textos, fecha, hora y usuario de cierre en el detalle), cierre sin acciones correctivas registradas ⇒ OK (FR-029), un reporte "Pendiente de clasificación" pasa a En Progreso y a Cerrado igual que uno clasificado (FR-027), cambiar el estado o registrar una acción no encola ninguna clasificación (FR-017), AC-16 (empleado ⇒ 403), dos cambios simultáneos ⇒ uno OK y el otro 409 (R7)
- [X] T078 [P] [US4] Tests de acciones correctivas en `backend/tests/Riesgos.Api.Tests/US4/AccionesTests.cs`: AC-24 (se guarda y aparece en el detalle), AC-54 (fecha del registro y admin responsable), AC-55 (reporte Cerrado ⇒ 409), en Abierto ⇒ OK (FR-028), descripción vacía o > 1.000 ⇒ 400, AC-37 (empleado ⇒ 403), el empleado creador ve las acciones en su detalle (FR-011)

### Implementation for User Story 4

- [X] T079 [US4] Implementar `Reporte.PasarAEnProgreso(adminId, ahora)` y `Reporte.Cerrar(adminId, accionRealizada, observacion, ahora)` con validación de transiciones y largos (1.000) en `backend/src/Riesgos.Core/Dominio/Reporte.cs` (hace pasar T076)
- [X] T080 [P] [US4] Crear entidad `AccionCorrectiva` (`ReporteId`, `Descripcion` string(1000), `Fecha`, `ResponsableId`) en `backend/src/Riesgos.Core/Dominio/AccionCorrectiva.cs`, su configuración y la migración `AccionesCorrectivas`
- [X] T081 [US4] Implementar `ServicioAdministracion.CambiarEstadoAsync` y `RegistrarAccionAsync` (rechaza reportes Cerrados; incrementa `RowVersion`) en `backend/src/Riesgos.Core/Servicios/ServicioAdministracion.cs`
- [X] T082 [US4] Implementar `POST /api/admin/reportes/{id}/estado` y `POST /api/admin/reportes/{id}/acciones` en `backend/src/Riesgos.Api/Endpoints/AdminEndpoints.cs`, e incluir inicio de progreso, cierre y acciones (ordenadas por fecha asc.) en el detalle de ambos roles en `backend/src/Riesgos.Api/Dtos/ReporteDetalleMapper.cs` (hace pasar T077–T078)
- [X] T083 [US4] Agregar al detalle, solo para admin, los botones "Pasar a En Progreso" y "Cerrar" (formulario con acción realizada y observación) y el formulario de nueva acción; mostrar a ambos roles las acciones (o "Sin acciones registradas", FR-033) y los datos de cierre; ante 409 avisar y recargar, en `frontend/src/components/GestionReporte.jsx` y `frontend/src/pages/DetalleReporte.jsx`
- [X] T084 [P] [US4] Tests de frontend de la gestión (botones visibles solo en estados válidos, validación del cierre) en `frontend/src/__tests__/US4.test.jsx`

**Checkpoint**: US1–US4 funcionan; el ciclo completo Abierto ⇒ Cerrado queda trazado.

### Orden por criticidad (FR-024, RF-02e)

- [X] T085 [P] [US3] Tests en `backend/tests/Riesgos.Api.Tests/US3/ListadoAdminTests.cs`: AC-70 `orden=criticidad` ⇒ Crítica, Alta, Media, Baja, Pendiente y fecha desc. dentro de cada nivel; AC-71 `orden` desconocido ⇒ 400; sin `orden` ⇒ por fecha
- [X] T086 [US3] Agregar el parámetro `orden` (`fecha` | `criticidad`) a `ServicioAdministracion.ListarAsync` (orden `RangoCriticidad, FechaCreacion DESC, Id DESC`) y a `GET /api/admin/reportes` en `backend/src/Riesgos.Core/Servicios/ServicioAdministracion.cs` y `backend/src/Riesgos.Api/Endpoints/AdminEndpoints.cs`
- [X] T087 [US3] Agregar el selector de orden al listado en `frontend/src/pages/ListadoAdmin.jsx`

---

## Phase 7: User Story 5 - Empleado corrige su reporte y adjunta evidencia (Priority: P3)

**Goal**: mientras el reporte sigue Abierto, el creador edita descripción y ubicación (se reclasifica) y adjunta hasta 5 fotos.

**Independent Test**: crear un reporte, editarlo y adjuntar imágenes; verificar reclasificación, límites de imágenes y bloqueo cuando pasa a En Progreso.

### Tests for User Story 5 ⚠️ (escribir primero, deben fallar)

- [X] T088 [P] [US5] Tests de edición en `backend/tests/Riesgos.Api.Tests/US5/EditarReporteTests.cs`: AC-10 (guarda ambos valores), AC-61 (campo vacío ⇒ 400 y no cambia), AC-69 (descripción > 2.000 ⇒ 400, no cambia, sin llamada al clasificador), la edición responde con clasificación `Pendiente` sin esperar, AC-52 (tras procesar, queda la nueva clasificación), AC-57 (el asunto del borrador refleja la nueva categoría y criticidad), AC-59 (falla ⇒ edición guardada y `Pendiente`), AC-62 (asunto con "Pendiente de clasificación"), dos ediciones seguidas con la cola sin procesar ⇒ solo se guarda el resultado de la última (FR-017), AC-11 (En Progreso o Cerrado ⇒ 409 y no cambia), AC-36 (otro empleado ⇒ 403), el admin edita un reporte propio Abierto (FR-009a)
- [X] T089 [P] [US5] Tests unitarios de validación de imágenes en `backend/tests/Riesgos.Core.Tests/Imagenes/ValidadorImagenTests.cs`: firma JPEG `FF D8 FF` y PNG `89 50 4E 47` con extensión válida ⇒ OK; `.txt` renombrado a `.jpg` ⇒ inválido; > 2.097.152 bytes ⇒ inválido; extensión no permitida ⇒ inválido
- [X] T090 [P] [US5] Tests de imágenes en `backend/tests/Riesgos.Api.Tests/US5/ImagenesTests.cs`: AC-19 (sexta ⇒ 400, siguen 5), AC-20 (formato inválido ⇒ 400), AC-21 (> 2 MB ⇒ 400), AC-22 (el detalle lista las imágenes del reporte), AC-53 (En Progreso o Cerrado ⇒ 409, cantidad igual), más de un archivo en la misma request ⇒ 400 (FR-013), dos subidas simultáneas con 4 cargadas ⇒ el reporte queda con 5 (R6), FR-013a y AC-73 (descarga: creador y admin ⇒ 200 con `Content-Type`; otro empleado ⇒ 403; sin sesión ⇒ 401; imagen de otro reporte ⇒ 404)
- [X] T091 [P] [US5] Completar AC-23 en `backend/tests/Riesgos.Api.Tests/US2/ClasificacionTests.cs`: reporte con imágenes adjuntas, al reclasificar la entrada registrada por el `ClasificadorSimulado` no contiene contenido de imagen

### Implementation for User Story 5

- [X] T092 [US5] Implementar `ServicioReportes.EditarAsync` (solo el creador, solo en Abierto, valida antes de todo, guarda, pone `Pendiente`, incrementa `VersionClasificacion`, regenera el borrador con "Pendiente de clasificación" y encola) en `backend/src/Riesgos.Core/Servicios/ServicioReportes.cs`
- [X] T093 [US5] Implementar `PUT /api/reportes/{id}` en `backend/src/Riesgos.Api/Endpoints/ReportesEndpoints.cs` (hace pasar T088)
- [X] T094 [P] [US5] Crear entidad `ImagenAdjunta` (`ReporteId`, `NombreArchivo` string(64), `TipoContenido`, `TamanoBytes`, `FechaCarga`) en `backend/src/Riesgos.Core/Dominio/ImagenAdjunta.cs`, su configuración y la migración `ImagenesAdjuntas`
- [X] T095 [P] [US5] Implementar `ValidadorImagen` (firma de bytes + extensión + tamaño) en `backend/src/Riesgos.Core/Imagenes/ValidadorImagen.cs` (hace pasar T089)
- [X] T096 [US5] Implementar `ServicioImagenes.AdjuntarAsync` (creador, estado Abierto, conteo < 5 e inserción en una transacción, guarda en `data/imagenes/{guid}.{ext}` configurable) y `ObtenerAsync` (verificación de acceso igual que el reporte) en `backend/src/Riesgos.Core/Servicios/ServicioImagenes.cs`
- [X] T097 [US5] Implementar `POST /api/reportes/{id}/imagenes` (multipart, un solo archivo `archivo`, `RequestSizeLimit` acorde a 2 MB) y `GET /api/reportes/{id}/imagenes/{imagenId}` en `backend/src/Riesgos.Api/Endpoints/ImagenesEndpoints.cs`, e incluir `imagenes` con `url` en el detalle (hace pasar T090–T091)
- [X] T098 [US5] Agregar al detalle, para el creador y solo en Abierto, el formulario de edición y la subida de una imagen por vez (muestra cuántas quedan de 5); mostrar a creador y admin la galería con las imágenes (o "Sin imágenes", FR-033), en `frontend/src/components/EditarReporte.jsx`, `frontend/src/components/Imagenes.jsx` y `frontend/src/pages/DetalleReporte.jsx`
- [X] T099 [P] [US5] Tests de frontend de edición e imágenes (oculto fuera de Abierto, errores de validación de archivo) en `frontend/src/__tests__/US5.test.jsx`

**Checkpoint**: US1–US5 funcionan; la edición reclasifica y las imágenes respetan límites y acceso.

---

## Phase 8: User Story 6 - Gestión de la cuenta y la sesión (Priority: P3)

**Goal**: el usuario cambia su contraseña verificando la actual; la sesión vence a las 24 h de inactividad; puede cerrar sesión.

**Independent Test**: cambiar la contraseña y verificar que solo la nueva funciona; avanzar el reloj 24 h y verificar que pide autenticación de nuevo.

### Tests for User Story 6 ⚠️ (escribir primero, deben fallar)

- [X] T100 [P] [US6] Tests de cambio de contraseña en `backend/tests/Riesgos.Api.Tests/US6/PasswordTests.cs`: AC-14 (solo la nueva permite login), AC-32 (actual incorrecta ⇒ 400 y sigue la anterior), AC-66 (nueva < 8 ⇒ 400 y sigue la anterior), las demás sesiones del usuario quedan invalidadas y la actual sigue válida (R4)
- [X] T101 [P] [US6] Tests de vencimiento en `backend/tests/Riesgos.Api.Tests/US6/VencimientoSesionTests.cs`: AC-48 (con `FakeTimeProvider`, > 24 h sin actividad ⇒ 401), actividad a las 23 h renueva el plazo (FR-005)
- [X] T102 [P] [US6] Test de hash en `backend/tests/Riesgos.Api.Tests/US6/HashTests.cs`: AC-47 (el valor guardado es distinto de la contraseña y tiene formato BCrypt `$2`)

### Implementation for User Story 6

- [X] T103 [US6] Implementar `ServicioCuentas.CambiarPasswordAsync` (verifica la actual, valida la nueva, hashea, borra las demás sesiones) en `backend/src/Riesgos.Core/Servicios/ServicioCuentas.cs`
- [X] T104 [US6] Implementar `PUT /api/auth/password` en `backend/src/Riesgos.Api/Endpoints/AuthEndpoints.cs` (hace pasar T100–T102)
- [X] T105 [P] [US6] Crear la página "Cambiar contraseña" en `frontend/src/pages/CambiarPassword.jsx`
- [X] T106 [P] [US6] Test de frontend del cambio de contraseña en `frontend/src/__tests__/US6.test.jsx`

### Cerrar sesión (FR-005a, RF-42)

- [X] T107 [P] [US6] Test en `backend/tests/Riesgos.Api.Tests/US6/LogoutTests.cs`: AC-72, tras `POST /api/auth/logout` (204), repetir un request con la misma cookie ⇒ 401; sin sesión ⇒ 401
- [X] T108 [US6] Implementar `POST /api/auth/logout` (borra la sesión y expira la cookie) en `backend/src/Riesgos.Api/Endpoints/AuthEndpoints.cs`
- [X] T109 [US6] Agregar el botón "Cerrar sesión" al layout y `logout` al contexto en `frontend/src/App.jsx` y `frontend/src/auth/AuthContext.jsx`
- [X] T110 [P] [US6] Test de frontend del cierre de sesión en `frontend/src/__tests__/US6.test.jsx`

**Checkpoint**: todas las user stories funcionan de forma independiente.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: medición contra el modelo real, seguridad, documentación y validación final.

- [X] T111 [P] Implementar `Riesgos.Evaluacion`: lee `datasets/validacion-v{N}.csv` (columnas descripcion, ubicacion, tipo, categoria, criticidad), clasifica cada caso con `ClasificadorClaude`, reporta aciertos por campo (los `Pendiente` cuentan como error) y p95 de latencia, en `backend/tools/Riesgos.Evaluacion/Program.cs` (SC-002, SC-003, AC-05, AC-45; fuera de `dotnet test`)
- [X] T112 [P] Crear `datasets/README.md` con el formato esperado del dataset, la regla de versionado y que debe correrse T111 al cambiar modelo, prompt o `kb.md`
- [X] T113 [P] Revisar los logs: que la clasificación, los logins fallidos y los accesos denegados se registren sin texto de reportes, contraseñas, tokens ni API key (R13), en `backend/src/Riesgos.Api/` y `backend/src/Riesgos.Core/`
- [X] T114 [P] Verificar que cada AC del PRD (AC-01…AC-75, salvo AC-05/AC-45, que valida T111, y AC-46, que valida T117) tiene al menos un test con su `Trait`; agregar los que falten en `backend/tests/`
- [X] T115 [P] Actualizar `AGENTS.md` con cómo correr la herramienta de evaluación y el procedimiento operativo para reemplazar al único administrador (FR-006) (las variables de entorno ya están documentadas)
- [ ] T116 Ejecutar la validación completa de [quickstart.md](quickstart.md): `dotnet test`, `npm test`, recorrido manual y `scripts/verificar-secretos.sh` (AC-46)
- [X] T117 [P] Crear `scripts/verificar-secretos.sh` (AC-46): falla si algún archivo versionado contiene el valor de `API_KEY` o `ADMIN_PASSWORD` (cuando están definidas), un archivo `.env` versionado o un patrón de clave de Anthropic (`sk-ant-`); documentar su uso en [quickstart.md](quickstart.md)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias. T003 depende de T001.
- **Foundational (Phase 2)**: depende de Setup. Bloquea todas las user stories.
- **US1 (Phase 3)**: depende de Foundational.
- **US2 (Phase 4)**: depende de Foundational; usa la entidad `Reporte` y `ServicioReportes.CrearAsync` de US1 (T037–T040).
- **US3 (Phase 5)**: depende de US1 (reportes) y, para el borrador y la acción sugerida con valores reales, de US2.
- **US4 (Phase 6)**: depende de US1; el detalle admin de US3 (T070) para mostrar la gestión.
- **US5 (Phase 7)**: depende de US1 y US2 (reclasificación); T092 usa el borrador de US3 (T066).
- **US6 (Phase 8)**: depende solo de Foundational (sesiones) y de US1 (registro) para los tests.
- **T085–T087 y T107–T110**: dependían de T006 (enmienda del PRD), ya completada.
- **Polish (Phase 9)**: depende de las user stories que se quieran entregar.

### User Story Dependencies

```text
Foundational ──▶ US1 ──▶ US2 ──▶ US3 ──▶ US4
                  │       └─────▶ US5 (también usa US3 para el borrador)
                  └─────────────▶ US6
T006 (PRD) ──▶ T085–T087 (orden por criticidad), T107–T110 (cerrar sesión)
```

### Within Each User Story

- Los tests se escriben primero y deben fallar.
- Entidades ⇒ servicios ⇒ endpoints ⇒ frontend.
- Las migraciones de EF Core se crean en orden (comparten el snapshot): T015 ⇒ T038 ⇒ T065 ⇒ T080 ⇒ T094; nunca en paralelo.

### Parallel Opportunities

- Setup: T002, T004, T005, T006 en paralelo tras T001.
- Foundational: T007–T009 (tests), T010–T013, T016, T022, T023, T025–T029, T031, T032 en paralelo.
- Dentro de cada story, todos los tests [P] juntos; luego entidades y componentes de frontend [P].
- Con más de una persona: tras US1, US6 puede avanzar en paralelo con US2; tras US2, US3 y US5 (salvo T092) en paralelo.

---

## Parallel Example: User Story 1

```bash
# Tests de US1 (escribir primero):
Task: "Tests de registro en backend/tests/Riesgos.Api.Tests/US1/RegistroTests.cs"
Task: "Tests de creación en backend/tests/Riesgos.Api.Tests/US1/CrearReporteTests.cs"
Task: "Tests de mis reportes en backend/tests/Riesgos.Api.Tests/US1/MisReportesTests.cs"
Task: "Tests de detalle en backend/tests/Riesgos.Api.Tests/US1/DetalleReporteTests.cs"

# Luego, en paralelo:
Task: "Entidad Reporte en backend/src/Riesgos.Core/Dominio/Reporte.cs"
Task: "DTOs en backend/src/Riesgos.Api/Dtos/"
Task: "Páginas Registro, NuevoReporte y MisReportes en frontend/src/pages/"
```

## Parallel Example: User Story 2

```bash
Task: "Tests del procesador en backend/tests/Riesgos.Core.Tests/Clasificacion/ProcesadorClasificacionTests.cs"
Task: "Tests del mapeo en backend/tests/Riesgos.Core.Tests/Clasificacion/MapeadorRespuestaTests.cs"
Task: "Tests de integración en backend/tests/Riesgos.Api.Tests/US2/ClasificacionTests.cs"
Task: "kb.md en backend/src/Riesgos.Clasificacion/kb.md"
Task: "MapeadorRespuesta en backend/src/Riesgos.Clasificacion/MapeadorRespuesta.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup (T006 puede ir en paralelo; no bloquea el MVP).
2. Phase 2: Foundational.
3. Phase 3: US1.
4. **Validar**: registrar, crear y listar reportes; acceso ajeno denegado.

El MVP útil para el negocio es **US1 + US2** (ambas P1): sin clasificación, el sistema es solo un
buzón.

### Incremental Delivery

1. Setup + Foundational ⇒ base lista.
2. US1 ⇒ reportes registrados (MVP técnico).
3. US2 ⇒ clasificación automática (MVP de negocio).
4. US3 ⇒ el administrador ve y prioriza.
5. US4 ⇒ ciclo completo hasta el cierre.
6. US5 ⇒ edición e imágenes.
7. US6 ⇒ gestión de cuenta.
8. Tras T006: orden por criticidad y cerrar sesión.
9. Polish: medición con el dataset cuando exista.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Cada test referencia su AC o FR con `Trait`; ningún test llama a Claude API real.
- Hacer commit por tarea o grupo lógico, con Conventional Commits.
- Revisar en cada PR: sin llamadas al modelo fuera de `Riesgos.Clasificacion`, test de acceso en cada endpoint nuevo y sin secretos en el diff (constitución).
