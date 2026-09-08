# CreaJuego — Workshop layout / ventanas separadas

Unity 6000.6.0f1, revisión f7f8ed4d1e24. 8 de septiembre de 2026.

## Problema
La ventana anterior reservaba simultáneamente catálogo y propiedades, incluso sin selección. Sus dos columnas requerían demasiado ancho y desplazaban al SceneView.

## Arquitectura final
Tres EditorWindow, sin otro modelo de datos:
- **CreaJuegoWindow / CreaJuego**: catálogo, Mi juego, selección, Duplicar, Eliminar, Encontrar y ayuda. Mínimo 280×400; ancho recomendado 280–380.
- **CreaJuegoPropertiesWindow / Propiedades**: ficha y propiedades educativas. Mínimo 300×250; ancho recomendado 300–420.
- **CreaJuegoPlayBarWindow / Jugar**: preflight y único CTA JUGAR/DETENER, preparación explícita y estado. Mínimo 420×160; recomendación 650 px o más de ancho, debajo del SceneView.

La guía horizontal se sustituye por una línea «Paso N de 4 · …». No dirige ni bloquea el flujo.
CreaJuego no contiene propiedades detalladas; Propiedades no contiene catálogo, listado ni acciones de creación.
El SceneView real sigue separado y puede ocupar el centro. No se toca Hierarchy.

## Reutilización y estado compartido
EducationalPropertiesView es una única vista UI Toolkit reutilizable, extraída del código existente. Usa GameItemDefinition y EducationalProperty para crear los controles, y SerializedObject/SerializedProperty para editar los GameItem seleccionados. Conserva mixed values, visibilidad condicional, color, ayudas y límites. Libera binding y suscripción al cerrar la ventana.

Las ventanas leen Selection, no mantienen copias de GameItem. Selection.selectionChanged actualiza ficha, filas y acciones. Undo/Redo y cambios de escena refrescan la presentación. El listado y preflight conservan los callbacks de jerarquía y ObjectChangeEvents. El panel de propiedades no se reconstruye por cada edición de un campo.
No se añadió polling. La barra conserva la actualización de ayuda durante Play que ya existía en V1.

ItemService y SceneItemService siguen creando/duplicando/eliminando/seleccionando. Encontrar encuadra la selección sólo al pulsarlo. Seleccionar una fila no mueve la cámara.

## Decisión del preflight
Se eligió una tercera ventana porque permite una franja horizontal bajo el SceneView y evita comprimir mensajes y un CTA ancho dentro del catálogo de 280 px. Tiene un solo propietario de Jugar; el catálogo y Propiedades no incluyen copias.
Abrir taller abre las tres. La barra puede cerrarse o abrirse por separado desde el menú; si se cierra, se recupera con Abrir barra de juego o Abrir taller.
Se reutilizan SceneService y WorkshopPresentation. No se cambian bloqueos ni reglas. Plataforma/interacción siguen siendo sugerencias donde antes no eran requisitos.

## Menús
- CreaJuego > Abrir taller: abre/reutiliza SceneView, Propiedades, Jugar y CreaJuego; enfoca CreaJuego.
- CreaJuego > Abrir elementos.
- CreaJuego > Abrir propiedades.
- CreaJuego > Abrir barra de juego.
- CreaJuego > Preparar escena (nombre normalizado; misma operación existente).
- Se conserva Preparar demo.

No se invoca Abrir taller al arrancar el Editor. No se cierran ventanas ajenas, no se escriben posiciones ni se aplica un layout al abrir.

## Investigación de API pública — Unity 6.6
[EditorWindow.GetWindow](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/EditorWindow.GetWindow.html) permite abrir/reutilizar ventanas y ofrece desiredDockNextTo para solicitar acoplamiento con tipos existentes. No especifica una división izquierda/centro/derecha con proporciones.

[EditorWindow](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/EditorWindow.html) expone Focus, Show, position, minSize y maxSize. SaveChanges guarda contenido de una ventana, no un layout global. No se encontró en la API documentada una operación pública para construir y guardar de forma fiable el árbol completo de divisiones requerido.

[El manual de distribución de Unity 6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/CustomizingYourWorkspace.html) documenta mover pestañas manualmente y guardar/cargar layouts mediante el menú Layout.

**Layout automático no implementado por depender de APIs internas** para conseguir la distribución exacta con el enfoque habitual. Esta es una decisión basada en la superficie pública documentada consultada, no una afirmación sobre cualquier futura API de Unity.
Actualización posterior: **Configurar editor** carga el archivo manual oficial mediante el método público EditorUtility.LoadWindowLayout. La conclusión anterior se limita a construir la distribución desde cero; fue incompleta respecto a cargar un archivo existente. No se usa reflection, DockArea/SplitView internos, escritura de archivos .wlt ni comandos ocultos.

## Layout oficial del taller: CreaJuego Taller
Ya existe una distribución creada y validada manualmente por el responsable del taller. La copia canónica está en [Layouts/CreaJuego-Taller.wlt](Layouts/CreaJuego-Taller.wlt); consulta [cómo cargarla, volver al layout personal y sus límites de portabilidad](Layouts/README.md).
Se preserva exactamente el archivo producido por Unity, sin reconstrucción ni edición interna.

La disposición guardada es CreaJuego a la izquierda, Propiedades en la columna intermedia, SceneView en el área grande derecha y Jugar abajo a todo el ancho. Hierarchy permanece como otra pestaña del grupo izquierdo; Game no está guardada. Esto difiere del esquema conceptual SceneView-centro/Propiedades-derecha y se conserva deliberadamente porque la prioridad es respetar el layout real del usuario.

La carga puede hacerse desde CreaJuego > Configurar editor o Layout > Load layout from file de Unity. Abrir taller no carga ni modifica ese layout. El layout y las ventanas siguen siendo independientes.

## Validación
**35/35 tests aprobados**, 0 fallos, 0 omitidos. XML local Docs/split-tests.xml; cierre 2026-09-08 13:02:20 UTC.
Se mantienen los 32 casos anteriores, adaptando las búsquedas de controles a su ventana real. El helper de tests sólo abre/cierra ventanas; no fusiona ni mueve sus árboles visuales.
Tres nuevos casos:
1. Abrir taller idempotente, ventanas correctas, responsabilidades separadas y CTA único; no cambia la posición de una ventana ya abierta.
2. Crear, seleccionar desde la lista y Selection, editar puntos reales, duplicar, eliminar, Undo/Redo y cambiar de escena con ambas ventanas abiertas.
3. Paneles de 280 y 300 px, barra horizontal y controles dentro de sus límites.

La suite conserva pruebas de multi-edit, visibilidad condicional, Undo, persistencia y ejecución real entrando/saliendo de Play Mode dentro del Editor. No se ejecuta una suite PlayMode independiente.
Sin errores C# ni avisos de compilación/USS detectados en el log final. Se corrigió la comparación de referencias ambigua del test V11 usando ReferenceEquals; su aserción se conserva.
No se generaron builds standalone ni se invocó BuildPipeline.BuildPlayer.

## Revisión manual y capturas del sprint de separación
Revisión propia pendiente en aquel sprint. El intento de Computer Use falló al iniciar con «windows sandbox failed: helper_unknown_error: setup refresh had errors». No se pudo observar ni acoplar realmente las ventanas. No hay capturas reales nuevas.
Las pruebas sí abren las EditorWindow, cambian sus tamaños y accionan controles. Eso no certifica aún la comodidad del layout acoplado.
Actualización posterior: el usuario creó y validó manualmente la distribución que ahora se conserva como CreaJuego Taller. No se realizó una nueva revisión propia ni una prueba de carga de la copia canónica.

## Límites y siguiente paso
La carga del archivo oficial requiere una acción explícita del facilitador desde Layout. El mínimo de una ventana acoplada depende también de Unity y del monitor. La barra necesita más alto si se quiere leer muchos avisos simultáneamente sin scroll.
El catálogo conserva los assets e iconos existentes. No hay nuevas mecánicas ni cambios en Runtime, adaptadores o Playground.
Próximo paso: probar la carga del archivo canónico y el regreso al layout personal en otro monitor; después una sesión breve con principiante.

## Control de cambios
Antes del sprint se hizo el commit solicitado de la dirección visual anterior: e0fe108. No se hizo push.
El nuevo sprint queda sin commit/push. La escena personal Assets/Scenes se conserva fuera de los cambios.
