# CreaJuego Tilemap Authoring Spike

## Decisión

Es viable añadir construcción por tiles sin reemplazar el authoring existente. La solución recomendada es híbrida:

- **Tilemap** para terreno modular, plataformas atravesables y decoración repetible.
- **Elementos CreaJuego** para jugador, enemigos, premios, peligros, meta, fondo y objetos con comportamiento.

Esto evita estirar sprites de suelo, reduce la cantidad de GameObjects y conserva el modelo educativo actual.

## Implementación del spike

- Unity objetivo: **6000.6.0f1**.
- Módulo público: `com.unity.modules.tilemap` 1.0.0.
- Paleta inicial: tres tiles reales de `2D Pixel Art Platformer Biome - Plains`.
- Herramientas: **Pintar** y **Borrar**, también accesibles con `P` y `E`.
- Acceso: botón **Nivel** → **Construir con tiles**.
- Capas preparadas: `terreno`, `plataformas` y `decoracion`.
- El JSON de proyecto guarda celdas como coordenadas y un id estable de tile.
- Undo/Redo registra un trazo completo al soltar el puntero.
- Los colliders de Tilemap permanecen desactivados durante la edición y se activan en la copia temporal de juego.

## Límites del spike

- La paleta sólo contiene tres piezas de prueba; falta seleccionar un set visual completo de bordes, esquinas, relleno y pendientes.
- No hay pinceles rectangulares, relleno de áreas, selección múltiple ni autotiling.
- Los paquetes importados usan escalas y PPU distintos. Cada pack deberá declarar una paleta normalizada para una cuadrícula de una unidad.
- Las rampas siguen siendo elementos hasta evaluar `RuleTile` o una familia de tiles de pendiente con colisión coherente.
- La personalización de tiles no usa la ventana Tile Palette del Editor, porque CreaJuego también necesita el mismo flujo en WebGL.

## Siguiente iteración recomendada

1. Crear un descriptor de pack con paletas separadas para terreno sólido, plataformas atravesables y decoración.
2. Añadir pincel rectangular y relleno para construir suelo y muros rápidamente.
3. Incorporar autotiling para bordes y esquinas sin exponer reglas técnicas al participante.
4. Migrar el nivel inicial a tiles de forma gradual, conservando una ruta de compatibilidad para niveles JSON anteriores.
5. Probar el tamaño del JSON y el rendimiento WebGL con niveles de 500, 2.000 y 10.000 celdas.

## Validación

- El proyecto compila en Unity 6000.6.0f1.
- `RuntimeAuthoringTests`: **14/14**.
- Se cubren pintura, borrado, deduplicación por celda, JSON, Undo y colliders en Play Mode.
- La suite Web completa pasa **62/62** pruebas EditMode.
