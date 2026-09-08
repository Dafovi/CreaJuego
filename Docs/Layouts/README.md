# CreaJuego Taller — layout oficial

[CreaJuego-Taller.wlt](CreaJuego-Taller.wlt) es la copia canónica del layout creado y validado manualmente por el responsable del taller. Es una referencia de workspace, no contenido Runtime. Unity es responsable de interpretarlo y cargarlo.

La copia conserva exactamente los bytes de layoutTaller.wlt; no se reconstruyó ni se editaron datos internos. El nombre oficial «CreaJuego Taller» se aplica al recurso y a su documentación. Para que aparezca con ese nombre en el menú, guárdalo con ese nombre desde Unity después de cargarlo.

## Archivo y procedencia
- Original: layoutTaller.wlt, guardado el 8 de septiembre de 2026. La ruta personal sólo se registra en documentación local excluida de Git.
- Tamaño: 49.792 bytes.
- Formato: texto YAML 1.1 serializado por Unity, etiqueta tag:unity3d.com,2011 y bloques !u!114. La extensión es .wlt.
- SHA256: 16153B3E341B606709B755CB9C2C7D6A5DCB87273EDA55F971EC1D73199B0D70.
- Versión de referencia: Unity 6000.6.0f1, según el proyecto y el contexto de creación comunicado. El archivo no declara por sí solo esa versión exacta.
- .gitattributes conserva sus bytes y saltos de línea al pasar por Git. No se reformatea el YAML.

## Distribución realmente guardada
La referencia oficial es el archivo manual, incluso donde difiere del esquema conceptual del sprint.

| Zona guardada | Función |
|---|---|
| Izquierda: CreaJuego, con Hierarchy como otra pestaña del mismo grupo | Qué puedo añadir, qué ya existe; añadir y seleccionar |
| Columna intermedia: Propiedades | Qué estoy modificando; personalizar |
| Área grande derecha: SceneView | Dónde construyo y coloco los elementos |
| Franja inferior a todo el ancho: Jugar | Comprobar si falta algo, ejecutar y detener |

La inspección encuentra CreaJuego en x=0, ancho 831; Propiedades en x=832, ancho 460; SceneView en x=1294, ancho 1265. Las tres áreas guardan altura 1061. Jugar guarda ancho 2560 y altura 180. Son coordenadas serializadas, no medidas verificadas en una pantalla nueva.

**Diferencia respecto al esquema propuesto:** el archivo guarda CreaJuego → Propiedades → SceneView, no CreaJuego → SceneView → Propiedades. No se intercambiaron paneles ni se estrechó el catálogo. SceneView es el área individual más ancha.
No contiene una pestaña Game guardada. Se puede abrir desde los menús habituales de Unity cuando haga falta; no se añadió a la copia.
El flujo pedagógico sigue siendo Añadir → Seleccionar → Personalizar → Jugar.

## Configurar el Editor con un clic
Selecciona **CreaJuego > Configurar editor** para cargar esta copia canónica. Es una acción explícita: cambia las ventanas del Editor, pero no edita el archivo del layout ni prepara una escena.
Antes de usarla por primera vez puedes guardar tu distribución personal desde Layout > Save Layout. Para volver, elige ese layout desde el menú de Unity.
La acción está deshabilitada durante Play o compilación. Si falta el recurso o falla la carga, muestra un mensaje.
Abrir taller y Preparar escena conservan sus responsabilidades; ninguno aplica el layout.

Corrección de la investigación anterior: existe el método público EditorUtility.LoadWindowLayout, visible en el [código oficial de Unity](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorUtility.cs). La implementación lo llama directamente, sin reflection ni referencias a WindowLayout internas. No debe confundirse cargar un archivo ya creado con construir por código un árbol de docking.

## Alternativa manual en Unity 6000.6
Los nombres ingleses siguientes están comprobados en el [manual oficial de Unity 6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/CustomizingYourWorkspace.html), que documenta Save Layout, Save layout to file y Load layout from file.

1. Abre el proyecto con las tres ventanas CreaJuego compiladas. El layout no instala el package ni soluciona errores de compilación.
2. Antes de cambiar de distribución, usa **Layout > Save Layout** y guarda tu workspace con un nombre propio, por ejemplo «Mi espacio». Como respaldo externo, puedes usar **Save layout to file**.
3. Usa **Layout > Load layout from file**.
4. Selecciona **Docs/Layouts/CreaJuego-Taller.wlt** dentro de este proyecto.
5. Si quieres tenerlo en la lista de layouts, usa **Layout > Save Layout** y pon **CreaJuego Taller**.

No es necesario copiarlo a carpetas privadas de Unity ni ejecutar scripts de CreaJuego. Cargarlo es una decisión explícita del usuario y cambia la distribución mediante Unity.
La comprobación de nombres y flujo es documental; no se realizó una nueva carga interactiva de la copia durante este trabajo.

## Volver a tu distribución
Selecciona «Mi espacio» (o el nombre que guardaste) desde Layout. También puedes recuperar el respaldo mediante Load layout from file o elegir una distribución estándar del menú.
No hace falta borrar CreaJuego, modificar escenas o usar Reset all Layouts, que afectaría a otras personalizaciones.

## Portabilidad y límites
**Razonablemente almacenable y reutilizable; portabilidad condicionada, no garantizada.**
- No se detectaron rutas absolutas Windows, UNC, /Users/, /home/ ni file:// en el archivo.
- Las referencias de scripts CreaJuego son GUID que coinciden con los .meta de CreaJuegoWindow, CreaJuegoPropertiesWindow y CreaJuegoPlayBarWindow. Deben conservarse los .meta y la identidad CreaJuego.Editor de las clases/assembly.
- Otros GUID/fileID remiten a ventanas y recursos de Unity; hay IDs locales del árbol de ventanas, m_WindowGUID y m_UndoId. No se interpretan como identificadores portables de contenido ni se modifican.
- Incluye estado de SceneView (modo 2D, orientación, encuadre, overlays), selección de pestañas y estado de Hierarchy. Un layout no sustituye la escena, el catálogo ni los assets del taller.
- La ventana principal guarda un rectángulo 2560×1349, origen (0,43), maximizado. El tamaño/resolución/DPI del equipo receptor y los mínimos de los paneles pueden cambiar el resultado. No se promete apariencia idéntica entre monitores.
- Si faltan scripts o no compilan, Unity puede no restaurar las ventanas personalizadas correctamente. Ese comportamiento no se probó. Instala/compila primero el package; después abre CreaJuego > Abrir taller y vuelve a cargar el archivo.
- En otro proyecto se requiere una versión compatible del package con sus .meta y dependencias. El layout no transporta escenas ni contenido. La carga en otro proyecto/OS/monitor no está validada.
- Las clases internas DockArea/SplitView aparecen porque Unity las serializa en su propio formato. CreaJuego no las invoca, no las modifica y no implementa APIs internas de carga.

## Independencia de las ventanas y validación
CreaJuego > Abrir taller sigue abriendo/reutilizando CreaJuego, Propiedades, Jugar y SceneView sin aplicar este archivo ni programar posiciones. Las ventanas pueden usarse con cualquier layout.
La adopción inicial sólo añadió archivos/documentación. La actualización Configurar editor incorpora un comando Editor que carga el archivo mediante una API pública; los resultados nuevos se documentan abajo.
Se verificaron copia byte a byte mediante SHA256, tamaño, referencias de scripts y ausencia de rutas absolutas detectables. La validación visual manual de la distribución original es la comunicada por el usuario; no se atribuye al asistente una carga o revisión visual nueva.
Sin builds standalone, commit ni push.

## Verificación de Configurar editor
- Método público EditorUtility.LoadWindowLayout compilado directamente en Unity 6000.6.0f1, sin reflection.
- 36/36 tests Editor aprobados, incluidos los 35 anteriores. Resultado local: Docs/configure-tests.xml.
- Prueba adicional de carga real en la copia aislada: carga correcta, una ventana de cada tipo CreaJuego y SceneView presente; la escena con cambios sin guardar y su objeto de prueba se conservaron.
- Registro local: Docs/configure-load-unity.log, CREAJUEGO_LAYOUT_LOAD_VERIFIED=True; unsaved scene preserved=True.
- Es una comprobación automatizada en Unity Editor, no una inspección visual manual de la distribución.
- Copia canónica sin modificaciones. No se aplicó el layout al Editor de trabajo del usuario durante estas comprobaciones.
- No se modificaron Abrir taller ni Preparar escena. Sin builds, commit ni push.
