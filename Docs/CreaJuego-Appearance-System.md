# CreaJuego — Apariencias y pulido visual

Actualizado 2026-09-09. Unity **6000.6.0f1** (`f7f8ed4d1e24`). Complementa [Combate MVP](CreaJuego-Combat-MVP.md).

## Arquitectura

GameItem conserva datos educativos de autoría. `AppearanceDefinition` define ID estable, nombre, tipo compatible, Sprite/miniatura, controller/perfil opcionales, escala, offset y orientación. `ContentPackDefinition.appearances` registra las opciones; el primer elemento compatible es el default del generador de contenido. `GameItemDefinition.appearancePack` elige el pack. Los prefabs guardan su apariencia inicial; cambiar el orden del pack no reescribe instancias existentes.

`ItemVisual` observa el movimiento de la raíz y el soporte expuesto por `IVisualMotionState`. Su hijo Visual contiene el SpriteRenderer y Animator opcional. Rigidbody2D, colliders y backend permanecen en la raíz. La interfaz no conoce clases de Playground.

Archivos centrales: Runtime/AppearanceDefinition.cs, AnimationProfile.cs, ItemVisual.cs, GameItem.cs, ContentPackDefinition.cs; Editor/ItemAppearance.cs, AppearanceSelector.cs, EducationalSelection.cs y EducationalPropertiesView.cs, dentro de `Packages/com.dafovi.creajuego`.

## Política de color vigente

**Apariencia preparada o Sprite personalizado → Color.white.** `ItemVisual.BaseColor` concentra la regla usada por Visual y el adapter. El color educativo de placeholders se oculta cuando el elemento tiene arte preparado/personalizado.

`tint` se conserva como dato de compatibilidad; no tiñe el renderer visible con arte. El renderer antiguo deshabilitado puede conservar ese valor, sin dibujarlo. Los flashes de daño son temporales, guardan el color visible y lo restauran exactamente. Esta política sustituye la conservación visual del tint implementada en el sprint anterior.

## Selección y propiedades

SceneView puede seleccionar el hijo Visual en lugar de la raíz. `EducationalSelection` resuelve GameObject o Component seleccionado hasta su GameItem padre y elimina duplicados. No cambia forzosamente Selection. Las ventanas de Propiedades y Elementos comparten esa resolución.

Propiedades escucha `Selection.selectionChanged`, cambios públicos de jerarquía/objetos, escena, Undo/Redo y Play Mode. Los avisos de cambios de objetos sólo reconstruyen el panel si cambia la selección relevante o su uso de arte, preservando el control activo. Cada reconstrucción hace Unbind, descarta el SerializedObject anterior y enlaza el nuevo. No usa polling. Las pruebas simulan tanto selección de raíces como de Visual y de su SpriteRenderer; la revisión de clics reales en SceneView queda pendiente.

## Catálogo visual

**30 apariencias**, con arte Tiny Dungeon y Pixel Platformer CC0 del mismo autor. Se conservan los ID/GUID de las antiguas definiciones para mantener referencias.

| Tipo | Opciones |
| --- | --- |
| Jugador (2) | Brujita, Exploradora |
| Plataforma (3) | Piedra clara, antigua, oscura |
| Premio (3) | Moneda dorada, Gema azul, Llave dorada |
| Peligro (2) | Pinchos, Trampa roja |
| Meta (2) | Puerta antigua, Puerta misteriosa |
| Enemigo (4) | Murciélago, Fantasma, Escarabajo, Seta |
| Decoración (14) | Mesa, Taburete, Cuenco, Lápida, Estatua, Cofre cerrado/abierto/alto, Hierba, Planta alta, Pino, Cactus, Hongo pequeño/alto |

Decoración es el séptimo elemento visible del catálogo. No tiene collider, premio, daño ni ataque. La tarjeta Cactus sigue siendo decoración sin daño: los tipos pedagógicos determinan las reglas, no los píxeles.

El selector de Propiedades tiene miniaturas, búsqueda por nombre y scroll con altura limitada. `Usar otra imagen…` acepta un Sprite asset y desactiva la animación. Elegir una tarjeta restaura la apariencia preparada y limpia el campo personalizado. Undo/Redo y overrides de prefab se mantienen.

## Compatibilidad y migración

El generador explícito `CombatMvpBuilder.Run` llama a `AppearancePackBuilder.Run`, actualiza definiciones y prefabs, y añade los componentes del combate. No se ejecuta automáticamente al abrir Unity. No regenera colliders de gameplay ni guarda escenas personales. Los prefabs mantienen GUID y sus instancias heredan los cambios.

El renderer raíz antiguo permanece deshabilitado como referencia de tamaño (`geometrySource.size`), preservando anchuras de plataformas y overrides anteriores. Un objeto antiguo independiente sigue usando su renderer; elegir apariencia lo migra con Undo. Vaciar su Sprite personalizado recupera la imagen antigua.

## Animación opcional

AnimationProfile mapea Idle/Move/Jump y Attack. Los estados se consultan con `Animator.HasState`; un estado, perfil, controller o Animator ausente nunca impide movimiento o combate. El pack usa animación vertical suave para Brujita y estáticos para el resto. No se añadió un ciclo de ataque: puede suministrarse después sin cambiar las reglas del golpe.

## Añadir contenido

Crear AppearanceDefinition con ID único, tipo y sprite; añadirlo al pack y asignar el pack a la definición del elemento. Para nuevas plantillas, guardar la apariencia inicial en el prefab. Controllers preparados sólo deben animar Visual. No hace falta modificar la UI. El generador incluido prepara este contenido concreto; volver a ejecutarlo reconfigura sus definiciones y no es un importador universal.

## Fuentes y licencias

- https://kenney.nl/assets/tiny-dungeon — Tiny Dungeon 1.0, CC0.
- https://kenney.nl/assets/pixel-platformer — Pixel Platformer 1.2, CC0.

Selección exacta y licencias originales: `Assets/CreaJuegoPacks/MundoMisterioso/CREDITS.md` y `Art/*License.txt`. No hubo assets seleccionados rechazados por licencia ni contenido privado de Asset Store.

## Validación y límites

Resultados y pruebas en CreaJuego-Combat-MVP.md. No se generaron Player Builds. Revisión manual no realizada: el helper de Windows falla con `windows sandbox failed: helper_unknown_error: setup refresh had errors` incluso tras reiniciar el kernel. Se inspeccionó el preview original del pack, no una captura del resultado dentro de Unity.

Queda validar con participantes la lectura de premios/peligros y calibrar proporciones: los sprites se ajustan a medidas existentes, no generan colliders por silueta. Los clips actuales presuponen offset vertical cero. La compatibilidad con el renderer antiguo es deuda explícita. Plataforma móvil sigue fuera del catálogo hasta resolver su comportamiento de transporte previo.

