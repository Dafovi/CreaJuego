# CreaJuego — Technical Spike

## Objetivo y estado
Vertical slice educativo 2D en español sobre Unity 6.6. La herramienta convive con SceneView; no sustituye Unity. **Implementado y validado automáticamente: 7/7 pruebas aprobadas.** La aceptación manual del taller sigue pendiente y no se declara completada.

## Inspección inicial
La carpeta del proyecto estaba vacía, sin repositorio Git, Assets, paquetes ni trabajo previo. No existían escenas, asmdefs ni configuración a preservar. No se hicieron commits.

Editor instalado y utilizado: **6000.6.0f1 (f7f8ed4d1e24)**. Verificado en ProductVersion del ejecutable `Unity.exe` de esa instalación, en logs y ProjectVersion.txt. No se usaron instalaciones anteriores.

La primera creación restringida falló por acceso a cachés/licencia; la ejecución autorizada posterior completó importación con código 0 (`unity-baseline-retry.log`). El Editor creó paquetes predeterminados innecesarios para este experimento; su inventario se conserva en `baseline-manifest.json`. Se redujeron a Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0 y módulos de física/UI/audio/imagen. El inventario efectivo y dependencias transitivas quedan en `Packages/packages-lock.json`.

Render pipeline: Built-in, sprites provisionales sin iluminación, sin URP ni conversión de materiales. Playground 1.8 ofrece ejemplos URP, pero sus scripts seleccionados no requieren ese pipeline. Física Rigidbody2D/Collider2D, gravedad de Unity, cámara ortográfica y modo Editor 2D. Input System nuevo exclusivamente (`activeInputHandler: 1`).

## Fuente, versión y licencia
- [Repositorio oficial](https://github.com/Unity-Technologies/UnityPlayground), commit **3d8acd7432ee115f28e05f2b1af39fa783376b4a**, 2026-05-01.
- [CHANGELOG fijado](https://github.com/Unity-Technologies/UnityPlayground/blob/3d8acd7432ee115f28e05f2b1af39fa783376b4a/CHANGELOG.md): **1.8.0**, actualizado a Input System y probado upstream hasta 6.3 LTS. ProjectVersion upstream: 6000.0.66f2. Hay mantenimiento reciente; no se presume soporte 6.6 por ese hecho.
- [Licencia MIT](https://github.com/Unity-Technologies/UnityPlayground/blob/3d8acd7432ee115f28e05f2b1af39fa783376b4a/License.md), copyright 2016 Unity Technologies – Evangelism Team. Texto íntegro conservado junto a los scripts.
- [Unity Learn](https://learn.unity.com/project/unity-playground?language=en): recurso educativo histórico. Distribución upstream como proyecto Git y Asset Store; no se importó como paquete UPM oficial.
- El README indexado anunciaba 1.1/2022.2: está desactualizado respecto al CHANGELOG y código realmente descargado. La referencia de 2018 es histórica, no la versión probada.

Se copiaron todos los scripts Runtime reales conservando estructura y meta. Se excluyeron inspectores originales (incluidos reemplazos globales de Transform, Collider, etc.), escenas, arte, ajustes y recursos gráficos de gizmos. La referencia completa descargada está en `.spike/UnityPlayground`, ignorada. Los scripts distribuidos, licencia y parche se conservan fuera de esa carpeta, por lo que no hace falta la descarga para abrir el prototipo.

## Unity Playground viability
**ADAPT — categoría B: utilizable con cambios menores**, comprobado en las pruebas descritas al final. Backend de la demo: **Unity Playground adaptado**, con una fachada CreaJuego y una acción de meta propia. No se implementó un reemplazo del movimiento/coleccionables/daño. Se distribuyen 42 scripts Runtime upstream, 38 sin cambios y 4 con parches identificados.

Problemas y cambios controlados:
1. Input System 1.18.0 de upstream produce CS0619 en PlayerInputEditor.cs: conversión EntityId → int. Se actualizó a **1.20.0**, publicado para 6.6 según [manual oficial](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.inputsystem.html). No se modificó PackageCache.
2. `InputUtils.GetAxis` y `Jump.Update` arrojaban NullReferenceException cuando no hay teclado (detectado ejecutando Play Mode sin gráficos). Se comprueba `Keyboard.current` antes de acceder. Otros componentes de entrada no usados aún pueden necesitar la misma protección.
3. `Utils.cs` importaba UnityEditor fuera de `#if UNITY_EDITOR`. Se protegió ese import para compilación de Player.
4. `Move.Update` calculaba la fuerza con `Time.smoothDeltaTime`, introduciendo dependencia del renderizado en una fuerza aplicada en FixedUpdate. Se usa `Time.fixedDeltaTime`. La normalización, lectura de teclas y AddForce siguen siendo originales. La fachada traduce velocidad educativa / 20 a la escala de fuerza upstream. El personaje usa material con fricción 0 y amortiguación lineal 3. El fallo inicial de entrada de las pruebas tenía además una causa distinta: el arnés debía dirigir dispositivos a GameView con `editorInputBehaviorInPlayMode`. No se confunde esa corrección de pruebas con un fallo del backend.
5. Se añadió `Playground.Runtime.asmdef` para aislar dependencias. Se mantiene el warning upstream CS0109 por `new Rigidbody2D`, sin parche cosmético.

Todos los cambios fuente llevan comentario CREAJUEGO. Véase `Playground-Compatibility.patch` y `Playground-Source-Inventory.csv` para comparar con el commit. No se modifican las condiciones/licencia originales.

## Componentes investigados
| Concepto | Componentes reales | Uso del spike |
|---|---|---|
| Personaje | Move, Jump, Physics2DObject, InputUtils | Movimiento horizontal, salto y teclado |
| Movimiento automático | AutoMove, AutoRotate | Compilados, no probados funcionalmente |
| Patrulla | Patrol | Plataforma móvil y enemigo horizontal; recorrido relativo traducido al iniciar |
| Colisiones/áreas | ConditionCollision, ConditionArea | ConditionArea detecta llegada a meta |
| Daño/vidas | ModifyHealthAttribute, HealthSystemAttribute | Daño, pérdida y desaparición |
| Premio/puntuación | CollectableAttribute, UIScript | Trigger real, suma y destrucción |
| Creación/destrucción | CreateObjectAction, DestroyAction, TimedSelfDestruct, ObjectCreatorArea, ObjectShooter | Localizados/compilados, no validados en juego |
| Condiciones/eventos | ConditionBase, ConditionRepeat, ConditionKeyPress, UnityEvent | Cadena de acciones de área utilizada; otras pendientes |
| Fin/meta | ConditionArea + ReachGoalAction propia | Completa experiencia; el puente detiene controles |
| UI | UIScript, UIItemScript, DialogueSystem, BalloonScript | Score/vidas con uGUI original; diálogos no probados |
| Cámara | CameraFollow | Compilado; demo usa cámara fija |
| Sonido/animación | AudioSource/Animator de Unity, acciones/eventos configurables | No se incluyó contenido de audio ni animación; pendiente |

Compilar un componente no demuestra su funcionamiento. Sólo las conductas indicadas en las pruebas se consideran comprobadas.

## Arquitectura implementada
`CreaJuegoWindow → ItemService → GameItem + IItemBackend → PlaygroundAdapter → Playground → Unity`.

- Runtime core: `GameItemDefinition` (id, texto, categoría, icono, prefab, ayuda, orden, tipo y propiedades) y `GameItem` (estado educativo serializado).
- Editor core: catálogo descubierto por AssetDatabase, creación mediante PrefabUtility, selección y Undo. La UI no referencia ningún tipo Playground; lo impide incluso su asmdef.
- Adaptador: montaje preconfigurado en prefabs; Awake temprano aplica la fachada antes de Start de Playground. No agrega ni reconstruye componentes durante una edición. Esto evita que Undo de una propiedad cree estados técnicos incoherentes.
- Las propiedades usan `nameof(GameItem...)` al construir definiciones y rutas serializadas como datos. No hay exploración masiva con reflection.
- Las modificaciones educativas tienen efecto al iniciar la prueba. No hay edición en vivo en Play Mode; los paneles se deshabilitan durante la prueba.
- Las definiciones y prefabs del taller están fuera del core. El builder pertenece al pack, no a la herramienta. Las rutas fijas sólo pertenecen a este generador y pruebas del pack; catálogo/servicios no dependen de ellas.
- Sustituir backend exige nuevos adaptadores y migrar los componentes de prefabs. No exige rehacer ventana, descriptores educativos ni datos GameItem. El paquete completo incluye un asmdef de adaptador que requiere la copia vendor; core Runtime/Editor siguen separados. No se anuncia como UPM autocontenido redistribuible todavía.

## Estructura
```
Packages/com.dafovi.creajuego/
  Runtime/                     definiciones, fachada y contrato
  Editor/                      ventana UI Toolkit y servicios
  Adapters/Playground/          backend y puente de meta/UI heredada
Assets/ThirdParty/UnityPlayground/  scripts originales adaptados + MIT
Assets/CreaJuegoPacks/Starter/
  Content/                     ocho definiciones, prefabs, sprite y material
  Demo/CreaJuegoPlaygroundDemo.unity
  Editor/                      generador idempotente y build
  Tests/Editor/                pruebas incluyendo entrada/salida de Play Mode
Docs/                          informe, fuentes, parches, logs y resultados
```

## UI y propiedades
EditorWindow UI Toolkit, fondo oscuro, cabecera CreaJuego / LAB, botón verde PROBAR/DETENER, catálogo por categorías a la izquierda y panel TU ELEMENTO a la derecha. Botones con nombre/explicación corta, ayuda contextual al pie. Se coloca junto al SceneView normal; no lo recrea. Es una descripción funcional, no una captura ni una validación visual manual.

`CreaJuego-scene.png` es un render real de la cámara inspeccionado durante el experimento: personaje azul, plataformas verdes, premios amarillos, peligro rojo y meta verde brillante. Se generó con Camera.Render; no incluye HUD superpuesto ni la ventana del Editor y no debe interpretarse como captura de esa ventana.

SerializedObject/SerializedProperty enlazan sliders float/int y PropertyField para bool, string y color. Soporta selección múltiple del mismo tipo; las selecciones mezcladas muestran ayuda. Undo/Redo y prefab overrides de la fachada son normales de Unity. Los controles están definidos por datos; no hay un switch por tipo de elemento en la vista. No se necesita enum educativo todavía y no se implementó uno artificialmente.

Elementos: Jugador (velocidad/salto/vidas), Plataforma (color), Plataforma móvil (velocidad/distancia), Premio (puntos), Peligro (daño/desaparecer), Enemigo (velocidad/distancia/daño), Meta (mensaje), Decoración (color).

## Unity 6.6 Hierarchy opportunities
Se consultaron las referencias XML **de la instalación 6000.6.0f1**, `UnityEditor.HierarchyModule.xml` y `UnityEngine.HierarchyModule.xml`, además del [API público HierarchyWindow](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.Hierarchy.Editor.HierarchyWindow.html) y el [anuncio oficial de 6.6](https://discussions.unity.com/t/unity-6-6-is-now-available/1735357).

APIs públicas comprobadas en esas referencias:
- `Unity.Hierarchy.Editor.HierarchyWindow`: eventos BindView/UnbindView, BindViewItem y registro de handlers.
- `HierarchyViewColumnDescriptorAttribute`: registra método que configura una columna por identificador único. Descriptor con Title, Tooltip, Icon, DefaultWidth, DefaultPriority y DefaultVisibility.
- `HierarchyViewCellDescriptorAttribute`: asocia columna y tipo de handler. Descriptor con BindCell/UnbindCell y BindColumn/UnbindColumn para contenido UI Toolkit.
- `HierarchyGameObjectHandler.GetGameObject(in HierarchyNode)` permite resolver el objeto y leer GameItem/definition. También GetEntityId; evitar conversiones a int.
- `HierarchyNodeTypeHandler`, handlers de escenas/subescenas y HierarchyView permiten representar tipos adicionales. Es una posibilidad real, no una necesidad del MVP.

Siguiente fase: columna opcional «Tipo» con nombre/icono del catálogo; celda de estado «Listo / Falta contenido»; acción contextual «Configurar en CreaJuego» usando Selection y la ventana propia. Categorías y ayudas salen de la misma definición. Nombres amigables ya se asignan al crear.

Conviene evaluar grupos educativos en una vista propia antes de reparentar escenas para agruparlas visualmente. Reducir ruido debe ser opcional y reversible; conservar acceso «Quiero saber cómo funciona». No ocultar componentes mediante hacks o reemplazar masivamente inspectores. No asumir que el callback IMGUI de la jerarquía antigua sirve para la nueva. No usar reflection ni tipos internos SceneHierarchyWindow. No se implementó una columna en este sprint: API investigada y arquitectura preparada, sin ampliar el alcance.

## Ejecutar la demo
1. Abrir esta carpeta desde Unity Hub con **6000.6.0f1** y esperar importación.
2. Abrir `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego > Abrir taller**. Acoplar ventana junto a Escena y Juego.
4. Usar botones del catálogo para añadir elementos en el centro de la vista Escena. Seleccionarlos y moverlos con herramientas normales de Unity.
5. Ajustar propiedades antes de probar; Ctrl+Z/Ctrl+Y para deshacer/rehacer.
6. **PROBAR**, enfocar Juego; flechas y Espacio. Recoger el premio, saltar el peligro y llegar a la puerta verde. **DETENER** vuelve a edición.

`CreaJuego > Preparar demo` genera únicamente recursos ausentes, conserva contenido ya generado y pide el guardado normal de escenas modificadas antes de abrir la demo. No regenera ni sobrescribe un taller existente.

## Añadir elementos y packs
Crear carpeta de pack fuera del core. Crear una definición con `Create > CreaJuego > Definición de elemento`, id único y textos en español. Duplicar un prefab del comportamiento apropiado, asignar definición y enlazarlo en la definición. Ajustar propiedades expuestas (ruta de campo de GameItem, control, etiqueta, ayuda y rango). El catálogo se actualiza al cambiar assets, sin modificar UI. Un comportamiento nuevo sí requiere implementación de IItemBackend y prefab válido; una nueva variante visual no.

## Pruebas y aceptación
Resultado: **7 pruebas, 7 aprobadas, 0 fallos**, ejecutadas en Unity 6000.6.0f1 con gráficos habilitados. Evidencia: `tests-editmode.xml` y `unity-tests-graphics.log`. Las pruebas de tipo UnityTest entran/salen de Play Mode; están en assembly Editor para poder validar también Undo, ventana y persistencia.

| Prueba | Evidencia funcional |
|---|---|
| CatalogHasUniqueIdsAndValidEducationalBindings | Ocho definiciones válidas, id único, prefab/backend y todas las rutas educativas resueltas |
| CreateUndoRedoPreservesPrefabAndSelection | Instanciación conectada al prefab, Selection, deshacer creación y rehacer |
| SerializedMultiEditUndoAndBackendMapping | Cambio múltiple, overrides, Undo/Redo y traducción al backend |
| RealPlaygroundMovementCollectDamageGoalAndPersistence | Entrada real de Input System; Move, Jump, Patrol, premio/puntos, daño, meta y configuración conservada al salir |
| RealPlaygroundHazardCanLoseGame | Trigger, daño letal, destrucción del jugador y mensaje de fin |
| DemoCanBeCompletedWithKeyboardWithoutTeleporting | Recorrido de inicio a meta sólo con flecha derecha y saltos; sin cambiar posición por código |
| WindowShowsCatalogAndBindsEducationalSliders | EditorWindow real, botones de catálogo/Play y modificación efectiva de velocidad/salto desde controles UI Toolkit |

Los contactos de la prueba aislada se provocan colocando al jugador sobre objetos; la prueba de recorrido continuo independiente evita esa simplificación. La prueba de ventana inspecciona y acciona controles, pero no verifica estéticamente cada píxel. Las primeras ejecuciones fallidas se conservan como evidencia de diagnóstico, no como resultado final.

La prueba manual de escritorio no se ha realizado: esta sesión no expone control nativo de Unity. El skill de computer-use exige node_repl, que no está disponible; el controlador CUA expuesto tiene apps nativas deshabilitadas. No se presenta una prueba automatizada como manual.

Checklist manual pendiente: abrir ventana; crear jugador/plataforma/premio/peligro/meta; ajustar velocidad y salto; ejecutar; completar con teclado; salir; comprobar escena y Undo. Conservar este punto como pendiente de aceptación de taller aunque las pruebas automáticas pasen.

Estado de criterios: proyecto importado/compilado en 6.6; investigación y licencia verificadas; catálogo, ocho elementos, ventana española y propiedades funcionales; desacoplamiento por assemblies; Undo, demo y Play Mode comprobados; packs separados; Hierarchy investigada; documentación creada. **Pendiente: checklist manual y evaluación del tacto de controles por una persona.**

Compilación adicional **Windows Standalone x64 Development: correcta**, marcador `CREAJUEGO_PLAYER_BUILD_OK`, salida 0 en `unity-player-build.log`. Artefacto local `Builds/CreaJuego/CreaJuego.exe` junto con su carpeta de datos y UnityPlayer.dll. No se ha hecho una sesión manual del ejecutable; la ejecución jugable comprobada corresponde a Play Mode.

Para repetir: Test Runner > EditMode > CreaJuego.Starter.Tests, con gráficos habilitados. En consola usar Unity con `-batchmode -projectPath <carpeta> -runTests -testPlatform EditMode -testFilter CreaJuego.Starter.Tests -testResults <resultado.xml> -logFile <log>`. No añadir `-quit` a runTests ni `-nographics` a la prueba de ventana.

## Respuestas del experimento
**A. ¿Playground sigue siendo buena base en 6.6?** Sí como backend acotado para validar talleres, con ADAPT y pruebas; no conviene adoptar sus inspectores globales ni acoplar la herramienta a su UI/tags. No se ha demostrado la compatibilidad funcional de toda la biblioteca.

**B. ¿Se puede sustituir sin rehacer UI?** Sí. La UI y servicios sólo conocen GameItem, definiciones e IItemBackend. Haría falta migrar prefabs y reemplazar el assembly de adaptador; esa migración no es automática.

**C. ¿Siguiente paso?** Validación observada del recorrido y la ventana con participantes; después herramientas de colocación/snapping y columna de identificación educativa, conservando Undo. Mejorar control del personaje/reinicio antes de añadir muchas reglas.

## Deuda, riesgos y próximo sprint
Playground conserva búsqueda global de UIScript y tags Player; se encapsulan, pero no son una base escalable para múltiples sesiones o jugadores. Jump permite reiniciar el salto tocando lados de objetos sin tag: mejorar comprobación de suelo antes de un taller real. El movimiento sigue siendo por fuerzas y necesita pruebas de tacto/control con participantes. La plataforma móvil no tiene un sistema propio de transporte de pasajeros. No hay recuperación al caer fuera del nivel, mando/táctil, guardado asistido, snapping, arrastre de catálogo, búsqueda, packs activables ni lógica visual. El puente UIScript conserva uGUI y referencias heredadas; no es la UI Toolkit del Editor. Gizmos originales carecen de recursos de flechas porque no se importó arte upstream.

Próximo sprint: sesión observada con participantes y validación visual manual; cerrar movimiento/suelo/reinicio, añadir colocación y snapping, duplicado/borrado con Undo y columna educativa opcional. Después introducir un manifiesto de pack y contratos pequeños de eventos «cuando/entonces», sin construir todavía un lenguaje visual completo. Un modo progresivo puede mostrar definición/comportamiento y seleccionar los componentes técnicos sin alterar el modo inicial.
