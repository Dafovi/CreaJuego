using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CreaJuego.Editor
{
    [InitializeOnLoad]
    public static class ItemAppearance
    {
        static ItemAppearance()
        {
            Undo.postprocessModifications += Changed;
            Undo.undoRedoPerformed += RefreshScene;
            EditorSceneManager.sceneOpened += (_, __) => RefreshScene();
        }
        private static UndoPropertyModification[] Changed(UndoPropertyModification[] changes)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                foreach (var item in changes.Select(c => c.currentValue.target).OfType<GameItem>().Distinct()) Apply(item, true);
            return changes;
        }
        public static void Apply(GameItem item, bool recordUndo)
        {
            if (item == null || EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(item)) return;
            if (!item.TryGetComponent<SpriteRenderer>(out var renderer) || renderer.color == item.tint) return;
            if (recordUndo) Undo.RecordObject(renderer, "Cambiar color");
            renderer.color = item.tint;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorSceneManager.MarkSceneDirty(item.gameObject.scene);
            SceneView.RepaintAll();
        }
        public static void RefreshScene()
        {
            foreach (var item in SceneObjects.All<GameItem>(SceneManager.GetActiveScene())) Apply(item, false);
        }
    }
}

