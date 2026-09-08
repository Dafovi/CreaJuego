# CreaJuego Lab

Prototipo educativo 2D en español, **Unity 6000.6.0f1**.

1. Abre esta carpeta en Unity Hub con esa versión.
2. Abre `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego > Abrir taller**.
4. Usa **Añadir al juego** para crear y **Mi juego** para seleccionar; configura las propiedades y mueve los elementos en Escena.
5. Pulsa **JUGAR**, enfoca Juego y usa **A/D o flechas + Espacio**. **DETENER** vuelve a edición.

Catálogo educativo: **jugador, plataforma, premio, peligro, meta y enemigo**. Plataforma móvil permanece experimental y oculta porque no transporta al personaje; decoración sigue fuera del catálogo.

Backend: Unity Playground 1.8.0 adaptado (MIT); UI Toolkit propia y capas Runtime/Editor separadas. **36/36 pruebas automáticas aprobadas** en la última ejecución local, incluida una ruta completa mediante teclado. La prueba manual con una persona sigue pendiente. Este repositorio contiene un prototipo en desarrollo: La dirección visual tiene pruebas automáticas verificadas en el Editor; falta la revisión visual propia y la prueba con principiante. La revisión inicial de V1 fue comunicada por el responsable del taller.

Consulta [el informe técnico](Docs/CreaJuego-Technical-Spike.md), [el análisis educativo](Docs/CreaJuego-Educational-Layer-Analysis.md), [el estado de V1](Docs/CreaJuego-Educational-Layer-V1.md) y las modificaciones de terceros en `Docs/Playground-Compatibility.patch`. Los logs, resultados brutos de pruebas y ejecutables son artefactos locales excluidos del repositorio; algunas referencias de los informes corresponden a esos artefactos.

El código de Unity Playground conserva su [licencia MIT](Assets/ThirdParty/UnityPlayground/License.md). La publicación del repositorio no asigna una licencia nueva al código propio de CreaJuego.

`CreaJuego > Preparar demo` crea recursos ausentes y abre la escena sin sobrescribir contenido existente. Las pruebas se ejecutan desde Test Runner, EditMode, `CreaJuego.Starter.Tests`; requieren gráficos para probar la ventana.

Para empezar un nivel vacío: crea una escena y pulsa **Preparar escena** en CreaJuego. Añade personaje, plataformas y meta; **Jugar** comprobará lo necesario antes de ejecutar.

Consulta los [ajustes de UX V1.1](Docs/CreaJuego-V1.1-UX-Adjustments.md).

Consulta la [dirección visual implementada](Docs/CreaJuego-Visual-Direction.md). La validación habitual se realiza en Unity Editor: compilación, tests, UI Toolkit, Undo/Redo, persistencia y flujo con SceneView. No generar builds standalone salvo petición explícita; no son un criterio normal de cierre.

El taller ahora abre **CreaJuego**, **Propiedades** y **Jugar** como ventanas separadas alrededor del SceneView real. Consulta [cómo acoplarlas y guardar el layout](Docs/CreaJuego-Workshop-Layout.md). El acoplamiento es manual y no modifica automáticamente tu distribución.

Layout oficial: [CreaJuego Taller](Docs/Layouts/README.md), creado y validado manualmente por el responsable del taller. Cárgalo con **CreaJuego > Configurar editor**, o desde Layout > Load layout from file. Sólo esa acción explícita aplica la distribución; Abrir taller y Preparar escena no la cambian.
