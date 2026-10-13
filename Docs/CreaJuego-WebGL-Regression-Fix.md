# CreaJuego WebGL Regression Fix

## Baseline

- Rama: feature/webgl-spike.
- Baseline: c7fcc3a Web runtime parity: camera world and appearances.
- Unity: **6000.6.0f1 (f7f8ed4d1e24)**.
- Alcance: corrección de regresiones de autoría, sin mecánicas ni contenido nuevos.
- Build: sólo WebGL Release en Builds/WebGLRegressionFix/. No se generaron builds de escritorio.

## Síntomas reproducidos

La revisión real mostró que “Mi juego” podía aparecer vacío o incompleto, Propiedades podía no corresponder a la selección, el Jugador podía perder la selección frente a una Plataforma y el zoom variaba según el delta entregado por el navegador. Los 106 tests anteriores validaban modelo y servicios, pero no la geometría real de los ScrollRect ni el recorrido completo selección → panel.

## Causas raíz

### Mi juego

Había tres causas acumuladas:

- Refresh podía ejecutarse varias veces en un frame. Clear usaba Destroy diferido, por lo que filas antiguas seguían activas, superpuestas y con listeners hasta terminar el frame.
- Las filas usaban coordenadas negativas desde arriba, pero conservaban ancla y pivote inferiores. Existían en el modelo aunque se dibujaban fuera de la máscara.
- La posición inicial del ScrollRect podía comenzar abajo y ocultar la primera instancia.

Ahora el contenido anterior se desactiva antes de destruirlo, cada fila usa ancla y pivote superiores, el panel comienza arriba y RuntimeItemRow conserva el instanceId estable.

### Propiedades

Propiedades compartía el error de ancla/pivote y la superposición diferida. Además, Apariencia se construía en la misma ruta que los controles educativos: una excepción podía abortar el panel completo.

El panel ahora obtiene la selección mediante RuntimeSelectionService → RuntimeItemData, muestra una instrucción cuando no hay selección, construye ajustes y Apariencia en secciones tolerantes a error y desactiva raycastTarget en textos informativos.

### Selección del Jugador

Physics2D.OverlapPoint devolvía un solo collider según el orden físico. Autoría dependía así de gameplay y no contemplaba el tamaño real del sprite, hijos visuales ni superposición.

RuntimeAuthoringHitTest evalúa SpriteRenderer.bounds de la instancia y sus hijos, usa collider sólo como fallback y prioriza sorting layer, sorting order, visual más específico por área, selección anterior y distancia. RuntimeSelectionService guarda SelectedInstanceId, resuelve la instancia de autoría actual tras Rebuild y permite Select(instanceId). La lista no depende del índice, nombre, posición ni clones de PLAY.

### Zoom lento

La fórmula anterior restaba scrollDelta por 0.005. Una rueda tradicional suele entregar cerca de 120 unidades; WebGL puede entregar pasos cercanos a 1, que producían un cambio imperceptible.

El nuevo zoom normaliza ambos casos y aplica un factor exponencial con sensibilidad interna 0.12. Mantiene límites 2–30; un paso ya se percibe y varios pasos conservan control fino.

## Enrutado final de eventos

    puntero
      ├─ EventSystem.RaycastAll encuentra UI bloqueante
      │    └─ ScrollRect procesa la rueda; BuildCamera no cambia
      └─ no hay UI y el punto está dentro de buildCamera.pixelRect
           ├─ clic: handles → RuntimeAuthoringHitTest → Select(instanceId)
           ├─ arrastre: mueve la instancia seleccionada
           ├─ fondo: desplaza BuildCamera
           └─ rueda: zoom exponencial de BuildCamera

El rectángulo amarillo y los handles son renderers sin collider ni GraphicRaycaster. Los textos informativos no capturan raycasts. Los paneles bloquean el mundo detrás.

## Correcciones

- selección estable mediante instanceId;
- filas identificadas por RuntimeItemRow;
- resolución de la instancia actual después de reconstruir;
- conservación de selección si la instancia existe;
- limpieza de UI sin objetos antiguos interactivos;
- ancla/pivote superiores para hijos de Contenido;
- posición inicial superior y sensibilidad 24 en ScrollRect;
- fallback educativo sin selección;
- Propiedades y Apariencia independientes y tolerantes a error;
- hit test visual separado de Physics2D;
- exclusión explícita de UI y zonas fuera del viewport;
- zoom exponencial normalizado;
- comando CreaJuego/Web/Generar WebGL Regression Fix.

## Pruebas automatizadas

Resultado final: **111/111 aprobadas**, 0 fallidas y 0 omitidas.

XML: .spike/WebGLValidation/TestResults-Regression-Final-4.xml.

Los 106 tests anteriores siguen pasando. Se añadieron cinco pruebas:

1. selección estable por instanceId tras Rebuild;
2. hit test por sprite con collider de gameplay deshabilitado;
3. prioridad del Jugador frente a Plataforma superpuesta;
4. equivalencia de rueda Web (1) y tradicional (120);
5. flujo integrado de filas, Propiedades, tipos, crear, borrar, Undo, Redo, scroll y BUILD → PLAY → BUILD.

La prueba integrada verifica también ancla y pivote superiores, filas activas, sensibilidad de paneles y separación entre UI lateral y viewport.

## WebGL real

Build final:

- Builds/WebGLRegressionFix/;
- 17 archivos;
- 11,051,918 bytes;
- Unity: Build Finished, Result: Success;
- Microsoft Edge 153.0.4234.32 mediante CDP visible;
- errores JavaScript: 0;
- excepciones Unity observadas: 0;
- advertencia ambiental: AudioContext espera un gesto del usuario.

La revisión visual confirmó filas reales, Propiedades de Jugador y Plataforma, indicador amarillo, scroll de Apariencias sin zoom del mundo, zoom visible sobre BUILD, entrada a PLAY con HUD/Cinemachine y vuelta a BUILD conservando el modelo y la lista.

## Resoluciones

Se probó y se esperó el redibujado completo en 1280×720, 1366×768 y 1920×1080. Catálogo, “Mi juego”, viewport, Propiedades y barras permanecieron disponibles. La plantilla conserva un canvas de 960×600 centrado y añade espacio exterior en pantallas grandes; el responsive queda fuera de este hotfix.

Las capturas y resultados CDP están en .spike/ y no forman parte del commit.

## Archivos principales

- Packages/com.dafovi.creajuego/RuntimeAuthoring/RuntimeServices.cs
- Packages/com.dafovi.creajuego/RuntimeAuthoring/RuntimeAuthoringController.cs
- Packages/com.dafovi.creajuego/RuntimeAuthoring/RuntimeAuthoringInput.cs
- Packages/com.dafovi.creajuego/RuntimeAuthoring/RuntimeAuthoringUI.cs
- Assets/CreaJuegoWeb/Tests/Editor/RuntimeRegressionTests.cs
- Assets/CreaJuegoWeb/Editor/WebSpikeBuilder.cs

## Pendientes

- Repetir mouse/trackpad en hardware de taller. CDP valida eventos y fórmulas, pero no la inercia de cada dispositivo.
- Añadir comparación visual de capturas en CI. La aserción geométrica evita esta causa, pero no sustituye una prueba visual completa.
- Decidir en un sprint de layout si el canvas debe crecer en pantallas grandes.
- Si se requiere más automatización, añadir telemetría sólo para builds de diagnóstico.

## Próximo paso recomendado

Hacer una sesión corta en un equipo real de sala centrada en mouse, trackpad y legibilidad. Si no aparecen regresiones, retomar la paridad Web pendiente. No conviene añadir nuevas herramientas antes de esa prueba humana.
