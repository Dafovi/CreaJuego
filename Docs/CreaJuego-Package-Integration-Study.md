# Integración de paquetes importados — Gino / Platformer Game Kit

Fecha: 10 de septiembre de 2026. Editor: Unity 6000.6.0f1.
Estado: integración de referencia completada en código y validada con los assets instalados localmente.

## Punto de partida

Commit realizado antes de investigar: **f06f926**, Add workshop world tools and category-based appearance libraries.
Incluye cambios propios de CreaJuego, categorías y reorganización, pruebas y documentación. Sin push.
Assets/Plugins y Assets/NovaDevs permanecen locales, fuera de ese commit.

## Resultado

**Gino funciona con sus imágenes y animaciones conservando el backend Playground.**
No hace falta Animancer para reproducir los cuatro clips verificados mediante Playables de Unity.
Esto reutiliza el arte animado del kit. No implica haber integrado su controlador completo, combos, escalada o física.

La implementación usa el jugador real de CreaJuego y permite declarar cuatro clips directamente en la opción de apariencia.
Unity Test Framework: **72/72 pruebas de la suite completa** y **2/2 pruebas específicas de integración** pasan, incluidas ejecución en Play Mode, movimiento y ataque completo.
No se generaron builds standalone.

## Qué hay instalado

### Platformer Game Kit

Versión declarada por Code/Utilities/PlatformerGameKitReadMe.cs: **1.2.1**.
Prefab: Assets/Plugins/Platformer Game Kit/Prefabs/Player/Player.prefab.

El hijo Sprite contiene SpriteRenderer, Animator y CharacterAnimancerComponent. Su Animator tiene m_Controller vacío.
Las animaciones las reproducen los estados del kit mediante Animancer, no un controller asignado al prefab.

Referencias relevantes:
- Code/Characters/Character.cs: cuerpo, salud, Animancer y máquina de estados.
- Code/Characters/CharacterAnimancerComponent.cs: reproducción, orientación y hitboxes.
- Code/Characters/States/LocomotionState.cs: ClipTransition para reposo, marcha, carrera y caída.
- Code/PlatformerGameKit.asmdef: cuatro referencias GUID de ensamblados que no se resuelven en Assets/Packages; la compilación previa confirmó tipos de Animancer ausentes.

El código permanece desactivado mediante CREAJUEGO_ENABLE_PLATFORMER_GAME_KIT, conforme a lo autorizado.
La documentación del proveedor indica que funciona con Animancer Lite, aunque algunas funciones requieren Pro.
La versión exacta compatible y las funciones Pro necesarias para el controlador completo aún deben comprobarse antes de reactivarlo.
Fuente: https://kybernetik.com.au/platformer/

### NovaDevs — Skull Garden

13 prefabs, ningún script C#, un clip FadeInScreen y un controller de transición de pantalla.
Es contenido de escenario, decoración y presentación; no contiene otro controlador de personaje en los archivos inspeccionados.

Algunos prefabs son compuestos: Stone Goblet incluye objetos de luz y partículas además de su sprite.
La selección actual de apariencias extrae el sprite; no reproduce esos efectos ni la jerarquía del prefab.
El proyecto usa Built-in (GraphicsSettings.m_CustomRenderPipeline vacío). Los efectos/materiales del pack requieren una revisión específica antes de prometer el aspecto del ejemplo.
No es necesario modificar el motor de movimiento para usar sus sprites en las listas.

## Mapeo de Gino verificado

| Estado CreaJuego | Archivo real |
|---|---|
| Reposo / Idle | Art/Gino/Gino-Idle.anim |
| Movimiento / Move | Art/Gino/Gino-Run.anim |
| En el aire / Jump | Art/Gino/Gino-Jump-Loop.anim |
| Ataque / Attack | Art/Gino/Gino-Attack1.anim |

Todos bajo Assets/Plugins/Platformer Game Kit.
Los cuatro animan únicamente SpriteRenderer.m_Sprite con ruta vacía, sin curvas de transform ni AnimationEvents.
Son compatibles con un Animator sobre el hijo Visual de CreaJuego.
Se comprobó reposo, cambio de fotogramas al correr, desplazamiento por teclado, salto y estado visual aéreo.
El ataque empieza, pero el código actual interrumpe el clip: véase el siguiente apartado.

Existen además clips de daño, muerte, aterrizaje, deslizamiento, bordes y variantes de ataque.
No se deben asignar a estados que CreaJuego todavía no implementa: el clip por sí solo no crea la mecánica.

## Problemas concretos que resolver

1. **Duración del ataque.** PlayerAttack.IsAttacking dura 0,12 s. Gino-Attack1 dura 0,5833333 s. ItemVisual abandona Attack cuando termina esa ventana. La prueba confirmó el inicio y el corte prematuro. Separar duración visual del golpe, ventana de daño y cooldown. No ampliar indiscriminadamente la detección de daño para que dure lo mismo que el clip.
2. **Carrera entre pasos de física.** ItemVisual.LateUpdate calcula velocidad a partir de diferencias de posición entre frames. Entre dos FixedUpdate puede observar cero y volver a Idle aunque se mantenga la tecla. La ejecución batch sin límite de FPS registró 20 frames Move y 2530 Idle durante el tramo de entrada sostenida; amplifica el efecto, no representa el FPS de uso normal. Conviene obtener velocidad/intención estable del backend, mediante IVisualMotionState o un contrato equivalente.
3. **Pivote y escala.** El sprite inicial de Gino tiene bounds de 4 × 4 unidades, centro (0,72; 1,81) y pivote (20,5; 3) píxeles. No está centrado como las imágenes anteriores. Calibrar pies y altura visible contra el collider existente, mantener la proporción y validar orientación. No escalar el cuerpo físico al cambiar la apariencia.
4. **Asignar sólo el prefab no basta.** Su Animator carece de controller y las referencias ClipTransition son parte de scripts desactivados. Hace falta declarar el mapeo de clips y preparar la reproducción.
5. **Sprites frente a prefabs compuestos.** La extracción actual no importa luces, partículas, materiales especiales ni varios renderizadores. Un futuro soporte de prefabs visuales completos necesita un contrato separado del prefab de gameplay.

## Dos alternativas

| Alternativa | Reutilización | Cambios | Recomendación |
|---|---|---|---|
| Arte y clips del kit + CreaJuego/Playground | Sprites y clips originales; Animator de Unity | Perfil de clips, controller generado, señal estable de movimiento, duración visual y pivote | Primera integración |
| Controlador completo Platformer Game Kit + Animancer | Máquina de estados, física, combos, hitboxes, entradas y animaciones del kit | Dependencia compatible y adapter para movimiento, salud, daño, meta, sesión, controles y propiedades educativas | Experimento posterior separado |

El segundo camino debe tener un único sistema responsable de mover al personaje.
No añadir simultáneamente el movimiento del kit y Playground.Move al mismo Rigidbody2D.
La documentación del proveedor explica que CharacterAnimancerComponent también gestiona orientación e hitboxes:
https://kybernetik.com.au/platformer/docs/characters/animancer/

## Siguiente implementación recomendada

1. Añadir a cada opción de la lista de Jugador un bloque de clips opcional: Reposo, Correr, Saltar y Atacar. Mantener un archivo por categoría, sin exigir assets individuales para cada opción.
2. Generar un AnimatorController auxiliar a partir de ese bloque, preservando los clips originales. Seguir aceptando un controller manual para contenido avanzado.
3. Corregir la señal visual de movimiento y permitir duración visual de ataque configurable, con reinicio en cada ataque aceptado.
4. Registrar Gino como opción, ajustar escala/pivote y comprobar teclado, orientación, salto, golpe, daño y salida de Play.
5. Verificar Undo/Redo, duplicado, guardado/reapertura y conservación de la física. Después aplicar el mismo camino a un enemigo sencillo.
6. Mantener para otra fase combos, armas arrojadizas, wall jump, agarre a bordes y soporte completo de prefabs con efectos.

Criterio de aceptación: seleccionar Gino en la lista de Jugador, jugar con él sin configurar componentes técnicos y ver animaciones estables y completas.

## Distribución

License.txt y la página del proveedor indican Asset Store EULA para el kit y una licencia separada para los sprites.
Esto no basta para dar por autorizada la redistribución pública de todos sus archivos fuente.
Se dejaron los paquetes importados fuera del commit; no se publicó ningún asset nuevo.
Conviene distribuir la integración como código y receta de configuración que cada instalación aplica a sus assets locales.
Fuente: https://kybernetik.com.au/platformer/docs/package/license


## Implementación de referencia completada

Gino está configurado en la lista **Jugador** usando el prefab real como fuente visual y cuatro clips directos: Idle, Run, Jump Loop y Attack 1. Se conserva el identificador que ya utilizaba la escena abierta, por lo que no se rompe su selección.

AppearanceOption admite ahora clips de reposo, movimiento, salto y ataque. ItemVisual los reproduce mediante Playables de Unity, sin AnimatorController generado y sin dependencia Runtime de Animancer. PlayerAttack conserva el golpe instantáneo, pero mantiene la animación visible durante la duración real del clip y evita reiniciarla antes de terminar. La selección de movimiento usa la velocidad física estable y conserva el fallback de desplazamiento para elementos sin velocidad.

La receta **CreaJuego → Contenido → Integrar Gino como apariencia** completa o actualiza esta opción y elimina únicamente duplicados del mismo prefab. Sirve como ejemplo para crear recetas equivalentes para otros personajes. El Inspector también permite llenar manualmente los cuatro clips.

Validación:
- Suite completa: **72/72 pruebas pasan**.
- Caso con el identificador manual ya guardado en la escena: **2/2 pruebas pasan**.
- Evidencia local: Docs/gino-final-tests.xml y Docs/gino-idempotent-tests.xml.
- La escena principal no fue guardada ni reemplazada por la automatización.
- Los archivos de Platformer Game Kit y NovaDevs siguen fuera de Git.
