# CreaJuego — Dirección visual
Unity Editor 6000.6.0f1 (f7f8ed4d1e24). Sprint del 7 de septiembre de 2026.

## Alcance
Aplicar la dirección del mockup a la ventana UI Toolkit existente: paneles claros, selección azul, Jugar verde y lenguaje pedagógico. No se cambian mecánicas, Playground, adaptadores, reglas Runtime ni distribución global de ventanas. No se hace commit.

La validación se realiza dentro de Unity Editor. No se invoca BuildPipeline.BuildPlayer, no se generan ejecutables ni se copian builds. Los ejecutables históricos del sprint anterior no forman parte de esta validación.

## Implementación
- Cabecera CreaJuego y guía discreta: Añadir → Seleccionar → Personalizar → Jugar. El paso actual se marca con subrayado y negrita, además de color; no es un asistente obligatorio.
- Añadir al juego: cuadrícula de seis tarjetas con los iconos existentes, signo +, nombres, tooltips de categoría/ayuda, foco, hover y estado pulsado.
- Mi juego: filas compactas, icono, nombre de presentación, selección azul con marca ✓. Se conserva la selección múltiple desde Unity; un clic en la lista no crea.
- Propiedades: ficha de contexto con icono, nombre y descripción breve del descriptor. Grupos, ayudas, edición serializada y acciones secundarias se mantienen.
- Jugar: un solo botón, junto al preflight en la barra inferior; pasa a Detener durante Play.
- Estados vacíos: el listado explica cómo comenzar y el panel de propiedades pide seleccionar.
- Botón Ver escena: abre/enfoca el SceneView real mediante EditorWindow. No mueve ni acopla ventanas y sólo actúa al pulsarlo.

## Preflight: presentación y reglas
SceneService.Validate y TryPlay siguen siendo la autoridad. WorkshopPresentation traduce los resultados para participantes sin importar tipos de Playground.

Los checks técnicos no se muestran literalmente. Los fallos de preparación reciben una explicación educativa; si Preparar escena no basta, se indica pedir ayuda al facilitador. El detalle técnico sigue disponible en SceneService para diagnóstico.

Jugador y Meta reflejan los checks originales. Plataforma y Algo con qué interactuar son indicadores orientativos: si faltan se muestran como opcionales. La lógica anterior no exige esos elementos y este sprint no introduce nuevos bloqueos. Cada fallo técnico original sigue impidiendo Jugar, incluso si el catálogo parece completo.

La lista y el preflight se actualizan con eventos de Unity, sin añadir polling. ObjectChangeEvents.changesPublished cubre modificaciones deshacibles externas, incluidos cambios estructurales; no reconstruye los controles de propiedades. [Referencia pública de Unity 6.6](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/ObjectChangeEvents-changesPublished.html).

## Estilo y accesibilidad
Tokens USS centralizados: fondo, panel, texto, ayuda, azul, éxito, aviso y peligro. Selectores limitados al contenedor .creajuego para no alterar Unity.
- Texto base 14 px; ayudas 12 px; títulos 17–25 px.
- Botón principal 48 px de alto; filas al menos 32 px; foco con borde visible.
- Scroll independiente en catálogo, listado, propiedades y mensajes del preflight.
- Sliders con entrada numérica, toggles, texto multilínea y controles nativos enlazados conservados.
- Contrastes calculados para los colores declarados: texto/panel 14,91:1; ayuda/panel 7,58:1; azul/panel 6,01:1; blanco/CTA verde 5,87:1; aviso/fondo 6,38:1. Es una comprobación de tokens, no una auditoría visual completa ni una certificación de accesibilidad.
- No se depende sólo de color: + para creación, ✓ en selección/checks, texto para avisos, subrayado en el paso activo.

## Diferencias respecto al mockup
La referencia sirve de dirección, no de captura del producto:
- Se conserva una EditorWindow con catálogo/lista a la izquierda y propiedades a la derecha. SceneView vive fuera de esa ventana. No se incrusta ni se recrea; no se obtiene automáticamente el layout de tres columnas del mockup.
- El usuario decide dónde acoplar CreaJuego y SceneView. No se reemplaza ni se oculta Hierarchy.
- Se mantienen los iconos geométricos del pack y el arte provisional 2D. No se generan personajes 3D, ilustraciones o un logo nuevo.
- Seis tarjetas en cuadrícula adaptable; no se añaden buscador, drag-and-drop ni jerarquía nueva.
- La apariencia y los campos disponibles siguen viniendo del catálogo existente. La ficha no añade propiedades o mecánicas sólo por aparecer en el mockup.

## Pruebas y revisión
La primera ejecución tras aplicar la presentación mantuvo 28/28 tests.
Se añaden cuatro casos para estructura/estados vacíos/flujo, CTA único, sincronización y Undo del preflight, conservación de todos los bloqueos, sugerencias opcionales y controles accesibles en tamaños 680×500 y 1100×760.
Las pruebas anteriores entran y salen de Play Mode dentro del Editor, ejercitan teclado, victoria/derrota, Undo y persistencia; no se presentan como builds ni como una suite PlayMode independiente.

Revisión manual propia: pendiente. Se intentó iniciar Computer Use, se reinició el kernel y se reintentó; ambos intentos terminaron con «trusted Node process exited unexpectedly». No fue posible observar ni capturar la ventana real. Los tests sí crean la EditorWindow y manipulan sus controles, pero esto no sustituye una revisión visual.
Capturas reales: no disponibles. La imagen aportada por el usuario es una referencia, no evidencia de implementación.

## Revisión posterior dentro del Editor
1. Abrir CreaJuego > Abrir taller y mantener SceneView visible; ajustar el acoplamiento manualmente.
2. Revisar ventanas compacta y amplia: recortes, foco, contraste real del tema de Unity, sliders, toggle y scroll.
3. Añadir dos plataformas; seleccionar cada una en Mi juego; comprobar selección, propiedades, Duplicar y Eliminar.
4. Quitar Meta, leer la ayuda y deshacer. Jugar debe volver al estado disponible sin cambiar las reglas.
5. Recorrer la demo con A/D o flechas y Espacio; Detener y comprobar persistencia.
6. Revisar con principiantes si las tarjetas comunican creación y las filas selección antes de explicarles el modelo.

## Próximos ajustes
La prioridad es la revisión visual real y después observar una sesión breve con principiantes. Decidir con esa evidencia si merece la pena un layout de taller opcional con paneles separables. No avanzar aún a nuevas mecánicas.

## Resultado técnico final
- 32/32 pruebas aprobadas: 0 fallos, 0 omitidas. XML local Docs/visual-tests.xml, finalizado 2026-09-08 02:11:49 UTC.
- Proyecto compilado y EditorWindow creada por los tests en Unity 6000.6.0f1. Sin errores C# ni advertencias USS detectadas en Docs/visual-unity.log. No se inspeccionó visualmente la Console del Editor abierto.
- Los cuatro nuevos tests pasan junto con los 28 existentes. Se corrigió un estado positivo obsoleto tras eliminar Meta externamente mediante la suscripción pública a ObjectChangeEvents.
- Validación en .spike/V1Validation para no guardar ni reemplazar la escena abierta del usuario. Integración con hashes idénticos a la copia validada; ningún destino presentaba cambios concurrentes.
- Archivos: [inventario del incremento](Visual-Changed-Files.txt). Además se actualizaron este informe y README.
- Se conservan sin cambios Runtime, adaptadores, Playground, SceneService, prefabs, escena demo y ProjectSettings.
- Cero Player Builds, ejecutables nuevos o copias a Builds. No se hizo commit ni push.
- Revisión visual y capturas reales pendientes por el fallo de Computer Use; no se declara el cierre manual del sprint.

Nota de Console: el log conserva el aviso CS0252 preexistente en V11UxTests.cs:40 (comparación de referencias en una prueba). No es un error de compilación ni un fallo de los 32 tests; no se modificó ese caso durante este sprint.
