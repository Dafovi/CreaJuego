# CreaJuego Educational Layer V1 — primer bloque implementado

Fecha: 7 de septiembre de 2026. Unity **6000.6.0f1**, revisión **f7f8ed4d1e24**.

## Alcance recibido y estado

Se leyó íntegramente [el análisis previo](CreaJuego-Educational-Layer-Analysis.md) y se utilizó como base, sin repetir el inventario de Playground.

El adjunto «SPRINT — CREAJUEGO EDUCATIONAL LAYER V1» termina literalmente en **GRU**, dentro de la fase 6, después de definir el slider Velocidad del jugador. Se solicitó la continuación. Este documento entrega un bloque verificable de las fases completas 1–5 y la parte recibida de la 6; **no declara terminado el sprint completo**. Las propuestas del informe previo no se presentan como requisitos recibidos de las fases ausentes.

## Cambios

- Catálogo educativo de cinco tarjetas: Jugador, Plataforma, Premio, Peligro y Meta. Cada tarjeta tiene un icono geométrico propio, nombre y descripción. Plataforma móvil, enemigo y decoración conservan sus assets y usos existentes, pero no aparecen en el catálogo de participantes. Los gates P1 siguen pendientes.
- `GameItemDefinition` incorpora `availableInWorkshop` y `allowMultiple`. `Catalog()` conserva el inventario completo; `WorkshopCatalog()` filtra la superficie del taller. La vista no decide por clase Playground qué publicar.
- `EducationalProperty` añade `group`, `unit` y `visibleWhen`: una única ruta booleana opcional, sin expresiones ni reflexión. Se muestra el control ante valores mixtos para permitir multi-edición. No hay un ScriptableObject por propiedad.
- La ventana existente mantiene su organización en catálogo y propiedades junto al SceneView normal. Añade iconos, grupos, ayudas visibles, campos enteros acotados, texto multilínea de hasta 120 caracteres, toggles y color. Los sliders mantienen sus rangos y entrada numérica. Todos los controles enlazan propiedades de `GameItem` mediante `SerializedObject`/`SerializedProperty`.
- «Vidas» pasa a **Resistencia inicial** en el panel educativo del jugador. El HUD de la demo conserva todavía su terminología anterior: pendiente de la fase de sesión/presentación.
- Acciones **Duplicar** y **Eliminar**, con Undo. El duplicado conserva conexión al prefab, overrides y desplazamiento visible. Si hay componentes u objetos añadidos/eliminados mediante edición técnica, informa que se use la vista Escena; no descarta esos cambios silenciosamente.
- Este preset impide crear o duplicar un segundo jugador desde CreaJuego. Eliminarlo permite volver a crearlo; Undo lo recupera. La edición directa de assets desde el panel se rechaza.
- No se añadieron campos a GameItem, ni cambios al adapter o al Runtime de Playground. Sus 42 scripts mantienen los hashes del spike anterior.

## Archivos principales

- [GameItemDefinition.cs](../Packages/com.dafovi.creajuego/Runtime/GameItemDefinition.cs): metadatos educativos y publicación del catálogo.
- [ItemService.cs](../Packages/com.dafovi.creajuego/Editor/ItemService.cs): catálogo, unicidad, duplicar y eliminar.
- [CreaJuegoWindow.cs](../Packages/com.dafovi.creajuego/Editor/CreaJuegoWindow.cs): controles UI Toolkit y acciones.
- [EducationalLayerV1.cs](../Assets/CreaJuegoPacks/Starter/Editor/EducationalLayerV1.cs): migración explícita de metadatos Starter e iconos geométricos.
- `Assets/CreaJuegoPacks/Starter/Content/*.asset` y `Content/Icons/`: definiciones actualizadas e iconos; prefabs y escena no regenerados.
- [EducationalLayerTests.cs](../Assets/CreaJuegoPacks/Starter/Tests/Editor/EducationalLayerTests.cs): cinco pruebas nuevas.
- [SpikeTests.cs](../Assets/CreaJuegoPacks/Starter/Tests/Editor/SpikeTests.cs): pruebas anteriores ajustadas al nuevo nombre del salto y la unicidad de creación. La prueba de multi-edición prepara un segundo jugador mediante Unity para verificar que el binding sigue siendo robusto ante edición avanzada.

La migración se ejecutó una vez con `CreaJuego.Starter.EducationalLayerV1.Apply`; antes de modificar cada definición guardó su original en `.spike/educational-v1-before/`. No es una migración automática al abrir ni al seleccionar. No ejecutarla de nuevo para restaurar valores de autoría: su función es aplicar los metadatos de esta versión. Los iconos existentes no se sobrescriben.

## Verificación

| Evidencia | Resultado |
|---|---|
| [Baseline](v1-baseline-tests.xml), sin cambios de código | 7/7 aprobadas |
| [Compilación y migración](v1-metadata-unity.log) | Finalización correcta; marcador `CREAJUEGO_EDUCATIONAL_METADATA_V1_READY` |
| [Suite posterior](v1-authoring-tests.xml) | **12/12 aprobadas**, ninguna omitida |
| [Log de la suite](v1-authoring-unity.log) | Sin errores de compilación C# de CreaJuego |
| Comparación SHA256 vendor contra `Playground-Source-Inventory.csv` | 42 archivos, 0 cambios |

Las pruebas nuevas cubren publicación de exactamente cinco elementos/iconos distintos, conservación de contenido experimental, guardas de jugador y assets, duplicado con valores y vínculo al prefab, eliminación/Undo/Redo, bindings de UI Toolkit, límites numéricos y visibilidad booleana reactiva. La visibilidad reactiva se probó en un objeto; su variante con valores mixtos queda sin cobertura específica. La multi-edición general sigue cubierta por la prueba anterior de SerializedObject. Después de ejecutar la suite sólo se renombró la prueba de visibilidad para describir correctamente su alcance; no cambió su cuerpo.

También siguen pasando las pruebas anteriores de movimiento, salto, premio/puntos, daño, derrota, meta y recorrido completo con teclado en Play Mode. Esto **no** elimina los riesgos de suelo y fin de partida documentados en el análisis: esas pruebas no cubren todos los casos límite.

No se realizó una prueba manual con participantes ni una revisión visual mediante captura real del Editor. Se verificó la ventana mediante sus controles UI Toolkit en el Editor de Unity. No se volvió a generar el ejecutable Windows porque este bloque no cambia comportamiento Runtime; la compilación y ejecución verificadas corresponden al Editor.

## Cómo revisar el bloque

1. Abrir el proyecto en Unity 6000.6.0f1.
2. Abrir `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego → Abrir taller**. Deben aparecer cinco tarjetas.
4. Seleccionar el jugador existente: ver icono, ayuda y grupos Movimiento, Salto y Resistencia. Intentar añadir otro debe explicar el límite.
5. Crear un premio, modificar Puntos, duplicarlo y eliminar la copia. Ctrl+Z permite recuperar/deshacer; Ctrl+Y rehacer.
6. Pulsar **PROBAR**, usar flechas/Espacio y llegar a la meta o tocar el peligro. **DETENER** devuelve a la edición.

El color de plataforma se aplica al probar, como indica su ayuda; este bloque todavía no incorpora preview inmediato ni Ancho. Una escena vacía todavía requiere preparación de servicios: no se debe presentar esta entrega como un flujo completo de creación desde cero.

## Pendiente antes de cerrar V1

Primero recibir la continuación del adjunto para fijar los requisitos restantes. El análisis previo recomienda: apoyo de salto fiable, opción Puede saltar, validación de escena antes de probar, estado único de victoria/derrota con bloqueo posterior, preview de color con Undo y decisión explícita sobre Ancho. Ninguno se considera entregado aquí. Los gates de plataforma móvil y enemigo, la persistencia guardar/reabrir y la observación con participantes también siguen pendientes.

Se mantiene la separación `UI → servicios Editor → GameItem/IItemBackend → adapter → Playground`. No hubo rediseño completo de interfaz, APIs internas, sistema de reglas ni commits.

