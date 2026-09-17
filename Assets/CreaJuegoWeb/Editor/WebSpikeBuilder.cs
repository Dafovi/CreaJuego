using System;
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
        const string ContentDirectory="Assets/CreaJuegoWeb/Content";
        [MenuItem("CreaJuego/Web/Preparar Parity 1")]
        public static void Prepare()
        {
            var pack=EnsureRuntimePack();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("CreaJuego Web",typeof(RuntimeAuthoringController),typeof(RuntimeAuthoringInput),typeof(RuntimeAuthoringUI),typeof(RuntimePerformanceProbe),typeof(RuntimeGameplayCamera));
            var controller=root.GetComponent<RuntimeAuthoringController>();controller.ui=root.GetComponent<RuntimeAuthoringUI>();controller.contentPack=pack;controller.definitions=pack.definitions;controller.sceneServicesPrefab=pack.sceneServices;
            controller.buildRoot=new GameObject("Runtime Authoring Root").transform;
            controller.buildCamera=MakeCamera("Cámara de construcción",new Vector3(0,1,-10),new Color(.04f,.06f,.1f));controller.buildCamera.rect=new Rect(230f/1280f,45f/720f,770f/1280f,615f/720f);controller.buildCamera.gameObject.AddComponent<AudioListener>();
            controller.gameCamera=MakeCamera("Cámara de juego",new Vector3(0,0,-10),new Color(.16f,.24f,.36f));controller.gameCamera.enabled=false;var gameListener=controller.gameCamera.gameObject.AddComponent<AudioListener>();gameListener.enabled=false;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("CREAJUEGO_WEB_PARITY_SCENE_READY");
        }
        static RuntimeContentPack EnsureRuntimePack()
        {
            EnsureFolder("Assets/CreaJuegoWeb","Content");EnsureFolder(ContentDirectory,"Prefabs");EnsureFolder(ContentDirectory,"Appearances");
            var kinds=new[]{ItemKind.Player,ItemKind.Platform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Enemy,ItemKind.Goal,ItemKind.Decoration};
            var sources=AssetDatabase.FindAssets("t:GameItemDefinition",new[]{"Assets/CreaJuegoPacks/Starter/Content"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d!=null&&kinds.Contains(d.kind)&&d.id!="piso").OrderBy(d=>d.order).ToArray();
            var preparedDefinitions=sources.Select(PrepareDefinition).ToArray();var sourcePack=sources.Select(d=>d.appearancePack).FirstOrDefault(p=>p!=null);var categories=kinds.Select(kind=>PrepareCategory(sourcePack?.CategoryFor(kind),kind)).Where(c=>c!=null).ToArray();
            var preparedPack=LoadOrCreate<ContentPackDefinition>(ContentDirectory+"/WebAppearances.asset");preparedPack.id="web-parity-1";preparedPack.categories=categories;preparedPack.appearances=Array.Empty<AppearanceDefinition>();preparedPack.sceneServices=null;EditorUtility.SetDirty(preparedPack);
            var defaults=sources.Select(source=>
            {
                var category=Array.Find(categories,c=>c.kind==source.kind);var sourceAppearance=source.prefab.GetComponent<GameItem>()?.SelectedAppearance;var option=category?.options.FirstOrDefault(o=>sourceAppearance!=null&&o.Preview==sourceAppearance.sprite)??category?.options.FirstOrDefault();return new RuntimeAppearanceDefault{kind=source.kind,appearanceId=option?.id??""};
            }).GroupBy(d=>d.kind).Select(g=>g.First()).ToArray();
            var runtime=LoadOrCreate<RuntimeContentPack>(ContentDirectory+"/WebRuntimePack.asset");runtime.id="web-parity-1";runtime.definitions=preparedDefinitions;runtime.preparedAppearances=preparedPack;runtime.defaults=defaults;runtime.sceneServices=AssetDatabase.LoadAssetAtPath<ContentPackDefinition>("Assets/CreaJuegoPacks/Starter/Content/StarterPack.asset").sceneServices;EditorUtility.SetDirty(runtime);return runtime;
        }
        static GameItemDefinition PrepareDefinition(GameItemDefinition source)
        {
            var safePrefabPath=$"{ContentDirectory}/Prefabs/{source.id}.prefab";var contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.prefab));
            try{var item=contents.GetComponent<GameItem>();if(item!=null){item.definition=null;item.appearanceCategory=null;if(source.kind==ItemKind.Player&&item.GetComponent<PlayerFallRecovery>()==null)item.gameObject.AddComponent<PlayerFallRecovery>();}PrefabUtility.SaveAsPrefabAsset(contents,safePrefabPath);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            var target=LoadOrCreate<GameItemDefinition>($"{ContentDirectory}/{source.id}.asset");EditorUtility.CopySerialized(source,target);target.runtimeOnly=true;target.appearancePack=null;target.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(safePrefabPath);EditorUtility.SetDirty(target);return target;
        }
        static AppearanceCategory PrepareCategory(AppearanceCategory source,ItemKind kind)
        {
            if(source==null)return null;var target=LoadOrCreate<AppearanceCategory>($"{ContentDirectory}/Appearances/{kind}.asset");target.kind=kind;target.options.Clear();
            foreach(var original in source.options.Where(o=>o!=null&&o.Preview!=null))
            {
                var data=(IAppearanceData)original;target.options.Add(new AppearanceOption{id=original.id,displayName=original.displayName,sprite=data.sprite,controller=data.controller,animationProfile=data.animationProfile,idleClip=data.idleClip,moveClip=data.moveClip,jumpClip=data.jumpClip,attackClip=data.attackClip,scale=data.scale,offset=data.offset,flipX=data.flipX,preserveAspectWithoutPrefab=data.preserveAspect});
            }
            target.EnsureIds();EditorUtility.SetDirty(target);return target;
        }
        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void EnsureFolder(string parent,string name){var path=parent+"/"+name;if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,name);}
        static Camera MakeCamera(string name,Vector3 position,Color color){var camera=new GameObject(name).AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=position;camera.backgroundColor=color;camera.clearFlags=CameraClearFlags.SolidColor;return camera;}
        [MenuItem("CreaJuego/Web/Generar WebGL Parity 1")]
        public static void BuildWebGLParity1()
        {
            Prepare();PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/WebGLParity1",target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL Parity 1 falló: "+report.summary.result);Debug.Log("CREAJUEGO_WEB_PARITY_BUILD_READY "+report.summary.totalSize);
        }
        public static void BuildWebGL()=>BuildWebGLParity1();
    }
}