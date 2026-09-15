# Estado actual de CreaJuego

Fecha de revisión: 14 de septiembre de 2026. Versión objetivo y validada: **Unity 6000.6.0f1**.

## Qué está listo

- Tres ventanas UI Toolkit en español alrededor del SceneView: Elementos, Propiedades y Jugar.
- Tema claro y oscuro compartido, layout de taller y GameView 16:9.
- Catálogo principal: Jugador, Plataforma, Premio, Peligro, Enemigo, Meta y Decoración.
- Plataforma móvil y **Piso largo** publicados como **Extra**; límites invisibles desde Escenario.
- Escenario admite fondos del computador y los guarda dentro del juego.
- SceneView muestra gizmos educativos para recorridos, interacciones, caída y límites.
- Apariencias por categoría, prefabs animados del Platformer Game Kit e importación amigable de PNG/JPG por juego.
- Backend educativo desacoplado de la vista. Unity Playground 1.8.0 adaptado sigue resolviendo movimiento, salto, patrulla, puntuación, salud, triggers y parte de la UI de sesión.
- Cinemachine sigue al Jugador.
- Preflight pedagógico para personaje único, superficie, apoyo inicial, recuperación de caída, cámara, sesión, marcador, meta y elementos válidos.
- Recuperación automática si el Jugador cae, con teletransporte al inicio y velocidad reiniciada.
- Creación de juegos por equipo con rutas únicas, metadatos persistentes y carpeta local de imágenes.
- Menú mínimo del facilitador para preparar, revisar, recuperar y localizar trabajos.

## Decisiones del piloto

La escala se presenta como **Tamaño de la imagen**. Sólo modifica el visual; collider, Rigidbody2D y comportamiento quedan intactos. Esta decisión evita que una imagen artística cambie accidentalmente el diseño físico.

Premio, Peligro, Enemigo y Decoración enriquecen el nivel, pero no bloquean Play. La Plataforma sí es obligatoria porque el inicio seguro requiere que el Jugador esté apoyado. La Plataforma móvil se mantiene fuera del recorrido básico para reducir opciones durante la primera hora.

Las imágenes externas se copian dentro de `Assets/TrabajosTaller/<equipo>/Imagenes`. Los nombres repetidos reciben una variante única y el archivo original externo nunca se modifica.

## Línea base y validación

El sprint partió del commit `a6d54ee` con el árbol limpio. La compilación era correcta. La batería heredada reportó 59/73 pruebas aprobadas: 13 fixtures antiguos suponían sprites legacy en la escena demo después de adoptar prefabs externos y un test de layout se ejecutó sin copiar `Docs`. No eran errores de compilación ni de uso del Editor. El sprint actualiza la cobertura para que las pruebas dependan de contratos educativos y recursos explícitos, no del visual concreto que haya elegido el autor de la demo.

La validación normal no incluye builds standalone. Se usan compilación del Editor, EditMode tests, UI Toolkit, Undo/Redo, persistencia de escenas y prefabs, y una prueba manual en SceneView/GameView.

Resultado final automatizado del sprint: **84/84 pruebas aprobadas**, sin errores de compilación en Unity 6000.6.0f1. La inspección visual automatizada del Editor no pudo ejecutarse porque el controlador de Windows falló al iniciar; no se contabiliza como prueba manual.

## Riesgos y pendientes

- La primera prueba presencial con participantes sigue siendo la evidencia más importante que falta.
- El ajuste de tamaño no edita la zona de choque; una futura herramienta visual podría ofrecer un modo avanzado para editar ambas dimensiones de forma consciente.
- Los assets externos pueden traer controladores y scripts propios. CreaJuego reutiliza su visual mediante adaptadores y mantiene desactivado el código del kit que depende de Animancer.
- El diagnóstico de apoyo usa las superficies educativas de CreaJuego y tolerancias 2D. Conviene probarlo con niveles muy inclinados o colliders no rectangulares antes de ampliar el catálogo.
- La acción “Volver jugador al inicio” usa la posición capturada al crear o preparar el Jugador; Play Mode vuelve a capturar la posición actual al comenzar.

## Siguiente paso recomendado

Realizar el piloto de tres horas con uno o dos equipos, registrar en qué momentos necesitan al facilitador y medir tres tareas: crear el recorrido, entender el preflight e importar una imagen propia. El siguiente sprint debe responder a esas observaciones antes de ampliar reglas visuales, edición de colliders o packs temáticos.