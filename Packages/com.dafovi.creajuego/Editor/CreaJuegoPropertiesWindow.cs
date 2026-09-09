using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CreaJuego.Editor
{
    public sealed class CreaJuegoPropertiesWindow : EditorWindow
    {
        private EducationalPropertiesView view;
        private string shownSelection;
        [MenuItem("CreaJuego/Abrir propiedades")]
        public static void Open()=>GetWindow<CreaJuegoPropertiesWindow>("Propiedades");
        private void OnEnable() {
            EditorApplication.projectChanged+=Refresh;ObjectChangeEvents.changesPublished+=ObjectsChanged;Selection.selectionChanged+=Refresh;EditorApplication.hierarchyChanged+=HierarchyChanged;Undo.undoRedoPerformed+=Refresh;
            EditorSceneManager.activeSceneChangedInEditMode+=SceneChanged;
            EditorApplication.playModeStateChanged+=PlayChanged;
        }
        private void OnDisable() {
            EditorApplication.projectChanged-=Refresh;ObjectChangeEvents.changesPublished-=ObjectsChanged;Selection.selectionChanged-=Refresh;EditorApplication.hierarchyChanged-=HierarchyChanged;Undo.undoRedoPerformed-=Refresh;
            EditorSceneManager.activeSceneChangedInEditMode-=SceneChanged;
            EditorApplication.playModeStateChanged-=PlayChanged;view?.Dispose();view=null;
        }
        public void CreateGUI() {
            minSize=new Vector2(300,250);view?.Dispose();WorkshopWindowStyle.Apply(this);
            view=new EducationalPropertiesView();rootVisualElement.Add(view);Refresh();
        }
        private static string SelectionStamp() => string.Join("|",EducationalSelection.Items().Select(i=>i==null ? "none" : i.GetEntityId().ToString()+":"+i.name+":"+(i.definition!=null ? i.definition.GetEntityId().ToString() : "none")+":"+(i.SelectedAppearance!=null || i.customSprite!=null)));
        private void ObjectsChanged(ref ObjectChangeEventStream stream) => HierarchyChanged();
        private void HierarchyChanged() { if(shownSelection!=SelectionStamp()) Refresh(); }
        private void Refresh() { shownSelection=SelectionStamp(); view?.ShowSelection(); }
        private void SceneChanged(Scene a,Scene b)=>Refresh();
        private void PlayChanged(PlayModeStateChange state)=>Refresh();
    }
}






