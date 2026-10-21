# Data Model: Sistema de Reporte y Prevención de Riesgos Laborales

**Feature**: `001-reporte-riesgos-laborales` | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

Persistencia en SQLite vía EF Core. Fechas en UTC (`DateTimeOffset`). Los textos se recortan
antes de validar; un texto solo con espacios cuenta como vacío.

## Enumeraciones

| Enum | Valores | Fuente |
|------|---------|--------|
| `Rol` | `Empleado`, `Administrador` | FR-008 |
| `EstadoReporte` | `Abierto`, `EnProgreso`, `Cerrado` | FR-027 |
| `TipoReporte` | `Pendiente`, `ActoInseguro`, `CondicionInsegura`, `CasiAccidente`, `RiesgoOperativo`, `Otro` | RF-25, FR-018 |
| `Categoria` | `Pendiente`, `ProblemaInfraestructura`, `RiesgoElectrico`, `UsoEpp`, `Otro` | RF-04, FR-018 |
| `Criticidad` | `Pendiente`, `Baja`, `Media`, `Alta`, `Critica` | RF-07, FR-018 |

`Pendiente` representa "Pendiente de clasificación". Se guardan como texto en la base. La
categoría sugerida usa `Categoria` sin `Pendiente`.

Orden por criticidad (FR-024): `Critica`=0, `Alta`=1, `Media`=2, `Baja`=3, `Pendiente`=4.

## Usuario

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `Email` | string(254) | Obligatorio, formato válido (FR-002), guardado normalizado (trim + minúsculas). **Índice único** (AC-31, AC-63). |
| `PasswordHash` | string | BCrypt, work factor 12. Nunca texto plano (FR-007, AC-47). |
| `Rol` | `Rol` | Registro ⇒ siempre `Empleado` (FR-001). `Administrador` solo por seed (FR-006). |
| `FechaCreacion` | DateTimeOffset | |

Reglas: contraseña ≥ 8 caracteres en registro y cambio (FR-002, FR-004); máximo 72 bytes
(límite de BCrypt) — se rechaza más larga como dato inválido.

## Sesion

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `UsuarioId` | FK → Usuario | Cascade delete. |
| `TokenHash` | string(64) | SHA-256 del token de la cookie. **Índice único**. |
| `FechaCreacion` | DateTimeOffset | |
| `UltimaActividad` | DateTimeOffset | Válida mientras `UltimaActividad + 24 h ≥ ahora` (FR-005). Se renueva con cada request (máx. una escritura por minuto). |

Ciclo de vida: se crea en el login; se borra al cerrar sesión (FR-005a); se borran las demás
sesiones del usuario al cambiar la contraseña; se descarta al vencer.

## Reporte

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `Descripcion` | string(2000) | Obligatoria, ≤ 2.000 (FR-009, RF-41). |
| `Ubicacion` | string(200) | Obligatoria, ≤ 200 (FR-009, RF-41). |
| `CategoriaSugerida` | `Categoria?` | Opcional, uno de los 4 valores válidos. Solo se fija al crear (el PRD excluye editarla). |
| `Tipo` | `TipoReporte` | Inicia en `Pendiente`. |
| `Categoria` | `Categoria` | Inicia en `Pendiente`. Es la categoría definitiva (FR-016). |
| `Criticidad` | `Criticidad` | Inicia en `Pendiente`. |
| `VersionClasificacion` | int | Se incrementa en cada creación/edición. El resultado de una clasificación solo se guarda si coincide (FR-017). |
| `Estado` | `EstadoReporte` | Inicia en `Abierto`. |
| `CreadorId` | FK → Usuario | Empleado o administrador (FR-009a). |
| `FechaCreacion` | DateTimeOffset | (FR-010) |
| `FechaInicioProgreso` | DateTimeOffset? | Al pasar a En Progreso. |
| `UsuarioInicioProgresoId` | FK → Usuario? | Al pasar a En Progreso. |
| `AccionRealizada` | string(1000)? | Obligatoria al cerrar (FR-029). |
| `ObservacionCierre` | string(1000)? | Obligatoria al cerrar (FR-029). |
| `FechaCierre` | DateTimeOffset? | (FR-029, RF-18) |
| `UsuarioCierreId` | FK → Usuario? | (FR-029, RF-18) |
| `RowVersion` | int | Token de concurrencia optimista. |

Índices: `(CreadorId, FechaCreacion)`, `(FechaCreacion, Id)`, `(Estado, Criticidad)` para el panel.

Valores derivados (no se guardan):
- **Ambas categorías visibles** (FR-016, FR-025): cuando `CategoriaSugerida` no es nula y difiere
  de `Categoria`.
- **Acción sugerida** (FR-020): `Critica` ⇒ "Detener actividad"; `Alta` ⇒ "Requiere intervención
  inmediata"; otro ⇒ ninguna. Solo en la respuesta para el administrador.

### Transiciones de estado (FR-027)

```text
Abierto ──(admin)──▶ EnProgreso ──(admin, con AccionRealizada + ObservacionCierre)──▶ Cerrado
```

| Desde | Hacia | Resultado |
|-------|-------|-----------|
| Abierto | EnProgreso | OK; registra fecha y administrador. |
| EnProgreso | Cerrado | OK si ambos textos no vacíos (si falta alguno: 400, AC-25); registra fecha y administrador. |
| Abierto | Cerrado | 409 (AC-34). |
| EnProgreso | Abierto | 409. |
| Cerrado | cualquiera | 409 (AC-33). |
| X | X (mismo estado) | 409. |

Operaciones según estado:

| Operación | Abierto | En Progreso | Cerrado |
|-----------|---------|-------------|---------|
| Editar descripción/ubicación (creador) | Sí | 409 | 409 |
| Adjuntar imagen (creador) | Sí | 409 | 409 |
| Registrar acción correctiva (admin) | Sí | Sí | 409 |

### Ciclo de la clasificación

```text
crear/editar ─▶ Tipo/Categoria/Criticidad = Pendiente, VersionClasificacion++ ─▶ encolar
procesar ─▶ éxito y versión vigente ─▶ guardar valores + regenerar borrador
         ─▶ falla/timeout/inválido     ─▶ queda Pendiente + regenerar borrador
         ─▶ versión vieja              ─▶ descartar
```

## ImagenAdjunta

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `ReporteId` | FK → Reporte | Máximo 5 por reporte (FR-013). |
| `NombreArchivo` | string(64) | GUID + extensión; ruta en `data/imagenes/`. |
| `TipoContenido` | string | `image/jpeg` o `image/png`, según la firma de bytes. |
| `TamanoBytes` | int | ≤ 2.097.152 (2 MB). |
| `FechaCarga` | DateTimeOffset | |

## AccionCorrectiva

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `ReporteId` | FK → Reporte | Reporte no Cerrado (FR-028). |
| `Descripcion` | string(1000) | Obligatoria. |
| `Fecha` | DateTimeOffset | Asignada automáticamente (AC-54). |
| `ResponsableId` | FK → Usuario | El administrador que la registra (AC-54). |

No se editan ni se eliminan. Se listan por `Fecha` ascendente en el detalle.

## EventoNotificacion

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Id` | int, PK | |
| `ReporteId` | FK → Reporte | **Único**: un evento por reporte (FR-021, AC-07). |
| `FechaCreacion` | DateTimeOffset | Creación del reporte. |
| `AsuntoBorrador` | string | `"[{Categoría}] Criticidad {Criticidad} - Reporte #{Id}"`; con pendiente: `"[Pendiente de clasificación] Reporte #{Id}"`. |
| `CuerpoBorrador` | string | Descripción, ubicación, fecha de creación y email del creador (FR-021). |
| `FechaActualizacion` | DateTimeOffset | Cada regeneración. |

El borrador se **reemplaza** (no se guarda historial) cada vez que el procesador guarda un
resultado o una falla de clasificación, y al editar (la clasificación pasa a `Pendiente`)
(FR-022).

## Relaciones

```text
Usuario 1──* Sesion
Usuario 1──* Reporte (creador)
Reporte 1──* ImagenAdjunta
Reporte 1──* AccionCorrectiva *──1 Usuario (responsable)
Reporte 1──1 EventoNotificacion
```
