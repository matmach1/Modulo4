# Feature Specification: Sistema de Reporte y Prevención de Riesgos Laborales

**Feature Branch**: `001-reporte-riesgos-laborales`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "PRD.md" (PRD-001: Sistema Inteligente de Reporte y Prevención de Riesgos Laborales)

## Clarifications

### Session 2026-10-21

- Q: Si un empleado sube varias imágenes juntas y con eso pasaría el límite de 5 por reporte, ¿qué debe hacer el sistema? → A: Se permite subir solo una imagen por operación; el caso de lote no puede ocurrir.
- Q: ¿Qué ve el empleado en el detalle de su propio reporte, además de la descripción, la ubicación, el estado y las fechas? → A: Ve la clasificación (tipo, categoría, criticidad), las acciones correctivas, los datos de cierre y las imágenes; no ve la acción preventiva sugerida ni el borrador de mail.
- Q: ¿Qué tamaño de página por defecto y qué tamaño máximo debe aceptar el listado global de reportes del administrador? → A: Por defecto 20, máximo 100; un tamaño ≤0 o >100 se rechaza como dato inválido.
- Q: ¿El administrador también puede crear reportes de riesgo, o crear reportes es solo para empleados? → A: Sí; el administrador puede crear reportes, que aparecen en el listado global, y mientras estén Abiertos los edita con las mismas reglas que cualquier creador.
- Q: ¿El usuario debe poder cerrar sesión explícitamente, con el efecto de que su sesión deje de servir de inmediato en el servidor? → A: Sí; "Cerrar sesión" invalida la sesión en el servidor y usarla después devuelve no autenticado.
- Q: Cuando un empleado crea o edita un reporte, ¿la respuesta debe esperar a que termine la clasificación (hasta 10 s) o el reporte se guarda al instante y se clasifica después? → A: No espera: el reporte se guarda al instante como "Pendiente de clasificación" y la clasificación se completa en segundo plano.
- Q: En el panel del administrador, ¿los contadores de "Críticos abiertos" y "pendientes de clasificación abiertos" deben contar solo los reportes en estado Abierto, o también los que están En Progreso? → A: Solo los reportes en estado Abierto; los En Progreso no cuentan.
- Q: El listado global del administrador, ¿debe seguir ordenado solo por fecha (como pide el PRD) o tiene que priorizar por criticidad, como sugiere el título de US3? → A: Orden por fecha por defecto, con opción de ordenar por criticidad; requiere actualizar el PRD.
- Q: ¿Quién puede ver o descargar las imágenes adjuntas de un reporte? → A: Las mismas reglas que el reporte: el creador y el administrador; otro empleado recibe acceso denegado y alguien sin sesión, no autenticado.
- Q: Para cerrar un reporte, ¿alcanza con escribir la "acción realizada" y la observación de cierre, o además tiene que haber al menos una acción correctiva registrada? → A: Alcanza con la acción realizada y la observación; las acciones correctivas son opcionales y sirven para registrar pasos intermedios.
- Q: Rechazar un registro por email duplicado revela qué emails tienen cuenta, ¿se acepta ese riesgo? → A: Sí; es una app interna con emails corporativos que no son secretos.
- Q: Además del vencimiento por 24 h sin actividad, ¿la sesión debe tener una duración máxima absoluta? → A: No; solo vence por 24 h de inactividad, como dice el PRD.
- Q: El aviso de que el contenido de los reportes se envía a un servicio externo de IA, ¿es un requisito de la app o queda fuera? → A: Queda fuera de la app: lo comunica la empresa por sus canales internos.
- Q: ¿La app debe exigir HTTPS o eso queda a cargo de la infraestructura? → A: La app lo exige: redirige HTTP a HTTPS y envía HSTS (en desarrollo vale `localhost`).
- Q: Un empleado podría redactar la descripción para manipular la criticidad que asigna la IA, ¿cómo se trata? → A: Se mitiga en las instrucciones del clasificador (la descripción es solo dato, nunca instrucciones) y se acepta el riesgo residual; el administrador ve todos los reportes.
- Q: SC-004 habla del 100% de los escenarios de control de acceso del PRD sin enumerarlos, ¿se listan en la spec? → A: Sí; se enumeran los AC de acceso del PRD y los escenarios de imágenes y cierre de sesión.
- Q: ¿Las listas cerradas de tipo, categoría y criticidad se agregan a FR-014? → A: Sí, indicando que no se amplían.
- Q: Un reporte "Pendiente de clasificación", ¿puede pasar a En Progreso y Cerrado igual que uno clasificado? → A: Sí; la clasificación no condiciona los cambios de estado ni las acciones correctivas.
- Q: Cuando el administrador cambia el estado o registra una acción correctiva, ¿se vuelve a clasificar? → A: No; solo se clasifica al crear y al editar.
- Q: Si un reporte Crítico se edita y la reclasificación falla, deja de contar como Crítico en el panel, ¿se acepta? → A: Sí; queda pendiente y pasa al contador de pendientes, como pide el PRD (RF-37).
- Q: Si la IA no alcanza los mínimos de precisión con el dataset de validación, ¿bloquea la salida a producción? → A: Sí; hasta tener el dataset y alcanzar los mínimos, solo se permite un piloto.
- Q: ¿Cuántos administradores puede haber? → A: Uno solo, el creado por la configuración del entorno; reemplazarlo es un procedimiento operativo fuera de la app.
- Q: ¿El listado global del administrador tiene filtros o búsqueda en v1? → A: No; quedan excluidos explícitamente del alcance.
- Q: ¿Se agrega a la spec qué se muestra cuando no hay datos? → A: Sí; los listados vacíos muestran un mensaje, los contadores muestran 0 y el detalle indica que no hay acciones ni imágenes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Empleado reporta un riesgo y sigue su estado (Priority: P1)

Un empleado se registra con email y contraseña, inicia sesión y reporta una situación peligrosa
indicando descripción y ubicación (y, si quiere, una categoría sugerida). Después puede ver la
lista de los reportes que él mismo creó, con su estado, y consultar el detalle de cada uno.

**Why this priority**: es el núcleo del problema: hoy los empleados no saben dónde reportar y las
observaciones se pierden. Sin esto no hay base unificada de reportes.

**Independent Test**: registrar un usuario, crear un reporte y verificar que aparece en su lista
en estado Abierto con fecha, hora y usuario de creación.

**Acceptance Scenarios**:

1. **Given** un email no registrado y una contraseña de al menos 8 caracteres, **When** el usuario
   se registra, **Then** la cuenta se crea con rol empleado y puede iniciar sesión con esas
   credenciales (AC-13, AC-42).
2. **Given** un email ya registrado, **When** alguien intenta registrarse con ese email, **Then**
   el sistema lo rechaza por duplicado y no crea una segunda cuenta (AC-31, AC-63).
3. **Given** un email con formato inválido o una contraseña de menos de 8 caracteres, **When** se
   intenta el registro, **Then** el sistema lo rechaza y no crea la cuenta (AC-64, AC-65).
4. **Given** un empleado autenticado, **When** crea un reporte con descripción y ubicación no
   vacías, **Then** el reporte se crea en estado Abierto (AC-27).
5. **Given** una descripción o ubicación vacía, o que supera 2.000 / 200 caracteres
   respectivamente, **When** se intenta crear el reporte, **Then** el sistema lo rechaza y no lo
   crea (AC-01, AC-28, AC-67, AC-68).
6. **Given** reportes creados por los empleados A y B, **When** A lista sus reportes, **Then**
   recibe únicamente los suyos (AC-29).
7. **Given** un reporte del empleado A, **When** A consulta su detalle, **Then** ve sus datos,
   incluyendo fecha, hora y usuario de creación (AC-17, AC-50), además de tipo, categoría y
   criticidad, las acciones correctivas, los datos de cierre y las imágenes, pero no la acción
   preventiva sugerida ni el borrador de mail.
8. **Given** un reporte del empleado A, **When** el empleado B intenta verlo, **Then** el sistema
   deniega el acceso (AC-04).
9. **Given** un usuario no autenticado, **When** intenta ver reportes, **Then** el sistema
   deniega el acceso y no muestra datos (AC-12).

---

### User Story 2 - Clasificación automática de cada reporte (Priority: P1)

Al crearse un reporte, el sistema lo guarda al instante como "Pendiente de clasificación" y lo
clasifica en segundo plano por tipo, categoría y criticidad usando IA y una base de conocimiento de
seguridad. Si el empleado sugirió una categoría distinta, se conservan ambas pero manda la
automática. Si la IA falla o tarda más de 10 s, el reporte queda como "Pendiente de
clasificación".

**Why this priority**: la priorización por criticidad es lo que permite atender primero los
riesgos más graves; es el diferencial del sistema frente a un buzón de reportes.

**Independent Test**: con un clasificador simulado, crear un reporte y verificar que queda con
tipo, categoría y criticidad válidos; simular un error y verificar que queda "Pendiente de
clasificación".

**Acceptance Scenarios**:

1. **Given** un reporte cuya clasificación terminó con éxito, **When** se consulta su detalle,
   **Then** su tipo es uno de
   Acto inseguro, Condición insegura, Casi accidente, Riesgo operativo u Otro (AC-40), su
   categoría es una de Problema de infraestructura, Riesgo eléctrico, Uso de elementos de
   protección personal u Otro, y su criticidad es una de Baja, Media, Alta o Crítica.
2. **Given** una categoría sugerida que difiere de la automática, **When** se crea el reporte,
   **Then** la categoría definitiva es la automática y ambas quedan almacenadas (AC-06).
3. **Given** que el servicio de clasificación devuelve error o no responde en 10 s, **When** un
   empleado crea un reporte válido, **Then** el reporte se crea igual y queda con tipo, categoría
   y criticidad "Pendiente de clasificación" (AC-56, AC-58).
4. **Given** un servicio de clasificación que todavía no respondió, **When** un empleado crea un
   reporte válido, **Then** la creación responde de inmediato con el reporte en "Pendiente de
   clasificación", sin esperar el resultado de la clasificación.
5. **Given** un reporte con imágenes adjuntas, **When** se clasifica, **Then** la solicitud al
   servicio de clasificación no contiene imágenes (AC-23).
6. **Given** el dataset de validación versionado, **When** se clasifica el conjunto completo,
   **Then** los aciertos son ≥ 90% en categoría, ≥ 90% en tipo y ≥ 85% en criticidad (AC-05).

---

### User Story 3 - Administrador ve los reportes priorizados (Priority: P2)

El administrador de Seguridad e Higiene ve todos los reportes en un listado paginado, ordenado del
más reciente al más antiguo o, si lo elige, por criticidad, con su estado y su criticidad. En su panel principal ve cuántos reportes Críticos
están en estado Abierto y cuántos en estado Abierto están pendientes de clasificación. En el detalle de cada
reporte ve la acción preventiva sugerida, las categorías sugerida y automática si difieren, y un
borrador de mail listo para copiar.

**Why this priority**: sin la vista del administrador los reportes quedan registrados pero nadie
los atiende; depende de que existan reportes (US1) y su clasificación (US2).

**Independent Test**: con reportes de distintos usuarios y criticidades ya cargados, verificar
listado, contadores del panel y detalle con acción sugerida y borrador de mail.

**Acceptance Scenarios**:

1. **Given** reportes existentes y parámetros de página y tamaño, **When** el administrador los
   lista, **Then** recibe como máximo ese tamaño de reportes de la página pedida, cada uno con su
   estado y criticidad, ordenados del más reciente al más antiguo (AC-02, AC-49).
10. **Given** reportes con distintas criticidades, **When** el administrador pide el listado
    ordenado por criticidad, **Then** recibe primero los Críticos, luego Alta, Media, Baja y al
    final "Pendiente de clasificación", y dentro de cada nivel del más reciente al más antiguo.
2. **Given** un empleado autenticado, **When** intenta acceder al listado global o al panel,
   **Then** el sistema deniega el acceso (AC-03, AC-38).
3. **Given** N reportes Críticos en estado Abierto y M reportes Abiertos pendientes de
   clasificación, **When** el administrador ve el panel, **Then** se muestran N y M (AC-26,
   AC-60); los reportes Críticos o pendientes que están En Progreso o Cerrados no se cuentan.
4. **Given** un reporte con criticidad Crítica, **When** el administrador lo visualiza, **Then** se muestra
   "Detener actividad"; con Alta, "Requiere intervención inmediata"; con Baja, Media o
   "Pendiente de clasificación", ninguna recomendación (AC-08, AC-09, AC-30).
5. **Given** un reporte recién creado cuya clasificación ya terminó, **When** el administrador
   consulta su detalle, **Then** ve
   un borrador de mail cuyo asunto contiene categoría y criticidad y cuyo cuerpo contiene
   descripción, ubicación, fecha de creación y usuario creador (AC-41).
6. **Given** un empleado que consulta un reporte propio, **When** se muestra el detalle, **Then**
   no incluye el borrador de mail (AC-39).
7. **Given** una categoría sugerida distinta de la automática, **When** el administrador consulta
   el detalle, **Then** ve ambas (AC-43).
8. **Given** cualquier reporte, **When** el administrador consulta su detalle, **Then** lo ve
   (AC-51).
9. **Given** un reporte recién creado, **When** termina su creación, **Then** existe un evento de
   notificación asociado (AC-07).

---

### User Story 4 - Administrador gestiona el reporte hasta su cierre (Priority: P2)

El administrador pasa un reporte de Abierto a En Progreso, registra acciones correctivas o
preventivas y finalmente lo cierra indicando la acción realizada y una observación de cierre,
quedando registrado quién y cuándo lo cerró.

**Why this priority**: cierra el ciclo y da trazabilidad completa; requiere US3 para encontrar los
reportes.

**Independent Test**: tomar un reporte Abierto, pasarlo a En Progreso, registrar una acción y
cerrarlo; verificar estados, acción con fecha y responsable, y datos de cierre.

**Acceptance Scenarios**:

1. **Given** un reporte Abierto, **When** el administrador lo pasa a En Progreso, **Then** el
   nuevo estado queda registrado (AC-15).
2. **Given** un reporte Abierto, **When** el administrador intenta cerrarlo directamente,
   **Then** el sistema lo rechaza por conflicto de estado y sigue Abierto (AC-34).
3. **Given** un reporte Cerrado, **When** el administrador intenta reabrirlo o pasarlo a En
   Progreso, **Then** el sistema lo rechaza y sigue Cerrado (AC-33).
4. **Given** un administrador, **When** registra una acción correctiva, **Then** la acción queda
   almacenada con la fecha del registro y ese administrador como responsable (AC-24, AC-54).
5. **Given** un reporte Cerrado, **When** se intenta registrar una acción correctiva, **Then** el
   sistema lo rechaza y no registra la acción (AC-55).
6. **Given** un reporte En Progreso, **When** el administrador intenta cerrarlo sin acción
   realizada o sin observación, **Then** el sistema lo rechaza y sigue En Progreso (AC-25).
7. **Given** un reporte En Progreso, con o sin acciones correctivas registradas, **When** el
   administrador lo cierra con ambos textos, **Then** queda Cerrado, ambos textos se muestran en el detalle junto con fecha, hora y usuario
   de cierre (AC-35, AC-18).
8. **Given** un empleado, **When** intenta cambiar el estado o registrar una acción, **Then** el
   sistema deniega el acceso (AC-16, AC-37).
9. **Given** una base de datos vacía y credenciales de administrador provistas por configuración
   del entorno, **When** se inicia la aplicación, **Then** es posible autenticarse como
   administrador y acceder al listado global (AC-44).

---

### User Story 5 - Empleado corrige su reporte y adjunta evidencia (Priority: P3)

Mientras el reporte sigue Abierto, el empleado puede editar la descripción y la ubicación (lo que
dispara una nueva clasificación) y adjuntar hasta 5 fotos como evidencia para el administrador.

**Why this priority**: mejora la calidad del reporte, pero el flujo principal funciona sin ello.

**Independent Test**: crear un reporte, editarlo y adjuntar imágenes; verificar reclasificación,
límites de imágenes y bloqueo una vez que el reporte pasa a En Progreso.

**Acceptance Scenarios**:

1. **Given** un reporte Abierto propio, **When** el empleado modifica descripción y ubicación,
   **Then** se guardan ambos valores (AC-10).
2. **Given** un reporte Abierto propio, **When** lo edita dejando un campo vacío o con una
   descripción de más de 2.000 caracteres, **Then** el sistema lo rechaza, el reporte no cambia y
   no se invoca la clasificación (AC-61, AC-69).
3. **Given** un reporte Abierto, **When** el creador lo edita, **Then** tipo, categoría y
   criticidad se recalculan y el borrador de mail refleja los nuevos valores (AC-52, AC-57).
4. **Given** un reporte Abierto ya clasificado y un servicio de clasificación que falla, **When**
   el creador edita la descripción, **Then** la edición se guarda, la clasificación queda
   "Pendiente de clasificación" y el asunto del borrador lo refleja (AC-59, AC-62).
5. **Given** un reporte En Progreso o Cerrado, **When** el creador intenta editarlo o adjuntarle
   una imagen, **Then** el sistema lo rechaza por conflicto de estado y el reporte no cambia
   (AC-11, AC-53).
6. **Given** un reporte Abierto del empleado A, **When** el empleado B intenta editarlo, **Then**
   el sistema deniega el acceso (AC-36).
7. **Given** un reporte con 5 imágenes, un archivo que no es JPG/JPEG/PNG o un archivo de más de
   2 MB, **When** se intenta adjuntar, **Then** el sistema lo rechaza y no se asocia el archivo
   (AC-19, AC-20, AC-21).
8. **Given** un reporte con imágenes, **When** se consulta su detalle, **Then** las imágenes se
   muestran asociadas a ese reporte (AC-22).
9. **Given** una imagen adjunta a un reporte del empleado A, **When** el empleado B intenta verla
   o descargarla, **Then** el sistema deniega el acceso; **When** lo intenta alguien sin sesión,
   **Then** el sistema lo trata como no autenticado; **When** lo intentan A o el administrador,
   **Then** la obtienen.

---

### User Story 6 - Gestión de la cuenta y la sesión (Priority: P3)

Un usuario autenticado puede cambiar su contraseña verificando la actual y cerrar sesión. La
sesión expira tras 24 h de inactividad.

**Why this priority**: necesario para seguridad básica, pero no bloquea el flujo de reportes.

**Independent Test**: cambiar la contraseña y verificar que solo la nueva funciona; simular 24 h
de inactividad y verificar que se pide autenticación de nuevo.

**Acceptance Scenarios**:

1. **Given** un usuario autenticado, **When** cambia su contraseña indicando bien la actual,
   **Then** solo la nueva permite iniciar sesión (AC-14).
2. **Given** una contraseña actual incorrecta o una nueva de menos de 8 caracteres, **When** se
   intenta el cambio, **Then** el sistema lo rechaza y sigue valiendo la anterior (AC-32, AC-66).
3. **Given** una sesión sin actividad por más de 24 h, **When** el usuario intenta acceder a
   reportes, **Then** el sistema exige autenticarse nuevamente (AC-48).
4. **Given** un usuario registrado, **When** se inspecciona su registro almacenado, **Then** la
   contraseña no está en texto plano sino como hash seguro (AC-47).
5. **Given** un usuario autenticado, **When** cierra sesión y luego intenta acceder a reportes con
   esa misma sesión, **Then** el sistema lo trata como no autenticado y deniega el acceso.

---

### Edge Cases

- El servicio de clasificación devuelve un valor fuera de las listas válidas de tipo, categoría o
  criticidad: se trata como falla y el reporte queda "Pendiente de clasificación".
- Una descripción redactada para manipular la clasificación (por ejemplo, con instrucciones al
  modelo): el clasificador trata el texto del reporte solo como dato a clasificar; si aun así la
  criticidad resulta incorrecta, se acepta el riesgo, porque el administrador ve todos los
  reportes en el listado.
- El servicio de clasificación falla al reclasificar tras una edición: la edición se guarda igual
  y la clasificación pasa a "Pendiente de clasificación" (no conserva la anterior). Si era
  Crítico, deja de contar en el contador de Críticos y pasa al de pendientes; se acepta ese
  comportamiento (RF-37).
- Registros simultáneos con el mismo email: exactamente uno tiene éxito.
- Un empleado intenta adjuntar más de una imagen en una sola operación: se rechaza la operación
  completa y no se asocia ningún archivo (cada operación admite exactamente una imagen).
- Página solicitada fuera de rango en el listado: se devuelve una página vacía.
- Tamaño de página ≤0 o mayor a 100 en el listado: se rechaza como dato inválido; si no se indica
  tamaño, se usan 20.
- Criterio de orden desconocido en el listado (distinto de fecha o criticidad): se rechaza como
  dato inválido.
- Reportes "Pendiente de clasificación" no se reintentan automáticamente; quedan visibles en el
  contador del panel. Mientras una clasificación en segundo plano está en curso, el reporte
  también figura como "Pendiente de clasificación" y cuenta en ese contador.
- Si el creador edita el reporte mientras sigue en curso la clasificación de la versión anterior,
  el resultado de esa clasificación anterior se descarta; solo se guarda el de la última versión.
- Si la aplicación se reinicia con clasificaciones en curso, esos reportes quedan como "Pendiente
  de clasificación" (no se reintentan).
- Un reporte reclasificado que pasa a Crítica o Alta muestra la nueva acción sugerida en la
  siguiente consulta.

## Requirements *(mandatory)*

### Functional Requirements

**Cuentas y acceso**

- **FR-001**: El sistema MUST permitir el registro con email y contraseña, asignando siempre el
  rol empleado (RF-14, RF-29).
- **FR-002**: El sistema MUST rechazar registros con email de formato inválido, email ya
  registrado o contraseña de menos de 8 caracteres (RF-39, RF-40).
- **FR-003**: El sistema MUST requerir autenticación con usuario y contraseña para toda
  funcionalidad de reportes (RF-12).
- **FR-004**: El sistema MUST permitir cambiar la contraseña previa verificación de la actual,
  exigiendo al menos 8 caracteres en la nueva (RF-15, RF-40).
- **FR-005**: El sistema MUST expirar la sesión tras 24 h de inactividad (RNF-06); cada uso
  renueva el plazo. No hay duración máxima absoluta: una sesión usada a diario no vence.
- **FR-005a**: El usuario MUST poder cerrar sesión explícitamente; la sesión MUST quedar
  invalidada en el servidor de inmediato (RF-42).
- **FR-006**: El sistema MUST crear un administrador inicial con credenciales provistas por la
  configuración del entorno (RF-30). En v1 existe un único administrador: la app no permite dar de
  alta otros; reemplazarlo es un procedimiento operativo fuera de la app.
- **FR-007**: El sistema MUST almacenar las contraseñas con un hash seguro, nunca en texto plano
  (RNF-05).
- **FR-008**: El sistema MUST reconocer exactamente dos roles: empleado y administrador.

**Reportes**

- **FR-009**: Los usuarios autenticados MUST poder crear un reporte con descripción (máx. 2.000
  caracteres) y ubicación (máx. 200) obligatorias, y opcionalmente fotos y una categoría sugerida
  (RF-01, RF-41).
- **FR-010**: El sistema MUST registrar fecha, hora y usuario de creación de cada reporte (RF-17).
- **FR-011**: El empleado MUST poder listar únicamente sus propios reportes y ver el detalle solo
  de ellos (RF-03, RF-31). El detalle para el empleado MUST incluir tipo, categoría, criticidad,
  acciones correctivas, datos de cierre e imágenes, y MUST NOT incluir la acción preventiva
  sugerida ni el borrador de mail. La lista de reportes propios MUST estar paginada (mismos
  límites que FR-024) y ordenada por fecha de creación descendente; el administrador también tiene
  su lista de reportes propios.
- **FR-009a**: El administrador MUST poder crear reportes con las mismas reglas que un empleado;
  sus reportes aparecen en el listado global como cualquier otro.
- **FR-012**: El creador (empleado o administrador) MUST poder editar descripción y ubicación
  solo mientras el reporte esté Abierto (RF-10, RF-11).
- **FR-013**: El sistema MUST permitir adjuntar hasta 5 imágenes JPG, JPEG o PNG de hasta 2 MB
  cada una, de a una imagen por operación, solo mientras el reporte esté Abierto, y almacenarlas
  asociadas al reporte (RF-19, RF-20, RF-33).
- **FR-013a**: Ver o descargar una imagen adjunta MUST estar permitido solo a quien puede ver el
  reporte (su creador y el administrador); el resto MUST recibir acceso denegado, o no
  autenticado si no tiene sesión (RF-43).

**Clasificación**

- **FR-014**: El sistema MUST clasificar cada reporte en tipo, categoría y criticidad usando IA
  con la base de conocimiento de seguridad como contexto, en una única consulta (RF-04, RF-07,
  RF-25, RNF-01). La clasificación MUST ejecutarse en segundo plano: la creación MUST responder
  sin esperarla, con el reporte en "Pendiente de clasificación" hasta que llegue el resultado
  (RNF-08).
  Los valores válidos son listas cerradas que MUST NOT ampliarse: tipo ∈ {Acto inseguro, Condición
  insegura, Casi accidente, Riesgo operativo, Otro}; categoría ∈ {Problema de infraestructura,
  Riesgo eléctrico, Uso de elementos de protección personal, Otro}; criticidad ∈ {Baja, Media,
  Alta, Crítica}.
- **FR-015**: La clasificación MUST basarse solo en la información de texto; las imágenes MUST
  NOT enviarse al servicio de clasificación (RF-21).
- **FR-016**: Cuando la categoría sugerida difiera de la automática, el sistema MUST conservar la
  automática como definitiva y almacenar ambas (RF-05, RF-06).
- **FR-017**: El sistema MUST reclasificar el reporte en segundo plano cada vez que su creador lo
  edita (RF-32); la edición MUST responder sin esperar la reclasificación, con el reporte en
  "Pendiente de clasificación", y el resultado de una clasificación de una versión anterior MUST
  descartarse. Solo crear y editar disparan una clasificación; cambiar el estado o registrar
  acciones correctivas MUST NOT hacerlo.
- **FR-018**: Si la clasificación falla, no responde en 10 s o devuelve valores inválidos, el
  reporte (o la edición) MUST quedar guardado con tipo, categoría y criticidad "Pendiente de
  clasificación" (RF-35, RF-37).
- **FR-019**: El sistema MUST validar los datos de entrada antes de solicitar la clasificación; un
  reporte inválido MUST NOT generar una consulta de clasificación (AC-67, AC-69).
- **FR-020**: El sistema MUST sugerir al administrador "Detener actividad" para criticidad Crítica
  y "Requiere intervención inmediata" para Alta, y ninguna acción en los demás casos (RF-09).

**Notificación**

- **FR-021**: El sistema MUST registrar un evento de notificación al crear cada reporte y generar
  un borrador de mail con categoría y criticidad en el asunto, y descripción, ubicación, fecha de
  creación y usuario creador en el cuerpo (RF-08, RF-26).
- **FR-022**: El sistema MUST regenerar el borrador cada vez que cambian tipo, categoría o
  criticidad, incluido el paso a "Pendiente de clasificación" (RF-36).
- **FR-023**: El borrador MUST mostrarse solo al administrador en el detalle del reporte; el
  sistema MUST NOT enviar mails reales (RF-27).

**Administración**

- **FR-024**: El administrador MUST poder listar todos los reportes, paginados según página y
  tamaño pedidos (tamaño por defecto 20, máximo 100; un tamaño ≤0 o >100 MUST rechazarse como
  inválido), con su estado y criticidad. El orden por defecto MUST ser por fecha de creación
  descendente (RF-02a–RF-02d); el administrador MUST poder pedir, en cambio, orden por criticidad
  (Crítica, Alta, Media, Baja, "Pendiente de clasificación"), con fecha de creación descendente
  dentro de cada nivel (RF-02e).
- **FR-025**: El administrador MUST poder ver el detalle de cualquier reporte, incluidas ambas
  categorías cuando difieran (RF-28, RF-31).
- **FR-026**: El panel del administrador MUST mostrar la cantidad de reportes en estado Abierto
  con criticidad Crítica y la de reportes en estado Abierto "Pendiente de clasificación" (RF-24,
  RF-38). Los reportes En Progreso o Cerrados MUST NOT contarse.
- **FR-027**: El administrador MUST poder cambiar el estado solo según Abierto → En Progreso →
  Cerrado; cualquier otra transición MUST rechazarse (RF-16). La clasificación (incluido
  "Pendiente de clasificación") MUST NOT condicionar los cambios de estado ni el registro de
  acciones correctivas.
- **FR-028**: El administrador MUST poder registrar acciones correctivas o preventivas con
  descripción, asignando automáticamente fecha y responsable, excepto en reportes Cerrados (RF-22,
  RF-34). "Correctiva o preventiva" es un único concepto: no se distinguen tipos.
- **FR-029**: Al cerrar, el sistema MUST exigir acción realizada y observación de cierre, y
  registrar fecha, hora y usuario de cierre (RF-18, RF-23). El cierre MUST NOT exigir acciones
  correctivas registradas; estas son opcionales y registran pasos intermedios.
- **FR-030**: Las operaciones de administración (listado global, panel, cambio de estado, acciones,
  borrador de mail) MUST estar denegadas al rol empleado.

**Seguridad**

- **FR-031**: Ningún secreto (clave del servicio de clasificación, credenciales del administrador)
  MUST estar en el código o en archivos versionados; se toman de la configuración del entorno
  (RNF-04, AC-46).
- **FR-032**: Toda comunicación con la aplicación MUST ser por HTTPS: el sistema MUST redirigir
  HTTP a HTTPS y enviar la cabecera HSTS, salvo en el entorno de desarrollo local (RNF-07).

**Interfaz**

- **FR-033**: Cuando no hay datos, la interfaz MUST mostrar un estado vacío explícito: todo
  listado sin reportes muestra un mensaje en lugar de una tabla vacía, los contadores del panel
  muestran 0 (no se ocultan) y el detalle indica "Sin acciones registradas" y "Sin imágenes".

### Key Entities

- **Usuario**: persona que usa el sistema. Email (único), contraseña hasheada, rol (empleado o
  administrador).
- **Sesión**: sesión autenticada de un usuario, registrada en el servidor, con última actividad;
  deja de ser válida al cerrar sesión o tras 24 h sin actividad.
- **Reporte**: riesgo u observación reportada. Descripción, ubicación, categoría sugerida
  (opcional), tipo, categoría y criticidad automáticos (o "Pendiente de clasificación"), estado
  (Abierto, En Progreso, Cerrado), creador y fecha/hora de creación, y al cerrar: acción
  realizada, observación, usuario y fecha/hora de cierre.
- **Imagen adjunta**: evidencia visual de un reporte (hasta 5 por reporte), con formato y tamaño
  validados.
- **Acción correctiva/preventiva**: medida registrada por un administrador sobre un reporte, con
  descripción, fecha y responsable.
- **Evento de notificación**: registro de que se creó un reporte, con su borrador de mail
  (asunto y cuerpo) que se regenera al cambiar la clasificación.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los reportes válidos queda registrado, incluso cuando la clasificación
  automática falla o no responde.
- **SC-002**: La clasificación automática acierta al menos el 90% de las categorías, el 90% de los
  tipos y el 85% de las criticidades sobre el dataset de validación de Seguridad e Higiene.
  Alcanzar los tres mínimos es condición para salir a producción; mientras no exista el dataset o
  no se alcancen, el sistema solo puede usarse en un piloto.
- **SC-003**: En el 95% de los casos, la llamada al servicio de clasificación responde en menos de
  3 segundos, medido sobre el dataset de validación (RNF-01, AC-45). La respuesta de creación o
  edición no depende de ese tiempo, y entre 3 y 10 s el reporte igual queda clasificado.
- **SC-004**: Ningún empleado puede ver, editar ni gestionar reportes de otro usuario, ni acceder a
  funciones de administrador: se cumple el 100% de estos escenarios de control de acceso: AC-03,
  AC-04, AC-12, AC-16, AC-36, AC-37, AC-38, AC-39, AC-42 y AC-48 (US1, US3, US4, US5, US6), el
  acceso a imágenes (US5-9, FR-013a) y el cierre de sesión (US6-5, FR-005a).
- **SC-005**: Todo reporte Cerrado tiene registrado quién lo creó, qué acción se realizó, quién lo
  cerró y cuándo.
- **SC-006**: Un empleado puede crear un reporte completando solo dos campos obligatorios
  (descripción y ubicación).
- **SC-007**: El administrador conoce, desde el panel principal y sin buscar, cuántos reportes
  Críticos están en estado Abierto y cuántos en estado Abierto están pendientes de clasificación.

## Assumptions

- El servicio de clasificación es Claude API, con la base de conocimiento `kb.md` como contexto;
  su elección viene dada por el PRD y la constitución.
- Las respuestas concretas de error (datos inválidos, no autenticado, acceso denegado, conflicto de
  estado) siguen la correspondencia con códigos HTTP 400/401/403/409 definida en los AC del PRD.
- El dataset de validación (≥ 100 casos) lo provee Seguridad e Higiene y todavía no existe; SC-002
  no puede verificarse hasta recibirlo.
- Los usuarios tienen conexión estable y usan la aplicación desde navegador web.
- Fuera de alcance según el PRD: CRM, multi-canal real, envío real de mails, más de dos roles,
  cambio de rol desde la app, multi-tenant, corrección manual de clasificación, reapertura o
  eliminación de reportes, recuperación de contraseña, reintento de clasificación, exportes y
  estadísticas más allá de los contadores del panel, notificaciones al empleado. Tampoco hay
  filtros ni búsqueda (por estado, criticidad, fecha o texto) en ningún listado en v1.
- La empresa informa a los usuarios, por sus canales internos y fuera de la app, que el contenido
  de los reportes se envía a un servicio externo de IA; la app no muestra ese aviso.
- Se acepta que el rechazo de un registro por email duplicado (AC-31) revele qué emails tienen
  cuenta: es una aplicación interna con emails corporativos que no son secretos.
- El PRD fue enmendado (RF-02e, RF-42, RF-43, RNF-07, RNF-08 y AC-70 a AC-75) para incluir las
  funcionalidades que surgieron de la clarificación: orden por criticidad, cierre de sesión,
  acceso a imágenes, HTTPS y clasificación en segundo plano.
