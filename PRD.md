# PRD-001: Sistema Inteligente de Reporte y Prevención de Riesgos Laborales — App web para reportar riesgos laborales, clasificarlos con IA y gestionarlos hasta su cierre

## Contexto y Problema
En muchas empresas los empleados detectan situaciones peligrosas, pero no saben dónde reportarlas, no hay seguimiento, se pierden observaciones, no se priorizan los riesgos más críticos, no se tiene una base unificada y no se genera conocimiento para prevenir accidentes.

### Personas
El sistema reconoce exactamente dos roles:
- **Empleado (usuario común):** detecta un riesgo en su trabajo. Necesita reportarlo de forma simple (descripción, ubicación, fotos) y ver el estado de los reportes que él mismo creó.
- **Administrador (Seguridad e Higiene):** recibe todos los reportes. Necesita verlos priorizados por criticidad, saber cuántos críticos siguen abiertos, registrar acciones correctivas y cerrarlos dejando constancia de lo realizado.

Los permisos de cada rol quedan definidos por los RF que los referencian explícitamente y se verifican mediante los AC de control de acceso (AC-03, AC-04, AC-16, AC-36, AC-37, AC-38, AC-39, AC-42).

## Objetivos
Que cada reporte de un empleado, sea un riesgo laboral detectado o una observación, quede registrado en una base única, clasificado automáticamente por tipo y categoría, priorizado por criticidad y con trazabilidad completa (quién lo reportó, qué se hizo y quién lo cerró) hasta su cierre con una acción realizada.

La v1 no compromete métricas de éxito de producto (ver Fuera de Alcance).

## Requerimientos Funcionales
- RF-01: El sistema debe permitir que cualquier usuario autenticado cree un reporte indicando descripción y ubicación (obligatorias) y, opcionalmente, fotos adjuntas y una categoría sugerida.
- RF-02a: El sistema debe permitir que el administrador liste todos los reportes.
- RF-02b: El sistema debe paginar el listado de RF-02a según los parámetros `page` y `size` provistos por el cliente.
- RF-02c: El sistema debe mostrar el estado (Abierto, En Progreso, Cerrado) de cada reporte en el listado de RF-02a.
- RF-02d: El sistema debe ordenar el listado de RF-02a por fecha de creación descendente.
- RF-03: El sistema debe permitir que el usuario común liste únicamente los reportes que él mismo creó.
- RF-04: El sistema debe clasificar automáticamente cada reporte, al momento de crearse, en una de las siguientes categorías: Problema de infraestructura, Riesgo eléctrico, Uso de elementos de protección personal u Otro, utilizando Claude API con la base de conocimiento `kb.md` como contexto.
- RF-05: El sistema debe conservar la clasificación automática de categoría (RF-04) como definitiva cuando la categoría sugerida por el usuario (RF-01) difiera de ella.
- RF-06: El sistema debe registrar ambas categorías, la sugerida por el usuario y la asignada automáticamente, cuando difieran entre sí.
- RF-07: El sistema debe asignar automáticamente a cada reporte, al momento de crearse, una criticidad entre Baja, Media, Alta o Crítica, basándose en la descripción, ubicación y demás información de texto ingresada, utilizando Claude API con la base de conocimiento `kb.md` como contexto.
- RF-08: El sistema debe registrar un evento de notificación cada vez que se crea un nuevo reporte.
- RF-09: El sistema debe sugerir una acción preventiva predefinida según la criticidad del reporte: "Requiere intervención inmediata" para Alta y "Detener actividad" para Crítica. Para Baja, Media o "Pendiente de clasificación" (RF-35, RF-37) no se muestra ninguna acción sugerida.
- RF-10: El sistema debe permitir que el usuario creador edite la descripción y la ubicación de un reporte mientras este permanezca en estado Abierto.
- RF-11: El sistema debe impedir la modificación de un reporte por parte de su creador una vez que el administrador lo cambie a En Progreso o Cerrado.
- RF-12: El sistema debe requerir autenticación (usuario + contraseña) para acceder a cualquier funcionalidad relacionada con reportes.
- (RF-13 removido: la existencia de los dos roles de usuario se documenta en Contexto y Problema → Personas, no como requerimiento funcional individual.)
- RF-14: El sistema debe permitir que un usuario se registre con email + contraseña.
- RF-15: El sistema debe permitir que un usuario autenticado modifique su contraseña, previa verificación de su contraseña actual.
- RF-16: El sistema debe permitir que el administrador cambie el estado de un reporte únicamente según el flujo Abierto → En Progreso → Cerrado.
- RF-17: El sistema debe registrar fecha, hora y usuario de creación de cada reporte.
- RF-18: El sistema debe registrar fecha, hora y usuario de cierre de cada reporte.
- RF-19: El sistema debe permitir que el usuario adjunte hasta 5 imágenes en formato JPG, JPEG o PNG, con un tamaño máximo de 2 MB por archivo.
- RF-20: El sistema debe almacenar las imágenes adjuntas en el repositorio documental de la aplicación, asociadas al reporte correspondiente.
- RF-21: En la v1, el sistema no debe utilizar las imágenes como input del modelo de clasificación; deben servir únicamente como evidencia visual para los administradores.
- RF-22: El sistema debe permitir que el administrador registre una acción correctiva o preventiva asociada a un reporte, indicando descripción; la fecha y el usuario responsable del registro se asignan automáticamente.
- RF-23: Al cerrar un reporte, el sistema debe exigir dos textos libres: la acción realizada y una observación de cierre.
- RF-24: El sistema debe mostrarle al administrador, en el panel principal, la cantidad de reportes con criticidad Crítica que se encuentren en estado Abierto.
- RF-25: El sistema debe clasificar automáticamente cada reporte, al momento de crearse, en uno de los siguientes tipos: Acto inseguro, Condición insegura, Casi accidente, Riesgo operativo u Otro, utilizando Claude API con la base de conocimiento `kb.md` como contexto.
- RF-26: El sistema debe generar, por cada evento de notificación (RF-08), un borrador de mail cuyo asunto incluya la categoría y la criticidad del reporte, y cuyo cuerpo incluya descripción, ubicación, fecha de creación y usuario creador.
- RF-27: El sistema debe mostrar el borrador de mail (RF-26) en el detalle del reporte, únicamente al administrador.
- RF-28: El sistema debe mostrarle al administrador, en el detalle del reporte, la categoría sugerida y la automática cuando difieran (RF-06).
- RF-29: El sistema debe asignar el rol usuario común a toda cuenta creada mediante registro (RF-14).
- RF-30: El sistema debe disponer de un usuario administrador inicial creado mediante seed, con credenciales provistas por variables de entorno.
- RF-31: El sistema debe permitir consultar el detalle de un reporte: al usuario común, solo de los reportes que él mismo creó; al administrador, de cualquier reporte.
- RF-32: El sistema debe volver a calcular tipo (RF-25), categoría (RF-04) y criticidad (RF-07) de un reporte cada vez que su creador lo edita (RF-10).
- RF-33: El sistema debe permitir adjuntar imágenes (RF-19) a un reporte únicamente mientras este permanezca en estado Abierto.
- RF-34: El sistema debe impedir el registro de acciones correctivas o preventivas (RF-22) sobre un reporte en estado Cerrado.
- RF-35: El sistema debe crear igualmente el reporte, con tipo, categoría y criticidad en valor "Pendiente de clasificación", cuando la llamada a Claude API falle o no responda dentro de 10 s al momento de crearlo.
- RF-36: El sistema debe regenerar el borrador de mail (RF-26) de un reporte cada vez que cambian su tipo, categoría o criticidad, incluido el paso a "Pendiente de clasificación" (RF-32, RF-37).
- RF-37: El sistema debe guardar la edición de un reporte (RF-10) y dejar su tipo, categoría y criticidad en valor "Pendiente de clasificación" cuando la llamada a Claude API de la reclasificación (RF-32) falle o no responda dentro de 10 s.
- RF-38: El sistema debe mostrarle al administrador, en el panel principal, la cantidad de reportes en estado Abierto con clasificación "Pendiente de clasificación".
- RF-39: El sistema debe rechazar el registro (RF-14) cuando el email no tenga un formato válido.
- RF-40: El sistema debe exigir que la contraseña tenga al menos 8 caracteres, tanto en el registro (RF-14) como en el cambio de contraseña (RF-15).
- RF-41: El sistema debe limitar la descripción de un reporte a 2.000 caracteres y la ubicación a 200 caracteres, tanto al crearlo (RF-01) como al editarlo (RF-10).

## Requerimientos No Funcionales
- RNF-01: La clasificación de tipo, categoría y criticidad de un reporte debe obtenerse en una única llamada a Claude API, con una latencia < 3 s (p95) medida sobre esa llamada.
- RNF-02: La clasificación de categoría y la de tipo deben obtener, cada una, al menos 90% de aciertos sobre el conjunto de casos de prueba de clasificación, definido y validado por el área de Seguridad e Higiene, de al menos 100 casos con al menos 10 casos por cada categoría y por cada tipo (dataset de validación a versionar como entregable del proyecto; ver Riesgos y Dependencias).
- RNF-03: La asignación de criticidad debe obtener al menos 85% de aciertos sobre el conjunto de casos de prueba de criticidad, definido y validado por el área de Seguridad e Higiene, de al menos 100 casos con al menos 10 casos por cada nivel de criticidad (dataset de validación a versionar como entregable del proyecto; ver Riesgos y Dependencias).
- RNF-04: La API key del modelo no debe estar en el código; se lee de la variable de entorno `API_KEY`.
- RNF-05: Las contraseñas deben almacenarse con hash seguro (bcrypt/argon2), nunca en texto plano.
- RNF-06: La sesión de usuario debe expirar tras 24 h de inactividad.

## Criterios de Aceptación
- AC-01 (RF-01): Dada una descripción vacía, cuando se intenta crear el reporte, entonces el sistema responde HTTP 400 y no lo crea.
- AC-02 (RF-02a, RF-02b, RF-02c): Dado un conjunto de reportes existentes y los parámetros `page` y `size` provistos por el cliente, cuando el administrador los lista, entonces el sistema devuelve como máximo `size` reportes correspondientes a la página `page` solicitada, y cada reporte incluye su estado.
- AC-03 (RF-02a): Dado un usuario común autenticado, cuando intenta acceder al endpoint de listado global de reportes, entonces el sistema responde HTTP 403.
- AC-04 (RF-03, RF-31): Dado el usuario A dueño de un reporte y el usuario B autenticado, cuando B intenta ver ese reporte, entonces el sistema responde HTTP 403 y no lo muestra. (control de acceso — OWASP #1)
- AC-05 (RF-04, RF-07, RF-25, RNF-02, RNF-03): Dado el dataset de validación versionado, cuando se ejecuta la clasificación automática sobre el conjunto completo, entonces el porcentaje de aciertos de categoría es ≥ 90%, el de tipo es ≥ 90% y el de criticidad es ≥ 85%.
- AC-06 (RF-05, RF-06): Dado un reporte cuya categoría sugerida por el usuario difiere de la clasificación automática, cuando se crea el reporte, entonces la categoría definitiva del reporte es la automática y ambas categorías (sugerida y automática) quedan almacenadas.
- AC-07 (RF-08): Dado un reporte recién creado, cuando finaliza el proceso de creación, entonces existe un evento de notificación asociado a ese reporte.
- AC-08 (RF-09): Dado un reporte con criticidad Crítica, cuando se visualiza el reporte, entonces el sistema muestra la recomendación "Detener actividad".
- AC-09 (RF-09): Dado un reporte con criticidad Alta, cuando se visualiza el reporte, entonces el sistema muestra la recomendación "Requiere intervención inmediata".
- AC-10 (RF-10): Dado un reporte en estado Abierto cuyo propietario es el usuario autenticado, cuando modifica la descripción y la ubicación y guarda los cambios, entonces el sistema guarda ambos valores modificados y los refleja en consultas posteriores.
- AC-11 (RF-11): Dado un reporte en estado En Progreso o Cerrado, cuando el usuario creador intenta editarlo, entonces el sistema responde HTTP 409 y el reporte no cambia.
- AC-12 (RF-12): Dado un usuario no autenticado, cuando intenta ver la lista de reportes, entonces el sistema responde HTTP 401 y no muestra ningún dato.
- AC-13 (RF-14): Dado un email no registrado previamente y una contraseña, cuando un usuario se registra, entonces el sistema crea la cuenta y permite autenticarse con esas credenciales.
- AC-14 (RF-15): Dado un usuario autenticado, cuando cambia su contraseña indicando correctamente la actual, entonces un inicio de sesión posterior solo es exitoso con la nueva contraseña.
- AC-15 (RF-16): Dado un administrador autenticado y un reporte Abierto, cuando cambia su estado a En Progreso, entonces el nuevo estado queda registrado y visible en futuras consultas.
- AC-16 (RF-16): Dado un usuario común autenticado, cuando intenta cambiar el estado de un reporte, entonces el sistema responde HTTP 403.
- AC-17 (RF-17): Dado un reporte recién creado, cuando se consulta el detalle, entonces se muestran la fecha, hora y usuario de creación.
- AC-18 (RF-18): Dado un reporte cerrado por un administrador, cuando se consulta el detalle del reporte, entonces se muestran fecha, hora y usuario que realizó el cierre.
- AC-19 (RF-19): Dado un reporte con 5 imágenes ya adjuntas, cuando el usuario intenta adjuntar una sexta imagen, entonces el sistema responde HTTP 400 y el reporte sigue teniendo 5 imágenes.
- AC-20 (RF-19): Dado un archivo con formato distinto de JPG, JPEG o PNG, cuando el usuario intenta adjuntarlo a un reporte, entonces el sistema responde HTTP 400 y el archivo no queda asociado al reporte.
- AC-21 (RF-19): Dado un archivo con tamaño mayor a 2 MB, cuando el usuario intenta adjuntarlo a un reporte, entonces el sistema responde HTTP 400 y el archivo no queda asociado al reporte.
- AC-22 (RF-20): Dado un reporte con imágenes adjuntas, cuando se consulta su detalle, entonces las imágenes almacenadas se muestran asociadas a ese reporte.
- AC-23 (RF-21): Dado un reporte con imágenes adjuntas, cuando el sistema lo clasifica, entonces la solicitud enviada a Claude API no contiene contenido de imagen (verificable con un cliente del modelo simulado).
- AC-24 (RF-22): Dado un administrador autenticado, cuando registra una acción correctiva sobre un reporte, entonces el sistema almacena la acción y la muestra en futuras consultas.
- AC-25 (RF-23): Dado un reporte En Progreso, cuando el administrador intenta cerrarlo sin acción realizada o sin observación de cierre, entonces el sistema responde HTTP 400 y el reporte sigue En Progreso.
- AC-26 (RF-24): Dados N reportes con criticidad Crítica en estado Abierto, cuando el administrador visualiza el panel principal, entonces el sistema muestra la cantidad N.
- AC-27 (RF-01): Dada una descripción y una ubicación no vacías, cuando un usuario autenticado crea el reporte, entonces el sistema responde HTTP 201 y el reporte queda en estado Abierto.
- AC-28 (RF-01): Dada una ubicación vacía, cuando se intenta crear el reporte, entonces el sistema responde HTTP 400 y no lo crea.
- AC-29 (RF-03): Dados reportes creados por el usuario A y por el usuario B, cuando A lista sus reportes, entonces recibe únicamente los creados por A.
- AC-30 (RF-09): Dado un reporte con criticidad Baja, Media o "Pendiente de clasificación", cuando se visualiza el reporte, entonces el sistema no muestra ninguna recomendación.
- AC-31 (RF-14): Dado un email ya registrado, cuando un usuario intenta registrarse con ese email, entonces el sistema responde HTTP 409 y no crea una segunda cuenta.
- AC-32 (RF-15): Dado un usuario autenticado, cuando intenta cambiar su contraseña indicando una contraseña actual incorrecta, entonces el sistema responde HTTP 400 y el inicio de sesión sigue funcionando solo con la contraseña anterior.
- AC-33 (RF-16): Dado un reporte Cerrado, cuando el administrador intenta cambiar su estado a Abierto o En Progreso, entonces el sistema responde HTTP 409 y el estado sigue Cerrado.
- AC-34 (RF-16): Dado un reporte Abierto, cuando el administrador intenta cambiar su estado directamente a Cerrado, entonces el sistema responde HTTP 409 y el estado sigue Abierto.
- AC-35 (RF-23, RF-16): Dado un reporte En Progreso, cuando el administrador lo cierra indicando acción realizada y observación de cierre, entonces el reporte queda Cerrado y ambos textos se muestran en su detalle.
- AC-36 (RF-10): Dado un reporte Abierto del usuario A, cuando el usuario B (común) intenta editarlo, entonces el sistema responde HTTP 403 y el reporte no cambia. (control de acceso)
- AC-37 (RF-22): Dado un usuario común autenticado, cuando intenta registrar una acción correctiva, entonces el sistema responde HTTP 403. (control de acceso)
- AC-38 (RF-24, RF-38): Dado un usuario común autenticado, cuando intenta acceder al panel principal de administrador, entonces el sistema responde HTTP 403. (control de acceso)
- AC-39 (RF-27): Dado un usuario común que consulta el detalle de un reporte propio, cuando se muestra el detalle, entonces la respuesta no incluye el borrador de mail. (control de acceso)
- AC-40 (RF-25): Dado un reporte recién creado, cuando se consulta su detalle, entonces su tipo es uno de: Acto inseguro, Condición insegura, Casi accidente, Riesgo operativo u Otro.
- AC-41 (RF-26, RF-27): Dado un reporte recién creado, cuando el administrador consulta su detalle, entonces se muestra un borrador de mail cuyo asunto contiene la categoría y la criticidad, y cuyo cuerpo contiene descripción, ubicación, fecha de creación y usuario creador.
- AC-42 (RF-29): Dado un usuario recién registrado, cuando intenta acceder al endpoint de listado global de reportes, entonces el sistema responde HTTP 403. (control de acceso)
- AC-43 (RF-28): Dado un reporte cuya categoría sugerida difiere de la automática, cuando el administrador consulta su detalle, entonces se muestran ambas categorías.
- AC-44 (RF-30): Dada una base de datos vacía y credenciales de administrador provistas por variables de entorno, cuando se inicia la aplicación, entonces es posible autenticarse con esas credenciales y acceder al listado global de reportes.
- AC-45 (RNF-01): Dado el dataset de validación, cuando se mide la latencia de la llamada a Claude API para cada caso, entonces el p95 es < 3 s.
- AC-46 (RNF-04): Dado el repositorio, cuando se busca el valor de la API key en todos los archivos versionados, entonces no hay coincidencias.
- AC-47 (RNF-05): Dado un usuario registrado, cuando se lee su registro en la base de datos, entonces el valor almacenado es distinto de la contraseña y tiene formato de hash bcrypt o argon2.
- AC-48 (RNF-06): Dada una sesión sin actividad durante más de 24 h, cuando el usuario intenta acceder a un endpoint de reportes, entonces el sistema responde HTTP 401.
- AC-49 (RF-02d): Dados reportes creados en distintos momentos, cuando el administrador los lista, entonces el sistema los devuelve ordenados del más reciente al más antiguo.
- AC-50 (RF-31): Dado un reporte creado por el usuario A, cuando A consulta su detalle, entonces el sistema responde HTTP 200 con los datos del reporte.
- AC-51 (RF-31): Dado un reporte creado por cualquier usuario, cuando el administrador consulta su detalle, entonces el sistema responde HTTP 200 con los datos del reporte.
- AC-52 (RF-32): Dado un reporte Abierto y un cliente de Claude API simulado que devuelve una clasificación distinta a la original, cuando el creador edita la descripción, entonces el reporte queda con el tipo, la categoría y la criticidad devueltos en la nueva llamada.
- AC-53 (RF-33): Dado un reporte En Progreso o Cerrado, cuando el creador intenta adjuntarle una imagen, entonces el sistema responde HTTP 409 y la cantidad de imágenes del reporte no cambia.
- AC-54 (RF-22): Dado un administrador autenticado, cuando registra una acción correctiva, entonces la acción queda con la fecha del momento del registro y con ese administrador como usuario responsable.
- AC-55 (RF-34): Dado un reporte Cerrado, cuando el administrador intenta registrar una acción correctiva, entonces el sistema responde HTTP 409 y no se registra la acción.
- AC-56 (RF-35): Dado un cliente de Claude API simulado que devuelve un error, cuando un usuario crea un reporte válido, entonces el sistema responde HTTP 201 y el reporte queda con tipo, categoría y criticidad "Pendiente de clasificación".
- AC-57 (RF-36): Dado un reporte Abierto y un cliente de Claude API simulado que devuelve una categoría y criticidad distintas a las originales, cuando el creador edita la descripción, entonces el asunto del borrador de mail contiene la nueva categoría y la nueva criticidad.
- AC-58 (RF-35): Dado un cliente de Claude API simulado que no responde dentro de 10 s, cuando un usuario crea un reporte válido, entonces el sistema responde HTTP 201 y el reporte queda con tipo, categoría y criticidad "Pendiente de clasificación".
- AC-59 (RF-37): Dado un reporte Abierto ya clasificado y un cliente de Claude API simulado que devuelve un error, cuando el creador edita la descripción, entonces la nueva descripción queda guardada y el reporte queda con tipo, categoría y criticidad "Pendiente de clasificación".
- AC-60 (RF-38): Dados N reportes en estado Abierto con clasificación "Pendiente de clasificación", cuando el administrador visualiza el panel principal, entonces el sistema muestra la cantidad N.
- AC-61 (RF-10): Dado un reporte Abierto del usuario autenticado, cuando lo edita dejando la descripción o la ubicación vacía, entonces el sistema responde HTTP 400 y el reporte no cambia.
- AC-62 (RF-36, RF-37): Dado un reporte Abierto ya clasificado y un cliente de Claude API simulado que devuelve un error, cuando el creador edita la descripción, entonces el asunto del borrador de mail contiene "Pendiente de clasificación" en lugar de la categoría y criticidad anteriores.
- AC-63 (RF-14): Dadas varias solicitudes simultáneas de registro con un mismo email no registrado, cuando el sistema las procesa, entonces exactamente una responde HTTP 201, las demás responden HTTP 409 y existe una única cuenta con ese email.
- AC-64 (RF-39): Dado un email con formato inválido (por ejemplo "noesmail"), cuando un usuario intenta registrarse, entonces el sistema responde HTTP 400 y no crea la cuenta.
- AC-65 (RF-40): Dada una contraseña de menos de 8 caracteres, cuando un usuario intenta registrarse, entonces el sistema responde HTTP 400 y no crea la cuenta.
- AC-66 (RF-40): Dado un usuario autenticado, cuando cambia su contraseña indicando correctamente la actual pero con una nueva de menos de 8 caracteres, entonces el sistema responde HTTP 400 y el inicio de sesión sigue funcionando solo con la contraseña anterior.
- AC-67 (RF-41): Dada una descripción de más de 2.000 caracteres, cuando se intenta crear el reporte, entonces el sistema responde HTTP 400, no lo crea y no realiza ninguna llamada a Claude API (verificable con un cliente del modelo simulado).
- AC-68 (RF-41): Dada una ubicación de más de 200 caracteres, cuando se intenta crear el reporte, entonces el sistema responde HTTP 400 y no lo crea.
- AC-69 (RF-41): Dado un reporte Abierto del usuario autenticado, cuando lo edita con una descripción de más de 2.000 caracteres, entonces el sistema responde HTTP 400, el reporte no cambia y no se realiza ninguna llamada a Claude API.

## Fuera de Alcance
- CRM completo.
- Multi-canal real (mail/WhatsApp en vivo).
- RBAC configurable / más de dos roles.
- Promoción o cambio de rol de usuarios desde la aplicación.
- Multi-tenant.
- Envío real de mails (el borrador queda para copiar/pegar — ver RF-26/RF-27).
- Ampliación de las categorías y tipos de clasificación más allá de los definidos en RF-04 y RF-25.
- Uso de imágenes como input del modelo (ver RF-21).
- Corrección manual de la categoría, tipo o criticidad por parte del administrador.
- Reapertura de reportes cerrados y cierre directo desde Abierto.
- Eliminación de reportes.
- Edición de imágenes ya adjuntas y de la categoría sugerida.
- Recuperación de contraseña por mail.
- Política de complejidad de contraseñas (mayúsculas, números, símbolos); solo se exige el largo mínimo de RF-40.
- Notificaciones al empleado que reportó.
- Exportes y reportes estadísticos más allá del contador de RF-24.
- Métricas de éxito de producto en v1.
- Reintento automático o manual de la clasificación de reportes "Pendiente de clasificación" (RF-35).

(La autenticación con dos roles (usuario común y administrador) sí forma parte del alcance — ver Personas y RF-12.)

## Riesgos y Dependencias
- Riesgo: clasificación incorrecta o inconsistente para descripciones ambiguas → mitigación: validación con dataset etiquetado y revisión periódica de casos erróneos.
- Riesgo: caída o latencia elevada de Claude API → mitigación: el reporte se guarda igual como "Pendiente de clasificación" (RF-35, RF-37), sin reintento en v1, y el administrador ve cuántos hay en su panel (RF-38).
- Riesgo: el contenido de los reportes se envía a un tercero (Anthropic) → mitigación: informar a los usuarios y evitar datos personales innecesarios en la descripción.
- Riesgo: costo por llamada a Claude API → mitigación: una única llamada por clasificación (RNF-01), solo al crear o editar un reporte (RF-04, RF-32), y con largo máximo de descripción y ubicación (RF-41).
- Riesgo: el dataset de validación (≥ 100 casos, ver RNF-02/RNF-03) todavía no existe → mitigación: acordar con Seguridad e Higiene su fecha de entrega antes de validar RNF-02/RNF-03.
- Dependencia: Claude API · base de conocimiento `kb.md` (contexto de clasificación para RF-04, RF-07, RF-25, RF-32) · SQLite · dataset de validación de Seguridad e Higiene.
