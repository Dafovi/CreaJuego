# CreaJuego V1.1 — Ajustes de UX
Fecha: 7 de septiembre de 2026 (Colombia). Unity 6000.6.0f1, revisión f7f8ed4d1e24.

## Problema observado
La revisión inicial comunicada por el responsable del taller detectó que el catálogo parecía una lista de objetos existentes. También resultaban menos intuitivos «Probar» y «Resistencia». Este sprint ajusta esas ambigüedades; no añade mecánicas ni pretende resolver la identidad visual.

## Añadir al juego / Mi juego
La columna izquierda conserva el tema USS y se divide en dos zonas con desplazamiento independiente:
- **AÑADIR AL JUEGO**: seis tarjetas con «+ Añadir …»; cada activación crea mediante ItemService, con sus guardas y Undo.
- **MI JUEGO**: sólo GameItem de la escena activa, incluidos los inactivos. Activar una fila únicamente cambia Selection. El resaltado pertenece a las filas, nunca al tipo del catálogo.
- Los nombres repetidos reciben números de presentación; el nombre técnico no cambia. Los nombres personalizados se conservan. La numeración puede cambiar al eliminar o reordenar objetos: no constituye un identificador persistente.
- Crear, duplicar y eliminar actualizan la lista inmediatamente. Los cambios externos usan hierarchyChanged; Undo/Redo y cambio de escena tienen callbacks propios. Selection sincroniza el resaltado y las propiedades. No se añadió polling.
- El panel de propiedades mantiene sus enlaces mientras se actualiza la lista. La primera ejecución descubrió que reconstruirlo ante hierarchyChanged invalidaba controles activos; se corrigió y se mantuvo el test de visibilidad existente.

## Arquitectura y archivos
SceneItemService (Editor) enumera, presenta y selecciona. CreaJuegoWindow utiliza este servicio y el ItemService existente; no importa Playground.
WorkshopInput (adaptador) convierte Keyboard en una sola dirección. PlaygroundAdapter la entrega a Move.
Los descriptors GameItemDefinition, SerializedObject, preflight, prefabs y Undo existentes se conservan.
Inventario exacto del incremento: [V1.1-Changed-Files.txt](V1.1-Changed-Files.txt). Incluye código, metadata, HUD, demo y pruebas; no incluye esta documentación ni README.

## Input
A o izquierda: -1. D o derecha: +1. Teclas equivalentes se combinan con OR; direcciones opuestas se cancelan. W/S no cambian el movimiento horizontal ni provocan salto. Espacio conserva Jump.
Move recibe un delegado opcional no serializado, movementSource. Si no está asignado conserva el KeyGroup original. No se duplica Rigidbody ni AddForce; el FixedUpdate original aplica la única fuerza.
El adaptador añade referencia a Unity.InputSystem. El core y la UI siguen sin depender de sus teclas.
Único cambio incremental de Playground: Move.cs, bajo MIT. [Patch](Playground-V1.1-Compatibility.patch) y [hashes antes/después](Playground-V1.1-Source-Inventory.csv). Las adaptaciones anteriores siguen vigentes.

## Terminología
«JUGAR» / «DETENER»; «Puntos de vida», grupo «VIDA», HUD «Vida:» y contador existente. La ayuda explica daño y fin al llegar a cero, sin prometer reaparición.
«Muévete con A/D o las flechas y salta con Espacio». Se actualizaron los textos del pack, servicios y generadores para conservar coherencia al crear recursos ausentes.
Se mantienen «Preparar escena» y las referencias a la vista Escena cuando indican dónde manipular un objeto. Los nombres internos health y las reglas de daño no cambian.

## Verificación
Base: 24/24 pruebas aprobadas antes de modificar.
La suite conserva esos casos y añade selección sin creación, acciones reales del catálogo/lista, sincronización de duplicación/eliminación/Undo/Redo/cambio de escena, nombres de presentación, input combinado, microcopy y movimiento físico con ambos esquemas.
Los logs y XML son artefactos locales excluidos de Git; no se publican rutas de máquina ni información de licencia de Unity.
Resultados finales y build: ver registro de cierre al final de este documento.

## Revisión manual
El usuario indica que realizó una revisión manual inicial de V1; se toma como origen de los problemas, no como validación de V1.1.
La revisión propia de V1.1 por control de ventanas no pudo realizarse: el kernel de Computer Use falla al iniciar (trusted Node process exited unexpectedly). La prueba automatizada activa botones UI Toolkit mediante NavigationSubmitEvent y verifica sus efectos; eso no sustituye una inspección visual.
No se realizó una prueba con principiantes ni se afirma que la separación resulte comprensible para ellos.

## Límites y deuda
- Dos listas sencillas, sin búsqueda, drag-and-drop, organización o jerarquía propia.
- Lista de escena activa; no representa escenas aditivas ni recursos del proyecto. Preflight sigue limitando el taller a una escena.
- Mostrar objetos inactivos permite encontrarlos, pero no añade un control educativo de activación.
- Los HUD de escenas personalizadas con textos copiados u overrides anteriores no se migran automáticamente. Se actualizan la demo guardada y SceneServices.prefab; no se guarda ni reemplaza la escena abierta del usuario.
- Plataforma móvil continúa experimental/oculta; no se implementaron audio editable, nodos, inventario ni nuevas reglas.
- Sigue pendiente la inspección visual propia y la sesión con una persona sin experiencia.

## Qué observar en la prueba con principiante
1. Pedir que añada dos plataformas sin indicar el botón. Observar si anticipa que crea.
2. Pedir que seleccione la primera y cambie su color. Comprobar si usa Mi juego y evita duplicaciones accidentales.
3. Observar si los números bastan para distinguir objetos y si necesita Encontrar.
4. Pedir jugar, detener y cambiar puntos de vida. Preguntar qué espera que ocurra al llegar a cero.
5. Dejar que elija A/D o flechas; comprobar que descubre Espacio sin suponer W.
6. Quitar la meta, pulsar Jugar y observar si puede resolver el aviso de preflight.
7. Registrar errores, dudas y necesidad de ayuda, sin explicar anticipadamente el modelo de las dos listas.

## Registro de cierre técnico
- 28/28 pruebas aprobadas; 0 fallos, 0 omitidas. Ejecución final local: 7 septiembre de 2026, 20:53 (Colombia). XML: Docs/v11-tests.xml (excluido de Git).
- Se conservan los 24 casos existentes y se añaden cuatro. El caso físico valida A, D, izquierda, derecha, parejas equivalentes, W sin salto y D+Espacio con salto. El test síncrono valida exactamente la dirección combinada y cancelación.
- Una primera aserción del test físico leía Keyboard.current después de esperar, mezclando comprobación de desplazamiento con estado de un dispositivo potencialmente distinto. Se retiró esa comprobación redundante; la magnitud exacta continúa cubierta en el test síncrono, y se mantienen todas las aserciones de movimiento.
- Build Windows: Success, Unity 6000.6.0f1. Ejecutable local: Builds/CreaJuegoV11/CreaJuego.exe. Log: Docs/v11-build-unity.log (excluido).
- Validación realizada en .spike/V1Validation para respetar la escena sin guardar abierta en el proyecto original.
- Integración con comprobación SHA256: ningún destino había cambiado respecto a la base; los archivos integrados coinciden con los validados. Copia previa en .spike/V11BeforeSync.
- Patch incremental de Move verificado con git apply --check --reverse --ignore-whitespace sobre la copia validada.
- No se hizo commit ni push.
- Cierre visual pendiente: las pruebas automáticas y el build no certifican claridad para principiantes.
