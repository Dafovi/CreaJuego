# CreaJuego Educational Layer V1 — primer bloque implementado

Fecha: 7 de septiembre de 2026. Unity **6000.6.0f1**, revisión **f7f8ed4d1e24**.

## Alcance recibido y estado

Se leyó íntegramente [el análisis previo](CreaJuego-Educational-Layer-Analysis.md) y se utilizó como base, sin repetir el inventario de Playground.

El adjunto «SPRINT — CREAJUEGO EDUCATIONAL LAYER V1» termina literalmente en **GRU**, dentro de la fase 6, después de definir el slider Velocidad del jugador. Se solicitó la continuación. Este documento entrega un bloque verificable de las fases completas 1–5 y la parte recibida de la 6; **no declara terminado el sprint completo**. Las propuestas del informe previo no se presentan como requisitos recibidos de las fases ausentes.

## Cambios

- Catálogo educativo de cinco tarjetas: Jugador, Plataforma, Premio, Peligro y Meta. Cada tarjeta tiene un icono geométrico propio, nombre y descripción. Plataforma móvil, enemigo y decoración conservan sus assets y usos existentes, pero no aparecen en el catálogo de participantes. Los gates P1 siguen pendientes.
- `GameItemDefinition` incorpora `availableInWorkshop` y `allowMultiple`. `Catalog()` conserva el inventario completo; `WorkshopCatalog()` filtra la superficie del taller. La vista no decide por clase Playground qué publicar.
- `EducationalProperty` añade `group`, `unit` y `visibleWhen`: una única ruta booleana opcional, sin expresiones ni reflexión. Se muestra el control ante valores mixtos para permitir multi-edición. No hay un ScriptableObject por propiedad.
- La ventana existente mantiene su organización en catálogo y propiedades junto al SceneView normal. Añade iconos, grupos, ayudas visibles, campos enteros acotados, texto multilínea de hasta 120 caracteres, toggles y color. Los sliders mantienen sus rangos y entrada numérica. Todos los controles enlazan propiedades de `GameItem` mediante `SerializedObject`/`SerializedProperty`.
- «Vidas» pasa a **Resistencia inicial** en el panel educativo del jugador. El HUD de la demo conserva todavía su terminología anterior: pendiente de la fase de sesión/presentación.
- Acciones **Duplicar** y **Eliminar**, con Undo. El duplicado conserva conexión al prefab, overrides y desplazamiento visible. Si hay componentes u objetos añadidos/eliminados mediante edición técnica, informa que se use la vista Escena; no descarta esos cambios silenciosamente.
- Este preset impide crear o duplicar un segundo jugador desde CreaJuego. Eliminarlo permite volver a crearlo; Undo lo recupera. La edición directa de assets desde el panel se rechaza.
- No se añadieron campos a GameItem, ni cambios al adapter o al Runtime de Playground. Sus 42 scripts mantienen los hashes del spike anterior.

## Archivos principales

- [GameItemDefinition.cs](../Packages/com.dafovi.creajuego/Runtime/GameItemDefinition.cs): metadatos educativos y publicación del catálogo.
- [ItemService.cs](../Packages/com.dafovi.creajuego/Editor/ItemService.cs): catálogo, unicidad, duplicar y eliminar.
- [CreaJuegoWindow.cs](../Packages/com.dafovi.creajuego/Editor/CreaJuegoWindow.cs): controles UI Toolkit y acciones.
- [EducationalLayerV1.cs](../Assets/CreaJuegoPacks/Starter/Editor/EducationalLayerV1.cs): migración explícita de metadatos Starter e iconos geométricos.
- `Assets/CreaJuegoPacks/Starter/Content/*.asset` y `Content/Icons/`: definiciones actualizadas e iconos; prefabs y escena no regenerados.
- [EducationalLayerTests.cs](../Assets/CreaJuegoPacks/Starter/Tests/Editor/EducationalLayerTests.cs): cinco pruebas nuevas.
- [SpikeTests.cs](../Assets/CreaJuegoPacks/Starter/Tests/Editor/SpikeTests.cs): pruebas anteriores ajustadas al nuevo nombre del salto y la unicidad de creación. La prueba de multi-edición prepara un segundo jugador mediante Unity para verificar que el binding sigue siendo robusto ante edición avanzada.

La migración se ejecutó una vez con `CreaJuego.Starter.EducationalLayerV1.Apply`; antes de modificar cada definición guardó su original en `.spike/educational-v1-before/`. No es una migración automática al abrir ni al seleccionar. No ejecutarla de nuevo para restaurar valores de autoría: su función es aplicar los metadatos de esta versión. Los iconos existentes no se sobrescriben.

## Verificación

| Evidencia | Resultado |
|---|---|
| [Baseline](v1-baseline-tests.xml), sin cambios de código | 7/7 aprobadas |
| [Compilación y migración](v1-metadata-unity.log) | Finalización correcta; marcador `CREAJUEGO_EDUCATIONAL_METADATA_V1_READY` |
| [Suite posterior](v1-authoring-tests.xml) | **12/12 aprobadas**, ninguna omitida |
| [Log de la suite](v1-authoring-unity.log) | Sin errores de compilación C# de CreaJuego |
| Comparación SHA256 vendor contra `Playground-Source-Inventory.csv` | 42 archivos, 0 cambios |

Las pruebas nuevas cubren publicación de exactamente cinco elementos/iconos distintos, conservación de contenido experimental, guardas de jugador y assets, duplicado con valores y vínculo al prefab, eliminación/Undo/Redo, bindings de UI Toolkit, límites numéricos y visibilidad booleana reactiva. La visibilidad reactiva se probó en un objeto; su variante con valores mixtos queda sin cobertura específica. La multi-edición general sigue cubierta por la prueba anterior de SerializedObject. Después de ejecutar la suite sólo se renombró la prueba de visibilidad para describir correctamente su alcance; no cambió su cuerpo.

También siguen pasando las pruebas anteriores de movimiento, salto, premio/puntos, daño, derrota, meta y recorrido completo con teclado en Play Mode. Esto **no** elimina los riesgos de suelo y fin de partida documentados en el análisis: esas pruebas no cubren todos los casos límite.

No se realizó una prueba manual con participantes ni una revisión visual mediante captura real del Editor. Se verificó la ventana mediante sus controles UI Toolkit en el Editor de Unity. No se volvió a generar el ejecutable Windows porque este bloque no cambia comportamiento Runtime; la compilación y ejecución verificadas corresponden al Editor.

## Cómo revisar el bloque

1. Abrir el proyecto en Unity 6000.6.0f1.
2. Abrir `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego → Abrir taller**. Deben aparecer cinco tarjetas.
4. Seleccionar el jugador existente: ver icono, ayuda y grupos Movimiento, Salto y Resistencia. Intentar añadir otro debe explicar el límite.
5. Crear un premio, modificar Puntos, duplicarlo y eliminar la copia. Ctrl+Z permite recuperar/deshacer; Ctrl+Y rehacer.
6. Pulsar **PROBAR**, usar flechas/Espacio y llegar a la meta o tocar el peligro. **DETENER** devuelve a la edición.

El color de plataforma se aplica al probar, como indica su ayuda; este bloque todavía no incorpora preview inmediato ni Ancho. Una escena vacía todavía requiere preparación de servicios: no se debe presentar esta entrega como un flujo completo de creación desde cero.

## Pendiente antes de cerrar V1

Primero recibir la continuación del adjunto para fijar los requisitos restantes. El análisis previo recomienda: apoyo de salto fiable, opción Puede saltar, validación de escena antes de probar, estado único de victoria/derrota con bloqueo posterior, preview de color con Undo y decisión explícita sobre Ancho. Ninguno se considera entregado aquí. Los gates de plataforma móvil y enemigo, la persistencia guardar/reabrir y la observación con participantes también siguen pendientes.

Se mantiene la separación `UI → servicios Editor → GameItem/IItemBackend → adapter → Playground`. No hubo rediseño completo de interfaz, APIs internas, sistema de reglas ni commits.



---

## Continuación implementada — 7 de septiembre de 2026

Esta sección añade la evidencia de la continuación completa sin sustituir la del primer bloque. **La implementación funcional está integrada; el cierre visual de V1 sigue pendiente.** No se realizó una revisión manual del Editor ni una prueba observada con participantes. No confundir las pruebas automatizadas de UI con esas dos actividades.

El repositorio público inicial se publicó como `Dafovi/CreaJuego` en el commit `9b34079`, por petición expresa anterior. **No se hicieron commits ni push de esta continuación.**

### Baseline, entorno e integración

Unity sigue siendo **6000.6.0f1 (f7f8ed4d1e24)**. El Editor del proyecto estaba abierto y bloqueó la segunda instancia. Se solicitó guardarlo/cerrarlo, y se ejecutó el trabajo verificable en una copia aislada de Assets, Packages y ProjectSettings en `.spike/V1Validation`.

- Baseline idéntico antes de cambiar código: **12/12** — `v1-continuation-baseline-tests.xml`.
- Bloque funcional antes de USS: **20/20** — `v1-completion-core-final-tests.xml`.
- Regresión con USS: **20/20** — `v1-completion-ui-tests.xml`.
- Gates P1: **2/2 pruebas completadas**; la plataforma NO aprobó transporte — `v1-p1-gates-tests.xml`.
- Suite final: **24/24**, cero omitidas — `v1-completion-final-tests.xml`.
- Reproductor Windows: **compilación correcta**, marcador `CREAJUEGO_PLAYER_BUILD_OK` en `v1-completion-player-build.log`.

Los 45 archivos de código/contenido distintos se copiaron al proyecto principal sólo después de verificar que éste no tenía cambios nuevos en Assets/Packages. Cada archivo existente se respaldó en `.spike/V1BeforeSync`. Una comparación posterior de hashes encontró **0 diferencias** entre esos archivos y los validados. El listado completo está en [V1-Changed-Files.txt](V1-Changed-Files.txt), además de esta documentación y README.

La instancia del Editor original seguía abierta con assemblies anteriores al integrar: necesita recuperar foco/refrescar o reabrirse. La compilación y las pruebas aquí citadas pertenecen a la copia aislada con el mismo contenido integrado; **no se afirma que se haya inspeccionado visualmente esa instancia original actualizada**. Los logs y XML permanecen locales, excluidos de Git por contener datos del equipo.

### Arquitectura final

`GameItemDefinition + EducationalProperty → CreaJuegoWindow → ItemService / SceneService / ItemAppearance → GameItem + IItemBackend → PlaygroundAdapter → Playground`.

- Runtime propio: añade `GameItem.canJump` (true por defecto), `GameSessionState`, contratos pequeños de validación y búsqueda acotada a una escena.
- `ContentPackDefinition`: sólo identidad y prefab de infraestructura del pack. No referencias a objetos de escena ni registro universal de plugins.
- Editor: catálogo, propiedades, validación previa, preparación explícita, duplicación/eliminación y preview.
- Backend: un único `PlaygroundAdapter` por elemento. La vista no referencia clases Playground.
- `DemoSession`: autoridad de resultado de la escena; conserva UIScript como contador/presentador de puntos y resistencia.
- `GroundedJumpGate`: contacto de apoyo propio, sin reflexión ni heredar Jump.

Los valores pedagógicos siguen guardados en GameItem. No se creó un formato de guardado propio ni un sistema de reglas.

### Salto y cambios vendor justificados

Se conserva `GameItem.speed → Move.speed / 20` y `GameItem.jump → Jump.jumpStrength`.

`GroundedJumpGate` consulta contactos de Rigidbody2D: normal ascendente > 0,65, cuerpo simulado y velocidad vertical ≤ 0,15. Una pared lateral no es apoyo. La comprobación no depende de Untagged ni de un tag concreto.

El auxiliar configura `checkGround=false` **sólo para delegar el apoyo al predicado propio**. El toggle educativo sigue siendo `GameItem.canJump`, nunca checkGround. Jump permanece activo mientras puede saltar y la sesión está jugando; su predicado decide si el contacto permite el impulso. Esto evita perder la pulsación del primer frame, problema encontrado al habilitar/deshabilitar Jump según apoyo. Las búsquedas de sesión se resuelven en Start, porque el filtrado por escena cargada no es fiable en Awake.

Se modificaron **4 de los 42 archivos vendor respecto al bloque anterior**; 38 mantienen exactamente sus hashes:

| Archivo | Cambio incremental | Motivo |
|---|---|---|
| Jump.cs | Predicado opcional `jumpAllowed` | Evaluar apoyo antes del impulso sin reflexión ni alternar el script cada aterrizaje |
| CollectableAttribute.cs | Guarda opcional `interactionAllowed` | No sumar/destruir el premio tras el final |
| ModifyHealthAttribute.cs | Guarda opcional `interactionAllowed` | Rechazar contactos posteriores, incluso callbacks pendientes |
| HealthSystemAttribute.cs | Guarda `modificationAllowed` y notificación `healthChanged` | Notificar derrota de forma síncrona y bloquear daño tardío |

Son delegates no serializados, opcionales y sin tipos CreaJuego. Si son null, se conserva el comportamiento upstream. No se copiaron inspectores ni se reemplazaron movimiento, daño, salud o puntuación. Jump ya era uno de los cuatro archivos adaptados en el spike: sumando cambios históricos hay **7 archivos distintos adaptados respecto a upstream**, no ocho.

Evidencia: [parche incremental](Playground-V1-Compatibility.patch) y [hashes V1](Playground-V1-Source-Inventory.csv). Se conserva la licencia MIT y el parche/inventario originales.

### Sesión Playing / Won / Lost

DemoSession fija UIScript en Endless durante ejecución para que no decida resultados por su cuenta. UIScript sigue mostrando la salud real de HealthSystemAttribute y sumando puntos reales.

- **Won:** una transición, mensaje final, movimiento/salto/patrulla deshabilitados, velocidades a cero, física y contactos de elementos suspendidos.
- **Lost:** la notificación síncrona de salud ≤ 0 produce una transición y bloquea el juego. HealthSystemAttribute conserva su destrucción original del personaje; no hay reaparición.
- Las guardas de contacto rechazan daño y premios después de ambos resultados. Una segunda meta, daño o solicitud de fin no cambia el resultado ni vuelve a emitirlo.
- Nueva entrada en Play con recarga normal de escena: Playing, resistencia inicial, puntos cero, premios/peligros presentes y controles restituidos.
- ReachGoalAction resuelve la sesión de su propia escena. Se elimina la búsqueda global de sesión de la meta.

El contrato cubre los comportamientos del Starter Pack. Scripts ajenos que invoquen directamente UIScript.AddPoints quedan fuera de él; la API vendor sigue siendo pública. Ese es uno de los motivos para sustituir más adelante el modelo/presentador de puntuación, cuando su coste esté justificado.

### Preflight y preparación

**PROBAR** pasa primero por SceneService. Comprueba:

1. Una única escena de taller, fuera de edición de prefab.
2. Exactamente un jugador activo, sin otro jugador inactivo oculto.
3. Cámara activa.
4. Una sesión y su configuración.
5. Marcador válido de puntos/resistencia, paneles y referencias esenciales.
6. Al menos una meta.
7. Definición, prefab y un backend válido por elemento; controles/acciones esenciales del backend.

Si falla, no entra en Play. Muestra **TU JUEGO NECESITA ALGO**, comprobaciones e indicaciones en español.

**Preparar escena** y el menú **CreaJuego → Preparar escena actual** instancian explícitamente `SceneServices.prefab` desde `StarterPack.asset`, con una operación Undo. Una cámara existente se conserva; no se duplica. Una sesión existente no se sustituye ni duplica. Si hay un Canvas sin sesión, se pide recuperar la infraestructura o usar una escena nueva: no se fusionan referencias potencialmente ajenas silenciosamente.

La preparación no añade jugador ni meta por cuenta del participante. Esos dos pasos permanecen visibles en el catálogo. Seleccionar un objeto nunca prepara ni repara una escena.

### Elementos entregados

| Elemento | Estado | Backend | Propiedades educativas | Workshop |
|---|---|---|---|---|
| Jugador | READY | Move + Jump con apoyo propio + HealthSystemAttribute | Velocidad 0,2–5; Fuerza de salto 5–18; Puede saltar; Resistencia inicial 1–10 | Sí |
| Plataforma | READY | SpriteRenderer + BoxCollider2D | Color con preview inmediato | Sí |
| Premio | READY | CollectableAttribute | Puntos 1–100 | Sí |
| Peligro | READY | ModifyHealthAttribute | Daño al personaje 1–10; Desaparecer después del contacto | Sí |
| Meta | READY | ConditionArea + ReachGoalAction + sesión | Mensaje al llegar, multilínea, hasta 120 caracteres | Sí |
| Plataforma móvil | EXPERIMENTAL | Patrol | Velocidad y distancia conservadas en definición experimental | No |
| Enemigo | READY | Patrol + ModifyHealthAttribute | Velocidad 0,2–3; Distancia de recorrido 0,5–6; Daño al personaje 1–10 | Sí |

READY significa verificación técnica del comportamiento; no sustituye la revisión visual pendiente. Decoración sigue HIDDEN y su contenido se conserva.

Resistencia reemplaza Vidas en el HUD visible y se corrigió el espaciado del marcador. Premio siempre desaparece al recogerlo; no presenta un toggle sin efecto. Daño educativo siempre positivo, convertido a healthChange negativo. Meta no exige recoger todos los premios. Enemigo no tiene IA, persecución, animación, detección de bordes ni derrota al saltarle encima.

### Plataforma: preview y Ancho

GameItem.tint es la fuente. ItemAppearance actualiza el SpriteRenderer derivado en modo edición con callbacks del sistema Undo y del binding. Se probó multi-edición, overrides, Undo y Redo; se recalcula tras abrir escena/deshacer.

**Ancho se pospone explícitamente.** Los prefabs utilizan SpriteRenderer Sliced y BoxCollider2D con tamaños coincidentes, pero el suelo existente multiplica su ancho con scale.x=6. Introducir un ancho absoluto requiere migrar esa combinación sin alterar niveles ya creados. No se añadió un segundo dato de tamaño ni se cambió la geometría del nivel.

### Acciones, interfaz y SceneView

La ventana existente conserva dos columnas y usa [CreaJuegoTheme.uss](../Packages/com.dafovi.creajuego/Editor/Styles/CreaJuegoTheme.uss). La paleta se concentra en tokens USS; ya no se reparten colores de interfaz por C#.

Incluye tarjetas, iconos, selección resaltada, estados hover/focus, grupos, ayudas visibles, controles grandes, texto multilínea, scroll, preparación, preflight y estado de la prueba. Puede saltar oculta Fuerza de salto cuando no aplica. La multi-selección compatible muestra mixed values de Unity y edita los valores comunes. Las selecciones incompatibles muestran una instrucción.

Duplicar conserva el vínculo al prefab, definición y overrides, desplaza la copia y la selecciona; agrupa Undo. Eliminar utiliza Undo y limpia selección. Si se elimina personaje o meta, muestra una advertencia educativa sin impedir borrarlos. El preflight impide jugar mientras falten. Encontrar enfoca la selección en SceneView.

No se reemplazó SceneView. No se incorporaron gizmos nuevos ni cambios de Hierarchy: se posponen tras la validación visual básica. La investigación previa de APIs públicas sigue vigente como evidencia; no se usó reflection ni APIs internas para personalizarlos.

### Gates P1

**Plataforma móvil — EXPERIMENTAL — NOT READY FOR WORKSHOP.**

Se verificaron ida/vuelta, velocidad mínima 0,2 y máxima 3, distancia corta, permanencia en límites y duplicación lejos del origen. No se utiliza directionChangeInterval. La prueba de pasajero con velocidad 3 y distancia 6 registró **6,00 unidades de separación máxima**. Patrullar bien no equivale a transportar al jugador.

Se evaluó conceptualmente parenting o compensación de desplazamiento. El jugador dinámico usa fuerzas y material sin fricción; modificar su Transform mediante parenting o sumar desplazamiento requiere arbitrar salto, colisiones y velocidad. No se introdujo esa segunda política física para forzar la publicación. Se conserva oculta. [Resultado del diagnóstico](P1-platform-gate.txt).

**Enemigo — READY.**

La prueba crea un enemigo real, comprueba avance/regreso y copia relativa, aplica 2 de daño a un jugador con 3 de resistencia, confirma que el enemigo no desaparece, provoca derrota con una reentrada y comprueba bloqueo tras Lost y Won. Sólo después de pasar se activó su definición en el catálogo. Su trayectoria debe quedar libre.

La prueba automatizada del diagnóstico de plataforma pasa porque registra el resultado y verifica que continúa oculta; **no significa que transporte haya aprobado**.

### Persistencia y flujo completo

Se probó crear, modificar por SerializedObject, guardar una escena nueva, abrirla de nuevo, verificar puntos, realizar un nuevo cambio y deshacer/rehacer, entrar/salir de Play y conservar los valores de autoría.

No se promete conservar la pila histórica de Undo al descargar/cerrar una escena: Unity no la trata como datos persistentes del nivel. La prueba verifica Undo/Redo de una edición realizada después de reabrir, además de la persistencia de los datos guardados.

El flujo automatizado desde una escena vacía prepara infraestructura, crea jugador/plataforma/premio/peligro/meta, duplica plataforma y premio, ajusta puntos y mensaje mediante SerializedObject y coloca los elementos. Con teclado se recogen **5 puntos**, se pierde **1 de resistencia** y se alcanza **Won** con «¡Llegaste!». Al detener, cambiar daño a 10 y probar de nuevo, se alcanza **Lost**. El salto se verifica por separado y en la ruta de la demo original.

Esto prueba los servicios que utiliza la UI y el Runtime real; no es una grabación de una persona pulsando todas las tarjetas. El checklist manual se conserva en [CreaJuego-V1-Manual-Checklist.md](CreaJuego-V1-Manual-Checklist.md).

### Revisión visual: bloqueo real y límite de evidencia

Se leyó la guía del plugin Computer Use y se intentó inicializar su runtime, incluyendo reinicio y recuperación. Falló antes de obtener una ventana con:

`node_repl kernel exited unexpectedly — windows sandbox failed: helper_unknown_error: setup refresh had errors`

No se obtuvieron capturas ni una observación visual del Editor actualizado. No se recurrió a APIs internas de Unity o a un cliente alternativo del controlador. Por tanto:

- **Revisión visual manual realizada: ninguna.**
- Se comprobaron automáticamente stylesheet cargado, scroll disponible y límites horizontales del panel en ventana de 680×500.
- Se probaron controles, visibilidad del salto, valores mixtos, catálogo y acciones.
- Contraste percibido, truncamientos reales, hover, comodidad, texto multilínea y flujo de clics requieren todavía observación manual.
- **El criterio de cierre visual de V1 queda abierto.**

### Deuda, riesgos y siguiente sprint

1. Completar revisión visual real y el checklist manual; después hacer un piloto acompañado con una persona sin experiencia.
2. UIScript conserva búsquedas globales y un modelo interno de puntuación acoplado a uGUI. El preset restringe el trabajo a una escena y un marcador; no promete multiescena/multijugador.
3. Los hooks vendor son mínimos y tienen hash/parche, pero deben revisarse al actualizar Playground.
4. La validación cubre los presets conocidos, no cualquier edición técnica arbitraria del proyecto.
5. Verificación realizada con la recarga normal de Play Mode. No se ha validado desactivar la recarga de escena.
6. Teclado, cámara fija y arte provisional; no hay accesibilidad de entrada mediante mando/táctil.
7. Ancho, transporte de plataforma móvil, gizmos y Hierarchy siguen pospuestos.
8. La preparación admite un pack de infraestructura; selección de varios packs no forma parte de V1.

En el piloto observar: comprensión de Resistencia/Puntos, facilidad para colocar elementos con SceneView, descubrimiento de Probar/Detener, interpretación del preflight, recuperación con Undo, lectura de ayudas y número de intervenciones del facilitador. Usar esas observaciones para priorizar placement, encuadre y presets. No iniciar aún un editor de reglas general.

