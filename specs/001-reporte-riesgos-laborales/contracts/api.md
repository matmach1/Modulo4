# API Contract: Sistema de Reporte y Prevención de Riesgos Laborales

**Base**: `/api` · JSON (`application/json`) salvo la subida de imágenes · Autenticación por
cookie de sesión `riesgos_sesion` (`HttpOnly`, `Secure`, `SameSite=Strict`).

Errores con `ProblemDetails` (RFC 9457). Códigos comunes:

| Código | Cuándo |
|--------|--------|
| 400 | Datos inválidos (validación, AC-01, AC-19–21, AC-25, AC-32, AC-64–69). |
| 401 | Sin sesión, sesión cerrada o vencida (AC-12, AC-48). |
| 403 | Autenticado sin permiso: rol o reporte ajeno (AC-03, AC-04, AC-16, AC-36–38, AC-42). |
| 404 | Reporte o imagen inexistente. |
| 409 | Conflicto de estado, email duplicado o concurrencia (AC-11, AC-31, AC-33, AC-34, AC-53, AC-55). |

Roles: **E** = empleado, **A** = administrador, **C** = creador del reporte (empleado o admin).

## Cuenta y sesión

| Método y ruta | Acceso | Request | Respuesta |
|---------------|--------|---------|-----------|
| `POST /api/auth/registro` | Público | `{ email, password }` | 201 `{ id, email, rol: "Empleado" }` · 400 · 409 |
| `POST /api/auth/login` | Público | `{ email, password }` | 200 `{ id, email, rol }` + cookie · 401 (mismo mensaje para email o contraseña incorrectos) |
| `POST /api/auth/logout` | E, A | — | 204; la sesión queda invalidada (FR-005a) |
| `GET /api/auth/yo` | E, A | — | 200 `{ id, email, rol }` · 401 |
| `PUT /api/auth/password` | E, A | `{ passwordActual, passwordNueva }` | 204 (cierra las demás sesiones) · 400 |

## Reportes

| Método y ruta | Acceso | Request | Respuesta |
|---------------|--------|---------|-----------|
| `POST /api/reportes` | E, A | `{ descripcion, ubicacion, categoriaSugerida? }` | 201 `ReporteDetalle` con clasificación `Pendiente` (no espera la IA) · 400 |
| `GET /api/reportes/mios` | E, A | `?page=1&size=20` | 200 `Pagina<ReporteResumen>` de los reportes propios, por fecha desc. |
| `GET /api/reportes/{id}` | C, A | — | 200 `ReporteDetalle` (según rol) · 403 · 404 |
| `PUT /api/reportes/{id}` | C | `{ descripcion, ubicacion }` | 200 `ReporteDetalle` con clasificación `Pendiente` · 400 · 403 · 409 |
| `POST /api/reportes/{id}/imagenes` | C | `multipart/form-data`, campo `archivo` (una imagen) | 201 `Imagen` · 400 · 403 · 409 |
| `GET /api/reportes/{id}/imagenes/{imagenId}` | C, A | — | 200 bytes con `Content-Type` de la imagen · 403 · 404 |

## Administración

| Método y ruta | Acceso | Request | Respuesta |
|---------------|--------|---------|-----------|
| `GET /api/admin/reportes` | A | `?page=1&size=20&orden=fecha\|criticidad` | 200 `Pagina<ReporteResumen>` · 400 (size ≤0 o >100, orden desconocido) |
| `GET /api/admin/panel` | A | — | 200 `{ criticosAbiertos, pendientesAbiertos }` (solo estado Abierto) |
| `POST /api/admin/reportes/{id}/estado` | A | `{ estado: "EnProgreso" }` o `{ estado: "Cerrado", accionRealizada, observacionCierre }` | 200 `ReporteDetalle` · 400 · 404 · 409 |
| `POST /api/admin/reportes/{id}/acciones` | A | `{ descripcion }` | 201 `Accion` · 400 · 404 · 409 |

Los endpoints `/api/admin/*` responden 403 a un empleado (FR-030).

## Esquemas

```text
ReporteResumen {
  id, descripcion (recortada a 140 caracteres), ubicacion, estado,
  tipo, categoria, criticidad, fechaCreacion, creadorEmail
}

ReporteDetalle {
  id, descripcion, ubicacion, estado,
  tipo, categoria, criticidad,          // "Pendiente" = Pendiente de clasificación
  categoriaSugerida?,                    // ver reglas por rol
  fechaCreacion, creadorEmail,
  inicioProgreso?: { fecha, usuarioEmail },
  cierre?: { fecha, usuarioEmail, accionRealizada, observacionCierre },
  acciones: Accion[],
  imagenes: Imagen[],
  // solo administrador:
  accionSugerida?: "Detener actividad" | "Requiere intervención inmediata" | null,
  borradorMail?: { asunto, cuerpo }
}

Accion  { id, descripcion, fecha, responsableEmail }
Imagen  { id, tipoContenido, tamanoBytes, fechaCarga, url }   // url = /api/reportes/{id}/imagenes/{imagenId}
Pagina<T> { items: T[], page, size, total }
```

Reglas por rol en `ReporteDetalle`:
- **Empleado**: no incluye `accionSugerida` ni `borradorMail` (las propiedades no existen en la
  respuesta; FR-011, AC-39). Incluye `categoriaSugerida` si la indicó.
- **Administrador**: incluye todo; `categoriaSugerida` y `categoria` permiten ver ambas cuando
  difieren (FR-025, AC-43).

Valores en JSON: los enums se serializan con sus nombres (`"EnProgreso"`, `"UsoEpp"`, `"Critica"`,
`"Pendiente"`…); el frontend los traduce a los textos de la spec.

## Trazabilidad a AC del PRD

| Endpoint | AC |
|----------|----|
| `POST /auth/registro` | AC-13, AC-31, AC-63, AC-64, AC-65, AC-47 |
| `POST /auth/login`, sesión | AC-13, AC-14, AC-48 |
| `PUT /auth/password` | AC-14, AC-32, AC-66 |
| `POST /reportes` | AC-01, AC-06, AC-07, AC-27, AC-28, AC-56, AC-58, AC-67, AC-68 |
| `GET /reportes/mios` | AC-12, AC-29 |
| `GET /reportes/{id}` | AC-04, AC-08, AC-09, AC-17, AC-18, AC-22, AC-30, AC-35, AC-39, AC-40, AC-41, AC-43, AC-50, AC-51 |
| `PUT /reportes/{id}` | AC-10, AC-11, AC-36, AC-52, AC-57, AC-59, AC-61, AC-62, AC-69 |
| `POST /reportes/{id}/imagenes` | AC-19, AC-20, AC-21, AC-23, AC-53 |
| `GET /admin/reportes` | AC-02, AC-03, AC-42, AC-44, AC-49 |
| `GET /admin/panel` | AC-26, AC-38, AC-60 |
| `POST /admin/reportes/{id}/estado` | AC-15, AC-16, AC-25, AC-33, AC-34, AC-35 |
| `POST /admin/reportes/{id}/acciones` | AC-24, AC-37, AC-54, AC-55 |

Sin AC en el PRD (agregados en la clarificación, pendientes de actualizar el PRD): `logout`,
creación por el administrador, `orden=criticidad`, `GET /imagenes/{imagenId}` y la visibilidad
del detalle para el empleado.
