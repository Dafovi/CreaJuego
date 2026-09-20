using CreaJuego.Web;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreaJuego.Web.Editor
{
    [InitializeOnLoad]
    public static class WebEditableSceneSync
    {
        static WebEditableSceneSync()=>EditorSceneManager.sceneSaving+=OnSceneSaving;
        static void OnSceneSaving(Scene scene,string path)=>Capture(scene,false);
        [MenuItem("CreaJuego/Web/Sincronizar escena editable")]
        public static void CaptureActiveScene()
        {
            Capture(SceneManager.GetActiveScene(),true);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        static void Capture(Scene scene,bool allowRemoval)
        {
            if(!scene.IsValid()||!scene.isLoaded)return;
            foreach(var controller in Object.FindObjectsByType<RuntimeAuthoringController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(controller.gameObject.scene==scene&&controller.CaptureEditableScene(allowRemoval))EditorUtility.SetDirty(controller);
        }
    }
}