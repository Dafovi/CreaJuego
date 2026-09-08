# CreaJuego — análisis de comportamientos y propuesta del siguiente sprint

**Decisión recomendada:** una primera capa educativa de **7 elementos**: Jugador, Plataforma, Premio, Peligro, Meta, Plataforma móvil y Enemigo. Los primeros cinco son P0; los dos últimos son P1 condicionados a validar su comportamiento compuesto. El sprint debe consolidar propiedades, creación segura y reglas fijas de esos elementos antes de ampliar la interfaz.

Este documento es una propuesta, **no una implementación nueva**. No se modificaron scripts, prefabs, escenas, dependencias ni UI, ni se hicieron commits. El directorio actual sigue sin repositorio Git inicializado.

## Alcance y evidencias de la inspección

Se leyeron los **42 scripts Runtime de Playground**, los tres archivos del adaptador, la fachada y definiciones propias, ItemService, la ventana actual, DemoBuilder, los ocho assets de definición, sus prefabs, el YAML de la demo, los tests y su resultado almacenado. Se cotejaron los SHA256 de los 42 scripts vendor contra el inventario del spike: **0 diferencias**. La inspección no supone una nueva ejecución de las pruebas.

- Unity del proyecto: **6000.6.0f1 (f7f8ed4d1e24)**.
- Dependencias declaradas: Input System **1.20.0**, uGUI **2.6.0**, Test Framework **1.8.0**; pipeline Built-in.
- Playground integrado: **1.8.0 adaptado**, con MIT preservada, commit upstream fijado en el informe anterior. Se analiza esta copia concreta, no una supuesta versión distinta.
- [Resultado almacenado](../Docs/tests-editmode.xml): **7/7 aprobadas**, fecha 2026-09-07. Incluye recorrido con teclado y pruebas de contactos aislados, UI y Undo. No demuestra que todas las propiedades, extremos y combinaciones sean estables.
- [Código de las pruebas](../Assets/CreaJuegoPacks/Starter/Tests/Editor/SpikeTests.cs): la prueba de Patrol sólo comprueba desplazamiento hacia la derecha durante un intervalo; no comprueba retorno, orientación, pasajeros ni enemigo.
- [Informe del spike](../Docs/CreaJuego-Technical-Spike.md): build Windows correcto; revisión manual con participantes pendiente. No se repitió compilación porque esta entrega sólo añade documentación.

### Uso real en la escena, verificado por referencias de prefab

| Elemento | Instancias en demo | Composición de comportamiento | Propiedades que la ventana ofrece hoy |
|---|---:|---|---|
| Jugador | 1 | Move + Jump + HealthSystemAttribute + PlaygroundAdapter | Velocidad 0,2–5; Salto 5–18; Vidas 1–10 |
| Plataforma | 2 | SpriteRenderer + BoxCollider2D sólido; fachada/adapter para color | Color |
| Plataforma móvil | 1 | Patrol + Rigidbody2D cinemático + collider sólido | Velocidad 0,2–3; Distancia 0,5–6 |
| Premio | 2 | CollectableAttribute + trigger | Puntos 1–100 |
| Peligro | 1 | ModifyHealthAttribute + trigger; desaparece tras contacto | Daño 1–10; Desaparecer al tocarlo |
| Meta | 1 | ConditionArea Enter/Player/una vez + ReachGoalAction propia | Mensaje al llegar |
| Enemigo | **0** | Prefab disponible: Patrol + ModifyHealthAttribute + cuerpo cinemático + trigger; no se destruye al tocar | Velocidad 0,2–3; Distancia 0,5–6; Daño 1–10 |
| Decoración | 1 | SpriteRenderer sin collider; fachada/adapter | Color |

Hay una cámara fija, un UIScript en modo **Life** para un jugador y el puente DemoSession. No hay CameraFollow, DialogueSystem ni AudioSource de efectos en la demo. Hay AudioListener, que por sí solo no reproduce sonidos. UIScript conserva `scoreToWin=5`, pero el modo Life no usa ese umbral para ganar. `inventory` y `resourceItemPrefab` están sin asignar.

Fuentes directas: [escena](../Assets/CreaJuegoPacks/Starter/Demo/CreaJuegoPlaygroundDemo.unity), [generador del pack](../Assets/CreaJuegoPacks/Starter/Editor/DemoBuilder.cs), [adapter efectivo](../Packages/com.dafovi.creajuego/Adapters/Playground/PlaygroundAdapter.cs). El generador ayuda a comprender intención; el inventario anterior se contrastó con los assets guardados.

## Hallazgos que deben influir en el sprint

1. **No hay clases Playground “Platform”, “Enemy” ni “Goal” en esta copia.** Son composiciones pedagógicas. La plataforma estática no necesita ningún comportamiento Playground; el enemigo combina patrulla/daño; la meta combina área/acción propia.
2. **No basta cambiar etiquetas.** Move.speed es fuerza escalada; FollowTarget.speed es factor Lerp; ObjectShooter.creationRate es espera entre pulsaciones; AutoMove no tiene speed. El descriptor debe respetar esas semánticas.
3. **Patrol.directionChangeInterval es un campo muerto.** Se declara pero nunca se lee. No mostrar “Pausa” ni “Cambiar dirección cada”. Patrol siempre añade el origen al ciclo; no hay opción nativa de no regresar.
4. **“Vidas” promete más de lo que existe.** HealthSystemAttribute representa salud; no reapariciones. Recomiendo “Resistencia inicial” con ayuda “Cada golpe resta resistencia; con cero termina la partida”. Si se prefieren “Vidas”, habrá que implementar vidas/reinicio como otra regla.
5. **“Puede saltar” requiere un nuevo dato de fachada que controle Jump.enabled.** checkGround significa comprobar contacto con suelo, no habilitar salto. La demo usa Untagged: tocar una pared sin tag también rearma el salto.
6. **El estado de fin está incompleto como contrato de juego.** ReachGoalAction busca cualquier DemoSession; Complete cambia texto y deshabilita todos los Move/Jump de la escena. No pone a cero velocidad ni impide daño/puntuación posteriores. UIScript tiene su propio booleano privado gameOver, que no se sincroniza con Completed de la meta.
7. **El catálogo puede crear objetos en una escena sin servicios.** ItemService valida prefab/fachada/backend, pero no cámara, UIScript, sesión o unicidad de jugador. Un premio puede desaparecer sin sumar si falta UI. Duplicar jugador puede causar que varios actores compartan el marcador del Player.
8. **Hay una fuente de datos educativa, pero no previsualización general.** El adapter aplica en Awake. Cambiar Color en la ventana no actualiza SpriteRenderer en edición. Los componentes vendor del prefab pueden mostrar valores distintos de los efectivos hasta aplicar la fachada. No enlazar ambas superficies a la vez.
9. **Los generadores Runtime colocan después de Instantiate.** CreateObjectAction/ObjectCreatorArea instancian antes de mover. Awake de PlaygroundAdapter ya calculó los puntos relativos de Patrol: una patrulla creada lejos del origen podría recibir un destino equivocado. Resolver la inicialización antes de ofrecer spawning.
10. **Inventario, diálogos y disparos no están listos sólo por compilar.** Faltan widgets/prefabs/tags Pickup y Bullet, y quedan accesos a Keyboard.current sin guardia en componentes no usados.
11. **Errores semánticos localizados por lectura:** PickUpAndHold compara distancia al cuadrado con alcance lineal; ConditionRepeat usa Time.deltaTime en el reloj inicial; BalloonScript llama un delegado sin null check; UIScript no implementa el significado “sin límite” del comentario scoreToWin=-1.
12. **No atribuir todo a APIs antiguas.** uGUI Text, Invoke y SceneManager no se declaran eliminados aquí. Son deuda de diseño/validación. La incompatibilidad compilatoria ya documentada fue Input System 1.18.0 con EntityId; la copia actual usa 1.20.0. El resto de estos hallazgos es análisis estático y necesita pruebas dirigidas, no se presenta como fallos nuevos reproducidos.

## Criterios de prioridad y alcance de Undo

**P0 imprescindible:** completa el circuito control → interacción → resultado, o garantiza sus dependencias. **P1 muy útil:** amplía el mismo circuito con poca novedad pedagógica. **P2 interesante:** exige otro modelo (inventario, mensajes encadenados, reglas). **P3 no necesario ahora:** infraestructura que no debe exponerse o un género distinto. Una prioridad P0 de infraestructura no significa que deba aparecer como tarjeta.

La facilidad de cada ficha evalúa una envoltura segura para autoría; no sólo el coste de AddComponent. “Probado” se limita a los casos almacenados. “Sólo inspeccionado/compilado previamente” no es evidencia funcional.

- **U1 — configuración:** serializar en GameItem u otro componente/asset propio, enlazar SerializedObject/SerializedProperty y aplicar antes de jugar. Permite Undo, persistencia y overrides. En propiedades derivadas no guardar dos fuentes independientes. La UI debe validar tipo, rango y dependencia, no asumir que cualquier path es seguro.
- **U2 — estructura/escena:** crear/duplicar/eliminar con servicios Editor, PrefabUtility y Undo; agrupar operaciones y registrar todos los objetos afectados. Para previsualizar color, tamaño o gizmos, recalcular al editar y tras Undo/Redo, sin crear componentes silenciosamente.
- **Runtime:** recoger, destruir, sumar puntos, teletransportar, cargar escenas y disparar no son acciones Undo del Editor. No usar SerializedObject para intentar deshacer una partida. El estado de autoría permanece al salir de Play Mode.
- Los campos públicos heredados de Unity (enabled, transform, etc.) se omiten de las listas salvo uso educativo explícito. `rigidbody2D` se indica como heredado de Physics2DObject. En condiciones, la ficha ConditionBase enumera **todos** los campos heredados para evitar perderlos en la evaluación.
- Las opciones propuestas que no existen hoy están señaladas; no se presupone que EducationalControl ya soporte dropdown, selector de objeto, propiedades condicionales o texto multilínea. Hoy ofrece float, int, bool, string y color.

## Inventario priorizado por categoría pedagógica

Cada ficha cubre nombre, archivo, operación técnica, superficie actual, selección/renombrado/control, ocultación, dificultad, prioridad, riesgos, adapter, Undo y decisión de MVP.

### Categoría: Personaje

#### 01. Playground.Movement.Move — P0

**Archivo:** [Scripts/Movement/Move.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Move.cs).

**Qué hace:** Normaliza flechas/WASD, restringe ejes y aplica AddForce en FixedUpdate; puede orientar todo el transform.

**Propiedades/campos actuales:** speed; typeOfControl; movementType; orientToDirection; lookAxis (+ rigidbody2D heredado).

**Mostrar y renombrar / controles:** speed → Velocidad [slider 0,2–5 sobre GameItem.speed]; typeOfControl → Controles [dropdown Flechas/WASD, ampliación opcional].

**Ocultar:** movementType=OnlyHorizontal para este pack; orientToDirection=false; lookAxis; rigidbody2D.

**Facilidad:** Fácil. **MVP educativo:** Sí, personaje. **Evidencia:** Probado en demo.

**Riesgos y dependencias:** La escala nativa es fuerza, no m/s: adapter actual speed/20. Depende de masa, fricción y damping. Está parcheado a fixedDeltaTime. Orientar el transform también gira la forma de colisión; no prometer giro visual independiente.

**Adapter/facade:** Sí; conservar conversión actual y fijar configuración física del pack.

**Undo/SerializedObject:** U1; speed y controles se editan en fachada; no enlazar el mismo valor simultáneamente al vendor.

#### 02. Playground.Movement.Jump — P0

**Archivo:** [Scripts/Movement/Jump.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Jump.cs).

**Qué hace:** Aplica impulso vertical al pulsar una tecla. Rearma el salto al comenzar cualquier colisión con groundTag.

**Propiedades/campos actuales:** key; jumpStrength; groundTag; checkGround (+ rigidbody2D).

**Mostrar y renombrar / controles:** jumpStrength → Fuerza de salto [slider 5–18]; enabled → Puede saltar [toggle mediante nuevo campo de fachada, NO mediante checkGround].

**Ocultar:** key=Space en MVP; groundTag; checkGround=true; rigidbody2D; estado privado canJump.

**Facilidad:** Media. **MVP educativo:** Sí, con corrección de suelo. **Evidencia:** Probado en demo.

**Riesgos y dependencias:** El adapter fija groundTag=Untagged: una pared también rearma. canJump comienza true, incluso en aire. checkGround=false permite saltos repetidos en aire; no es desactivar salto. Teclado ausente ya protegido. Altura depende de gravedad/masa/damping.

**Adapter/facade:** Sí; conservar impulso y añadir una comprobación de apoyo fiable antes de ampliar opciones.

**Undo/SerializedObject:** U1; nuevo canJump educativo controla enabled al iniciar. No editar el estado privado runtime.

#### 03. Playground.Attributes.HealthSystemAttribute — P0

**Archivo:** [Scripts/Attributes/HealthSystemAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/HealthSystemAttribute.cs).

**Qué hace:** Inicializa máximo de salud en Start, limita curación, actualiza UIScript y destruye el objeto con salud <=0.

**Propiedades/campos actuales:** health; método público ModifyHealth(int); privados maxHealth, playerNumber, ui..

**Mostrar y renombrar / controles:** health → Resistencia inicial [campo entero 1–10; mostrar corazones si conviene].

**Ocultar:** ui, playerNumber, maxHealth, tags y conexiones de HUD; no presentar ModifyHealth como control de configuración.

**Facilidad:** Fácil. **MVP educativo:** Sí, soporte interno del jugador. **Evidencia:** Probado en demo.

**Riesgos y dependencias:** No son vidas con reaparición: es salud que se consume. Start debe ejecutarse antes de recibir curación. Busca cualquier UIScript; más de una sesión puede cruzar datos. No genera un evento propio de derrota desacoplado del HUD.

**Adapter/facade:** Sí; GameItem.health ya lo alimenta. Evitar renombrar a Vidas si no implementamos reaparición.

**Undo/SerializedObject:** U1 para salud inicial; la salud restante runtime no es un cambio Undo de autoría.


### Categoría: Movimiento

#### 04. Playground.Movement.AutoMove — P2

**Archivo:** [Scripts/Movement/AutoMove.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/AutoMove.cs).

**Qué hace:** Aplica continuamente una fuerza direction*2 en coordenadas locales o globales.

**Propiedades/campos actuales:** direction (Vector2); relativeToRotation (+ rigidbody2D).

**Mostrar y renombrar / controles:** direction descompuesto → Dirección [dropdown cardinal] + Impulso continuo [slider de magnitud]; relativeToRotation → Seguir orientación [toggle sólo modo ampliado].

**Ocultar:** Vector2 crudo, referencia física; fijar espacio global en primera experiencia.

**Facilidad:** Fácil. **MVP educativo:** Fuera del primer MVP. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** No tiene speed: llamar Velocidad a la magnitud promete una rapidez constante inexistente. Acelera y depende de damping. Los gizmos cargan meshes ausentes en la copia.

**Adapter/facade:** Sí; descomposición dirección/magnitud, preset físico y límites.

**Undo/SerializedObject:** U1; guardar dirección conceptual y convertir antes de ejecutar.

#### 05. Playground.Movement.Push — P2

**Archivo:** [Scripts/Movement/Push.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Push.cs).

**Qué hace:** Mientras la tecla está mantenida aplica fuerza continua sobre X/Y, local o global.

**Propiedades/campos actuales:** key; pushStrength; axis; relativeAxis (+ rigidbody2D).

**Mostrar y renombrar / controles:** pushStrength → Potencia del impulso [slider]; axis → Dirección [dropdown horizontal/vertical]; key → Tecla [dropdown acotado].

**Ocultar:** relativeAxis y rigidbody2D en modo inicial; no llamarlo salto.

**Facilidad:** Fácil. **MVP educativo:** Fuera; propulsión futura. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Keyboard.current sin protección. El nombre sugiere impulso único, pero usa IsPressed y AddForce, no Impulse. Espacio entra en conflicto con Jump.

**Adapter/facade:** Sí; contrato de propulsión y teclas no conflictivas.

**Undo/SerializedObject:** U1; valores editables, actuación física sólo durante prueba.

#### 06. Playground.Movement.Rotate — P3

**Archivo:** [Scripts/Movement/Rotate.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Rotate.cs).

**Qué hace:** Lee eje horizontal del teclado y aplica torque negativo proporcional a speed.

**Propiedades/campos actuales:** typeOfControl; speed (+ rigidbody2D).

**Mostrar y renombrar / controles:** speed → Intensidad de giro [slider]; typeOfControl → Controles [dropdown].

**Ocultar:** rigidbody2D y torque firmado; no exponer al personaje de plataformas.

**Facilidad:** Fácil. **MVP educativo:** No; candidato para pack de naves. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Requiere rotación libre, incompatible con freezeRotation del jugador actual. Comparte flechas con Move. Torque no equivale a grados/segundo.

**Adapter/facade:** Sí, si se habilita un pack específico.

**Undo/SerializedObject:** U1; no modifica la jerarquía al editar.


### Categoría: Plataformas

### Plataforma estática — componentes nativos, no Playground

**Clases:** UnityEngine.SpriteRenderer + UnityEngine.BoxCollider2D. **Archivo de proyecto que las configura:** [DemoBuilder.cs](../Assets/CreaJuegoPacks/Starter/Editor/DemoBuilder.cs); sus implementaciones son nativas de Unity, no scripts Platform.cs del repositorio.

**Técnicamente:** imagen con tamaño y superficie sólida. **Superficie relevante actual:** SpriteRenderer.sprite/color/drawMode/size; BoxCollider2D.size/isTrigger/sharedMaterial; transform y escala. No es un inventario completo del Inspector nativo. **Mostrar:** color → Color [paleta]; tamaño horizontal → Ancho [número/slider propuesto]. **Ocultar:** resto de configuración técnica, tamaño de collider separado, material y colisiones. **Facilidad:** Fácil. **Riesgo:** desajustar tamaño visual/físico o duplicar escalado; fijar isTrigger=false. **Adapter:** servicio propio de forma/aspecto, no dependencia Playground. **Undo:** U1+U2 al actualizar más de un objeto. **MVP:** Sí, **P0**. Color existe; ancho y preview inmediato son propuestas.

#### 07. Playground.Movement.Patrol — P1

**Archivo:** [Scripts/Movement/Patrol.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Patrol.cs).

**Qué hace:** Copia puntos absolutos en Start, añade origen y recorre el ciclo con MovePosition; considera llegado a <=0,1 unidades.

**Propiedades/campos actuales:** speed; directionChangeInterval; orientToDirection; lookAxis; waypoints (Vector2[]) (+ rigidbody2D).

**Mostrar y renombrar / controles:** speed → Velocidad [slider 0,2–3]; waypoints generados desde Distancia [slider 0,5–6] y, si se aprueba, Dirección [dropdown derecha/izquierda/arriba/abajo].

**Ocultar:** directionChangeInterval NO SE USA; array absoluto waypoints; lookAxis; orientToDirection=false para plataformas; rigidbody2D.

**Facilidad:** Media. **MVP educativo:** Sí, extensión condicionada del MVP. **Evidencia:** Movimiento en demo probado parcialmente.

**Riesgos y dependencias:** No existe distance, ni toggle de regreso: CreaJuego genera un destino y Patrol añade el origen siempre. Start cachea recorrido: editar waypoints en Play no actualiza newWaypoints. Sin puntos y orientToDirection=true accede a índice 1 inexistente. Puede sobrepasar y oscilar si paso/tolerancia son incompatibles. No garantiza transportar al pasajero.

**Adapter/facade:** Sí; recorrido relativo ya existe. Añadir validación de trayectoria y prueba de ida/vuelta/transporte.

**Undo/SerializedObject:** U1; duplicar no debe copiar destinos absolutos. U2 si se dibujan/editar gizmos de puntos.


### Categoría: Enemigos

### Enemigo actual — composición, no clase Enemy

El [prefab de enemigo](../Assets/CreaJuegoPacks/Starter/Content/enemigo.prefab) contiene **Playground.Movement.Patrol + Playground.Attributes.ModifyHealthAttribute**. Sus propiedades y controles se evalúan en Plataformas y Peligros. Dificultad combinada **Media**, prioridad **P1** y gate de prueba de patrulla/contacto. No tiene HealthSystemAttribute ni una acción de morir al saltarle encima. Es un cuerpo cinemático con trigger: no asumir navegación ni respuesta sólida a paredes.

#### 08. Playground.Movement.FollowTarget — P2

**Archivo:** [Scripts/Movement/FollowTarget.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/FollowTarget.cs).

**Qué hace:** MovePosition hacia un Transform usando Lerp; opcionalmente orienta hacia él.

**Propiedades/campos actuales:** target; speed; lookAtTarget; useSide (+ rigidbody2D).

**Mostrar y renombrar / controles:** target → A quién sigue [selector restringido a personajes]; speed → Intensidad de seguimiento [slider]; lookAtTarget → Mirar al personaje [toggle ampliado].

**Ocultar:** useSide; Transform libre; rigidbody2D.

**Facilidad:** Media. **MVP educativo:** Fuera; enemigo perseguidor posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Tolera target=null quedándose inmóvil. speed es factor de aproximación, no velocidad constante; sin navegación ni evitación de obstáculos. Una referencia de escena no debe persistirse en un prefab asset.

**Adapter/facade:** Sí; resolver objetivo por referencia de instancia/rol y mostrar falta de objetivo.

**Undo/SerializedObject:** U1 para referencia serializada de instancia; resolución runtime separada.

#### 09. Playground.Movement.Wander — P2

**Archivo:** [Scripts/Movement/Wander.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/Wander.cs).

**Qué hace:** Cambia dirección aleatoria por corrutina y aplica fuerza; cerca del límite heurístico apunta al origen.

**Propiedades/campos actuales:** speed; directionChangeInterval; keepNearStartingPoint; orientToDirection; lookAxis (+ rigidbody2D).

**Mostrar y renombrar / controles:** speed → Energía del movimiento [slider]; directionChangeInterval → Cambiar rumbo cada [campo segundos]; keepNearStartingPoint → Mantenerse cerca [toggle].

**Ocultar:** lookAxis y rigidbody2D; orientación fijada por pack.

**Facilidad:** Media. **MVP educativo:** Fuera; fauna/decoración viva posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Aquí sí se usa directionChangeInterval y se limita a >=0,1. Aleatoriedad sin semilla/puntos controlados. No es radio estricto: usa 1+speed*0,1 como heurística. Física puede sacar al objeto del área.

**Adapter/facade:** Sí; no ofrecer radio exacto ni convertirlo en enemigo básico del primer taller.

**Undo/SerializedObject:** U1; la trayectoria aleatoria durante ejecución no se guarda ni deshace.


### Categoría: Peligros

#### 10. Playground.Attributes.ModifyHealthAttribute — P0

**Archivo:** [Scripts/Attributes/ModifyHealthAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/ModifyHealthAttribute.cs).

**Qué hace:** En entrada de trigger o colisión modifica HealthSystemAttribute del otro objeto; puede destruirse tras activarse.

**Propiedades/campos actuales:** healthChange; destroyWhenActivated.

**Mostrar y renombrar / controles:** healthChange → Daño [entero positivo 1–10, backend = -daño]; destroyWhenActivated → Desaparecer al tocar al personaje [toggle].

**Ocultar:** Signo de healthChange, búsqueda del componente, triggers/colliders y referencias técnicas.

**Facilidad:** Muy fácil. **MVP educativo:** Sí, peligro; reutilizado por enemigo. **Evidencia:** Probado con peligro, no combinación enemigo.

**Riesgos y dependencias:** No filtra sólo jugador: afecta cualquier objeto con HealthSystemAttribute. Daño al entrar, no periódico mientras permanece dentro. Sin invulnerabilidad temporal; múltiples contactos/reentradas pueden sumar daño. Health positivo sería curación y merece otro concepto.

**Adapter/facade:** Sí; traducción de signo ya existe. Añadir política de objetivo si se amplía el catálogo.

**Undo/SerializedObject:** U1 para valores iniciales; Destroy y cambios de salud runtime no son Undo.


### Categoría: Coleccionables

#### 11. Playground.Attributes.CollectableAttribute — P0

**Archivo:** [Scripts/Attributes/CollectableAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/CollectableAttribute.cs).

**Qué hace:** Al entrar Player/Player2 suma puntos al UIScript encontrado y destruye este objeto.

**Propiedades/campos actuales:** pointsWorth.

**Mostrar y renombrar / controles:** pointsWorth → Puntos [campo entero 1–100, predeterminado 1].

**Ocultar:** Tags, UIScript, trigger; desaparición fijada, no mostrar un toggle inexistente.

**Facilidad:** Muy fácil. **MVP educativo:** Sí. **Evidencia:** Probado con suma y desaparición.

**Riesgos y dependencias:** Sin UIScript desaparece igual y no registra puntuación. No hay evento público recogido ni referencia de sesión asignable. Dos colliders pueden requerir guardia de activación única, a probar. El backend no lee GameItem.disappear para premios.

**Adapter/facade:** Sí; GameItem.points ya existe; validar marcador de la escena.

**Undo/SerializedObject:** U1; configuración normal. Desaparición runtime no se refleja en escena de autoría.

#### 12. Playground.Attributes.ResourceAttribute — P2

**Archivo:** [Scripts/Attributes/ResourceAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/ResourceAttribute.cs).

**Qué hace:** Añade cantidad de recurso al inventario UIScript usando índice y sprite; luego destruye el objeto.

**Propiedades/campos actuales:** resourceIndex; amount.

**Mostrar y renombrar / controles:** resourceIndex → Recurso [selector de definición con id estable, traducido internamente]; amount → Cantidad [entero >=1].

**Ocultar:** Índice crudo, SpriteRenderer, tags, UI/inventario y widget prefab.

**Facilidad:** Media. **MVP educativo:** No, inventario no necesario aún. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** RequireComponent SpriteRenderer; falta UI produce warning y destruye recurso. En demo inventory/resourceItemPrefab están vacíos: no basta crear este componente. Reordenar índices cambia significado de partidas/contenido.

**Adapter/facade:** Sí; recursos con ids estables y servicio de inventario, no sólo etiquetas.

**Undo/SerializedObject:** U1 para definiciones y cantidad; compras/consumo no Undo runtime.


### Categoría: Meta

### Meta actual — extensión propia sobre condición real

**Clases:** Playground.Conditions.ConditionArea + **CreaJuego.PlaygroundBackend.ReachGoalAction**, apoyadas por **CreaJuego.PlaygroundBackend.DemoSession**. Archivos propios: [ReachGoalAction.cs](../Packages/com.dafovi.creajuego/Adapters/Playground/ReachGoalAction.cs), [DemoSession.cs](../Packages/com.dafovi.creajuego/Adapters/Playground/DemoSession.cs).

**Operación:** ConditionArea invoca ExecuteAction; la acción lee GameItem.message y llama Complete. **Propiedades actuales:** ReachGoalAction no declara campos; DemoSession expone playgroundUI, status y Completed con setter privado; GameItem aporta message. **Mostrar:** message → Mensaje al llegar [texto]. **Ocultar:** Text, UIScript, booleano runtime Completed y configuración fija de condición. **Facilidad:** Fácil para mensaje, Media para fin fiable. **Riesgos:** sesión global ambigua, inercia/daño posterior y dos estados de fin independientes. **Adapter:** sí, ya existe; consolidar servicio de resultado. **Undo:** U1 para mensaje, sin Undo de partida. **MVP:** Sí, **P0**. No llamar a esto Goal de Playground; la parte propia es explícita.

#### 13. Playground.Conditions.Actions.LoadLevelAction — P2

**Archivo:** [Scripts/Conditions/Actions/LoadLevelAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/LoadLevelAction.cs).

**Qué hace:** Carga escena única por nombre; la cadena especial "0" recarga la activa.

**Propiedades/campos actuales:** levelName; constante SAME_SCENE="0" (no editable).

**Mostrar y renombrar / controles:** levelName → Al terminar [dropdown Reiniciar / Ir a nivel] + Nivel [selector validado de niveles, posterior].

**Ocultar:** Nombre libre y valor mágico "0"; Build Settings, paths, modo Single.

**Facilidad:** Media. **MVP educativo:** No para meta básica; reinicio como acción del sistema. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Carga descarta estado de la partida actual y requiere escena incluida/disponible. No es un componente de victoria. No hay validación de existencia antes de LoadScene.

**Adapter/facade:** Sí; descriptor de nivel o catálogo de escenas cuando haya varios niveles.

**Undo/SerializedObject:** U1 configuración; ejecutar LoadScene no tiene Undo ni debe dispararse editando controles.


### Categoría: Cámara

#### 14. Playground.Movement.CameraFollow — P1

**Archivo:** [Scripts/Movement/CameraFollow.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/CameraFollow.cs).

**Qué hace:** Interpola hacia target en FixedUpdate y mueve cámara en LateUpdate; opcionalmente limita borde visible.

**Propiedades/campos actuales:** target; limitBounds; left; right; bottom; top.

**Mostrar y renombrar / controles:** target → Seguir a [selector de personaje, auto por defecto]; limitBounds → Limitar al nivel [toggle]; límites → Área del nivel [rectángulo visual acotado, no cuatro números inicialmente].

**Ocultar:** Z=-10 y suavizado*10 están codificados, no son propiedades; referencia Camera y límites crudos.

**Facilidad:** Media. **MVP educativo:** Útil después del MVP fijo, no séptimo elemento nuevo. **Evidencia:** No usado: cámara fija actual.

**Riesgos y dependencias:** No RequireComponent Camera; limitBounds necesita Camera real. Rectángulo menor que viewport produce límites contradictorios. Mezcla actualización física y visual; suavidad/aspectos no probados. Zoom pertenece a Camera, no CameraFollow.

**Adapter/facade:** Sí; configuración de escena y autoobjetivo, validación de bounds.

**Undo/SerializedObject:** U1 para configuración de cámara; seguimiento runtime no debe registrarse con Undo.


### Categoría: Sonido

### Sonido — no hay componente Playground dedicado en esta copia

**Clase candidata:** UnityEngine.AudioSource (nativa). **Archivo:** no hay Sound.cs ni un adapter de sonido en el proyecto; referencia pública instalada comprobada en `UnityEngine.AudioModule.xml` de Unity 6000.6.0f1.

**Operación técnica:** reproducir clips de audio. **Propiedades relevantes verificadas:** clip, volume, loop, playOnAwake, spatialBlend (no es un inventario completo del componente nativo). **Mostrar después:** clip → Sonido [selector de clips aprobados del pack], volume → Volumen [slider 0–100% traducido a 0–1], loop → Repetir [toggle para ambiente]. **Ocultar:** spatialBlend fijado a 2D y playOnAwake fijado según intención; mixers/atenuación/spatialización no forman parte de los controles propuestos.

**Facilidad:** Fácil para ambiente; Media para feedback de recogida. **Riesgos/dependencias:** faltan clips; se necesita AudioListener; al destruir el premio se cortaría un AudioSource situado en él. Usar un emisor de sesión o efecto independiente si se incorpora feedback. CollectableAttribute no expone evento de recogida público, por lo que conectar sonido sin duplicar la lógica requiere una extensión deliberada. **Adapter:** propio, de Unity, no “heredado de Playground”. **Undo:** U1 para selección/volumen; reproducir muestra no es cambio serializado. **MVP:** No entre las siete tarjetas, **P2** para configuración; un sonido fijo de feedback puede evaluarse más adelante.


### Categoría: Puntuación

#### 15. Playground.UserInterface.UIScript — P0

**Archivo:** [_Internal/Scripts/UserInterface/UIScript.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/UserInterface/UIScript.cs).

**Qué hace:** Mantiene arrays privados de puntos/salud, diccionario de recursos y decide panel de victoria/derrota; actualiza uGUI Text.

**Propiedades/campos actuales:** numberOfPlayers; gameType; scoreToWin; numberLabels[]; rightLabel; leftLabel; winLabel; statsPanel; gameOverPanel; winPanel; inventory; resourceItemPrefab. Métodos: AddPoints/AddOnePoint/RemoveOnePoint, SetHealth/ChangeHealth, GameWon/GameOver, AddResource/CheckIfHasResources/ConsumeResource..

**Mostrar y renombrar / controles:** Ninguna referencia al participante. Futuro: gameType → Objetivo [dropdown de reglas de sesión]; scoreToWin → Puntos para ganar [entero sólo si esa regla existe]. En MVP mostrar marcador, sin configurarlo..

**Ocultar:** numberOfPlayers=OnePlayer; gameType=Life actual; scoreToWin inactivo en demo; todas las referencias y arrays; no exponer métodos como campos.

**Facilidad:** Difícil. **MVP educativo:** Sí como infraestructura temporal, no tarjeta del catálogo. **Evidencia:** Puntos/salud/derrota probados; inventario/multijugador no.

**Riesgos y dependencias:** Acopla estado a presentación; no getters/eventos de puntuación. El comentario scoreToWin=-1 no coincide con AddPoints: en modo Score cualquier puntuación >=-1 gana. GameWon produce inglés y sólo conmuta paneles; no detiene simulación. Varios UIScript ambiguos por FindAnyObjectByType.

**Adapter/facade:** Sí; servicio de sesión mínimo y puente legado. Reemplazar esta responsabilidad progresivamente, no toda Playground.

**Undo/SerializedObject:** U1 para ajustes iniciales; arrays/diccionario privados y eventos de partida no son edición serializada.

#### 16. Playground.Attributes.DestroyForPointsAttribute — P3

**Archivo:** [Scripts/Attributes/DestroyForPointsAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/DestroyForPointsAttribute.cs).

**Qué hace:** Al tocar Bullet otorga puntos a BulletAttribute.playerId si encuentra UIScript y destruye el blanco.

**Propiedades/campos actuales:** pointsWorth.

**Mostrar y renombrar / controles:** pointsWorth → Puntos al acertar [entero >=1].

**Ocultar:** Tag Bullet, playerId, colisiones y búsqueda de UI.

**Facilidad:** Media. **MVP educativo:** No, disparos fuera. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Depende del sistema de proyectiles y del tag ausente. Destruye aun sin score efectivo. playerId fuera de 0/1 afecta arrays. No usarlo para premio al tocar personaje.

**Adapter/facade:** Sí si existe un pack de tiro; compartir servicio de puntuación.

**Undo/SerializedObject:** U1 configuración; destrucción/suma runtime fuera de Undo.

#### 17. Playground.UserInterface.UIItemScript — P3

**Archivo:** [_Internal/Scripts/UserInterface/UIItemScript.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/UserInterface/UIItemScript.cs).

**Qué hace:** Widget uGUI de icono y cantidad de recurso.

**Propiedades/campos actuales:** resourceIcon (Image); resourceAmount (Text); métodos ShowNumber/DisplayIcon..

**Mostrar y renombrar / controles:** Ninguna directa; icono del recurso pertenece a definición de contenido..

**Ocultar:** Referencias de widgets y métodos.

**Facilidad:** Fácil. **MVP educativo:** No; inventario posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Sin null guards. Depende de jerarquía/prefab uGUI que no se importó como contenido del taller.

**Adapter/facade:** No una fachada por widget; encapsular/reemplazar presentación en servicio de inventario.

**Undo/SerializedObject:** U1 sólo para autor del pack; no controles de alumno.


### Categoría: Texto / mensajes

#### 18. Playground.Conditions.Actions.DialogueBalloonAction — P2

**Archivo:** [Scripts/Conditions/Actions/DialogueBalloonAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/DialogueBalloonAction.cs).

**Qué hace:** Busca DialogueSystem, crea globo, escucha su destrucción y opcionalmente encadena followingText.

**Propiedades/campos actuales:** textToDisplay; backgroundColor; textColor; targetObject; disappearMode; timeToDisappear; keyToPress; followingText.

**Mostrar y renombrar / controles:** textToDisplay → Mensaje [texto multilínea]; disappearMode → Cerrar [dropdown Después de un tiempo / Con una tecla]; timeToDisappear → Duración [segundos]; colores → Fondo/Texto [color, paleta]; targetObject → Sobre quién [selector restringido].

**Ocultar:** followingText (grafo de componentes), keyToPress fijada a Enter, conexión DialogueSystem y BalloonScript.

**Facilidad:** Media. **MVP educativo:** Fuera; la meta ya admite texto sin diálogos. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Demo no tiene DialogueSystem/balloonPrefab. Cadena puede formar ciclos; retorna true al crear, no espera a terminar para que ConditionBase continúe con acciones siguientes. Corrutina WaitUntil no bloquea la cadena. Destrucción del dueño/suscriptor requiere revisión.

**Adapter/facade:** Sí; lista de mensajes declarativa y backend de texto acotado si se aborda.

**Undo/SerializedObject:** U1 para texto/colores; referencias entre componentes no como UI inicial.

#### 19. Playground.UserInterface.DialogueSystem — P3

**Archivo:** [_Internal/Scripts/UserInterface/DialogueSystem.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/UserInterface/DialogueSystem.cs).

**Qué hace:** Instancia balloonPrefab, lo parenta a su transform y llama Setup.

**Propiedades/campos actuales:** balloonPrefab; método CreateBalloon con texto, tecla, duración, colores y target.

**Mostrar y renombrar / controles:** Ninguna directa; los contenidos pertenecen al mensaje educativo..

**Ocultar:** balloonPrefab y todos los parámetros de conexión del servicio.

**Facilidad:** Media. **MVP educativo:** No, dependencia interna de diálogos futuros. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Sin null checks de prefab/componente. Necesita jerarquía Canvas correcta. No hay prefab de globo en este pack.

**Adapter/facade:** Sí como infraestructura oculta, o sustituir por presentación propia.

**Undo/SerializedObject:** U1 sólo para contenido de pack; no para globos generados runtime.

#### 20. Playground.UserInterface.BalloonScript — P3

**Archivo:** [_Internal/Scripts/UserInterface/BalloonScript.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/UserInterface/BalloonScript.cs).

**Qué hace:** Configura Text/Image, sigue a objeto con Camera.main y se destruye por tiempo/tecla.

**Propiedades/campos actuales:** dialogueText; buttonText; BalloonDestroyed (UnityAction pública, no propiedad serializable de autoría). Configuración restante por Setup..

**Mostrar y renombrar / controles:** Ninguna directa; texto/duración se configuran en concepto Mensaje..

**Ocultar:** Text, delegado, RectTransform, Camera.main y estado privado.

**Facilidad:** Difícil. **MVP educativo:** No directo. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** OnDestroy invoca BalloonDestroyed() sin comprobar null. Keyboard.current sin guardia. Texto "press" en inglés. Requiere Image/RectTransform/Text válidos; seguir un objetivo no valida cámara ni layout.

**Adapter/facade:** No crear fachada de alumno para este widget; reemplazar o corregir internamente si se incorporan mensajes.

**Undo/SerializedObject:** U1 sólo referencias de pack; delegado no se edita mediante SerializedProperty.


### Categoría: Spawning

#### 21. Playground.Conditions.Actions.CreateObjectAction — P2

**Archivo:** [Scripts/Conditions/Actions/CreateObjectAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/CreateObjectAction.cs).

**Qué hace:** Instancia prefabToCreate y lo sitúa en coordenada absoluta o desplazamiento respecto al dueño.

**Propiedades/campos actuales:** prefabToCreate; newPosition; relativeToThisObject.

**Mostrar y renombrar / controles:** prefabToCreate → Qué aparece [selector de definición del catálogo]; newPosition → Dónde aparece [marcador de escena/offset]; relativeToThisObject → Cerca de este elemento [toggle avanzado].

**Ocultar:** Prefab bruto, Vector2 absoluto y objeto de evento (ignorado).

**Facilidad:** Fácil. **MVP educativo:** No; no confundir con crear en Editor. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Null prefab devuelve false y corta cadena. No limita cantidad ni vida. Awake de instanciado ocurre antes de asignar posición: el adapter actual Patrol calcula destino desde origen anterior; importante si se crean patrullas así.

**Adapter/facade:** Sí; spawn con posición en Instantiate o inicialización posterior explícita y límite.

**Undo/SerializedObject:** U1 configuración; Instantiate runtime no usa Undo. Creación de autoría sigue ItemService/PrefabUtility (U2).

#### 22. Playground.Gameplay.ObjectCreatorArea — P2

**Archivo:** [Scripts/Gameplay/ObjectCreatorArea.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Gameplay/ObjectCreatorArea.cs).

**Qué hace:** Corrutina infinita instancia un prefab cada intervalo dentro de un rectángulo calculado con BoxCollider2D.size.

**Propiedades/campos actuales:** prefabToSpawn; spawnInterval (+ requisito BoxCollider2D).

**Mostrar y renombrar / controles:** prefabToSpawn → Elemento que aparece [selector catálogo]; spawnInterval → Aparece cada [segundos >=0,25]; área → Zona de aparición [gizmo futuro].

**Ocultar:** Collider técnico, prefab libre y bucle; límite de cantidad tendría que ser propio.

**Facilidad:** Media. **MVP educativo:** No. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** No usa offset/rotación/escala del BoxCollider al generar posiciones; no configura isTrigger. Sin límite de instancias o null guards; intervalos <=0 generan repetidamente. Mismo problema de posición después de Awake.

**Adapter/facade:** Sí; límite obligatorio, ciclo de vida y transformación correcta del área.

**Undo/SerializedObject:** U1 para configuración; U2 gizmos de autoría; instancias runtime no Undo.

#### 23. Playground.Gameplay.TimedSelfDestruct — P2

**Archivo:** [Scripts/Gameplay/TimedSelfDestruct.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Gameplay/TimedSelfDestruct.cs).

**Qué hace:** En Start programa DestroyMe mediante Invoke por nombre.

**Propiedades/campos actuales:** timeToDestruction.

**Mostrar y renombrar / controles:** timeToDestruction → Desaparecer después de [segundos >=0,1].

**Ocultar:** Nombre de método Invoke y Destroy; no controles técnicos adicionales.

**Facilidad:** Muy fácil. **MVP educativo:** No imprescindible; auxiliar de spawn. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Valor cero por defecto; vida empieza en Start, cambiar campo después no reprograma. Invoke con string es frágil al renombrar, no una API eliminada de 6.6.

**Adapter/facade:** Sí, fachada pequeña al añadir vida temporal.

**Undo/SerializedObject:** U1 configuración; efecto runtime no deshacible.

#### 24. Playground.Gameplay.ObjectShooter — P3

**Archivo:** [Scripts/Gameplay/ObjectShooter.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Gameplay/ObjectShooter.cs).

**Qué hace:** En pulsación crea objeto, asigna tag Bullet, orienta, aplica impulso y añade/configura BulletAttribute.

**Propiedades/campos actuales:** prefabToSpawn; keyToPress; creationRate; shootSpeed; shootDirection; relativeToRotation.

**Mostrar y renombrar / controles:** prefabToSpawn → Qué lanza [selector pack]; shootSpeed → Potencia [slider]; shootDirection → Dirección [dropdown]; creationRate → Espera entre lanzamientos [segundos].

**Ocultar:** Tag Bullet, playerId, creación de componente, vectores/rotación, tecla fija si se usa.

**Facilidad:** Media. **MVP educativo:** No en taller inicial de plataformas. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Comentario dice mantener tecla, código usa wasPressedThisFrame: no fuego continuo. Keyboard.current y prefab sin guardia. Tag Bullet no está configurado. Vector no normalizado cambia potencia. Conflicto Espacio/Jump y vida ilimitada de proyectiles.

**Adapter/facade:** Sí; presets de proyectil, teclas y máximo de instancias, no lista técnica.

**Undo/SerializedObject:** U1 configuración; acciones runtime sin Undo.


### Categoría: Eventos

#### 25. Playground.Conditions.ConditionArea — P0

**Archivo:** [Scripts/Conditions/ConditionArea.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/ConditionArea.cs).

**Qué hace:** Dispara acciones al entrar/salir/permanecer en trigger con filtro de tag.

**Propiedades/campos actuales:** eventType; frequency; más todos los campos de ConditionBase (ficha común)..

**Mostrar y renombrar / controles:** eventType → Cuando el personaje [dropdown Entra / Sale / Permanece, futuro]; frequency → Cada [segundos sólo Permanecer]; happenOnlyOnce → Sólo una vez [toggle futuro]. En Meta se fija Entra + Sólo una vez..

**Ocultar:** filterByTag/filterTag, actions, useCustomActions/customActions, trigger. frequency oculto en Enter/Exit.

**Facilidad:** Fácil. **MVP educativo:** Sí oculto dentro de Meta; no editor de reglas general. **Evidencia:** Entrada y acción de meta probadas.

**Riesgos y dependencias:** CompareTag se evalúa antes de !filterByTag: un tag inválido puede fallar aunque filtro esté desactivado. Un Collider trigger y otro cuerpo físico son necesarios. StayInside debe tener intervalo positivo y el reloj se comparte entre visitantes.

**Adapter/facade:** Sí; preset de condición y objetivo pedagógico, no exponer lista UnityEvent.

**Undo/SerializedObject:** U1 para configuración generada; U2 si se crean/eliminan componentes de reglas. Disparo runtime no Undo.

#### 26. Playground.BaseClasses.ConditionBase — P0

**Archivo:** [_Internal/Scripts/BaseClasses/ConditionBase.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/BaseClasses/ConditionBase.cs).

**Qué hace:** Recorre acciones en orden; si una devuelve false interrumpe. Al acabar invoca UnityEvent opcional y marca alreadyHappened.

**Propiedades/campos actuales:** actions (List<Playground.BaseClasses.Action>); useCustomActions; customActions (UnityEvent); happenOnlyOnce; filterByTag; filterTag..

**Mostrar y renombrar / controles:** happenOnlyOnce → Sólo una vez [toggle de preset]; otros sólo como futura regla Cuando/Entonces, no editor técnico..

**Ocultar:** actions, customActions, useCustomActions, tags y alreadyHappened.

**Facilidad:** Difícil. **MVP educativo:** Sí como infraestructura heredada; no exposición directa. **Evidencia:** Cadena de meta probada.

**Riesgos y dependencias:** Los filtros se implementan en hijas, no aquí. Si una acción falla, alreadyHappened queda false: puede reintentarse. No hay transacción ni rollback: efectos de acciones previas permanecen. UnityEvent habilitado sin asignar puede fallar. No es motor de lógica visual listo.

**Adapter/facade:** Sí; crear reglas validadas por presets con backend sustituible.

**Undo/SerializedObject:** U1 para preset; listas de componentes/eventos requieren U2 para cambios estructurales; runtime no Undo.

#### 27. Playground.BaseClasses.Action — P0

**Archivo:** [_Internal/Scripts/BaseClasses/Action.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/BaseClasses/Action.cs).

**Qué hace:** Base abstracta con ExecuteAction(GameObject other), implementación predeterminada devuelve true.

**Propiedades/campos actuales:** Sin campos educativos; método virtual ExecuteAction..

**Mostrar y renombrar / controles:** Ninguna directa..

**Ocultar:** Instancias técnicas, firma GameObject y retorno booleano.

**Facilidad:** Fácil. **MVP educativo:** Sólo base del puente de meta. **Evidencia:** Usado por ReachGoalAction.

**Riesgos y dependencias:** Contrato no transporta contexto tipado ni distingue causas de fallo; clases pueden ignorar other. No confundir con System.Action.

**Adapter/facade:** Sí en borde backend; UI no referencia esta clase.

**Undo/SerializedObject:** Sin campos; U2 si se añade un componente concreto; ejecución fuera de Undo.

#### 28. Playground.Conditions.ConditionCollision — P2

**Archivo:** [Scripts/Conditions/ConditionCollision.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/ConditionCollision.cs).

**Qué hace:** Ejecuta la cadena en OnCollisionEnter2D si pasa filtro; no usa OnTriggerEnter2D.

**Propiedades/campos actuales:** Sólo campos heredados de ConditionBase; RequireComponent Collider2D y Reset con diálogo..

**Mostrar y renombrar / controles:** Concepto futuro Al chocar con [selector de tipo/elemento]; happenOnlyOnce → Sólo una vez [toggle].

**Ocultar:** Collider2D abstracto, filterTag/filterByTag, acciones y eventos.

**Facilidad:** Media. **MVP educativo:** No; meta ya usa área. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Colisión sólida ≠ trigger. RequireComponent apunta a clase base Collider2D: crear BoxCollider primero. Comentario habla de trigger pero callback es colisión. Filtro también evalúa CompareTag primero.

**Adapter/facade:** Sí; preset «chocar» y validación de física.

**Undo/SerializedObject:** U1 configuración; U2 montaje estructural.

#### 29. Playground.Conditions.ConditionRepeat — P2

**Archivo:** [Scripts/Conditions/ConditionRepeat.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/ConditionRepeat.cs).

**Qué hace:** En Update compara Time.time contra último disparo+frequency y ejecuta cadena con dataObject=null.

**Propiedades/campos actuales:** initialDelay; frequency + campos ConditionBase.

**Mostrar y renombrar / controles:** initialDelay → Esperar al comenzar [segundos >=0]; frequency → Repetir cada [segundos >=0,25].

**Ocultar:** Estado temporal, actions/customActions y tags no aplicables.

**Facilidad:** Media. **MVP educativo:** No para este MVP. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Start usa Time.deltaTime + initialDelay - frequency, no Time.time: al añadirlo tarde puede disparar sin respetar retraso. Es un hallazgo estático, aunque changelog menciona un arreglo histórico. No validar por changelog. Acciones que esperan objeto de colisión reciben null.

**Adapter/facade:** Sí; corregir origen del reloj, validar intervalos y compatibilidad de acciones.

**Undo/SerializedObject:** U1 configuración, ejecución temporal fuera de Undo.

#### 30. Playground.Conditions.ConditionKeyPress — P2

**Archivo:** [Scripts/Conditions/ConditionKeyPress.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/ConditionKeyPress.cs).

**Qué hace:** Ejecuta cadena al pulsar/soltar/mantener una tecla; mantenimiento limitado por frequency.

**Propiedades/campos actuales:** keyToPress; eventType; frequency + campos ConditionBase.

**Mostrar y renombrar / controles:** keyToPress → Tecla [dropdown pequeño]; eventType → Cuando [Pulsar/Soltar/Mantener]; frequency → Repetir cada [s sólo Mantener].

**Ocultar:** Key enum completo, tags no aplicables, lista de componentes y UnityEvent.

**Facilidad:** Media. **MVP educativo:** No, reglas posteriores. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Keyboard.current sin guardia; conflictos con controles de juego. Dispara con null. Frecuencia sin validación. No toda acción acepta null.

**Adapter/facade:** Sí; acciones semánticas y validación de combinaciones.

**Undo/SerializedObject:** U1; la pulsación en runtime no es un cambio de autoría.


### Categoría: Interacciones

#### 31. Playground.Gameplay.PickUpAndHold — P2

**Archivo:** [Scripts/Gameplay/PickUpAndHold.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Gameplay/PickUpAndHold.cs).

**Qué hace:** Busca objeto Pickup cercano, lo parenta al portador y lo vuelve cinemático; al soltar desparenta y fuerza Dynamic.

**Propiedades/campos actuales:** pickupKey; dropKey; pickUpDistance; métodos PickUp/Drop..

**Mostrar y renombrar / controles:** pickUpDistance → Alcance para recoger [slider]; teclas → Recoger/Soltar [un botón fijo o dropdown pequeño].

**Ocultar:** Tag Pickup, carriedObject, reparentado, bodyType y teclas independientes inicialmente.

**Facilidad:** Difícil. **MVP educativo:** Fuera; interacción posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Compara sqrMagnitude con pickUpDistance sin elevarlo al cuadrado: alcance real no coincide. Keyboard.current sin guardia, tag Pickup ausente. Drop público supone carriedObject no null; no restaura padre/bodyType original. Puede recoger objetos de otro portador y permitir ciclos si catálogo no valida.

**Adapter/facade:** Sí, pero conviene reemplazar implementación al priorizar cargar objetos.

**Undo/SerializedObject:** U1 configuración; parentado runtime no Undo. No invocar PickUp/Drop desde un inspector en edición.

#### 32. Playground.Conditions.Actions.TeleportAction — P2

**Archivo:** [Scripts/Conditions/Actions/TeleportAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/TeleportAction.cs).

**Qué hace:** Mueve objectToMove o al propio dueño a newPosition; opcionalmente pone velocidades lineal/angular a cero.

**Propiedades/campos actuales:** objectToMove; newPosition; stopMovements.

**Mostrar y renombrar / controles:** objectToMove → Quién viaja [selector restringido]; newPosition → Destino [marcador]; stopMovements → Detener al llegar [toggle].

**Ocultar:** Coordenadas libres y referencia implícita al dueño.

**Facilidad:** Media. **MVP educativo:** No, portal posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Ignora dataObject: conectado a portal no mueve automáticamente a quien entra. Si objectToMove=null mueve el portal. No valida destino libre de obstáculos; usa transform en cuerpo físico.

**Adapter/facade:** Sí; resolver viajero explícitamente y validación de destino.

**Undo/SerializedObject:** U1 configuración; teletransporte runtime no Undo; mover marcador Editor U2.

#### 33. Playground.Conditions.Actions.OnOffAction — P2

**Archivo:** [Scripts/Conditions/Actions/OnOffAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/OnOffAction.cs).

**Qué hace:** Alterna activo/inactivo o visibilidad SpriteRenderer del objetivo.

**Propiedades/campos actuales:** objectToAffect; justMakeInvisible.

**Mostrar y renombrar / controles:** objectToAffect → Elemento [selector]; justMakeInvisible → Qué cambia [dropdown Visibilidad / Actividad, avanzado].

**Ocultar:** Referencia libre, uso de SetActive y Renderer.

**Facilidad:** Media. **MVP educativo:** No, interruptores posteriores. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Es alternar, no establecer Encender/Apagar; un segundo evento revierte el estado. Invisible sigue teniendo colisiones. Desactivar dueño/ancestros puede detener su lógica. Si falta sprite devuelve false y corta cadena.

**Adapter/facade:** Sí; preferible acciones explícitas Mostrar/Ocultar con objetivos validados.

**Undo/SerializedObject:** U1 configuración; operación runtime no Undo; no confundir ocultación con eliminación.

#### 34. Playground.Conditions.Actions.DestroyAction — P2

**Archivo:** [Scripts/Conditions/Actions/DestroyAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/DestroyAction.cs).

**Qué hace:** Destruye dueño o quien colisionó y opcionalmente instancia efecto.

**Propiedades/campos actuales:** target (Enums.Targets); deathEffect.

**Mostrar y renombrar / controles:** target → Qué desaparece [dropdown Este elemento / El que lo toca, sólo presets seguros]; deathEffect → Efecto [selector de pack futuro].

**Ocultar:** GameObject libre, invocación directa y enum técnico.

**Facilidad:** Media. **MVP educativo:** No directo; coleccionable ya resuelve desaparición. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Puede borrar jugador/cámara/infraestructura si se conecta mal. Con other=null y target=ObjectThatCollided no destruye pero devuelve true; puede generar efecto. Efecto sin vida limitada se acumula.

**Adapter/facade:** Sí; whitelist de objetivos; no ofrecer «eliminar al tocar» universal.

**Undo/SerializedObject:** U1 configuración. Borrar autoría debe usar Undo.DestroyObjectImmediate (U2), nunca este Destroy.

#### 35. Playground.Conditions.Actions.ConsumeResourceAction — P2

**Archivo:** [Scripts/Conditions/Actions/ConsumeResourceAction.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Conditions/Actions/ConsumeResourceAction.cs).

**Qué hace:** Comprueba cantidad de recurso en UIScript y la consume si alcanza; resultado controla continuación de cadena.

**Propiedades/campos actuales:** checkFor; amountNeeded.

**Mostrar y renombrar / controles:** checkFor → Recurso requerido [selector id estable]; amountNeeded → Cantidad necesaria [entero >=1].

**Ocultar:** Índices, referencia UIScript, dependencia de orden de acciones.

**Facilidad:** Media. **MVP educativo:** No, inventario fuera. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** No es sólo condición: consume inmediatamente. Si una acción posterior falla, no reembolsa. Sin inventario completo de demo no sirve. Cantidades negativas tendrían semántica incorrecta.

**Adapter/facade:** Sí; separar comprobar de consumir si se construyen reglas.

**Undo/SerializedObject:** U1 parámetros; transacciones runtime no son Undo.


### Categoría: Decoración

### Decoración estática — contenido de Unity

**Clase:** UnityEngine.SpriteRenderer, creada por [DemoBuilder.cs](../Assets/CreaJuegoPacks/Starter/Editor/DemoBuilder.cs). **Operación:** sólo representación; el prefab actual no tiene collider. **Campos relevantes:** sprite, color, size, drawMode y orden de dibujo; GameItem.tint es la fuente educativa actual. **Mostrar:** Color [paleta], eventual Apariencia [selector del pack]. **Ocultar:** sorting layers/order, material, shader, drawMode y referencias internas. **Facilidad:** Muy fácil. **Riesgo:** asumir colisión donde no existe o modificar el orden de dibujo sin entenderlo. **Adapter:** servicio de aspecto sin Playground. **Undo:** U1 y preview derivado U2. **MVP:** fuera de las siete tarjetas seleccionadas, **P2**; conservar contenido ya creado sin borrarlo.

#### 36. Playground.Movement.AutoRotate — P2

**Archivo:** [Scripts/Movement/AutoRotate.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Movement/AutoRotate.cs).

**Qué hace:** Acumula ángulo y lo aplica con Rigidbody2D.MoveRotation cada paso físico.

**Propiedades/campos actuales:** rotationSpeed (+ rigidbody2D).

**Mostrar y renombrar / controles:** rotationSpeed → Ritmo de giro [slider de magnitud] + Sentido [dropdown horario/antihorario].

**Ocultar:** rigidbody2D; constante .02 y escala*10; no confundir con grados/segundo exactos.

**Facilidad:** Fácil. **MVP educativo:** Fuera; mejora visual posterior. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Usa .02 fijo en lugar de fixedDeltaTime. Ángulo inicial parte de 0 y pierde orientación inicial al ejecutar. Gizmo depende de mesh ausente. Añade física innecesaria para decoración sin colisiones.

**Adapter/facade:** Sí si se reutiliza; para adorno puro preferir rotación visual propia mínima sin Rigidbody2D.

**Undo/SerializedObject:** U1 configuración; animación runtime sin Undo.


### Categoría: Otras

#### 37. Playground.BaseClasses.Physics2DObject — P0

**Archivo:** [_Internal/Scripts/BaseClasses/Physics2DObject.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/BaseClasses/Physics2DObject.cs).

**Qué hace:** Awake obtiene Rigidbody2D para las clases de movimiento.

**Propiedades/campos actuales:** rigidbody2D [HideInInspector, público]; RequireComponent Rigidbody2D..

**Mostrar y renombrar / controles:** Ninguna..

**Ocultar:** Referencia rígida y ciclo Awake.

**Facilidad:** Muy fácil. **MVP educativo:** Sí como dependencia interna. **Evidencia:** Usado por movimiento probado.

**Riesgos y dependencias:** Warning CS0109 por new innecesario; no es error de compilación. No es comportamiento de catálogo.

**Adapter/facade:** No adicional; mantener sólo dentro de backend.

**Undo/SerializedObject:** No exponer campo; creación del cuerpo técnico como parte de prefab/U2.

#### 38. Playground.Utilities.InputUtils — P0

**Archivo:** [_Internal/Scripts/Utilities/InputUtils.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/Utilities/InputUtils.cs).

**Qué hace:** Helper estático traduce eje/grupo a teclas y suma dirección digital; devuelve cero sin teclado.

**Propiedades/campos actuales:** Sin campos editables; GetAxis(Enums.Axes, Enums.KeyGroups)..

**Mostrar y renombrar / controles:** Ninguna propia; grupo de controles pertenece al personaje..

**Ocultar:** API de teclado y ejes.

**Facilidad:** Muy fácil. **MVP educativo:** Sí soporte interno. **Evidencia:** Usado por Move; guardia adaptada.

**Riesgos y dependencias:** Sólo teclado, no InputAction remapeable, mando o táctil. No presentar como soporte universal de entrada.

**Adapter/facade:** Encapsulado por adapter de personaje; sustituir entrada después si se requiere accesibilidad.

**Undo/SerializedObject:** No SerializedObject: clase estática; serializar configuración en GameItem.

#### 39. Playground.Attributes.BulletAttribute — P3

**Archivo:** [Scripts/Attributes/BulletAttribute.cs](../Assets/ThirdParty/UnityPlayground/Scripts/Attributes/BulletAttribute.cs).

**Qué hace:** Marca proyectil con identificador de jugador; Reset solicita collider si falta.

**Propiedades/campos actuales:** playerId [HideInInspector].

**Mostrar y renombrar / controles:** Ninguna..

**Ocultar:** playerId y collider.

**Facilidad:** Muy fácil. **MVP educativo:** No, auxiliar de disparos. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** No mueve ni hace daño por sí mismo. Requiere consumidores como DestroyForPointsAttribute. No hay tag Bullet creado.

**Adapter/facade:** No fachada educativa independiente; componente montado por adapter de proyectil.

**Undo/SerializedObject:** Campo serializable técnicamente U1, pero no mostrar al alumno.

#### 40. Playground.Utilities.InventoryResources — P3

**Archivo:** [_Internal/Scripts/Utilities/InventoryResources.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/Utilities/InventoryResources.cs).

**Qué hace:** ScriptableObject de nombres de recurso; GetResourceTypes copia lista a array.

**Propiedades/campos actuales:** resourcesTypes (List<string>); método GetResourceTypes..

**Mostrar y renombrar / controles:** resourcesTypes → Tipos de recurso [lista sólo para autor de pack].

**Ocultar:** Índices como identidad, relación con widgets.

**Facilidad:** Media. **MVP educativo:** No; preferir definición propia si llega inventario. **Evidencia:** Sólo inspeccionado/compilado previamente.

**Riesgos y dependencias:** Lista no inicializada puede ser null; índices posicionales frágiles. Runtime ResourceAttribute no referencia este asset: sólo transporta el índice, sin validar contra nombres.

**Adapter/facade:** Reemplazar identidad por ids estables; no crear ahora.

**Undo/SerializedObject:** U1 para asset; reordenar debe migrar referencias si se conserva el formato legado.

#### 41. Playground.Utilities.Utils — P3

**Archivo:** [_Internal/Scripts/Utilities/Utils.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/Utilities/Utils.cs).

**Qué hace:** Helpers de orientación/gizmos y diálogo Editor para agregar Collider2D.

**Propiedades/campos actuales:** moveArrowMesh; shootArrowMesh; rotateArrowMesh (estáticos); métodos SetAxisTowards, GetVectorFromAxis, GetVector2FromVector3, DrawMoveArrowGizmo, DrawShootArrowGizmo, DrawRotateArrowGizmo, DrawGizmo, Angle, Collider2DDialogWindow..

**Mostrar y renombrar / controles:** Ninguna directa..

**Ocultar:** Meshes, métodos técnicos y diálogo en inglés.

**Facilidad:** Fácil. **MVP educativo:** No como superficie; helpers indirectos. **Evidencia:** Compilado; gizmos no validados.

**Riesgos y dependencias:** Carga Resources/Meshes no incluidos; no asumir flechas disponibles. El diálogo agrega componentes sin flujo Undo propio; evitarlo creando collider antes. using UnityEditor ya protegido por compilación condicional.

**Adapter/facade:** Evitar diálogo; gizmos educativos propios en servicios Editor si se necesitan.

**Undo/SerializedObject:** Estáticos no SerializedObject. No heredar AddComponent directo del diálogo como creación educativa.

#### 42. Playground.Utilities.Enums — P3

**Archivo:** [_Internal/Scripts/Utilities/Enums.cs](../Assets/ThirdParty/UnityPlayground/_Internal/Scripts/Utilities/Enums.cs).

**Qué hace:** Contenedor de enums compartidos de ejes, direcciones, jugadores, controles y destinos.

**Propiedades/campos actuales:** Axes; Directions; KeyGroups; MovementType; Players; Targets (tipos, no campos de instancia)..

**Mostrar y renombrar / controles:** Traducir opciones semánticas en el adapter: Horizontal/Vertical, Flechas/WASD, Este elemento/El que lo toca..

**Ocultar:** Nombres en inglés, enteros ordinales y tipos vendor en UI.

**Facilidad:** Muy fácil. **MVP educativo:** No tarjeta; algunas traducciones internas. **Evidencia:** Usado por scripts.

**Riesgos y dependencias:** No mezclar Directions (orientación) con trayectoria, ni Targets con selección universal. Persistir enum propio permite sustituir vendor.

**Adapter/facade:** Sí como traducción de enums, no nuevo servicio global.

**Undo/SerializedObject:** No SerializedObject para clase de tipos; sí para enum de fachada.


## PROPUESTA DE CAPA EDUCATIVA

Propuesta de contenido de los paneles, **sin rediseñar ahora la ventana completa**. Conservar catálogo, panel de propiedades y SceneView. Mostrar primero un máximo de cuatro controles por elemento; los detalles técnicos quedan fuera del modo inicial.

Acciones comunes recomendadas: **Crear, Seleccionar, Duplicar, Eliminar, Probar**. Crear ya existe; duplicar y eliminar aún no tienen botones/servicios propios en CreaJuego, aunque Unity permite hacerlo. Duplicar debe conservar definición y overrides, elegir un desplazamiento visible y generar una sola operación Undo. Eliminar debe usar Undo del Editor, no DestroyAction. Probar es una acción de escena, no una ejecución independiente del componente. Las configuraciones se cambian fuera de Play Mode.

### JUGADOR — P0

| Propiedad visible | Control | Datos/backend | Estado |
|---|---|---|---|
| Velocidad | Slider 0,2–5 con valor | GameItem.speed → Move.speed /20 | Ya existe |
| Fuerza de salto | Slider 5–18 | GameItem.jump → Jump.jumpStrength | Existe con etiqueta Salto |
| Resistencia inicial | Campo entero 1–10 | GameItem.health → HealthSystemAttribute.health | Existe con etiqueta Vidas; cambiar significado visible |
| Puede saltar | Toggle | Nuevo bool de fachada → Jump.enabled | Propuesto, no existe |

Ocultar: cuerpo físico, forma de colisión, masa, amortiguación, gravedad, constraints, key, groundTag, checkGround, lookAxis, tags y referencias de UI. No incluir “Dirección inicial” sólo porque el ejemplo la menciona: no hay campo equivalente en Move y el sprite actual no necesita orientación.

Acciones: Crear si no hay jugador; Seleccionar, Eliminar con advertencia contextual de que falta protagonista; Probar. Duplicar deshabilitado en el preset de un jugador, explicando el motivo. No permitir accidentalmente dos Player compartiendo controles/puntos.

Ayuda: **“Este es el personaje que controla quien juega. Muévelo con las flechas y salta con Espacio.”**

Antes de publicar: soporte de suelo fiable, estado de fin, preflight de escena. “Puede saltar” no debe permitir saltar indefinidamente: checkGround permanece responsabilidad técnica.

### PLATAFORMA — P0

| Propiedad visible | Control | Datos/backend | Estado |
|---|---|---|---|
| Ancho | Campo numérico o slider acotado por pack | Nuevo dato/tamaño semántico → tamaño visible y collider coherentes | Propuesto |
| Color | Paleta/color | GameItem.tint → SpriteRenderer.color | Existe, sólo se aplica al probar |

Ocultar: BoxCollider2D/isTrigger, material físico, SpriteRenderer.drawMode, sprite técnico, sorting layer/order y escala Z. Alto fijo del pack al inicio. Mover con SceneView normal; no crear todavía un editor completo de posición.

Acciones: Crear, Duplicar, Eliminar, Probar. Ayuda: **“Un lugar firme donde el personaje puede caminar y apoyarse.”**

No crear un “PlatformBehaviour” sólo para envolver ausencia de lógica. Basta servicio de forma/aspecto. Si se añade Ancho, migrar las escalas actuales —el suelo tiene scale.x=6— sin multiplicar dimensiones dos veces ni romper otros prefabs. La edición debe mantener imagen y colisión sincronizadas, también después de Undo.

### PLATAFORMA MÓVIL — P1, sujeta a prueba de pasajeros

| Propiedad visible | Control | Datos/backend | Estado |
|---|---|---|---|
| Velocidad | Slider 0,2–3 | GameItem.speed → Patrol.speed | Existe |
| Distancia de recorrido | Slider 0,5–6 | GameItem.distance → punto relativo convertido a waypoints | Existe |
| Ancho | Mismo control de plataforma | Servicio de tamaño compartido | Propuesto, si se entrega Ancho |

Ocultar: puntos absolutos, Rigidbody2D cinemático, orientation/lookAxis y directionChangeInterval. Mostrar texto de sólo lectura **“Va hacia la derecha y vuelve”**; no ofrecer toggle de regresar ni pausas que Patrol no soporta. Un dropdown de dirección es una ampliación posible, **fuera del compromiso inicial**; no justifica ahora un nuevo sistema de controles.

Acciones: Crear, Duplicar, Eliminar, Probar. Ayuda: **“Esta plataforma recorre un tramo y vuelve al inicio. Prueba cuánto tarda y si puedes viajar sobre ella.”**

Gate: ida y vuelta, velocidades límite, duplicado a otra posición y transporte real del personaje. Si no se resuelve transporte con un cambio acotado, no anunciarla como plataforma para viajar; retirar esta tarjeta del MVP publicado y conservarla experimental.

### PREMIO — P0

Visible: **Puntos**, campo entero 1–100, predeterminado 1, ayuda junto al control. GameItem.points → CollectableAttribute.pointsWorth.

Ocultar: Player/Player2, isTrigger, UIScript, índice de jugador y Destroy. **No ofrecer “Desaparecer al tocarlo” aquí:** el backend siempre destruye el premio y el adapter ignora GameItem.disappear para este tipo. Aspecto viene del pack; un selector de variantes puede llegar después.

Acciones: Crear, Duplicar, Eliminar, Probar. Ayuda: **“El personaje recoge este premio al tocarlo y suma los puntos que elijas.”**

Dependencia obligatoria: una sesión/marcador válido. Si falta, la herramienta debe ofrecer preparación o un mensaje accionable antes de probar, no permitir una recogida silenciosa sin puntuación.

### PELIGRO — P0

Visible: **Daño al personaje** (entero 1–10) y **Desaparecer después del contacto** (toggle). GameItem.damage → -healthChange; GameItem.disappear → destroyWhenActivated.

Ocultar: número negativo interno, trigger/collider y búsqueda de HealthSystemAttribute. No ofrecer “Daño por segundo”: el código sólo actúa al entrar.

Acciones: Crear, Duplicar, Eliminar, Probar. Ayuda: **“Al tocar este peligro, el personaje pierde resistencia. Si llega a cero, termina la partida.”**

Gate: un contacto produce el daño esperado, contacto prolongado no se describe como repetido, reentrada controlada, varios colliders y coherencia tras ganar/perder.

### ENEMIGO — P1, sujeta a prueba de combinación

Visible: **Velocidad** (slider 0,2–3), **Distancia de recorrido** (slider 0,5–6), **Daño al personaje** (entero 1–10). Hereda el recorrido relativo de Patrol y daño negativo del mismo adapter.

Ocultar: waypoints, bodyType, isTrigger=true, tags, orientación, directionChangeInterval y desaparición fijada a false. No ofrecer salud de enemigo, “vencer saltando encima”, detección de bordes, persecución ni animación: el prefab no implementa esas conductas.

Acciones: Crear, Duplicar, Eliminar, Probar. Ayuda: **“Este enemigo va y vuelve por un tramo. Evita tocarlo para no perder resistencia.”**

La trayectoria no esquiva paredes ni detecta precipicios; validar colocación y advertirlo de manera educativa, por ejemplo “Su recorrido debe quedar libre”. Eliminar del alcance si no pasa prueba compuesta de patrulla/daño y derrota.

### META — P0

Visible: **Mensaje al llegar**, campo de texto (idealmente multilínea, máximo inicial propuesto 120 caracteres). GameItem.message → ReachGoalAction → servicio de sesión/presentación.

Ocultar: eventType=Enter, happenOnlyOnce=true, Player/filterByTag, lista de acciones, UnityEvent, UIScript, búsqueda de sesión y referencias Text.

Acciones: Crear, Duplicar (varias metas equivalentes permitidas si la regla de fin es idempotente), Eliminar, Probar. Ayuda: **“Llega aquí para completar el recorrido. El mensaje celebra el final de tu juego.”**

No añadir aún selector de siguiente nivel, puntos requeridos o consumo de premios. El mensaje de objetivo actual invita a recoger premios, pero llegar a la meta **no exige** haberlos recogido; la UI no debe afirmar lo contrario. Gate: ganar una sola vez, no recibir daño posterior ni seguir moviéndose por inercia, y reiniciar con estado limpio.

## MAPEO PLAYGROUND → CREAJUEGO

Los campos de la izquierda son **reales**; los de la columna central son la fuente educativa. “Propuesto” no está implementado.

| Playground component | Concepto CreaJuego / nombre visible | Propiedades y transformación | Situación |
|---|---|---|---|
| Move.cs — Playground.Movement.Move | Movimiento / Movimiento del personaje | GameItem.speed “Velocidad” → Move.speed = valor/20; movementType fijo OnlyHorizontal | Actual |
| Jump.cs — Playground.Movement.Jump | Salto / Salto del personaje | jumpStrength ← GameItem.jump “Fuerza de salto”; nuevo canJump → enabled; NO usar checkGround | Fuerza actual; toggle propuesto |
| HealthSystemAttribute.cs | Resistencia / Resistencia del personaje | health ← GameItem.health “Resistencia inicial” | Actual con renombrado propuesto |
| Ninguno de Playground | Mundo / Plataforma | GameItem.tint “Color” → SpriteRenderer.color; Ancho → SpriteRenderer.size/BoxCollider2D.size mediante servicio | Color actual; tamaño propuesto |
| Patrol.cs — Playground.Movement.Patrol | Recorrido / Movimiento de plataforma o enemigo | speed ← GameItem.speed “Velocidad”; waypoints ← posición inicial + derecha*GameItem.distance “Distancia de recorrido” | Actual |
| CollectableAttribute.cs | Premio / Premio al tocar | pointsWorth ← GameItem.points “Puntos” | Actual; desaparición obligatoria |
| ModifyHealthAttribute.cs | Peligro / Daño al contacto | healthChange = -GameItem.damage “Daño”; destroyWhenActivated ← GameItem.disappear “Desaparecer después del contacto” | Actual |
| ConditionArea.cs | Llegada / Al llegar a la meta | eventType=Enter, filterByTag=true, filterTag=Player, happenOnlyOnce=true; no controles técnicos | Actual |
| ReachGoalAction.cs **propio** | Final / Meta | lee GameItem.message “Mensaje al llegar”; invoca sesión | Actual; mejorar fin |
| UIScript.cs | Marcador / Resistencia y puntos | numberLabels y paneles ocultos; configuración de un jugador preparada por escena | Actual; puente temporal |
| CameraFollow.cs | Cámara / Seguir al personaje | target → selector/autoobjetivo; limitBounds → “Limitar al nivel”; límites desde región validada | Futuro P1 |
| AutoMove.cs | Movimiento automático / Impulso continuo | direction → vector de Dirección × magnitud de Impulso; relativeToRotation fijado por pack | Futuro P2, no inventar speed |
| FollowTarget.cs | Enemigo / Seguir a alguien | target → referencia semántica; speed → “Intensidad de seguimiento” | Futuro P2, no rapidez constante |
| DialogueBalloonAction.cs | Mensaje / Mostrar un mensaje | textToDisplay → “Mensaje”; timeToDisappear → “Duración”; colores → paleta; followingText oculto | Futuro P2 |
| CreateObjectAction.cs | Aparición / Hacer aparecer | prefabToCreate ← definición de catálogo; newPosition ← marcador; relativeToThisObject ← modo de colocación | Futuro P2, arreglar inicialización |
| TimedSelfDestruct.cs | Duración / Desaparecer después | timeToDestruction → “Duración” | Futuro P2 |

No existe `Move.direction`. No existe `Patrol.distance`. `Patrol.directionChangeInterval` no se convierte a ningún control porque no se usa. `checkGround` no se mapea a “Puede saltar”. Esas cuatro distinciones evitan una fachada engañosa.

El mapeo detallado de los restantes scripts y sus propiedades está en las fichas anteriores. Para futuras traducciones usar enums/tipos propios, nunca etiquetas obtenidas directamente de enums Playground en inglés.

## Componentes que NO conviene exponer directamente

| Motivo | Componentes concretos | Decisión |
|---|---|---|
| Son infraestructura, no ideas de juego | Physics2DObject, Action, InputUtils, Utils, Enums, BulletAttribute | Invisibles en catálogo; montados por pack/adapter |
| Referencias y presentación frágiles | UIScript, UIItemScript, DialogueSystem, BalloonScript | Nunca mostrar conexiones de widgets al participante |
| Combinaciones de acciones pueden romper una sesión | ConditionBase, DestroyAction, LoadLevelAction, OnOffAction, TeleportAction | Usar presets de intención; no un inspector traducido de todas sus opciones |
| Índices y dependencias incompletas | ResourceAttribute, ConsumeResourceAction, InventoryResources | No ofrecer inventario hasta diseñar identidad estable y montar infraestructura |
| Consumo ilimitado de objetos o física difícil | ObjectCreatorArea, ObjectShooter, AutoMove, Push | Fuera del primer MVP; límites, presets y validación antes de añadir |
| Conducta distinta de la etiqueta intuitiva | Patrol.directionChangeInterval, Jump.checkGround, FollowTarget.speed, OnOffAction.justMakeInvisible | Ocultar o traducir con semántica explícita; no “arreglar” mediante una etiqueta falsa |
| Problemas estáticos que requieren intervención | PickUpAndHold, ConditionRepeat, BalloonScript, AutoRotate | Corregir/reemplazar partes sólo cuando se priorice su caso de uso |
| Opciones para otro género | Rotate, ObjectShooter, DestroyForPointsAttribute | Packs posteriores; no recargar al alumno de plataformas |

Las APIs antiguas por sí solas no justifican retirar un comportamiento compilado y útil. Aquí pesan más los contratos, referencias, errores concretos y pruebas que la antigüedad del nombre de clase.

## MVP REALISTA: siete elementos, cinco obligatorios

| Prioridad | Qué incluir | Por qué | UI mínima | Dependencias |
|---|---|---|---|---|
| P0 | Jugador | Agencia inmediata y aprendizaje de causa/efecto | Velocidad, fuerza de salto, resistencia, puede saltar | Move, Jump, HealthSystemAttribute; preset físico/input y sesión |
| P0 | Plataforma | Permite diseñar un recorrido con geometría simple | Color; Ancho si se cierra coherencia de tamaño | SpriteRenderer/BoxCollider2D de Unity; servicio de forma |
| P0 | Premio | Feedback positivo y objetivo local comprensible | Puntos | CollectableAttribute; marcador/sesión; jugador identificable |
| P0 | Peligro | Permite experimentar con consecuencias | Daño, desaparición | ModifyHealthAttribute; HealthSystemAttribute; derrota |
| P0 | Meta | Hace visible qué significa completar la experiencia | Mensaje | ConditionArea + acción propia; estado de fin |
| P1 | Plataforma móvil | Reutiliza recorrido y agrega temporización | Velocidad, distancia, ancho compartido si existe | Patrol; cuerpo cinemático; verificación de pasajeros |
| P1 | Enemigo | Reutiliza patrulla y daño sin nuevo sistema de IA | Velocidad, distancia, daño | Patrol + ModifyHealthAttribute; trayectoria libre |

El límite se refiere a **7 elementos/comportamientos educativos del catálogo**, no a exponer cada componente de Unity. Comparten **8 clases concretas de Playground**: Move, Jump, HealthSystemAttribute, CollectableAttribute, ModifyHealthAttribute, ConditionArea, Patrol y UIScript como soporte oculto. No se proponen ocho paneles técnicos adicionales. Si las dos extensiones P1 no pasan sus gates, publicar el MVP de **5**, sin sustituirlas por dos funciones no probadas para completar una cuota.

Dejar fuera de la primera capa: cámara configurable (conservar fija), sonido editable, diálogos, disparos, inventario, spawning, portales, interruptores, lógica visual general, navegación de enemigos, multijugador, animación y personalización compleja de Hierarchy. La decoración existente puede conservarse como contenido experimental fuera del catálogo principal del taller; no hace falta borrarla del proyecto. Cada exclusión reduce dependencias y nuevos conceptos, no invalida reutilización futura.

### Adapters y servicios mínimos que necesitamos

**No crear siete interfaces ni un registro global de plugins.** Mantener IItemBackend y un adapter Playground por GameItem. ItemService.Backend usa SingleOrDefault: añadir varios IItemBackend a un mismo objeto rompería la resolución actual.

1. **PlaygroundAdapter existente, ampliación acotada:** traducción de propiedades, habilitar salto, preparar salud, daño, puntos y recorrido relativo. Mantener una sola fuente educativa y orden de inicialización. Su código puede separarse en métodos privados por comportamiento; no hacen falta siete MonoBehaviours.
2. **Apoyo de suelo propio:** corregir el contrato de “está apoyado” antes de permitir configurar salto. Elegir entre parche mínimo de Jump o sensor/componente auxiliar propio según la prueba; el canJump privado actual impide imponer limpiamente el gate desde fuera sin cambiar el contrato. No recurrir a reflection.
3. **Servicio de sesión local + puente Playground:** un único estado Jugando/Ganado/Perdido; resultado idempotente y referencias explícitas de escena; la meta delega aquí. Conservar UIScript temporalmente para salud/puntos. No fingir que existe un getter/evento público de score; reemplazarlo totalmente requiere adaptar también los consumidores.
4. **Servicio Editor de escena/validación:** crear o comprobar cámara, sesión y marcador como operación explícita con Undo; validar exactamente un jugador para este preset y metas disponibles; informar si faltan. No “reparar” la escena silenciosamente al seleccionar.
5. **Servicio Editor de forma/aspecto:** color y ancho coherentes con render/collider, preview y Undo. No necesita depender de Playground. Se puede integrar en ItemService hasta que el tamaño justifique separarlo.
6. **Acciones Editor de duplicar/eliminar:** reutilizar PrefabUtility/Undo y preservar fachada. El duplicado debe generar recorrido relativo nuevo al jugar, no copiar coordenadas mundiales del vendor.

“Necesitar” aquí significa trabajo propuesto. No se implementó ningún nuevo servicio en esta entrega.

### ScriptableObjects y descriptors

- **Conservar GameItemDefinition**. Ya tiene identidad, categoría, icono, prefab, descripción, ayuda, orden y propiedades. No crear otro “BehaviorDefinition” que duplique exactamente estos datos. El campo ItemKind puede seguir sirviendo al pack, sin usarse como switch de la vista.
- **Ampliar EducationalProperty**, estructura serializable existente, sólo con lo necesario: agrupación (Movimiento/Apariencia/Resultado), unidad visible, valor predeterminado o referencia al del prefab, validación de tipo/rango y condición simple de visibilidad para el salto. No crear un ScriptableObject por cada slider.
- **Conservar GameItem como fuente de autoría** para siete tipos. Añadir como máximo canJump y tamaño semántico si se aprueban. Definir migración/defaults para prefabs actuales. No inflar ahora con campos de inventario, audio, eventos y multijugador.
- **Un ContentPackDefinition opcional y acotado** sí aporta algo que falta: id del pack, título, lista explícita de GameItemDefinition y referencia al prefab de servicios de juego/preset inicial. Evita que FindAssets muestre automáticamente experimentos de otros packs. No guardar referencias a objetos de escena dentro de este asset.
- **No crear todavía RuleGraph, sistema genérico de bindings por reflection, descriptors de every vendor component ni un service locator.** Si sólo hay un pack, la validación/preset puede implementarse primero sin el asset opcional; es menos prioritario que suelo, fin y Undo.

El editor puede resolver qué propiedades mostrar a partir de la definición, comprobar su ruta con SerializedObject y usar controles propios. Un selector de referencia futuro debe apuntar a GameItem/definición de CreaJuego, no a un tipo vendor. No añadir dropdowns sólo para ampliar la lista de controles: las siete tarjetas iniciales funcionan sin editar direcciones arbitrarias.

### Dependencias de autoría y Runtime

- **Escena preparada** → cámara + sesión/marcador → jugador válido.
- **Jugador** → movimiento/entrada + salto/apoyo + resistencia.
- **Premio** → identifica jugador → puntuación/marcador.
- **Peligro / enemigo** → resistencia del jugador → derrota de sesión.
- **Meta** → condición de contacto → acción propia → victoria de la misma sesión.
- **Plataforma móvil / enemigo** → recorrido relativo validado → Patrol; la plataforma agrega superficie sólida y eventual transporte.
- **Definición → prefab con GameItem + un IItemBackend** → servicios Editor → selección/propiedades. El pack proporciona componentes técnicos, no el alumno.

Crear un elemento no debe intentar crear otro jugador o un HUD duplicado de manera implícita. Las dependencias deben prepararse a nivel de escena, validarse antes de Probar y quedar visibles como mensajes educativos accionables.

## QUÉ PODEMOS HEREDAR Y QUÉ NO

### A. Funcionalidad que podemos reutilizar directamente desde Playground

Conservar implementación Runtime ya comprobada de CollectableAttribute para contacto/desaparición, ModifyHealthAttribute para modificar salud, HealthSystemAttribute para contador inicial y pérdida, ConditionArea/ConditionBase para la cadena simple de meta y Patrol para mover entre puntos en su rango seguro. “Directamente” significa no reescribir esas funciones por gusto; **no** significa mostrar sus inspectores sin fachada. Move/Jump ya se usan adaptados, no deben etiquetarse como upstream intactos.

### B. Funcionalidad que conviene envolver

Todos los valores que vea el participante: escala de Move, configuración de Jump, resistencia, signo de daño, puntos y generación de waypoints. También selección de objetivo, manejo de escena y parámetros de fin. Preservar separación actual de assemblies: CreaJuego.Editor no referencia Playground.Runtime. Conservar UIScript como implementación temporal de marcador detrás de un puente hasta que sea necesario sustituirlo.

### C. Funcionalidad que conviene reemplazar o corregir

**Siguiente sprint:** contrato de apoyo de Jump y estado de sesión de DemoSession; etiquetado “Vidas”; validaciones ausentes; preview derivado con Undo. Son cambios localizados, no reemplazo total de backend.

**Cuando se priorice su uso:** separar modelo de puntuación/inventario de UIScript y su uGUI; reemplazar identidad por índices con definiciones estables; corregir/reemplazar PickUpAndHold y ciclo de vida de BalloonScript; corregir reloj de ConditionRepeat. No se recomienda gastar el próximo sprint en funciones fuera del MVP.

**No heredar como UX:** inspectores globales antiguos, diálogos de collider de Utils, listas UnityEvent/Action y selección libre de prefabs técnicos. Tampoco copiar sus formas de instanciar Runtime como mecanismo de autoría Editor.

### D. Funcionalidad que todavía no necesitamos

Disparo/proyectiles, recursos/inventario, seguimiento/persecución, movimiento aleatorio, propulsión/rotación de naves, generadores, destrucción temporizada, portales, interruptores, carga de niveles, diálogos encadenados y reglas generales. Son candidatos de packs posteriores, no una lista de componentes que haya que retirar ahora.

## Plan de siguiente sprint y criterios de cierre

**Bloque 1 — hacer fiables las cinco bases P0.** Estado de sesión único, validación de dependencias y unicidad del jugador, apoyo de suelo correcto y nombres semánticos. Añadir pruebas dirigidas: pared no permite salto, falta HUD no produce pérdida silenciosa de puntos, contactos posteriores a victoria no cambian el resultado, derrota/reinicio restaura estado.

**Bloque 2 — autoría mínima completa sin rediseño.** Reutilizar ventana actual; agrupar propiedades, añadir Puede saltar, campo entero para puntos/daño/resistencia, texto de meta legible, duplicar/eliminar con Undo. Color inmediato y Ancho sólo si imagen/collider y migración de escalas están cubiertos. Probar editar → guardar → reabrir → Undo/Redo donde corresponda, además de entrar/salir de Play Mode.

**Bloque 3 — gates P1.** Prueba de Patrol ida/vuelta con rangos extremos, duplicado lejos del origen, pasajero y enemigo real en escena. El test existente de movimiento no cubre estos requisitos. Si hay oscilación/arrastre defectuoso, resolver con cambio pequeño o no activar la tarjeta en el catálogo de taller.

**Bloque 4 — prueba educativa observada.** Una persona sin experiencia crea personaje, suelo, premio, peligro y meta; cambia dos parámetros; duplica y elimina; deshace; juega y termina. Registrar dónde necesita ayuda, no sólo ausencia de excepciones. No reconstruir SceneView ni presentar una UI final para medir esto.

Orden de corte si falta tiempo: quitar Ancho, retirar las dos tarjetas P1, posponer ContentPackDefinition; **no** quitar validación de escena, suelo correcto, estado de fin o Undo. Cierre mínimo: cinco elementos fiables, creación/configuración accesible y circuito de prueba completo.

## Respuestas para decidir

1. **Cinco clases con mejor relación utilidad/esfuerzo:** **CollectableAttribute**, **ModifyHealthAttribute**, **Move**, **ConditionArea** y **Patrol**. Las dos primeras exponen un valor claro; Move aporta control ya adaptado; ConditionArea permite meta y futuras interacciones; Patrol sirve a dos elementos. No son cinco clases autosuficientes: salud requiere HealthSystemAttribute, puntos requieren UIScript y condición requiere acción. Jump sigue siendo P0 para plataformas aunque su corrección de suelo reduce su relación utilidad/esfuerzo.
2. **Tres clases más importantes para esta demo de plataformas:** **Move, Jump y ConditionArea**. Hacen posible controlar, superar geometría y terminar. No constituyen por sí solas la demo completa; recompensa/daño añaden el feedback pedagógico y dependen de su infraestructura.
3. **Qué propiedades deben ver:** velocidad, fuerza de salto, puede saltar, resistencia inicial, color/ancho de plataforma, distancia de recorrido, puntos, daño, desaparición del peligro y mensaje final. Mantener controles/dirección como presets al inicio. Cada tarjeta muestra sólo su subconjunto, no todos los campos de GameItem.
4. **Qué ocultar siempre en modo participante:** referencias de componentes/HUD, tags/layers, índices de jugador/recurso, física técnica, prefabs crudos, waypoints absolutos, listas Action/UnityEvent y campos sin efecto. “Siempre” se refiere a esa superficie educativa; un futuro modo “Quiero saber cómo funciona” puede mostrar explicación e inspección técnica progresiva sin convertirlos en requisitos para crear.
5. **Arquitectura mínima:** definición y descriptores propios → ventana UI Toolkit → servicios Editor con Undo/SerializedObject → GameItem + un IItemBackend → adapter Playground → componentes reales. Pack fuera del core y sesión por escena. Evitar un adapter por propiedad, reflexión automática y UI generada desde el inspector vendor.
6. **Qué implementar después:** primero preflight de escena, suelo y estado de fin; después mejoras puntuales de propiedades y duplicar/eliminar con Undo; finalmente validar las dos variantes Patrol con pruebas de composición y una sesión con participantes. No ampliar ahora hacia un editor de reglas ni rediseño completo.

