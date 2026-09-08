# CreaJuego Lab

Prototipo educativo 2D en español, **Unity 6000.6.0f1**.

1. Abre esta carpeta en Unity Hub con esa versión.
2. Abre `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego > Abrir taller**.
4. Usa **Añadir al juego** para crear y **Mi juego** para seleccionar; configura las propiedades y mueve los elementos en Escena.
5. Pulsa **JUGAR**, enfoca Juego y usa **A/D o flechas + Espacio**. **DETENER** vuelve a edición.

Catálogo educativo: **jugador, plataforma, premio, peligro, meta y enemigo**. Plataforma móvil permanece experimental y oculta porque no transporta al personaje; decoración sigue fuera del catálogo.

Backend: Unity Playground 1.8.0 adaptado (MIT); UI Toolkit propia y capas Runtime/Editor separadas. **28/28 pruebas automáticas aprobadas** en la última ejecución local, incluida una ruta completa mediante teclado. La prueba manual con una persona sigue pendiente. Este repositorio contiene un prototipo en desarrollo: V1.1 tiene pruebas automáticas y build Windows verificados; falta la revisión visual propia de V1.1 y la prueba con principiante. La revisión inicial de V1 fue comunicada por el responsable del taller.

Consulta [el informe técnico](Docs/CreaJuego-Technical-Spike.md), [el análisis educativo](Docs/CreaJuego-Educational-Layer-Analysis.md), [el estado de V1](Docs/CreaJuego-Educational-Layer-V1.md) y las modificaciones de terceros en `Docs/Playground-Compatibility.patch`. Los logs, resultados brutos de pruebas y ejecutables son artefactos locales excluidos del repositorio; algunas referencias de los informes corresponden a esos artefactos.

El código de Unity Playground conserva su [licencia MIT](Assets/ThirdParty/UnityPlayground/License.md). La publicación del repositorio no asigna una licencia nueva al código propio de CreaJuego.

`CreaJuego > Preparar demo` crea recursos ausentes y abre la escena sin sobrescribir contenido existente. Las pruebas se ejecutan desde Test Runner, EditMode, `CreaJuego.Starter.Tests`; requieren gráficos para probar la ventana.

Para empezar un nivel vacío: crea una escena y pulsa **Preparar escena** en CreaJuego. Añade personaje, plataformas y meta; **Jugar** comprobará lo necesario antes de ejecutar.

Consulta los [ajustes de UX V1.1](Docs/CreaJuego-V1.1-UX-Adjustments.md).
