# CreaJuego — Feedback de daño y combate MVP

Fecha: 2026-09-09. Unity **6000.6.0f1**, revisión `f7f8ed4d1e24`.

## Resultado y alcance

El jugador puede golpear con X, los enemigos tienen vida de partida y dejan de participar al ser derrotados. Daño por contacto con flash, empuje pequeño e invulnerabilidad breve. Apariencias y combate siguen separados; no se modifican scripts vendor. No hay combos, inventario, armas, proyectiles, IA nueva ni builds standalone.

## Selección: causas y solución

Se identificaron dos puntos concretos:

1. El sprite vive en el hijo Visual. SceneView puede seleccionar ese hijo o su SpriteRenderer; la vista anterior sólo buscaba GameItem en el objeto seleccionado.
2. Reconstruir el panel en todos los avisos de jerarquía desconecta controles que todavía se están editando. La prueba de visibilidad de propiedades detectó este problema durante la integración.

`EducationalSelection.Items` resuelve hasta el GameItem padre, admite Component/GameObject y deduplica. Propiedades, resaltado de Mi juego y acciones usan la misma resolución. La selección de Unity permanece intacta. `Selection.selectionChanged`, Undo/Redo, cambios de escena y Play Mode actualizan el panel. Los eventos públicos de jerarquía y cambios de objetos sólo lo reconstruyen si cambia la identidad, nombre, definición o uso de arte seleccionado; no hay polling. Cada reconstrucción libera el binding y SerializedObject anteriores.

## Política visual y contenido

Preparado/personalizado se dibuja en blanco. La propiedad Color se oculta para arte, conservando el dato antiguo como compatibilidad. El flash no borra tint ni apariencia: guarda y restaura el color visible.

Premios: Moneda dorada, Gema azul, Llave dorada. Peligros: Pinchos, Trampa roja. Decoración: 14 opciones (muebles, estatuas/lápida, cofres y vegetación), con miniaturas, scroll y búsqueda. Decoración no tiene collider ni daño/puntos. Los ID/GUID anteriores de premio/peligro se mantienen; sus imágenes visibles cambian.

Fuentes CC0: [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) y [Pixel Platformer](https://kenney.nl/assets/pixel-platformer), ambos Kenney. Créditos, tiles exactos y licencias en `Assets/CreaJuegoPacks/MundoMisterioso`. El detalle de los 30 presets está en [Sistema de apariencias](CreaJuego-Appearance-System.md).

## Arquitectura mínima

Código propio en `Packages/com.dafovi.creajuego`:

| Componente | Responsabilidad |
| --- | --- |
| Runtime/PlayerDamageReceiver | Recibir daño autorizado, intervalo de protección y reacción física. Delega la vida/HUD al backend existente. |
| Runtime/DamageFeedback | Flash de 0,15 s, restauración exacta de color y limpieza al desactivar. Sin Animator requerido. |
| Runtime/PlayerAttack | Una consulta de área frontal por pulsación, filtro a enemigos, cooldown y estado temporal de ataque. |
| Runtime/EnemyVitality | Vida restante de partida y derrota; nunca escribe la vida de autoría. |
| Adapters/Playground/PlaygroundContactDamage | Llevar contactos y contactos sostenidos a PlayerDamageReceiver, usando valores configurados de Playground. |
| Adapters/Playground/PlaygroundAdapter | Conectar sesión, entrada, notificaciones de salud y desactivación del patrullaje/colliders del enemigo derrotado. |
| Adapters/Playground/WorkshopInput | Mantener movimiento e incorporar X. |
| Runtime/ItemVisual + AnimationProfile | Facing visual y estado Attack opcional; no decide daño. |

Las vistas sólo editan GameItem mediante descriptors/SerializedObject. No acceden a clases vendor. El adapter usa los hooks `modificationAllowed` y `healthChanged` ya existentes en HealthSystemAttribute. Cuando existe la fachada propia de contacto, el gate de ModifyHealthAttribute evita una segunda entrega del mismo daño. Sus valores siguen disponibles como configuración de compatibilidad; no se cambió el código de Playground.

## Daño al jugador

Contacto de enemigo/peligro → PlayerDamageReceiver.TryReceive → HealthSystemAttribute.ModifyHealth → notificación de salud → HUD existente + flash + protección.

- Flash rojo: **0,15 s**.
- Invulnerabilidad: **0,65 s**, sin control educativo.
- Empuje: impulso horizontal pequeño alejándose del origen y una componente vertical mínima, proporcional a la masa. No cambia masa, gravedad, velocidad educativa, escala ni rotación.
- Nuevos contactos durante la protección se ignoran. Un peligro que desaparece sólo se consume si el golpe fue aceptado.
- La sesión bloquea interacción tras ganar/perder. El comportamiento mortal del jugador continúa usando el final y destrucción de Playground; no se añadió una pantalla nueva.

## Ataque

X produce una consulta `Physics2D.OverlapBoxAll` de **0,8 × 0,8 unidades**, centrada a **0,65 unidades** delante del jugador. El hijo AttackPoint indica esa posición; no reutiliza el collider corporal ni añade un collider físico de ataque. Un HashSet evita duplicar daño si un enemigo tiene varios colliders. Sólo EnemyVitality de la misma escena recibe el golpe.

Cooldown interno **0,35 s**; estado visual de ataque **0,12 s**. Hay una sola consulta instantánea por pulsación, no daño cada frame. La orientación usa el facing/flipX del Visual y nunca rota la raíz. Mantener X no produce ataques automáticos.

La UI expone únicamente **Puede golpear** y **Daño (1–5)**. Si Puede golpear está apagado, X no actúa. No se exponen área, layers, cooldown, offsets ni eventos de animación.

## Vida y derrota del enemigo

**Puntos de vida (1–5), default 2** en el prefab; los valores de escenas con overrides se conservan. `EnemyVitality.Remaining` se inicia desde GameItem.health y se modifica sólo durante la partida. Al llegar a cero:

1. Se marca derrotado inmediatamente.
2. El adapter detiene Patrol y desactiva cuerpo/colliders.
3. Se muestra el flash durante 0,15 s.
4. Se desactiva el objeto durante la partida.

No se destruye su autoría. Salir de Play Mode recupera el enemigo y su configuración. No se añadió knockback de enemigos para evitar conflictos con el patrullaje por transform.

## Animator opcional

AnimationProfile incorpora el nombre Attack. ItemVisual comprueba que exista antes de reproducirlo. El daño no depende del estado, la duración de un clip, Animation Events ni de Animator. El pack actual no incorpora un ciclo nuevo de ataque: usa el flash de impacto; una apariencia futura puede añadir su estado Attack sin modificar PlayerAttack. La prueba de teclado elimina Animator y verifica igualmente impactos, facing y derrota.

## Preparación y controles

Prefabs y definiciones actualizados mediante `Assets/CreaJuegoPacks/Starter/Editor/CombatMvpBuilder.cs`. Menú explícito: **CreaJuego → Contenido → Preparar combate MVP**. El contenido entregado ya está preparado; no hace falta ejecutar el generador para jugar. Reejecutarlo puede reconfigurar las definiciones del pack, por lo que no sustituye un editor de contenido general.

Abrir CreaJuegoPlaygroundDemo, añadir Enemigo desde el catálogo, situarlo en una plataforma alcanzable y pulsar Jugar. Se conserva el layout del taller y su carga separada.

**“Muévete con A/D o las flechas, salta con Espacio y golpea con X.”** W no salta. La ayuda está en la definición del jugador, la barra Jugar y el HUD de las nuevas instancias de servicios. No se reescriben escenas personales; un texto HUD con override previo puede conservar su texto antiguo.

## Pruebas y evidencia

Se conservan las 43 pruebas previas. Se actualizaron únicamente expectativas afectadas por el alcance: siete elementos visibles, 30 apariencias y espera del intervalo de protección en el segundo contacto del test antiguo. Se añadieron ocho pruebas agrupadas para:

- selección de raíces, hijos Visual y componentes, cambio de tipo, Undo/Redo;
- creación, duplicación, eliminación y recuperación sin binding antiguo;
- color blanco preparado/personalizado y Color oculto;
- visibilidad de Color al migrar un placeholder a Sprite personalizado y regresar, sin reselección;
- premios/peligros válidos, decoración sin física y búsqueda en panel real;
- descriptors de ataque/vida;
- pérdida de vida, flash/restauración y rechazo/aceptación durante/después del intervalo;
- teclado X, facing, desactivar ataque, ausencia de Animator, daño/flash/derrota, otros elementos intactos y restauración al salir de Play.

El runner de EditMode entra realmente en Play Mode para las pruebas runtime. Se usa copia aislada del proyecto con la misma versión de Unity. El cierre de validación al final de este documento registra el resultado definitivo. Logs/XML locales ignorados por Git en Docs/combat-*. No Player Build.

## Revisión manual

**No realizada.** El helper de control de Windows falla con `windows sandbox failed: helper_unknown_error: setup refresh had errors`, incluso tras reintentar su inicialización. No se afirma haber hecho clics reales en SceneView ni validado con un participante la claridad del arte. Los tests ejercitan Selection y UI Toolkit en el Editor; no reemplazan esa revisión visual.

Checklist pendiente: alternar los siete tipos desde Mi juego/Hierarchy/SceneView; revisar color natural; comparar premios/peligros; elegir Sprite personalizado; tocar enemigo/peligro; golpear a ambos lados; desactivar Puede golpear; derrotar; parar y comprobar reaparición.

## Límites y siguiente paso

No hay bloqueo del ataque por paredes ni autoapuntado. El alcance fijo está pensado para los tamaños de este pack. La consulta depende de los colliders de enemigos y no interpreta siluetas. La animación de ataque y el knockback de enemigos no forman parte del MVP. La protección de salud actual es apropiada para el flujo de daño del taller; incorporar curación requerirá distinguirla explícitamente del daño en el contrato del backend.

Siguiente paso recomendado: revisión manual corta con principiante para ajustar alcance, empuje, legibilidad de Trampa roja y proporciones. Después, sólo si aporta claridad, añadir una indicación visual de golpe fallido y un ciclo de ataque preparado. Sin ampliar todavía a armas, inventario o combos.


## Cierre de validación — 2026-09-09

- **51/51 pruebas aprobadas**, cero fallos, en Unity Editor 6000.6.0f1: 43 anteriores conservadas y ocho nuevas. Compilación final sin errores C#.
- Se corrigió además la preparación del test antiguo de migración: ahora captura el jugador antes de crear un GameItem incompleto; evita depender del orden de enumeración de objetos de Unity.
- Evidencia local: `Docs/combat-tests.xml` y `Docs/combat-unity.log`, ignorados por Git.
- Ningún cambio en scripts vendor. Verificados los bloques serializados de Rigidbody2D/colliders: idénticos a los anteriores en los seis prefabs de gameplay.
- Archivos sincronizados tras comparar SHA-256 con el estado inicial, sin conflictos concurrentes. Respaldo local previo en `.spike/CombatBeforeSync`.
- Escenas personales y layout sin reescribir. No Player Build, commit ni push.
- Revisión manual pendiente por el fallo del helper de Windows descrito arriba. El resultado automático no certifica todavía claridad visual con principiantes.
