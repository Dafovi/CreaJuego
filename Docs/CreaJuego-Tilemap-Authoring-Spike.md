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
- Herramientas: **Lápiz**, **Línea**, **Rectángulo** y **Rampa**, utilizables tanto para pintar como para borrar.
- **Rampa** usa las piezas diagonales declaradas por el tileset y ajusta el trazo a 45°. **Rellenar debajo** añade un soporte triangular de tiles que después puede editarse normalmente.
- Acceso: botón **Nivel** → **Construir con tiles**.
- Capas preparadas: `terreno`, `plataformas` y `decoracion`.
- El JSON de proyecto guarda celdas como coordenadas y un id estable de tile.
- Undo/Redo registra un trazo completo al soltar el puntero.
- El Rule Tile comprueba sus vecinos ortogonales y cambia entre superficie, laterales, esquinas, base e interior. El relleno de tierra se dibuja en una capa visual inferior para que las piezas transparentes mantengan continuidad.
- Los colliders de Tilemap permanecen desactivados durante la edición y se activan en la copia temporal de juego.

## Familias de terreno

La paleta admite familias educativas. Cuando los packs están instalados, el generador añade **Naturaleza**, **Aldea**, **Jardín de calaveras** y dos variantes de **Cueva**. Village Props utiliza conexión automática; los sprites de Cave y Skull Garden se normalizan a una celda sin modificar los originales. Si falta un pack opcional, se omite y el terreno base continúa disponible.

Los packs opcionales de Asset Store permanecen excluidos del repositorio público. El código sólo conserva sus rutas de descubrimiento y genera los tiles derivados al abrir el proyecto local que tenga esos packs instalados. Los builds pueden incorporarlos, pero no se deben publicar sus fuentes desde este repositorio sin comprobar una licencia que lo permita.

Rocky World y Crystal World incluyen láminas completas sin slicing de Unity. Se detectaron durante este sprint, pero no se exponen todavía: requieren un descriptor de atlas que indique qué regiones son tiles conectables. Usar la lámina completa como un tile produciría escalas incorrectas.

## Límites del spike

- La paleta automática cubre terreno ortogonal. Las pendientes se guardan como trazos separados porque necesitan una superficie inclinada continua y un collider de una sola dirección.
- No hay cubeta de relleno, selección múltiple ni sustitución masiva de un tipo de terreno.
- Los paquetes importados usan escalas y PPU distintos. Cada pack deberá declarar una paleta normalizada para una cuadrícula de una unidad.
- La herramienta sólo se habilita cuando la familia declara dos sprites de pendiente: subida y bajada. **Roca gris**, del pack Cave Platformer Tileset, es la primera familia compatible mediante sus tiles triangulares originales 16 y 18.
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
