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
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén el juego antes de crear.");
            if (definition == null || definition.prefab == null) throw new ArgumentException("Este elemento no tiene contenido asignado.");
            if (!CanCreate(definition)) throw new InvalidOperationException("Este taller usa un solo " + definition.displayName.ToLowerInvariant() + ". Selecciona el que ya está en la escena.");
            var source = definition.prefab.GetComponent<GameItem>();
            if (source == null || Backend(source) == null) throw new ArgumentException("Este elemento no tiene un comportamiento disponible.");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
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
            var category=definition.appearancePack?.CategoryFor(definition.kind);
            var option=category?.options.FirstOrDefault(o=>o!=null && o.Preview!=null);
            if(option!=null) ItemAppearance.ChooseOption(new[]{item},category,option.id);
            if(definition.kind==ItemKind.Player && SceneObjects.All<WorkshopCameraRig>(go.scene).Length>0) WorldAuthoringService.EnsureCamera();
            Undo.CollapseUndoOperations(group);
            return item;
        }

        private static void RequireEditable(GameItem item)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén el juego antes de editar.");
            if (item == null || EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid() || item.definition == null)
                throw new InvalidOperationException("Elige un elemento de la escena.");
        }

        public static GameItem Duplicate(GameItem source)
        {
            RequireEditable(source);
            if (!source.definition.allowMultiple) throw new InvalidOperationException("Este taller usa un solo personaje.");
            if (!PrefabUtility.IsPartOfPrefabInstance(source))
                throw new InvalidOperationException("Este elemento no se puede duplicar desde el catálogo.");
            // The Visual child is a CreaJuego-authored upgrade for old prefabs, so it is safe to recreate.
            var addedComponents=PrefabUtility.GetAddedComponents(source.gameObject);
            var addedObjects=PrefabUtility.GetAddedGameObjects(source.gameObject);
            bool onlyEducationalVisual=addedComponents.All(a=>a.instanceComponent is ItemVisual) &&
                addedObjects.All(a=>a.instanceGameObject!=null && a.instanceGameObject.name=="Visual" &&
                    a.instanceGameObject.transform.parent==source.transform &&
                    a.instanceGameObject.GetComponents<Component>().All(c=>c is Transform || c is SpriteRenderer || c is Animator));
            if (!onlyEducationalVisual || PrefabUtility.GetRemovedComponents(source.gameObject).Count > 0 || PrefabUtility.GetRemovedGameObjects(source.gameObject).Count > 0)
                throw new InvalidOperationException("Este elemento tiene cambios avanzados. Duplícalo desde la vista Escena.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
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
            var item=go.GetComponent<GameItem>();
            if(source.GetComponent<ItemVisual>()!=null && item.GetComponent<ItemVisual>()==null) ItemAppearance.EnsureVisual(item);
            ItemAppearance.Apply(item,true);
            Selection.activeGameObject = go;
            Undo.CollapseUndoOperations(group);
            return item;
        }

        public static string Delete(GameItem item)
        {
            RequireEditable(item);
            var kind = item.definition.kind;
            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(item.gameObject);
            Selection.activeGameObject = null;
            return kind == ItemKind.Player ? "Tu juego necesita un personaje para poder jugarse." : kind == ItemKind.Goal ? "Agrega una Meta para indicar dónde termina el recorrido." : "Elemento eliminado. Ctrl+Z lo recupera.";
        }

        private static UnityEngine.SceneManagement.Scene SceneManagerSetup() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    }
}


