using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CreaJuego.Editor
{
    public sealed class CreaJuegoPropertiesWindow : EditorWindow
    {
        private EducationalPropertiesView view;
        [MenuItem("CreaJuego/Abrir propiedades")]
        public static void Open()=>GetWindow<CreaJuegoPropertiesWindow>("Propiedades");
        private void OnEnable() {
            Selection.selectionChanged+=Refresh;Undo.undoRedoPerformed+=Refresh;
            EditorSceneManager.activeSceneChangedInEditMode+=SceneChanged;
            EditorApplication.playModeStateChanged+=PlayChanged;
        }
        private void OnDisable() {
            Selection.selectionChanged-=Refresh;Undo.undoRedoPerformed-=Refresh;
            EditorSceneManager.activeSceneChangedInEditMode-=SceneChanged;
            EditorApplication.playModeStateChanged-=PlayChanged;view?.Dispose();
        }
        public void CreateGUI() {
            minSize=new Vector2(300,250);view?.Dispose();WorkshopWindowStyle.Apply(this);
            view=new EducationalPropertiesView();rootVisualElement.Add(view);Refresh();
        }
        private void Refresh()=>view?.ShowSelection();
        private void SceneChanged(Scene a,Scene b)=>Refresh();
        private void PlayChanged(PlayModeStateChange state)=>Refresh();
    }
}
