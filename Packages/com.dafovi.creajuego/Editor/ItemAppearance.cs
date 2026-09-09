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
        static UndoPropertyModification[] Changed(UndoPropertyModification[] changes)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
                foreach(var item in changes.Select(c=>c.currentValue.target).OfType<GameItem>().Distinct()) Apply(item,true);
            return changes;
        }
        public static void Choose(GameItem[] items, AppearanceDefinition appearance, Sprite custom)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Cambiar apariencia");
            foreach(var item in items)
            {
                if(appearance!=null && (item.definition==null || appearance.kind!=item.definition.kind)) continue;
                // Legacy objects are upgraded only on an explicit appearance change.
                if(item.GetComponent<ItemVisual>()==null)
                {
                    var child=new GameObject("Visual"); Undo.RegisterCreatedObjectUndo(child,"Crear apariencia");
                    Undo.SetTransformParent(child.transform,item.transform,"Crear apariencia"); child.transform.localPosition=Vector3.zero;
                    var visual=Undo.AddComponent<ItemVisual>(item.gameObject);
                    visual.renderer=Undo.AddComponent<SpriteRenderer>(child);
                    visual.animator=Undo.AddComponent<Animator>(child);
                    var legacy=item.GetComponent<SpriteRenderer>();
                    visual.geometrySource=legacy;
                    if(legacy!=null) { visual.renderer.sprite=legacy.sprite; visual.renderer.sortingLayerID=legacy.sortingLayerID; visual.renderer.sortingOrder=legacy.sortingOrder; Undo.RecordObject(legacy,"Cambiar apariencia"); legacy.enabled=false; }
                }
                using(var data=new SerializedObject(item))
                {
                    data.FindProperty(nameof(GameItem.appearance)).objectReferenceValue=appearance;
                    data.FindProperty(nameof(GameItem.customSprite)).objectReferenceValue=custom;
                    data.ApplyModifiedProperties();
                }
                Apply(item,true);
            }
            Undo.CollapseUndoOperations(group);
        }
        public static void Apply(GameItem item,bool recordUndo)
        {
            if(item==null || EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.IsPersistent(item)) return;
            bool changed=false;
            var visual=item.GetComponent<ItemVisual>();
            var legacy=item.GetComponent<SpriteRenderer>();
            if(visual!=null && visual.renderer!=null)
            {
                UnityEngine.Object[] targets=visual.animator!=null ? new UnityEngine.Object[]{visual.renderer,visual.renderer.transform,visual.animator} : new UnityEngine.Object[]{visual.renderer,visual.renderer.transform};
                var before=targets.Select(target=>EditorJsonUtility.ToJson(target)).ToArray();
                if(recordUndo) Undo.RecordObjects(targets,"Cambiar apariencia");
                visual.Apply();
                for(int index=0;index<targets.Length;index++) if(before[index]!=EditorJsonUtility.ToJson(targets[index])) { changed=true; PrefabUtility.RecordPrefabInstancePropertyModifications(targets[index]); }
            }
            var legacyColor=visual==null ? ItemVisual.BaseColor(item) : item.tint;
            if(legacy!=null && legacy.color!=legacyColor) { if(recordUndo) Undo.RecordObject(legacy,"Cambiar color"); legacy.color=legacyColor; changed=true; PrefabUtility.RecordPrefabInstancePropertyModifications(legacy); }
            if(changed) { EditorSceneManager.MarkSceneDirty(item.gameObject.scene); SceneView.RepaintAll(); }
        }
        public static void RefreshScene() { foreach(var item in SceneObjects.All<GameItem>(SceneManager.GetActiveScene())) Apply(item,false); }
    }
}



