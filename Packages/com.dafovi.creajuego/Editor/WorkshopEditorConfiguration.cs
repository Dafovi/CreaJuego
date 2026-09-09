using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace CreaJuego.Editor
{
    public static class WorkshopEditorConfiguration
    {
        // This is a project workspace resource, intentionally outside the runtime package.
        public const string LayoutRelativePath="Docs/Layouts/CreaJuego-Taller.wlt";
        public static string LayoutPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",LayoutRelativePath));

        [MenuItem("CreaJuego/Configurar editor",false,30)]
        public static void ConfigureEditor()
        {
            if(!TryConfigure(out var error))
                EditorUtility.DisplayDialog("Configurar editor",error,"Entendido");
        }

        [MenuItem("CreaJuego/Configurar editor",true)]
        private static bool CanConfigure()=>!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        public static void ConfigureGameView()
        {
            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
            PlayModeWindow.SetCustomRenderingResolution(1280,720,"CreaJuego 16:9");
        }
        public static bool TryConfigure(out string error)
        {
            error=null;
            if(!CanConfigure()) {error="Detén el juego y espera a que Unity termine de preparar el proyecto.";return false;}
            if(!File.Exists(LayoutPath)) {error="No se encuentra CreaJuego Taller. Recupera "+LayoutRelativePath+" desde el proyecto.";return false;}
            try {
                if(EditorUtility.LoadWindowLayout(LayoutPath)) { ConfigureGameView(); foreach(var view in Resources.FindObjectsOfTypeAll<SceneView>()) view.in2DMode=true; return true; }
                error="Unity no pudo cargar CreaJuego Taller. Puedes recuperar tu distribución desde el menú Layout.";
            } catch(Exception exception) {
                Debug.LogException(exception);
                error="No se pudo cargar CreaJuego Taller. Revisa la Console de Unity o recupera tu distribución desde Layout.";
            }
            return false;
        }
    }
}
