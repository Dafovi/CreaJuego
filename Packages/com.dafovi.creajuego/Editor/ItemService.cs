using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CreaJuego.Editor
{
    public static class ItemService
    {
        public static GameItemDefinition[] Catalog() => AssetDatabase.FindAssets("t:GameItemDefinition")
            .Select(id => AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(d => d != null).OrderBy(d => d.order).ThenBy(d => d.displayName).ToArray();

        public static IItemBackend Backend(GameItem item) => item.GetComponents<MonoBehaviour>().OfType<IItemBackend>().SingleOrDefault();

        public static GameItemDefinition[] WorkshopCatalog() => Catalog().Where(d => d.availableInWorkshop).ToArray();

        public static bool CanCreate(GameItemDefinition definition) => definition != null && (definition.allowMultiple ||
            !SceneManagerSetup().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameItem>(true)).Any(i => i.definition == definition));

        public static GameItem Create(GameItemDefinition definition, Vector3 position)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén la prueba antes de crear.");
            if (definition == null || definition.prefab == null) throw new ArgumentException("Este elemento no tiene contenido asignado.");
            if (!CanCreate(definition)) throw new InvalidOperationException("Este taller usa un solo " + definition.displayName.ToLowerInvariant() + ". Selecciona el que ya está en la escena.");
            var source = definition.prefab.GetComponent<GameItem>();
            if (source == null || Backend(source) == null) throw new ArgumentException("Este elemento no tiene un comportamiento disponible.");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(definition.prefab, SceneManagerSetup());
            go.name = definition.displayName;
            go.transform.position = position;
            var item = go.GetComponent<GameItem>();
            item.definition = definition;
            Undo.RegisterCreatedObjectUndo(go, "Añadir " + definition.displayName);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            return item;
        }

        private static void RequireEditable(GameItem item)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén la prueba antes de editar.");
            if (item == null || EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid() || item.definition == null)
                throw new InvalidOperationException("Elige un elemento de la escena.");
        }

        public static GameItem Duplicate(GameItem source)
        {
            RequireEditable(source);
            if (!source.definition.allowMultiple) throw new InvalidOperationException("Este taller usa un solo personaje.");
            if (!PrefabUtility.IsPartOfPrefabInstance(source))
                throw new InvalidOperationException("Este elemento no se puede duplicar desde el catálogo.");
            // Workshop duplication preserves prefab overrides; structural technical edits need Unity's own tools.
            if (PrefabUtility.GetAddedComponents(source.gameObject).Count > 0 || PrefabUtility.GetRemovedComponents(source.gameObject).Count > 0 ||
                PrefabUtility.GetAddedGameObjects(source.gameObject).Count > 0 || PrefabUtility.GetRemovedGameObjects(source.gameObject).Count > 0)
                throw new InvalidOperationException("Este elemento tiene cambios avanzados. Duplícalo desde la vista Escena.");
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, source.gameObject.scene);
            PrefabUtility.SetPropertyModifications(go, PrefabUtility.GetPropertyModifications(source.gameObject));
            go.transform.SetParent(source.transform.parent, false);
            go.transform.position = source.transform.position + new Vector3(1, .5f, 0);
            go.name = source.gameObject.name + " (copia)";
            Undo.RegisterCreatedObjectUndo(go, "Duplicar " + source.definition.displayName);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            return go.GetComponent<GameItem>();
        }

        public static void Delete(GameItem item)
        {
            RequireEditable(item);
            Undo.DestroyObjectImmediate(item.gameObject);
        }

        private static UnityEngine.SceneManagement.Scene SceneManagerSetup() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    }
}
