# CreaJuego Lab

Prototipo educativo 2D en español, **Unity 6000.6.0f1**.

1. Abre esta carpeta en Unity Hub con esa versión.
2. Abre `Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity`.
3. Menú **CreaJuego > Abrir taller**.
4. Añade elementos, muévelos en Escena y configura sus propiedades en CreaJuego.
5. Pulsa **PROBAR**, enfoca Juego y usa **flechas + Espacio**. **DETENER** vuelve a edición.

Catálogo educativo: **jugador, plataforma, premio, peligro y meta**. Plataforma móvil, enemigo y decoración se conservan como contenido experimental fuera del catálogo principal.

Backend: Unity Playground 1.8.0 adaptado (MIT); UI Toolkit propia y capas Runtime/Editor separadas. **12/12 pruebas automáticas aprobadas** en la última ejecución local, incluida una ruta completa mediante teclado. La prueba manual con una persona sigue pendiente. Este repositorio contiene un prototipo en desarrollo: el sprint Educational Layer V1 todavía no está cerrado.

Consulta [el informe técnico](Docs/CreaJuego-Technical-Spike.md), [el análisis educativo](Docs/CreaJuego-Educational-Layer-Analysis.md), [el estado de V1](Docs/CreaJuego-Educational-Layer-V1.md) y las modificaciones de terceros en `Docs/Playground-Compatibility.patch`. Los logs, resultados brutos de pruebas y ejecutables son artefactos locales excluidos del repositorio; algunas referencias de los informes corresponden a esos artefactos.

El código de Unity Playground conserva su [licencia MIT](Assets/ThirdParty/UnityPlayground/License.md). La publicación del repositorio no asigna una licencia nueva al código propio de CreaJuego.

`CreaJuego > Preparar demo` crea recursos ausentes y abre la escena sin sobrescribir contenido existente. Las pruebas se ejecutan desde Test Runner, EditMode, `CreaJuego.Starter.Tests`; requieren gráficos para probar la ventana.
