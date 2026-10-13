# Gamer UI redesign: matriz de alcance

## Objetivo

Migrar la interfaz WebGL de Gamer a la dirección visual aprobada sin reescribir los sistemas estables de autoría. La escena continúa siendo el área principal y la UI actúa como presentación sobre `RuntimeAuthoringController`, `RuntimeAuthoringInput` y los servicios existentes.

Versión objetivo verificada: **Unity 6000.6.0f1**.

## Hallazgos de arquitectura

- `RuntimeAuthoringController` conserva proyecto, selección, historial, creación, duplicado, borrado, guardado, importación, reproducción y terreno.
- `RuntimeAuthoringInput` conserva mover, tamaño, zoom, desplazamiento, cuadrícula, pintura, borrado, línea, rectángulo y rampa.
- `RuntimeAuthoringUI` construye y refresca la interfaz uGUI. Es el punto principal de la migración visual.
- `WebAuthoringSpike` contiene un Canvas ajustado manualmente. La migración debe actualizarlo de forma controlada y no regenerarlo en cada inicio.
- La biblioteca ya soporta importación múltiple, cámara, recorte, giro, color único, eliminación aproximada de fondo, edición y persistencia. El rediseño sólo debe sacarla de Propiedades y convertir su navegación en una vista modal.
- `RuntimePreflight` y el HUD ya calculan el estado necesario para la barra inferior, vidas, puntos, enemigos, objetivo y victoria.
- Los tiles son datos del nivel y no deben generar filas en `Mi juego`.

## Matriz

| Elemento | Estado comprobado | Clasificación | Acción inicial |
|---|---|---|---|
| Catálogo de objetos | Funciona y se genera desde definiciones | A · Migración directa | Conservar creación y cambiar tarjetas/distribución |
| Mi juego | Funciona con icono, selección y scroll | A · Migración directa | Conservar filas; retirar el chulito y no añadir ojos o menús |
| Selección en escena/lista | Funciona mediante `RuntimeSelectionService` | A · Migración directa | Mantener lógica y reforzar estado visual |
| Mover | Funciona con herramienta explícita | A · Migración directa | Migrar a toolbar contextual de Objetos |
| Tamaño | Funciona en ambos ejes | A · Migración directa | Migrar a toolbar contextual de Objetos |
| Zoom y desplazamiento | Funcionan; clic derecho/central desplaza | A · Migración directa | Mantener y presentar ayuda cerrable |
| Deshacer/Rehacer | Funcionan mediante historial de proyecto | A · Migración directa | Mover a cabecera |
| Duplicar/Eliminar | Funcionan para el elemento seleccionado | A · Migración directa | Conservar como acciones inferiores |
| Preflight | Funciona para Plataformas y Atrapa y esquiva | A · Migración directa | Rediseñar barra inferior sin cambiar validación |
| Jugar/Volver | Funciona y conserva el proyecto editable | A · Migración directa | Mantener un único CTA JUGAR en preflight |
| HUD y resultado | Vidas, puntos, enemigos, objetivo y resultado funcionan | A · Migración directa | Adaptar visualmente |
| Objetos/Terreno | Terreno existe dentro de Propiedades del nivel | B · Adaptación | Crear pestañas y toolbars contextuales |
| Lápiz/Línea/Rectángulo/Rampa | Funcionan en `RuntimeAuthoringInput` | B · Adaptación | Mover del panel derecho a Terreno |
| Familias de tiles | Funcionan desde `RuntimeTilePaletteDefinition` | B · Adaptación | Mostrar en panel Terreno, con rampa sólo cuando sea compatible |
| Piezas rápidas | Plataforma, suelo, muro y rampa siguen siendo definiciones | B · Adaptación | Agrupar como compatibilidad; no mezclarlas con el flujo principal |
| Propiedades | Funcionan, pero tamaño aparece antes que las propiedades pedagógicas | B · Adaptación | Reordenar secciones y mantener sólo propiedades reales |
| Biblioteca de imágenes | Completa, actualmente incrustada en Propiedades | B · Adaptación | Abrir como modal desde `Cambiar imagen` |
| Editor de imagen/cámara | Implementados en el plugin WebGL | B · Adaptación | Conservar puente y adaptar acceso visual |
| Nuevo/tipo de juego | Funciona para Plataformas y Atrapa y esquiva | B · Adaptación | Migrar el selector al diseño aprobado |
| Guardar/Abrir | Funcionan con almacenamiento del navegador | B · Adaptación | Agrupar bajo Proyecto |
| Descargar/Importar copia | Funcionan con archivos `.creajuego` | B · Adaptación | Usar lenguaje educativo, sin “JSON” visible |
| Responsive | Parcial y desactivado para proteger el Canvas manual | B · Adaptación | Implementar estados 1280×720 y 1366×600 y paneles plegables |
| Ayuda | Existe como textos permanentes y estado contextual | B · Adaptación | Separar ayuda inicial cerrable de mensajes contextuales |
| Renombrar objeto | No existe | C · Posterior | No mostrar |
| Visibilidad por objeto | No existe | C · Posterior | No mostrar ojos |
| Puede saltar | No existe como propiedad educativa | C · Posterior | No mostrar |
| Color del objeto | No existe como propiedad persistida | C · Posterior | No mostrar |
| Menú por objeto | No tiene acciones definidas | C · Posterior | No mostrar tres puntos |
| Minimapa | No existe | C · Posterior | Mantener Ver todo, Encontrar y marco del nivel |
| Controles similares a Unity | No corresponden al producto WebGL | D · No aplicable | Omitir |
| Scene/Game/Global/2D duplicado | No corresponde al producto | D · No aplicable | Omitir |

## Orden de implementación

1. Introducir estado explícito `Objetos` / `Terreno` sin alterar el proyecto.
2. Separar toolbars y contenido contextual.
3. Migrar cabecera, paneles, escena y preflight a componentes visuales reutilizables.
4. Convertir la biblioteca en modal y simplificar Apariencia.
5. Agrupar acciones reales en Proyecto.
6. Añadir plegado lateral y variante de poca altura.
7. Adaptar Nuevo, HUD y resultado.
8. Ejecutar pruebas de selección, herramientas, historial, archivos, preflight y juego.

La validación WebGL se reserva para el cierre del rediseño porque la cámara y el selector de archivos sólo pueden probarse completamente en navegador.
