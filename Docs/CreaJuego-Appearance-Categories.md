# Apariencias organizadas por categoría

## Editar las opciones

Abre **Assets/CreaJuegoPacks/MundoMisterioso/Categorías** y selecciona la lista correspondiente:

- Jugador
- Enemigos
- Plataformas
- Plataformas móviles
- Premios
- Peligros
- Metas
- Decoración
- Fondos

En el Inspector amplía **Opciones**, añade una entrada y rellena **Nombre** y **Sprite**, o asigna un **Prefab de referencia visual**. No hay que crear un ScriptableObject por opción. Las entradas se guardan dentro del archivo de su categoría.

Un Sprite explícito tiene prioridad sobre el prefab. El prefab debe contener exactamente un SpriteRenderer con imagen. Se reutilizan su sprite y, si procede, su controlador Animator; no se instancian sus scripts, físicas, materiales ni jerarquía. Los prefabs que forman un escenario con muchas piezas no son una apariencia individual. Para ellos selecciona la imagen que quieres usar. Ajusta Escala y Desplazamiento si hace falta.

El controlador de animación es opcional. Sus clips deben ser compatibles con el SpriteRenderer del hijo Visual de CreaJuego; un controlador que anima rutas complejas del prefab necesita adaptación. El perfil de estados permite indicar las animaciones de reposo, movimiento, salto y ataque.

El orden de la lista es el orden del selector. La primera opción con imagen válida es la apariencia inicial de los nuevos elementos creados desde el catálogo. Las opciones sin imagen válida se muestran deshabilitadas.

Para elegir una opción: selecciona un elemento de Mi juego y pulsa su apariencia en Propiedades. Los fondos se ofrecen en Escenario y conservan el comportamiento de fondo único.

## Crear otro pack

Create → CreaJuego → Lista de apariencias por categoría crea una lista vacía. Elige su categoría en el Inspector. Añádela a **Listas por categoría** del ContentPackDefinition que usan tus definiciones de elementos. Usa una sola lista de cada categoría por pack. Las listas se pueden compartir entre packs.

## Compatibilidad

Las 30 apariencias originales están copiadas en las listas. Plataformas móviles empieza con las tres imágenes de Plataformas; Fondos empieza vacío para que lo completes. Total inicial: nueve archivos de categoría con 33 opciones.

Los antiguos archivos individuales se movieron a **Compatibilidad**, manteniendo sus .meta y GUID. No los borres todavía: escenas y prefabs anteriores pueden referenciarlos. El menú de creación ya no ofrece nuevas apariencias individuales. Al elegir una opción nueva, CreaJuego guarda la referencia a la categoría y un identificador estable de la entrada.

Las selecciones anteriores conservan su referencia antigua hasta que se elige una opción de la lista nueva. Reelige la apariencia desde Propiedades si quieres que un objeto antiguo use los datos de la lista.

Renombrar o reordenar entradas conserva las selecciones. Duplicar una entrada genera otra identidad. Borrar una opción que está en uso muestra un aviso para elegir otra; Undo permite recuperarla. Una lista vacía no vuelve a mostrar automáticamente las opciones antiguas.

CreaJuego → Contenido → Organizar listas por categoría convierte packs antiguos sin sobrescribir listas ya configuradas. El preparador histórico de Mundo misterioso conserva la configuración manual cuando existen estas listas.

## Implementación y pruebas

AppearanceCategory contiene opciones serializables inline, no subassets. IAppearanceData permite al renderizador leer tanto opciones nuevas como referencias antiguas. GameItem guarda categoría + id, con serialización y Undo/Redo a través de ItemAppearance. El Inspector de las listas usa UI Toolkit y SerializedObject.

Pruebas: migración idempotente, IDs estables, renombrado/reordenado y persistencia, Undo/Redo, prefab visual sin alterar física, opciones duplicadas/borradas, creación/duplicación y ejecución real con Playground.

La demo modificada por el usuario se conserva. Los fixtures de pruebas aíslan sus propios fondos/límites y distinguen las filas de elementos de las filas de límites.

## Platformer Game Kit

La importación inicial con el código del kit activo no compila: faltan tipos de Animancer (ClipTransition, Units, Validate, entre otros). El usuario autorizó desactivar únicamente el código del kit, conservando las imágenes y archivos.

Se utiliza una condición de compilación en Code/PlatformerGameKit.asmdef. Para reactivarlo después de instalar sus dependencias compatibles, elimina la condición CREAJUEGO_ENABLE_PLATFORMER_GAME_KIT o añade ese símbolo a las opciones de compilación. Los prefabs y escenas propios del kit pueden mostrar scripts no disponibles mientras esté desactivado; sus sistemas de juego completos no funcionan en ese estado.

No se generaron builds standalone ni se hizo commit o push.

Validación final: **67/67 tests pasan, 0 fallos**, con todos los assets presentes y Platformer Game Kit desactivado únicamente por defineConstraints. Unity 6000.6.0f1. Evidencia local: Docs/categories-gated-tests.xml y Docs/categories-gated-tests.log. Se verificaron hashes al trasladar los cambios al proyecto principal; respaldo previo en .spike/CategoriesBeforeSync. La revisión visual manual en el Editor no se ha ejecutado.
