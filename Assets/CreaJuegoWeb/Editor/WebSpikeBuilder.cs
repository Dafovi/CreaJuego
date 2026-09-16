using System.Linq;
using CreaJuego.Web;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CreaJuego.Web.Editor
{
    public static class WebSpikeBuilder
    {
        public const string ScenePath="Assets/CreaJuegoWeb/Scenes/WebAuthoringSpike.unity";
        [MenuItem("CreaJuego/Web/Preparar spike")]
        public static void Prepare()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("CreaJuego Web",typeof(RuntimeAuthoringController),typeof(RuntimeAuthoringInput),typeof(RuntimeAuthoringUI),typeof(RuntimePerformanceProbe));
            var controller=root.GetComponent<RuntimeAuthoringController>();controller.ui=root.GetComponent<RuntimeAuthoringUI>();
            controller.definitions=EnsureWebCatalog();
            controller.sceneServicesPrefab=AssetDatabase.LoadAssetAtPath<ContentPackDefinition>("Assets/CreaJuegoPacks/Starter/Content/StarterPack.asset").sceneServices;
            controller.buildRoot=new GameObject("Runtime Authoring Root").transform;
            controller.buildCamera=MakeCamera("Cámara de construcción",new Vector3(0,1,-10),new Color(.04f,.06f,.1f));controller.buildCamera.gameObject.AddComponent<AudioListener>();
            controller.gameCamera=MakeCamera("Cámara de juego",new Vector3(0,0,-10),new Color(.16f,.24f,.36f));controller.gameCamera.enabled=false;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("CREAJUEGO_WEB_SCENE_READY");
        }
        static GameItemDefinition[] EnsureWebCatalog()
        {
            const string directory="Assets/CreaJuegoWeb/Content";
            const string prefabDirectory=directory+"/Prefabs";
            if(!AssetDatabase.IsValidFolder(directory))AssetDatabase.CreateFolder("Assets/CreaJuegoWeb","Content");
            if(!AssetDatabase.IsValidFolder(prefabDirectory))AssetDatabase.CreateFolder(directory,"Prefabs");
            var kinds=new[]{ItemKind.Player,ItemKind.Platform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Enemy,ItemKind.Goal,ItemKind.Decoration};
            var sources=AssetDatabase.FindAssets("t:GameItemDefinition",new[]{"Assets/CreaJuegoPacks/Starter/Content"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d=>d!=null&&kinds.Contains(d.kind)&&d.id!="piso").OrderBy(d=>d.order).ToArray();
            return sources.Select(source=>
            {
                var sourcePrefabPath=AssetDatabase.GetAssetPath(source.prefab);
                var safePrefabPath=$"{prefabDirectory}/{source.id}.prefab";
                var contents=PrefabUtility.LoadPrefabContents(sourcePrefabPath);
                try
                {
                    var item=contents.GetComponent<GameItem>();
                    if(item!=null){item.definition=null;item.appearanceCategory=null;}
                    PrefabUtility.SaveAsPrefabAsset(contents,safePrefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
                var definitionPath=$"{directory}/{source.id}.asset";
                var target=AssetDatabase.LoadAssetAtPath<GameItemDefinition>(definitionPath);
                if(target==null){target=ScriptableObject.CreateInstance<GameItemDefinition>();AssetDatabase.CreateAsset(target,definitionPath);}
                EditorUtility.CopySerialized(source,target);
                target.appearancePack=null;
                target.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(safePrefabPath);
                EditorUtility.SetDirty(target);
                return target;
            }).ToArray();
        }
        static Camera MakeCamera(string name,Vector3 position,Color color){var c=new GameObject(name).AddComponent<Camera>();c.orthographic=true;c.orthographicSize=5;c.transform.position=position;c.backgroundColor=color;c.clearFlags=CameraClearFlags.SolidColor;return c;}
        public static void BuildWebGL()
        {
            Prepare();PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/WebGLSpike",target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("WebGL build falló: "+report.summary.result);Debug.Log("CREAJUEGO_WEB_BUILD_READY "+report.summary.totalSize);
        }
    }
}