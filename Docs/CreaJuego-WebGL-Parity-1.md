# CreaJuego WebGL — Runtime Parity 1

## Baseline

- Rama: `feature/webgl-spike`.
- Spike funcional: `e52841d`.
- Consolidación de metadatos Unity: `b8cbb04` (`WebGL spike validated`).
- Unity: `6000.6.0f1 (f7f8ed4d1e24)`.
- `main` permanece en `debb2d1`.

## Resultado

Parity 1 cierra las diferencias principales de cámara, plataformas, límites, recuperación y apariencias sin recrear Unity Editor. El frontend Editor sigue siendo independiente. El frontend Web usa únicamente referencias serializadas y servicios runtime.

## Cámara

BUILD conserva una cámara 2D ortográfica con pan y zoom. Su viewport ocupa el área central entre los paneles. `Ver todo` calcula los bounds de los elementos y ajusta centro/zoom; `Encontrar` encuadra la selección.

PLAY añade un `CinemachineBrain` a la cámara de juego y crea una `CinemachineCamera` con `CinemachinePositionComposer`. El `Follow` apunta al clon temporal del Jugador. BUILD y PLAY alternan cámaras y `AudioListener`; al volver se destruye el rig temporal y se restaura la cámara de construcción.

## Plataforma y handles

`RuntimeItemData.platformWidth` representa el ancho educativo. `RuntimePlatformGeometry` modifica el eje X del `BoxCollider2D` y ajusta el visual a la misma anchura sin escalar la raíz ni alterar la altura.

Una Plataforma seleccionada muestra handles izquierdo y derecho. Durante el arrastre se actualizan visual y collider; al soltar se registra un solo snapshot. Los handles no tienen collider, no son `GameItem`, no se serializan y se destruyen al entrar en PLAY.

## Snap y encuadre

`Alinear automáticamente` está activado por defecto y usa pasos internos de 0,25 para posición y ancho. La UI no expone unidades técnicas. Puede desactivarse y su estado se guarda por proyecto.

## Límites y recuperación

El proyecto guarda un preset `Pequeño`, `Mediano` o `Grande` y los bounds derivados. BUILD muestra un marco azul sutil. PLAY crea límites físicos izquierdo, derecho y superior; no crea piso inferior.

El Jugador reutiliza `PlayerFallRecovery`. Al empezar PLAY se registra su posición de autoría como posición segura y se configura el límite inferior del nivel. Al caer, vuelve a esa posición y limpia velocidades lineal y angular. La posición JSON de autoría no cambia.

## Preflight pedagógico

Además de un Jugador, una Plataforma, una Meta y referencias válidas, ahora valida:

- tamaño de nivel válido;
- cámara de juego disponible;
- `PlayerFallRecovery` en el prefab preparado;
- que el Jugador parezca apoyado en una Plataforma.

Los mensajes evitan términos de collider, backend o Cinemachine. Premio, Peligro, Enemigo y Decoración siguen siendo opcionales.

## Content pack runtime

`RuntimeContentPack` declara definiciones disponibles, categorías de apariencias, defaults, thumbnails resueltos y prefabs runtime seguros. `WebSpikeBuilder` genera una representación derivada explícita en `Assets/CreaJuegoWeb/Content`.

Las opciones que originalmente apuntaban a prefabs externos se convierten en referencias directas a sprite, controlador y clips. Los prefabs externos con scripts desactivados no entran al build. Las definiciones derivadas se marcan `runtimeOnly`, de modo que no duplican el catálogo Editor.

Opciones preparadas:

| Tipo | Opciones |
|---|---:|
| Jugador | 3 |
| Plataforma | 3 |
| Premio | 3 |
| Peligro | 3 |
| Enemigo | 9 |
| Meta | 2 |
| Decoración | 14 |

## Selector de apariencias

Propiedades muestra thumbnail y nombre. Cambiar opción afecta sólo a `ItemVisual`, conserva gameplay, persiste `appearanceId` y participa en Undo/Redo. Decoración usa scroll y búsqueda; la vista limita resultados visibles para no crear decenas de botones gigantes.

`Elegir imagen…` está integrado en el mismo bloque. El bridge Web acepta únicamente PNG/JPG de hasta 2 MB y 2048 × 2048. JavaScript valida extensión, MIME, bytes y dimensiones antes de enviar; C# vuelve a validar cabecera, tamaño y dimensiones antes de crear `Texture2D`.

## Schema y migración

`CreaJuegoProjectData.schemaVersion` actual es 2. Añade:

- `platformWidth`;
- `alignAutomatically`;
- `levelSize` y bounds;
- `appearanceId` y custom image existentes.

`ProjectSerializer` detecta JSON v1 sin `schemaVersion`, aplica ancho 3, snap activado y límites medianos, y conserva objetos/metadata. Rechaza versiones futuras con un mensaje explícito.

## Undo/Redo

Continúa usando hasta 40 snapshots JSON. Añade ancho, apariencias, snap y preset de nivel. El historial elimina snapshots idénticos. Movimiento, resize y sliders actualizan en vivo y registran una sola operación al soltar.

## Pruebas

Unity EditMode con dispositivo gráfico:

- total: **106**;
- aprobadas: **106**;
- fallidas: **0**.

Incluye las 93 pruebas anteriores y 13 pruebas Parity 1 para prefab seguro, schema/migración, ancho visual/collider, persistencia, Undo/Redo, snap, preflight, pack, selector, imágenes, cámara, 100 objetos, handles y recovery.

## Rendimiento WebGL

Edge `153.0.4234.32`, headless mediante CDP, renderizado del mismo build Release. Las cifras sirven como comparación técnica; deben repetirse en hardware LabCo visible.

| Métrica | 43 objetos | 100 objetos |
|---|---:|---:|
| Carga hasta ready | 2,92 s | 2,62 s |
| BUILD FPS | 103,2 | 105,4 |
| Entrada a PLAY | 22 ms | 21 ms |
| PLAY FPS | 179,3 | 179,3 |
| Regreso a BUILD | 24 ms | 47 ms |
| Memoria Unity en BUILD | 44.275.745 B | 45.848.538 B |
| Memoria Unity en PLAY | 44.504.908 B | 45.651.788 B |
| Errores / excepciones | 0 / 0 | 0 / 0 |

Con 100 objetos, `Mi juego` crea filas sencillas dentro de un `ScrollRect`. No hubo degradación que justificara pooling en esta máquina. Se reevaluará con 200 objetos o si LabCo muestra latencia.

## Build

Salida: `Builds/WebGLParity1/`.

- WebGL Release exclusivamente;
- Gzip + decompression fallback;
- memoria inicial 128 MB;
- resolución 1280 × 720;
- 17 archivos;
- **11.051.776 bytes**.

El spike anterior medía 10.894.818 bytes. Parity 1 añade 156.958 bytes comprimidos.

Servidor:

```powershell
node Tools/serve-webgl.cjs
```

Escenarios: `?stress=1` para 43 objetos y `?stress=100` para 100.

## Revisión realizada

Automatizada en Unity y WebGL real:

- crear/cargar proyecto y catálogo;
- ancho visual/físico, persistencia y Undo/Redo;
- snap activo/inactivo;
- cambio de apariencia;
- pan, zoom y métodos de encuadre cubiertos por componentes;
- BUILD → PLAY → BUILD mediante clic real en canvas;
- Cinemachine enlazado al Jugador;
- recovery y autoría intacta;
- 43 y 100 objetos;
- guardado/serialización mediante abstracción.

No se declara como revisión visual humana completa: quedan por ejecutar manualmente en navegador visible caminar, saltar, recibir daño, derrotar enemigo, llegar a Meta, selector de archivo nativo y recargar IndexedDB. Checklist: pasos 1–28 de la especificación del sprint, con especial atención a 14–21 y 26–28.

## Diferencias frente al Editor

Web ya ofrece cámara de autoría 2D, selección, movimiento, ancho con handles, snap, nivel, recovery, Cinemachine, catálogo y apariencias. Editor conserva SceneView, gizmos avanzados, multi-selección, Inspector/SerializedObject, PrefabUtility, AssetDatabase, herramientas de facilitador y edición nativa de escenas/prefabs.

## Limitaciones

- UI runtime funcional, sin diseño final ni accesibilidad completa.
- Sin plataforma móvil, rotación, tilemap ni touch.
- Imagen custom persiste como base64 y puede aumentar JSON/memoria pese a límites.
- Guardado depende del origen/perfil del navegador.
- La cámara usa configuración mínima; faltan zonas muertas y presets educativos.
- No hay export/import `.creajuego` ni nube.
- Rendimiento LabCo presencial pendiente.

## Siguiente sprint recomendado

Piloto técnico en los equipos LabCo: navegador visible, sesión de 60 minutos, teclado/audio, carga fría/caliente e IndexedDB tras reiniciar. En paralelo, añadir export/import `.creajuego`, feedback de carga/error de imágenes y pulido accesible del selector sin ampliar todavía el género ni la lógica visual.