# CreaJuego Web — interfaz de autoría

Fecha: 2026-09-20  
Unity: 6000.6.0f1

## Destino de publicación

Los builds Web se publicarán en <https://github.com/Dafovi/creajuego-web>. Ese repositorio conserva su propia configuración de exportación y su `index`; al actualizar un build no se deben reemplazar esos ajustes de forma automática.

Este cambio no generó ni publicó un build. La validación se realizó dentro de Unity Editor.

## Distribución implementada

El Canvas runtime utiliza la misma jerarquía visual de las ventanas de CreaJuego:

- cabecera con marca, progreso educativo y estado de guardado;
- catálogo lateral con tarjetas e iconos de las definiciones;
- lista **Mi juego** con la apariencia activa de cada elemento;
- barra de herramientas sobre la escena;
- área central reservada para la cámara de construcción;
- panel derecho de propiedades con ficha del elemento, grupos educativos y apariencias;
- barra inferior de preparación con requisitos y la acción **Jugar**;
- acción **Volver a construir** en la cabecera durante la prueba.

La paleta oscura coincide con el tema del Editor: fondo `#171D27`, panel `#222B39`, texto `#EDF2FA`, acento `#80B4FF` y éxito `#48C78A`.

## Iconografía

El catálogo obtiene los iconos desde `GameItemDefinition.icon`. La lista usa primero el sprite de la apariencia activa de la instancia y recurre al icono de la definición si no hay otra vista previa. Así Gino, ScareCrow y las apariencias elegidas se representan de forma consistente sin duplicar una tabla de iconos en la UI.

## Validación

- prueba específica de jerarquía, iconos, progreso, estado y viewport: 1/1;
- suite `CreaJuego.Web.Tests`: 30/30;
- suite EditMode completa: 118/118;
- `git diff --check`: sin errores.

No se ejecutó `BuildPipeline.BuildPlayer`.