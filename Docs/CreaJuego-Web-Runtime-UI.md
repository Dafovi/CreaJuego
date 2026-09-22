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
## Elementos de mundo añadidos

### Fondo

- aparece en el catálogo y en **Mi juego**;
- sólo puede existir uno;
- usa una apariencia azul predeterminada o una imagen PNG/JPG elegida por la persona;
- ocupa todo el lienzo visible de la cámara, tanto al construir como al jugar, y se dibuja detrás de los objetos;
- se conserva en Guardar/Abrir y participa en Undo/Redo;
- el Canvas no tiene una imagen opaca a pantalla completa, por lo que la cámara de construcción permanece visible.

### Plataforma móvil

- utiliza la definición `movil` y el `Patrol` real del backend Playground;
- permite editar **Velocidad** y **Distancia**;
- conserva sus apariencias preparadas;
- se incluye en el escenario inicial para poder probarla inmediatamente.

### Guías de movimiento

Al seleccionar una Plataforma móvil aparece una guía azul; al seleccionar un Enemigo aparece una guía naranja. Ambas muestran inicio, final, dirección y longitud del recorrido. La guía usa el mismo cálculo `posición + derecha × distancia` que configura el adapter de Playground y desaparece durante el modo Jugar.

Validación posterior: 32/32 pruebas Web y 120/120 pruebas EditMode completas. No se generó un build.