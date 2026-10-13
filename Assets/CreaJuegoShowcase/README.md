# CreaJuego Showcase

`Scenes/CreaJuegoShowcase.unity` es una demostración amplia y editable del flujo real de CreaJuego. Usa el mismo `RuntimeAuthoringController`, las mismas definiciones educativas y el mismo Canvas que `WebAuthoringSpike`, pero tiene un `RuntimeContentPack` propio para permitir más arte y ambientación.

## Abrir o regenerar

- Abre `Assets/CreaJuegoShowcase/Scenes/CreaJuegoShowcase.unity` para recorrer, editar y jugar la muestra.
- Usa **CreaJuego > Showcase > Preparar escena completa** para regenerarla de forma idempotente.
- Usa **CreaJuego > Contenido > Actualizar opciones recomendadas** para volver a publicar las opciones ligeras en el catálogo habitual.

La escena no se añade a `EditorBuildSettings`. El build principal sigue usando únicamente `Assets/CreaJuegoWeb/Scenes/WebAuthoringSpike.unity`. Para una presentación independiente se puede crear manualmente otro perfil de build que incluya sólo la escena Showcase.

## Contenido seleccionado

El catálogo habitual recibe una selección pequeña de `PlatformerTileset` y `2D Pixel Art Platformer Biome - Plains`: fondos, terreno, plataforma móvil, pinchos, árbol, casa, planta y cofres.

El pack Showcase añade sprites y audio autocontenidos seleccionados del paquete oficial **2D Game Kit | 2D Sample Project**. No importa sus scripts, prefabs, URP ni configuración completa. El aviso original de terceros se conserva en `ThirdParty/2DGameKit/2DGameKit_Third-PartyNotice.txt`.

La muestra incluye un nivel grande con Gino, plataformas estáticas y móviles, rampas, escalera, muro, premios, pinchos, varios ScareCrow, una meta, decoraciones, niebla, luz ambiental y sonido de ambiente.