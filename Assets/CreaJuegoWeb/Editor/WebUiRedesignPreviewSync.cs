using CreaJuego.Web;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CreaJuego.Web.Editor
{
    [InitializeOnLoad]
    static class WebUiRedesignPreviewSync
    {
        // Mantiene la vista del Canvas disponible al editar la escena y después de salir de Play Mode.
        static WebUiRedesignPreviewSync()
        {
            EditorApplication.delayCall+=ApplyWhenNeeded;
            EditorApplication.playModeStateChanged+=OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened+=OnSceneOpened;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=ApplyWhenNeeded;
        }

        static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene,OpenSceneMode mode)
        {
            if(scene.path==WebSpikeBuilder.ScenePath)EditorApplication.delayCall+=ApplyWhenNeeded;
        }

        static void ApplyWhenNeeded()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ApplyWhenNeeded;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=WebSpikeBuilder.ScenePath)return;
            var ui=Object.FindAnyObjectByType<RuntimeAuthoringUI>();if(ui==null||!ui.NeedsEditableLayoutRefresh)return;
            WebSpikeBuilder.ApplyUiRedesignPreview();
        }
    }
}
