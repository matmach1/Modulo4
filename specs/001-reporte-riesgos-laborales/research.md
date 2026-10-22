# Research: Sistema de Reporte y Prevención de Riesgos Laborales

**Feature**: `001-reporte-riesgos-laborales` | **Fecha**: 2026-10-21 | **Plan**: [plan.md](plan.md)

Decisiones técnicas que resuelven los puntos abiertos del Technical Context y los ítems de los
checklists que la spec deja para el plan. Cada decisión indica qué requisito o ítem cubre.

---

## R1. Clasificación en segundo plano (FR-014, FR-017, FR-018)

- **Decision**: cola en memoria (`System.Threading.Channels.Channel<TrabajoClasificacion>`)
  consumida por un `BackgroundService` del mismo proceso. Crear o editar un reporte guarda el
  reporte con tipo, categoría y criticidad en `Pendiente`, incrementa `VersionClasificacion` y
  encola `{ReporteId, Version}`. El procesador lee el texto del reporte, llama al clasificador con
  un timeout de 10 s y guarda el resultado **solo si** `VersionClasificacion` sigue siendo la
  encolada (actualización condicional: `UPDATE … WHERE Id = @id AND VersionClasificacion = @v`).
- **Rationale**: cumple "responder sin esperar" con el mínimo de piezas. El control de versión
  resuelve "una clasificación de una versión anterior se descarta" sin locks. Si la app se
  reinicia, la cola se pierde y el reporte queda `Pendiente`, que es justo lo que pide la spec
  (sin reintentos).
- **Alternatives considered**: Hangfire/Quartz con cola persistente (agrega reintentos y
  persistencia que el PRD excluye); `Task.Run` sin cola (sin control de concurrencia ni forma
  limpia de esperar en tests).
- **Testabilidad**: la cola se expone detrás de `IColaClasificacion`. En tests de integración se
  reemplaza por una implementación que acumula trabajos y los procesa cuando el test llama a
  `ProcesarPendientesAsync()`, así los AC del PRD que miran la clasificación (AC-40, AC-52, AC-56,
  AC-57, AC-58, AC-59, AC-62) son deterministas.

## R2. Integración con Claude API (Principio I, FR-014, FR-015)

- **Decision**: SDK oficial de C# (`dotnet add package Anthropic`), aislado en el proyecto
  `Riesgos.Clasificacion`, que implementa `IClasificadorReportes` (definida en `Riesgos.Core`).
  Una única llamada `client.Messages.Create` por clasificación con:
  - `System` = instrucciones fijas + contenido de `kb.md`, marcado con `CacheControlEphemeral`
    para que la base de conocimiento quede en caché entre llamadas (solo cachea si el prefijo
    supera el mínimo del modelo; si `kb.md` es corto, simplemente no se cachea).
  - Mensaje de usuario con **solo texto**: descripción, ubicación y categoría sugerida (si hay).
    Nunca imágenes ni datos del usuario (ni email ni nombre).
  - Salida estructurada con `OutputConfig.Format = JsonOutputFormat` y un JSON Schema con `enum`
    cerrados para `tipo`, `categoria` y `criticidad` (ver [contracts/clasificador.md](contracts/clasificador.md)).
  - `MaxTokens` 2048 (incluye el razonamiento adaptativo del modelo), sin streaming.
- **Rationale**: el Principio I exige un módulo dedicado detrás de una interfaz; la salida
  estructurada con enums elimina el parseo frágil y reduce las respuestas inválidas. Igual se
  valida la respuesta contra las listas (FR-018: valor inválido = falla).
- **Alternatives considered**: tool use forzado (`tool_choice` forzado devuelve 400 en los modelos
  actuales); parsear texto libre (frágil); HTTP crudo (la skill de Claude API indica usar el SDK
  oficial cuando existe).

## R3. Modelo, timeout y reintentos (FR-018, SC-003)

- **Decision**:
  - Modelo `claude-opus-5-5` por defecto, configurable con la variable de entorno `CLAUDE_MODEL`
    sin tocar código. Effort `low` (`OutputConfig.Effort`), adecuado para clasificación.
  - API key leída de `API_KEY` (RNF-04) y pasada explícitamente al cliente
    (`ApiKey = Environment.GetEnvironmentVariable("API_KEY")`), porque el SDK por defecto lee
    `ANTHROPIC_API_KEY`.
  - Reintentos del SDK en 0 y timeout de 10 s aplicado con un `CancellationTokenSource` propio
    alrededor de la llamada (el timeout del SDK multiplicado por reintentos podría superar 10 s).
  - Errores tipados del SDK, timeout, `stop_reason` distinto de `end_turn` (por ejemplo
    `refusal` o `max_tokens`), JSON inválido o valores fuera de lista ⇒ resultado `Falla`; el
    procesador deja el reporte en `Pendiente` y registra el motivo en el log.
- **Rationale**: "sin reintento" en el PRD y en la spec; los 10 s son un límite duro. Mantener el
  modelo configurable permite medir AC-45 (p95 < 3 s) con el dataset y, si no se cumple, que el
  equipo decida cambiar de modelo sin un cambio de código.
- **Riesgo abierto**: AC-45 exige p95 < 3 s sobre la llamada. Con un modelo Opus el tiempo puede
  quedar cerca del límite; hay que medirlo con el dataset (R9) antes de dar el AC por cumplido.
  Cambiar a un modelo más rápido es una decisión del equipo, no de este plan.
- **Alternatives considered**: reintentos del SDK (prohibidos por PRD); timeout solo del SDK (no
  garantiza el tope total).

## R4. Autenticación y sesión (FR-003, FR-005, FR-005a, RNF-05, RNF-06)

- **Decision**: sesiones del lado del servidor guardadas en SQLite (tabla `Sesion`). Al iniciar
  sesión se genera un token aleatorio de 256 bits; en la base se guarda solo su hash SHA-256. El
  token viaja en una cookie `HttpOnly`, `Secure`, `SameSite=Strict`. Un handler de autenticación
  propio (`AuthenticationHandler`) valida la cookie, rechaza la sesión si
  `UltimaActividad + 24 h < ahora` y renueva `UltimaActividad` en cada request autenticado
  (escritura como máximo una vez por minuto para no escribir en cada request).
  - **Cerrar sesión** borra la fila ⇒ la sesión deja de valer de inmediato (FR-005a).
  - **Cambiar contraseña** borra todas las demás sesiones del usuario y mantiene la actual
    (security CHK009).
  - Contraseñas con `BCrypt.Net-Next` (work factor 12) (RNF-05, AC-47).
  - Login fallido responde siempre lo mismo para email inexistente y contraseña incorrecta
    (security CHK001).
  - Email normalizado (trim + minúsculas) antes de guardar y buscar; índice único sobre el email
    normalizado (security CHK003, AC-63).
- **Rationale**: JWT no permite invalidar en el servidor sin una lista de revocación, que es
  equivalente a guardar sesiones. Cookie `HttpOnly` evita que el token quede expuesto a XSS;
  `SameSite=Strict` más API solo-JSON cubre CSRF para una SPA en el mismo origen.
- **Alternatives considered**: ASP.NET Core Identity (trae mucho más de lo necesario: roles
  configurables, lockout, 2FA); JWT con lista de revocación.
- **Fuera de alcance, explícito**: bloqueo por intentos fallidos y limitación de tasa de login
  (no están en el PRD; se registra como riesgo aceptado, security CHK002).

## R5. Autorización (Principio III, FR-011, FR-013a, FR-030)

- **Decision**: dos políticas (`Empleado`, `Administrador`) basadas en el claim de rol, más
  verificación de propiedad en el servicio de reportes (`EsCreadorOAdmin`). Un reporte inexistente
  responde 404; uno ajeno, 403 (AC-04). La misma verificación se aplica a la descarga de imágenes
  (FR-013a). El DTO de detalle se arma según el rol: para el empleado, el borrador de mail y la
  acción sugerida **no se incluyen en la respuesta** (no solo se ocultan en el frontend).
- **Rationale**: el Principio III exige control en el backend y test de acceso por endpoint.
- **Alternatives considered**: filtros genéricos por recurso (más abstracción de la que justifica
  un único recurso protegido).

## R6. Imágenes adjuntas (FR-013, FR-013a)

- **Decision**: una imagen por request (`multipart/form-data`, campo `archivo`). Se valida:
  tamaño ≤ 2 MB (límite también en Kestrel/`RequestSizeLimit`), **contenido real** por firma de
  bytes (JPEG `FF D8 FF`, PNG `89 50 4E 47`) además de la extensión `.jpg/.jpeg/.png`, cantidad
  < 5 dentro de una transacción y estado Abierto. Se guarda en disco en `data/imagenes/` con un
  nombre generado (GUID); el nombre original no se usa en rutas (security CHK019, CHK021). Se
  sirven solo por el endpoint autenticado, nunca como archivos estáticos.
- **Concurrencia del límite de 5**: el conteo y la inserción ocurren en una transacción
  serializada (SQLite serializa escrituras), así dos subidas simultáneas no pueden dejar 6.
- **Rationale**: validar por firma evita que un archivo arbitrario pase renombrado.
- **Alternatives considered**: guardar en la base como BLOB (agranda SQLite y complica backups);
  quitar metadatos EXIF (deseable, queda como mejora; no lo exige el PRD).

## R7. Concurrencia y transiciones de estado (FR-027, workflow CHK017, CHK018)

- **Decision**: token de concurrencia optimista (`RowVersion` como entero incrementado por la
  app, configurado como `IsConcurrencyToken`) en `Reporte`. Si dos operaciones chocan (por
  ejemplo, el creador edita mientras el admin pasa a En Progreso), la segunda recibe 409 y el
  cliente vuelve a leer. Las transiciones se validan en una máquina de estados del dominio
  (`Reporte.CambiarEstado`), con tests unitarios test-first.
- **Rationale**: SQLite no tiene `rowversion` nativo; un entero manejado por EF Core cubre el caso
  sin locks.

## R8. Fechas, textos y validaciones (workflow CHK013–CHK015)

- **Decision**:
  - Fechas en UTC en la base (`DateTimeOffset`), mostradas en la hora local del navegador.
  - Textos se recortan (trim) antes de validar; un texto solo con espacios cuenta como vacío.
  - Largos máximos: descripción 2.000, ubicación 200 (PRD), acción correctiva 1.000, acción
    realizada 1.000, observación de cierre 1.000 (decisión de plan; no los fija el PRD).
  - Orden estable del listado: `FechaCreacion DESC, Id DESC` (y para criticidad,
    `RangoCriticidad, FechaCreacion DESC, Id DESC`).
  - Errores de validación con formato `ProblemDetails` (RFC 9457).

## R9. Medición de precisión y latencia (SC-002, SC-003, AC-05, AC-45)

- **Decision**: herramienta de consola aparte, `backend/tools/Riesgos.Evaluacion`, que lee el dataset
  versionado (`datasets/validacion-v{N}.csv`), llama al clasificador real y reporta aciertos por
  campo y p95 de latencia. **No** forma parte de `dotnet test` (la constitución prohíbe llamar a
  Claude API real en tests). Los casos que terminan en `Pendiente` cuentan como error
  (ai-classification CHK026). Se corre antes de cada release y cada vez que cambian el modelo, el
  prompt o `kb.md` (CHK028). No alcanzar los mínimos bloquea la salida a producción (SC-002);
  hasta entonces solo se permite un piloto.
- **Dependencia abierta**: el dataset todavía no existe (Seguridad e Higiene).

## R10. Base de conocimiento `kb.md`

- **Decision**: `backend/src/Riesgos.Clasificacion/kb.md`, versionado en el repo y copiado al
  output como contenido. Define tipos, categorías y criterios de criticidad (ai-classification
  CHK003, CHK004). Se carga una vez al inicio; cambiarlo implica volver a correr R9.
- **Dependencia abierta**: el contenido lo valida Seguridad e Higiene; el plan solo crea un
  borrador inicial basado en las definiciones del PRD.

## R11. Frontend

- **Decision**: React 18 + Vite 5, `react-router-dom` para rutas, `fetch` nativo con
  `credentials: 'include'`, sin librería de estado global. Tests con Vitest + Testing Library. En
  desarrollo, el proxy de Vite (`/api` → backend) mantiene el mismo origen para la cookie. El
  detalle de un reporte "Pendiente de clasificación" recién creado se refresca con un polling
  corto (cada 2 s, hasta 15 s) para mostrar la clasificación cuando llega.
- **Rationale**: stack fijado por la constitución; mismo origen evita CORS con credenciales.

## R12. Administrador inicial (FR-006, security CHK018)

- **Decision**: al iniciar, si no existe ningún administrador y están `ADMIN_EMAIL` y
  `ADMIN_PASSWORD`, se crea. Si ya existe, no se modifica (la contraseña del entorno no pisa la
  actual). Si no hay administrador y faltan las variables, la app no arranca y lo informa en el
  log. Si falta `API_KEY`, la app arranca y los reportes quedan `Pendiente` (se registra una
  advertencia al inicio) (ai-classification CHK014).

## R13. Observabilidad mínima (ai-classification CHK032, security CHK023, CHK028)

- **Decision**: logging estructurado de `Microsoft.Extensions.Logging` (consola). Se registran:
  resultado de cada clasificación (éxito/falla, motivo, latencia, sin el texto del reporte),
  logins fallidos y accesos denegados (sin contraseñas, tokens ni API key). Sin métricas ni
  trazas distribuidas en v1.
