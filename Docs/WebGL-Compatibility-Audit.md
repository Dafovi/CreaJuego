# WebGL Compatibility Audit

## Línea base

- Rama: `feature/webgl-spike` desde `debb2d1`.
- Unity: `6000.6.0f1 (f7f8ed4d1e24)`.
- Módulo WebGL Support: instalado.
- Frontend Editor: se conserva y sigue compilando.
- Frontend Web: assembly runtime separado, sin referencias a `UnityEditor`.

## Clasificación

| Sistema | Clase | Evidencia y decisión |
|---|---|---|
| Elementos y gameplay | A | `GameItem`, movimiento, premios, peligros, meta, combate y recuperación funcionan como runtime normal. |
| Apariencias | A/B | `ItemVisual`, sprites y animaciones son runtime. El catálogo Web crea copias seguras de siete descriptores/prefabs para no incluir categorías con prefabs externos incompletos. |
| Daño y combate | A | `PlayerDamageReceiver`, `PlayerAttack` y `EnemyVitality` se reutilizan en clones frescos de PLAY. |
| Adaptación Playground | A/B | Los adapters y comportamientos existentes se reutilizan. Los servicios de escena sólo se instancian durante PLAY. |
| Cámara | B | Cinemachine es runtime y compatible, pero el spike usa una cámara de juego estática para aislar la prueba de autoría. |
| Metadata | A | Nombre de proyecto y equipo viven en el DTO JSON. |
| Catálogo y packs | B | Los `ScriptableObject` se serializan en la escena; en Web no se consulta `AssetDatabase`. |
| EditorWindow y UI Toolkit Editor | C | Permanecen intactos. El frontend Web usa uGUI runtime generado por código. |
| `ItemService`, `SceneService`, propiedades Editor | C | Usan `AssetDatabase`, `PrefabUtility`, `Selection`, `Undo`, `SerializedObject` o `SceneView`. Se reemplazan por servicios runtime pequeños. |
| Undo/Redo de Unity | C | No existe en una build. Se reemplaza por snapshots acotados de `CreaJuegoProjectData`. |
| Importación de imagen Editor | C | El pipeline de `AssetDatabase` no existe en WebGL. |
| Importación de imagen Web | B | Plugin `.jslib`, `<input type=file>`, `FileReader`, data URL, `Texture2D` y `Sprite`. Experimental. |
| Guardado local | B | `Application.persistentDataPath` y `System.IO`; Unity WebGL sincroniza el sistema virtual con almacenamiento del navegador/IndexedDB. |
| Rutas arbitrarias del escritorio | D | El sandbox del navegador exige selección explícita de archivos. |
| Reflection runtime | A | La nueva capa no introduce reflection. |

## Dependencias Editor que no se trasladan

`EditorWindow`, `SceneView`, `Selection`, `Undo`, `SerializedObject`, `SerializedProperty`, `AssetDatabase` y `PrefabUtility` siguen siendo herramientas del frontend Editor. El runtime Web opera sobre datos JSON, referencias serializadas y servicios propios. No intenta emular el Editor.

## Riesgos verificados

- El almacenamiento es específico del origen y perfil del navegador y el usuario puede borrarlo.
- Las imágenes base64 aumentan el JSON y la memoria; faltan límites de resolución y peso.
- Las categorías de Mundo misterioso incluyen prefabs del kit externo cuyo código está desactivado. El build Web los excluye mediante un catálogo seguro generado; no se modifican los packs originales.
- Los snapshots reconstruyen objetos y pueden encarecer proyectos mucho mayores que el escenario de 43 objetos.
- La cámara estática valida el modo PLAY, pero falta validar seguimiento Cinemachine en Web.

## Fuentes oficiales

- [Unity 6.6: Application.persistentDataPath](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-persistentDataPath.html)
- [Unity 6.6: compatibilidad de navegador Web](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-browsercompatibility.html)
- [Unity 6.6: interacción con JavaScript del navegador](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-interactingwithbrowserscripting.html)