# CreaJuego Tilemap Authoring Spike

## Decisión

Es viable añadir construcción por tiles sin reemplazar el authoring existente. La solución recomendada es híbrida:

- **Tilemap** para terreno modular, plataformas atravesables y decoración repetible.
- **Elementos CreaJuego** para jugador, enemigos, premios, peligros, meta, fondo y objetos con comportamiento.

Esto evita estirar sprites de suelo, reduce la cantidad de GameObjects y conserva el modelo educativo actual.

## Implementación del spike

- Unity objetivo: **6000.6.0f1**.
- Módulos públicos: `com.unity.modules.tilemap` 1.0.0 y `com.unity.2d.tilemap.extras` 9.0.0, versión publicada para Unity 6000.6.
- Paleta inicial: un **Rule Tile** de terreno automático construido con piezas reales de `2D Pixel Art Platformer Biome - Plains`.
- Herramientas: **Lápiz**, **Línea** y **Rectángulo**, utilizables tanto para pintar como para borrar.
- Acceso: botón **Nivel** → **Construir con tiles**.
- Capas preparadas: `terreno`, `plataformas` y `decoracion`.
- El JSON de proyecto guarda celdas como coordenadas y un id estable de tile.
- Undo/Redo registra un trazo completo al soltar el puntero.
- El Rule Tile comprueba sus vecinos ortogonales y cambia entre superficie, laterales, esquinas, base e interior. El relleno de tierra se dibuja en una capa visual inferior para que las piezas transparentes mantengan continuidad.
- Los colliders de Tilemap permanecen desactivados durante la edición y se activan en la copia temporal de juego.

## Límites del spike

- La primera paleta automática cubre terreno ortogonal. Las pendientes continúan como elementos porque requieren reglas y colliders distintos.
- No hay cubeta de relleno, selección múltiple ni sustitución masiva de un tipo de terreno.
- Los paquetes importados usan escalas y PPU distintos. Cada pack deberá declarar una paleta normalizada para una cuadrícula de una unidad.
- Las rampas siguen siendo elementos hasta evaluar `RuleTile` o una familia de tiles de pendiente con colisión coherente.
- La personalización de tiles no usa la ventana Tile Palette del Editor, porque CreaJuego también necesita el mismo flujo en WebGL.

## Siguiente iteración recomendada

1. Crear un descriptor de pack con paletas separadas para terreno sólido, plataformas atravesables y decoración.
2. Añadir una cubeta de relleno y una herramienta para sustituir un terreno por otro.
3. Crear más Rule Tiles por pack temático sin exponer sus reglas técnicas al participante.
4. Migrar el nivel inicial a tiles de forma gradual, conservando una ruta de compatibilidad para niveles JSON anteriores.
5. Probar el tamaño del JSON y el rendimiento WebGL con niveles de 500, 2.000 y 10.000 celdas.

## Validación

- El proyecto compila en Unity 6000.6.0f1.
- `RuntimeAuthoringTests`: **14/14**.
- Se cubren pintura, borrado, deduplicación por celda, JSON, Undo y colliders en Play Mode.
- La suite Web completa pasa **62/62** pruebas EditMode.
