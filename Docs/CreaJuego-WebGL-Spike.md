# CreaJuego WebGL Spike

## 1. Objetivo y resultado

Rama `feature/webgl-spike`, creada desde `debb2d1`, con Unity `6000.6.0f1 (f7f8ed4d1e24)`. El spike responde esta pregunta: ¿podemos crear, seleccionar, mover, configurar y jugar un nivel pequeño desde una build WebGL reutilizando el runtime actual?

**Resultado: GO condicionado.** La arquitectura y el flujo técnico funcionan. No se recomienda sustituir todavía el taller basado en Editor: primero deben validarse los computadores LabCo, límites de imágenes, persistencia tras reinicios y cámara de seguimiento.

## 2. Arquitectura

```text
CreaJuego Runtime existente
├── GameItem, ItemVisual y apariencias
├── movimiento, colisiones, premios, peligros y meta
├── combate, vida y daño
└── adapters de gameplay

CreaJuego Runtime Authoring
├── CreaJuegoProjectData + JSON
├── RuntimeSelectionService
├── RuntimeHistory
├── RuntimePreflight
├── RuntimeAuthoringController
├── RuntimeAuthoringInput
├── RuntimeAuthoringUI (uGUI)
├── FileProjectStorage
└── RuntimeImageImport

CreaJuego Editor
└── permanece intacto
```

`CreaJuego.RuntimeAuthoring` no referencia `UnityEditor`. La UI nunca usa `AssetDatabase`, `Selection`, `Undo`, `SerializedObject`, `PrefabUtility` ni `SceneView`.

El builder genera en `Assets/CreaJuegoWeb/Content` siete descriptores y prefabs Web seguros desde el Starter Pack. Quita sus referencias Editor a definiciones/categorías completas, lo que evita incluir prefabs incompletos del kit externo sin modificar el contenido original.

## 3. Escena y catálogo

Escena: `Assets/CreaJuegoWeb/Scenes/WebAuthoringSpike.unity`.

Catálogo runtime:

- Jugador
- Plataforma
- Premio
- Peligro
- Enemigo
- Meta
- Decoración

La plataforma móvil queda fuera del spike según alcance. Las referencias están serializadas en la escena; WebGL no descubre assets dinámicamente.

## 4. Flujo BUILD

- Clic sobre un elemento o su hijo visual: selecciona el `GameItem` raíz.
- Clic y arrastre: mueve el elemento manteniendo Z, colliders y comportamiento.
- Arrastre sobre fondo: desplaza la cámara ortográfica.
- Rueda: zoom.
- Rectángulo amarillo: selección visible sin depender sólo del color.
- Catálogo: crea y selecciona.
- Mi juego: lista, selecciona, duplica y elimina.
- Propiedades seguras: velocidad, salto, vida, puntos, daño y mensaje de meta.
- Historial: crear, mover, eliminar, duplicar y cambiar propiedades; 40 snapshots como máximo.
- Atajos: Ctrl+Z, Ctrl+Y y Ctrl+Shift+Z.

BUILD desactiva scripts de gameplay y simulación `Rigidbody2D` en la copia de autoría.

## 5. Flujo PLAY

`JUGAR` ejecuta un preflight y crea una copia temporal fresca desde los datos JSON. Activa gameplay y cámara de juego, y bloquea las herramientas de edición. `VOLVER A CONSTRUIR` destruye la partida temporal y reconstruye la autoría. Así reaparecen premios y enemigos, y se restauran vida y posiciones.

El preflight exige exactamente un Jugador, al menos una Plataforma, exactamente una Meta y definiciones/prefabs válidos. Los mensajes están redactados para participantes.

## 6. Datos, guardado y autosave

`CreaJuegoProjectData` guarda:

- nombre del proyecto y equipo;
- ids estables de definición e instancia;
- transform;
- propiedades educativas;
- id de apariencia;
- imagen externa opcional en base64.

No serializa `GameObject`. Guardar/Abrir último usan una abstracción `IProjectStorage`; la implementación escribe JSON en `Application.persistentDataPath`. En WebGL, la persistencia depende del almacenamiento del navegador y de IndexedDB para el origen. El autosave usa un debounce de un segundo y muestra `Guardando…` / `Guardado ✓`.

Nuevo, Guardar y Abrir último están implementados. Exportar/importar un archivo `.creajuego` se aplaza.

## 7. Imágenes externas

`Assets/Plugins/WebGL/CreaJuegoImagePicker.jslib` abre el selector PNG/JPG del navegador, lee un data URL y llama a `ReceiveImageDataUrl`. Unity decodifica los bytes a `Texture2D`/`Sprite` y conserva el data URL en JSON.

Estado: experimental y no bloqueante. Antes de producción hacen falta límites de tipo, dimensiones y bytes, reducción de imagen, mensajes de error y prueba manual en los navegadores de sala.

## 8. UI runtime

Se eligió uGUI porque ya está instalado, es estable en runtime/WebGL y permitió validar el flujo sin trasladar `EditorWindow`. La interfaz se genera por código para reducir assets del spike. Es una prueba funcional, no el diseño final. Un siguiente sprint puede evaluar UI Toolkit Runtime con la arquitectura ya separada.

## 9. Build WebGL

Salida: `Builds/WebGLSpike/`.

Configuración:

- sólo `BuildTarget.WebGL`;
- Release (`BuildOptions.None`);
- Gzip con decompression fallback;
- archivos comprimidos `.unityweb`;
- memoria inicial: 128 MB;
- resolución configurada: 1280 × 720;
- cámara 2D ortográfica;
- WebGL 2 en el navegador probado.

Tamaño reportado por Unity: **10.893.245 bytes**. Carpeta de entrega sin archivos antiguos: **10.894.818 bytes** en 18 archivos. El build Development usado durante diagnóstico medía 73.133.818 bytes y no es el artefacto final.

## 10. Servidor local

Desde la raíz del repositorio:

```powershell
node Tools/serve-webgl.cjs
```

Abrir `http://127.0.0.1:8000/`. No abrir `index.html` mediante `file://`. Para ejecutar el escenario de rendimiento: `http://127.0.0.1:8000/?stress=1`.

El servidor sólo expone `Builds/WebGLSpike`, envía `Content-Length` y evita infraestructura adicional.

## 11. Pruebas

Suite EditMode con dispositivo gráfico en Unity `6000.6.0f1`:

- total: **93**;
- aprobadas: **93**;
- fallidas: **0**;
- resultado: **Passed**.

Cobertura específica del spike: selección del hijo visual, creación, movimiento, propiedad, undo/redo, serialización, apariencia, abstracción de almacenamiento, preflight y recorrido BUILD → PLAY → BUILD con restauración. El build Release terminó con código 0.

Una ejecución descartada con `-nographics` produjo 22 fallos de vistas por no disponer de dispositivo gráfico; al repetir con la configuración correcta de pruebas de Editor, 93/93 pasaron.

## 12. Prueba de navegador y rendimiento

Navegador: **Microsoft Edge 153.0.4234.32**, Windows 10/11, ejecución headless controlada mediante Chrome DevTools Protocol.

Escenario:

- 1 Jugador
- 20 Plataformas
- 10 Decoraciones
- 5 Premios
- 3 Peligros
- 3 Enemigos
- 1 Meta
- total: **43 objetos**

Resultados del build Release comprimido:

| Métrica | Resultado |
|---|---:|
| Carga hasta `CREAJUEGO_WEB_READY` | 2,44 s |
| Respuesta JUGAR | 7 ms |
| Respuesta VOLVER A CONSTRUIR | 4 ms |
| FPS, primera ventana de 5 s | 106,3 |
| FPS, ventana estable de 5 s | 179,9 |
| Memoria asignada reportada por Unity | 39.786.458 bytes |
| JS heap usado al final | 11.823.468 bytes |
| Errores JavaScript | 0 |
| Excepciones | 0 |

Se comprobó mediante clic real sobre el canvas: BUILD → PLAY → BUILD, conservando 43 objetos. Las cifras pertenecen a esta máquina y a Edge headless con fallback de renderizado por software; sirven para comparar el spike, no para prometer rendimiento en LabCo. La medición obligatoria siguiente es sobre los equipos reales de sala y con navegador visible.

Advertencias ambientales observadas: Edge headless avisa sobre SwiftShader y el audio requiere un gesto de usuario. No aparecieron los avisos de scripts faltantes después de aislar el catálogo Web.

## 13. Compatibilidad objetivo

Chrome/Edge modernos de escritorio con WebAssembly, WebGL 2, IndexedDB, teclado y mouse. No se promete compatibilidad universal, móvil ni touch. La memoria, cuotas de almacenamiento y políticas de limpieza dependen del navegador.

## 14. Limitaciones y riesgos

- Cámara de juego estática; falta integrar y medir Cinemachine.
- La UI funcional no tiene acabado de taller ni accesibilidad completa.
- La lista de 43 objetos no está virtualizada.
- Las imágenes base64 carecen de límites y pueden inflar memoria/guardado.
- El guardado es local al navegador; no hay nube, cuentas ni colaboración.
- Los snapshots reconstruyen la escena y deben perfilarse con proyectos mayores.
- No hay export/import de archivo de proyecto.
- No hay automatización de selector de archivo del navegador.
- La validación de hardware LabCo sigue pendiente.

## 15. Decisión GO / NO-GO

**GO condicionado para continuar el prototipo Web.** La evidencia demuestra que el mismo runtime puede alimentar dos frontends, que las sustituciones Editor → runtime son pequeñas y que creación, selección, movimiento, edición, historial, persistencia y juego funcionan en WebGL.

**NO-GO para reemplazar ahora el taller Editor.** Falta una prueba presencial en hardware LabCo, robustecer imágenes y almacenamiento, integrar cámara de seguimiento, mejorar UI y comprobar una sesión completa de tres horas.

## 16. Siguiente sprint recomendado

1. Prueba controlada en 2–3 equipos LabCo: carga fría/caliente, FPS visibles, memoria y estabilidad de 60 minutos.
2. Integrar Cinemachine en PLAY y encuadre automático en BUILD.
3. Añadir límites y reducción de PNG/JPG, más feedback de errores.
4. Virtualizar `Mi juego` y medir 100 objetos.
5. Añadir exportar/importar `.creajuego` como recuperación portable.
6. Probar persistencia tras cerrar navegador, reiniciar equipo y cambiar sesión.
7. Diseñar una UI runtime accesible a partir del flujo validado.

## Continuación: Runtime Parity 1

El resultado histórico de este spike se conserva. La siguiente iteración añadió cámara Cinemachine, ancho de plataformas, handles, snap, límites, recovery y apariencias runtime-safe. Consulta [CreaJuego-WebGL-Parity-1.md](CreaJuego-WebGL-Parity-1.md).
