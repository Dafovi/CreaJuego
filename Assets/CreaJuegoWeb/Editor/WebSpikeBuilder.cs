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
            var root=new GameObject("CreaJuego Web",typeof(RuntimeAuthoringController),typeof(RuntimeAuthoringInput),typeof(RuntimeAuthoringUI));
            var controller=root.GetComponent<RuntimeAuthoringController>();controller.ui=root.GetComponent<RuntimeAuthoringUI>();controller.contentPack=pack;controller.definitions=pack.definitions;controller.sceneServicesPrefab=pack.sceneServices;
            controller.buildRoot=new GameObject("Runtime Authoring Root").transform;
            controller.buildCamera=MakeCamera("Cámara de construcción",new Vector3(0,1,-10),new Color(.04f,.06f,.1f));controller.buildCamera.rect=new Rect(260f/1280f,82f/720f,740f/1280f,564f/720f);controller.buildCamera.gameObject.AddComponent<AudioListener>();
            controller.gameCamera=MakeCamera("Cámara de juego",new Vector3(0,0,-10),new Color(.16f,.24f,.36f));controller.gameCamera.enabled=false;var gameListener=controller.gameCamera.gameObject.AddComponent<AudioListener>();gameListener.enabled=false;
            controller.ui.PrepareEditableLayout();controller.PrepareEditableScene();controller.ui.Refresh();EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.ui);EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("CREAJUEGO_WEB_PARITY_SCENE_READY");
        }
        static RuntimeContentPack EnsureRuntimePack()
        {
            EnsureFolder("Assets/CreaJuegoWeb","Content");EnsureFolder(ContentDirectory,"Prefabs");EnsureFolder(ContentDirectory,"Appearances");
            var kinds=new[]{ItemKind.Player,ItemKind.Platform,ItemKind.MovingPlatform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Enemy,ItemKind.Goal,ItemKind.Decoration,ItemKind.Background};
            var sources=AssetDatabase.FindAssets("t:GameItemDefinition",new[]{"Assets/CreaJuegoPacks/Starter/Content"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d!=null&&kinds.Contains(d.kind)&&d.id!="piso").OrderBy(d=>d.order).ToArray();
            var preparedDefinitions=sources.Select(PrepareDefinition).Concat(new[]{PrepareBackgroundDefinition(EnsureBackgroundSprite())}).ToArray();var sourcePack=sources.Select(d=>d.appearancePack).FirstOrDefault(p=>p!=null);var categories=kinds.Select(kind=>PrepareCategory(sourcePack?.CategoryFor(kind),kind)).Where(c=>c!=null).ToArray();
            var preparedPack=LoadOrCreate<ContentPackDefinition>(ContentDirectory+"/WebAppearances.asset");preparedPack.id="web-parity-1";preparedPack.categories=categories;preparedPack.appearances=Array.Empty<AppearanceDefinition>();preparedPack.sceneServices=null;EditorUtility.SetDirty(preparedPack);
            var defaults=sources.Select(source=>
            {
                var category=Array.Find(categories,c=>c.kind==source.kind);var option=PreferredDefault(category,source);return new RuntimeAppearanceDefault{kind=source.kind,appearanceId=option?.id??""};
            }).Concat(new[]{new RuntimeAppearanceDefault{kind=ItemKind.Background,appearanceId="web-cielo-azul"}}).GroupBy(d=>d.kind).Select(g=>g.First()).ToArray();
            var runtime=LoadOrCreate<RuntimeContentPack>(ContentDirectory+"/WebRuntimePack.asset");runtime.id="web-parity-1";runtime.definitions=preparedDefinitions;runtime.preparedAppearances=preparedPack;runtime.defaults=defaults;runtime.sceneServices=AssetDatabase.LoadAssetAtPath<ContentPackDefinition>("Assets/CreaJuegoPacks/Starter/Content/StarterPack.asset").sceneServices;EditorUtility.SetDirty(runtime);return runtime;
        }
        static AppearanceOption PreferredDefault(AppearanceCategory category,GameItemDefinition source)
        {
            if(category==null)return null;
            if(source.kind==ItemKind.Player){var gino=category.options.FirstOrDefault(o=>o!=null&&(o.id=="platformer-kit-gino"||o.idleClip!=null&&o.idleClip.name=="Gino-Idle"));if(gino!=null)return gino;}
            if(source.kind==ItemKind.Enemy){var scarecrow=category.Find("platformer-kit-scarecrow");if(scarecrow!=null)return scarecrow;}
            var sourceAppearance=source.prefab.GetComponent<GameItem>()?.SelectedAppearance;return category.options.FirstOrDefault(o=>sourceAppearance!=null&&o.Preview==sourceAppearance.sprite)??category.options.FirstOrDefault();
        }
        static GameItemDefinition PrepareDefinition(GameItemDefinition source)
        {
            var preparedDefault=source.appearancePack?.CategoryFor(source.kind)?.Default;
            var safePrefabPath=$"{ContentDirectory}/Prefabs/{source.id}.prefab";var contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.prefab));
            try
            {
                var item=contents.GetComponent<GameItem>();
                if(item!=null)
                {
                    item.definition=null;item.appearanceCategory=null;item.appearanceId="";
                    var visual=item.GetComponent<ItemVisual>();
                    if(preparedDefault!=null&&visual!=null&&visual.renderer!=null)
                    {
                        visual.renderer.sprite=preparedDefault.Preview;visual.renderer.color=Color.white;
                        visual.renderer.transform.localScale=new Vector3(preparedDefault.scale.x,preparedDefault.scale.y,1);
                        visual.renderer.transform.localPosition=preparedDefault.offset;visual.renderer.flipX=preparedDefault.flipX;
                        if(visual.geometrySource!=null&&visual.geometrySource!=visual.renderer)visual.geometrySource.enabled=false;
                    }
                    if(source.kind==ItemKind.Player&&item.GetComponent<PlayerFallRecovery>()==null)item.gameObject.AddComponent<PlayerFallRecovery>();
                }
                PrefabUtility.SaveAsPrefabAsset(contents,safePrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            var target=LoadOrCreate<GameItemDefinition>($"{ContentDirectory}/{source.id}.asset");EditorUtility.CopySerialized(source,target);target.runtimeOnly=true;target.appearancePack=null;target.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(safePrefabPath);if(preparedDefault?.Preview!=null)target.icon=preparedDefault.Preview;EditorUtility.SetDirty(target);return target;
        }
        static AppearanceCategory PrepareCategory(AppearanceCategory source,ItemKind kind)
        {
            if(source==null&&kind!=ItemKind.Background)return null;var target=LoadOrCreate<AppearanceCategory>($"{ContentDirectory}/Appearances/{kind}.asset");target.kind=kind;target.defaultAppearanceId=source!=null?source.defaultAppearanceId:"";target.options.Clear();
            foreach(var original in (source!=null?source.options:Enumerable.Empty<AppearanceOption>()).Where(o=>o!=null&&o.Preview!=null))
            {
                var data=(IAppearanceData)original;target.options.Add(new AppearanceOption{id=original.id,displayName=original.displayName,sprite=data.sprite,controller=data.controller,animationProfile=data.animationProfile,idleClip=data.idleClip,moveClip=data.moveClip,jumpClip=data.jumpClip,attackClip=data.attackClip,scale=data.scale,offset=data.offset,flipX=data.flipX,preserveAspectWithoutPrefab=data.preserveAspect});
            }
            if(kind==ItemKind.Background&&target.options.Count==0){var sprite=EnsureBackgroundSprite();target.options.Add(new AppearanceOption{id="web-cielo-azul",displayName="Cielo azul",sprite=sprite,scale=Vector2.one,preserveAspectWithoutPrefab=true});target.defaultAppearanceId="web-cielo-azul";}
            target.EnsureIds();EditorUtility.SetDirty(target);return target;
        }
        static GameItemDefinition PrepareBackgroundDefinition(Sprite sprite)
        {
            var prefabPath=ContentDirectory+"/Prefabs/fondo.prefab";var go=new GameObject("Fondo",typeof(GameItem),typeof(RuntimeCanvasBackground));
            try
            {
                var item=go.GetComponent<GameItem>();item.visualScale=1;
                PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
            var definition=LoadOrCreate<GameItemDefinition>(ContentDirectory+"/fondo.asset");definition.id="fondo";definition.displayName="Fondo";definition.category="Escenario";definition.description="La imagen que aparece detrás del nivel.";definition.learningHint="Sólo puede haber un fondo. Puedes elegir una imagen preparada o cargar la tuya.";definition.icon=sprite;definition.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);definition.kind=ItemKind.Background;definition.order=90;definition.runtimeOnly=true;definition.availableInWorkshop=true;definition.extraInWorkshop=false;definition.allowMultiple=false;definition.properties=Array.Empty<EducationalProperty>();EditorUtility.SetDirty(definition);return definition;
        }
        static Sprite EnsureBackgroundSprite()
        {
            const string path=ContentDirectory+"/DefaultBackground.png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++){float t=y/63f;var color=Color.Lerp(new Color(.09f,.16f,.3f),new Color(.2f,.52f,.78f),t);for(int x=0;x<64;x++)texture.SetPixel(x,y,color);}texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        [MenuItem("CreaJuego/Web/Generar WebGL Regression Fix")]
        public static void BuildWebGLRegressionFix()
        {
            Prepare();PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/WebGLRegressionFix",target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL Regression Fix falló: "+report.summary.result);Debug.Log("CREAJUEGO_WEB_REGRESSION_BUILD_READY "+report.summary.totalSize);
        }
        public static void BuildWebGL()=>BuildWebGLParity1();
    }
}