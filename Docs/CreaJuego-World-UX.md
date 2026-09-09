# CreaJuego — ajustes de experiencia y escenario

Fecha: 9 de septiembre de 2026. Unity 6000.6.0f1 (f7f8ed4d1e24).
Validación en Editor, sin generar ejecutables ni ejecutar BuildPipeline.BuildPlayer.

## Cambios y uso

1. **Modo oscuro**: interruptor en cada una de las tres ventanas. Cambia todas inmediatamente, sin reconstruir sus controles enlazados. Preferencia local persistente en EditorPrefs; el modo claro sigue disponible.
2. **Miniaturas reales**: catálogo, Mi juego y encabezado de Propiedades muestran el sprite personalizado o apariencia elegida; en el catálogo se usa el del elemento seleccionado cuando corresponde. Sin selección se utiliza la apariencia del contenido predeterminado. El icono antiguo queda como último respaldo.
3. **Escenario → Fondo**: selecciona un Sprite (también se puede arrastrar desde Project). Cambiarlo reemplaza el fondo existente. Quitar fondo admite Undo. La herramienta incluye objetos inactivos al buscar y rechaza varios fondos manualmente duplicados en lugar de borrarlos. Se escala uniformemente para cubrir la cámara: puede recortarse si su proporción es diferente; acompaña a la cámara, sin paralaje ni colisiones.
4. **Encuadre**: línea azul alrededor del área de cámara y sombreado exterior en SceneView 2D. El sombreado no captura el ratón: se puede seguir colocando contenido fuera del encuadre. En perspectiva se dibuja sólo el contorno. Configurar editor activa SceneView 2D.
5. **Configurar editor**: conserva el layout manual y establece Game a 1280 × 720, proporción 16:9. Usa PlayModeWindow.SetCustomRenderingResolution, API pública. No modifica el archivo .wlt.
6. **Escenario → Añadir límite invisible**: crea una pared sin imagen, en el centro de trabajo de SceneView. Aparece en Mi juego. Seleccionarla permite editar Anchura y Altura desde Propiedades; moverla se hace con SceneView. Se puede usar para lados, suelo o techo. El dibujo rosa sólo existe en Editor. Crear, cambiar tamaño y eliminar admite Undo.
7. **Cinemachine**: seguimiento suave del objeto lógico Jugador, independiente de su sprite. Se prepara con Preparar escena y antes de Jugar desde CreaJuego. La demo incluida ya contiene la cámara preparada, también para el Play normal de Unity. Crear un jugador cuando ya existe el seguimiento actualiza su destino. Start vuelve a resolver el personaje.

No se cambia el catálogo de siete elementos ni el backend Playground de movimiento, combate o reglas.

## Arquitectura

- Runtime: WorkshopBackground (presentación del fondo) e InvisibleBoundary (identificación del límite).
- Adaptador separado CreaJuego.Cinemachine: WorkshopCameraRig, destino del seguimiento.
- Editor: WorldAuthoringService crea/configura objetos usando Undo y APIs públicas. WorldToolsView y EducationalPropertiesView usan servicios y enlaces SerializedObject.
- WorkshopTheme y USS compartido aplican el tema a las tres ventanas.
- WorkshopSceneGuides dibuja ayudas con SceneView.duringSceneGui y DrawGizmo.

Cinemachine usa su Brain, CinemachineCamera y CinemachinePositionComposer. La copia validada resuelve **com.unity.cinemachine 6.6.0, source builtin**. La consulta inicial al registro devolvió 3.1.7, pero no era la versión utilizada por este Editor. Manifest, dependencia del core y lock declaran la versión realmente instalada. Splines se resuelve a 2.9.0.

Si la escena contiene una cámara Cinemachine ajena a CreaJuego, el servicio no añade otra que compita con ella. Ese montaje avanzado conserva su configuración y requiere que su autor asigne el seguimiento.

Fuentes:
- [Position Composer, Cinemachine 6.6](https://docs.unity.cn/Packages/com.unity.cinemachine%406.6/manual/CinemachinePositionComposer.html).
- [API pública de resolución de Game](https://docs.unity3d.com/ja/current/ScriptReference/PlayModeWindow.SetCustomRenderingResolution.html).
- Verificación de firmas contra UnityEditor.xml de la instalación 6000.6.0f1 y código del paquete resuelto en Library/PackageCache.

## Validación

Se emplea una copia aislada .spike/V1Validation para no cerrar el Editor ni alterar escenas de trabajo abiertas.
La importación/compilación inicial de los cambios terminó con código 0.
Resultado final: **60/60 pruebas pasan, 0 fallos**, incluida ejecución real en Play Mode desde Unity Test Framework. Evidencia local: world-final-tests.xml y world-final-tests.log. Los 35 archivos de código, metadatos, configuración y demo se sincronizaron al proyecto principal con comprobación SHA256 contra el estado inicial y respaldo en .spike/WorldBeforeSync.

Pruebas nuevas:
- tema compartido y persistente al recrear la ventana;
- cambio de miniatura al elegir un sprite personalizado;
- fondo único, reemplazo/eliminación y Undo/Redo;
- cobertura del encuadre y persistencia de fondo/límites;
- creación y tamaño de límites con Undo/Redo;
- resolución efectiva 1280 × 720;
- configuración idempotente de cámara y destino lógico;
- Undo/Redo del montaje de cámara;
- seguimiento real al entrar en Play Mode y colisión física con una pared invisible.

Se ajustan fixtures anteriores para no depender de la apariencia inicial elegida en la demo ni del enemigo colocado libremente sobre la ruta de una prueba de daño. La expectativa de icono geométrico se actualiza al sprite real.

La revisión manual visual está pendiente: el controlador de Windows falla al inicializar su proceso Node. No se presenta la prueba automatizada de UI Toolkit como una inspección visual manual.

## Comprobación manual recomendada

Abrir CreaJuego y alternar Modo oscuro desde cada ventana. Seleccionar un personaje y cambiar su apariencia: revisar miniaturas. En Escenario asignar, cambiar y quitar Fondo; usar Ctrl+Z/Ctrl+Y. Crear límites, editar anchura/altura, moverlos, eliminar y deshacer. Configurar editor y comprobar Game 16:9 y sombreado en SceneView 2D. Preparar escena, jugar, recorrerla en ambas direcciones y verificar seguimiento y límites. Salir de Play, guardar/reabrir una copia de la escena y comprobar la configuración.

## Límites de este sprint

- Fondo único por escena de taller; cubre la cámara, no es un paisaje con paralaje.
- Los límites bloquean físicamente al personaje; no restringen la cámara mediante Confiner.
- Encuadre pensado para cámaras ortográficas 2D alineadas con el plano XY.
- No se personalizan sistemas Cinemachine externos ni montajes multicámara.
- El paquete Cinemachine emite avisos de un asmref de ejemplo HDRP sin destino y clases parciales de su Editor durante la recarga; no son errores de compilación de CreaJuego y no se parchea el paquete.
- Pendiente revisión visual con una persona en el Editor y ajuste fino del espacio de las ventanas pequeñas.

