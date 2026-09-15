# Piloto de 3 horas: guía de operación

Esta guía está pensada para quien facilita el primer taller con CreaJuego en Unity **6000.6.0f1**. CreaJuego funciona dentro del Editor; el taller no requiere ni genera un ejecutable.

## Antes de recibir al grupo

1. Abre el proyecto con Unity 6000.6.0f1 y espera a que termine la importación.
2. Comprueba que la Console no tenga errores rojos de compilación.
3. Ejecuta **CreaJuego > Configurar editor**. Esto carga el layout del taller y deja Juego en 16:9.
4. Ejecuta **CreaJuego > Abrir taller** si alguna de las tres ventanas no aparece.
5. Haz una prueba corta con la escena demo: Jugador sobre Plataforma, una Meta y **JUGAR**.

## Inicio de cada equipo

1. Ejecuta **CreaJuego > Nuevo juego**.
2. Escribe el nombre del juego y del equipo. Si hay cambios sin guardar, Unity preguntará qué hacer antes de abrir la escena nueva.
3. CreaJuego guarda una ruta propia en `Assets/TrabajosTaller/Equipo_Nombre/Juego_Nombre.unity` y una carpeta `Imagenes` junto a la escena. Si el nombre ya existe, crea una variante numerada; nunca sobrescribe otro juego.
4. La escena nueva incluye cámara, seguimiento, marcador, sesión, indicaciones y un fondo inicial cuando el pack lo ofrece.

## Secuencia recomendada del taller

1. Añadir **Jugador**.
2. Añadir una **Plataforma** y colocar al Jugador encima.
3. Construir un recorrido corto con más plataformas.
4. Añadir **Premio**, **Peligro** o **Enemigo**.
5. Añadir una **Meta**.
6. Revisar la barra inferior. Sólo bloquean la prueba: la preparación de escena, un único Jugador, una Plataforma, el Jugador apoyado, la recuperación de caída y una Meta. Premio, Peligro, Enemigo y Decoración son opcionales.
7. Pulsar **JUGAR**, enfocar Juego y probar con A/D o flechas, Espacio y X.

La **Plataforma móvil** aparece en **EXTRAS** para usarla cuando el recorrido básico ya funcione.

## Imágenes propias

Selecciona un elemento, abre **APARIENCIA** y pulsa **Elegir imagen del computador…**. Se aceptan PNG y JPG. CreaJuego copia el archivo a la carpeta `Imagenes` del juego, lo importa como Sprite 2D y lo aplica únicamente al visual. El original queda intacto; movimiento, colisiones y reglas no cambian.

**Tamaño de la imagen** escala sólo el dibujo. No cambia la zona de choque. Si la imagen no coincide con la superficie, ajusta el diseño en SceneView o usa una apariencia preparada.

## Recuperación y menú del facilitador

Las acciones de emergencia están en **CreaJuego > Facilitador**:

- **Revisar juego** abre la barra y escribe el diagnóstico en Console.
- **Preparar escena** recupera servicios, cámara y protección contra caídas sin reemplazar contenido válido.
- **Volver jugador al inicio** devuelve al personaje a su posición segura y pone su velocidad a cero. En edición admite Undo.
- **Abrir carpeta de trabajos** muestra los juegos guardados.
- **Reabrir ventanas del taller** recupera Elementos, Propiedades y Jugar.

Si el Jugador comienza en el aire, **JUGAR** se bloquea y muestra: “Coloca al Jugador encima de una Plataforma antes de jugar”. Si cae muy por debajo de su inicio durante la partida, vuelve automáticamente al punto inicial con la velocidad reiniciada.

## Cierre y recogida

1. Sal de Play Mode.
2. Guarda la escena con Ctrl+S.
3. Comprueba que cada equipo tenga su escena y su carpeta `Imagenes` bajo `Assets/TrabajosTaller`.
4. Abre de nuevo una escena de muestra y confirma que sus apariencias y metadatos persisten.

No uses Player Build, `BuildPipeline.BuildPlayer` ni la carpeta `Builds/` para este piloto. La evidencia de cierre es: compilación del Editor, EditMode tests, Console limpia, persistencia y recorrido jugable dentro de Unity.
## Checklist manual pendiente

Esta revisión debe realizarse dentro del Editor antes del piloto. No se marcó como realizada en este sprint:

- [ ] Configurar editor.
- [ ] Crear un juego nuevo.
- [ ] Escribir equipo y nombre.
- [ ] Añadir Jugador.
- [ ] Añadir Plataforma.
- [ ] Importar un PNG y asignarlo al Jugador.
- [ ] Comprobar que el Jugador sigue moviéndose.
- [ ] Crear un hueco, caer y comprobar la recuperación.
- [ ] Añadir Premio, Peligro, Enemigo y Meta.
- [ ] Jugar, recibir daño, atacar y derrotar al Enemigo.
- [ ] Llegar a la Meta y detener Play Mode.
- [ ] Guardar, cerrar, reabrir y volver a jugar.
- [ ] Revisar selección, scroll, miniaturas, layout y claridad de mensajes.
- [ ] Confirmar que no aparecen tecnicismos innecesarios.