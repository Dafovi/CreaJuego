using System;
using System.Collections.Generic;
using System.Linq;
using CreaJuego.Web;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CreaJuego.Web.Editor
{
    public static class ShowcaseBuilder
    {
        public const string ScenePath="Assets/CreaJuegoShowcase/Scenes/CreaJuegoShowcase.unity";
        const string Root="Assets/CreaJuegoShowcase";
        const string Content=Root+"/Content";
        const string Kit=Root+"/ThirdParty/2DGameKit";
        const string Plains="Assets/2D Pixel Art Platformer Biome - Plains/Sprites.png";
        const string Tiles="Assets/PlatformerTileset";

        [MenuItem("CreaJuego/Contenido/Actualizar opciones recomendadas")]
        public static void UpdateRecommendedOptions()
        {
            EnsureFolders();
            var categories=AssetDatabase.FindAssets("t:AppearanceCategory",new[]{"Assets/CreaJuegoPacks/MundoMisterioso/Categorías"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<AppearanceCategory>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c!=null).ToArray();
            Add(categories,ItemKind.Background,Option("platformer-sky-day","Pradera luminosa",Sprite(Tiles+"/Background/Background1.png"),Vector2.one,true));
            Add(categories,ItemKind.Background,Option("platformer-sky-evening","Pradera al atardecer",Sprite(Tiles+"/Background/Background2.png"),Vector2.one,true));
            Add(categories,ItemKind.MovingPlatform,Option("plains-moving-ground","Plataforma de césped",Sprite(Plains,"TileGround1"),Vector2.one,true));
            Add(categories,ItemKind.Hazard,Option("plains-spikes","Pinchos de pradera",Sprite(Plains,"TileSpikes"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("platformer-tree","Árbol frondoso",Sprite(Tiles+"/Objects/ObjTree.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("platformer-house","Casa del bosque",Sprite(Tiles+"/Objects/ObjHouse.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("platformer-chest","Cofre",Sprite(Tiles+"/Objects/Obj_chest.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("plains-plant","Planta silvestre",Sprite(Plains,"TilePlant1"),Vector2.one,true));
            Add(categories,ItemKind.Prize,Option("platformer-treasure","Cofre del tesoro",Sprite(Tiles+"/Objects/Obj_chest.png"),new Vector2(.65f,.65f),true));
            var goals=categories.FirstOrDefault(c=>c.kind==ItemKind.Goal);if(goals!=null){goals.options.RemoveAll(o=>o!=null&&(o.id=="platformer-finish-chest"||o.id=="web-meta-cofre"));EditorUtility.SetDirty(goals);}
            AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_RECOMMENDED_OPTIONS_READY");
        }

        [MenuItem("CreaJuego/Showcase/Preparar escena completa")]
        public static void Prepare()
        {
            UpdateRecommendedOptions();
            var basePack=WebSpikeBuilder.EnsureRuntimePack();
            var pack=EnsureShowcasePack(basePack);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("CreaJuego Showcase",typeof(RuntimeAuthoringController),typeof(RuntimeAuthoringInput),typeof(RuntimeAuthoringUI));
            var controller=root.GetComponent<RuntimeAuthoringController>();
            controller.ui=root.GetComponent<RuntimeAuthoringUI>();controller.contentPack=pack;controller.definitions=pack.definitions;controller.sceneServicesPrefab=pack.sceneServices;
            controller.buildRoot=new GameObject("Nivel editable del showcase").transform;
            controller.buildCamera=MakeCamera("Cámara de construcción",new Vector3(0,1,-10),new Color(.04f,.06f,.1f),8.5f);controller.buildCamera.rect=new Rect(260f/1280f,82f/720f,740f/1280f,564f/720f);controller.buildCamera.gameObject.AddComponent<AudioListener>();
            controller.gameCamera=MakeCamera("Cámara de juego",new Vector3(0,0,-10),new Color(.16f,.24f,.36f),5);controller.gameCamera.enabled=false;controller.gameCamera.gameObject.AddComponent<AudioListener>().enabled=false;
            controller.ui.PrepareEditableLayout();controller.PrepareEditableScene();
            Populate(controller);
            CreateAtmosphere();
            controller.ui.Refresh();EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.ui);EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolders();EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_SHOWCASE_READY (fuera de Build Settings)");
        }

        static void Populate(RuntimeAuthoringController c)
        {
            c.Project.objects.Clear();c.Project.projectName="El santuario perdido";c.Project.teamName="Demo CreaJuego";c.Project.levelSize=RuntimeLevelSize.Large;c.Project.bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Large);c.Project.alignAutomatically=true;c.Rebuild();
            AddItem(c,"fondo",Vector3.zero,"platformer-sky-evening");
            AddItem(c,"plataforma",new Vector3(-10,-3),"platformer-stone-tile",9);
            AddItem(c,"plataforma",new Vector3(0,-3),"showcase-2dkit-bridge",8);
            AddItem(c,"plataforma",new Vector3(10,-3),"platformer-stone-tile",9);
            AddItem(c,"plataforma",new Vector3(-6,0),"platformer-stone-tile",4);
            AddItem(c,"plataforma",new Vector3(1,2),"showcase-2dkit-bridge",4);
            AddItem(c,"plataforma",new Vector3(8,.5f),"platformer-stone-tile",4);
            AddItem(c,"muro",new Vector3(-14,0),"creajuego-wall-light",6,rotation:90);
            AddItem(c,"rampa",new Vector3(-3,-1.5f),"creajuego-ramp",4,rotation:18);
            AddItem(c,"movil",new Vector3(-1,.1f),"showcase-2dkit-moving-platform",speed:1.4f,distance:4);
            AddItem(c,"movil",new Vector3(11,3),"plains-moving-ground",speed:1,distance:3);
            var player=AddItem(c,"jugador",new Vector3(-11,-2.1f),"platformer-kit-gino");
            if(player!=null){player.health=5;player.jump=13;player.speed=3.2f;player.visualScale=.85f;}
            foreach(var p in new[]{new Vector3(-7,-1.7f),new Vector3(-5,.8f),new Vector3(0,3),new Vector3(7,1.5f),new Vector3(11,-1.7f)})AddItem(c,"premio",p,"platformer-treasure",visualScale:.65f);
            AddItem(c,"peligro",new Vector3(-2.2f,-2.2f),"showcase-2dkit-spikes",visualScale:.7f);
            AddItem(c,"peligro",new Vector3(3,-2.2f),"plains-spikes",visualScale:.8f);
            AddItem(c,"peligro",new Vector3(8.5f,-2.2f),"showcase-2dkit-spikes",visualScale:.7f);
            AddItem(c,"enemigo",new Vector3(-5,-2.05f),"platformer-kit-scarecrow",speed:1.1f,distance:2.5f);
            AddItem(c,"enemigo",new Vector3(3,-2.05f),"platformer-kit-scarecrow",speed:1.5f,distance:3);
            AddItem(c,"enemigo",new Vector3(9,-2.05f),"platformer-kit-scarecrow",speed:.9f,distance:2);
            AddItem(c,"meta",new Vector3(13,-1.7f),"showcase-2dkit-door",visualScale:.8f,message:"¡Encontraste el santuario perdido!");
            AddItem(c,"decoracion",new Vector3(-12,-.8f),"platformer-tree",visualScale:1.4f);
            AddItem(c,"decoracion",new Vector3(12,-1.1f),"platformer-house",visualScale:1.1f);
            AddItem(c,"decoracion",new Vector3(-8,-2.05f),"showcase-2dkit-box",visualScale:.65f);
            AddItem(c,"decoracion",new Vector3(5,-2.05f),"showcase-2dkit-info",visualScale:.7f);
            AddItem(c,"decoracion",new Vector3(7,-2.15f),"showcase-2dkit-pressure",visualScale:.7f);
            AddItem(c,"decoracion",new Vector3(-4,-2.1f),"plains-plant",visualScale:1.2f);
            c.Rebuild();c.CaptureEditableScene(true);if(player!=null)c.Selection.Select(player.instanceId);
        }

        static RuntimeItemData AddItem(RuntimeAuthoringController c,string id,Vector3 position,string appearance=null,float width=0,float speed=-1,float distance=-1,float visualScale=-1,float rotation=0,string message=null)
        {
            if(c.Create(id,position)==null)return null;var data=c.SelectedData();if(data==null)return null;
            if(!string.IsNullOrEmpty(appearance)){data.appearanceId=appearance;data.appearanceChosen=true;}
            if(width>0)data.platformWidth=width;if(speed>=0)data.speed=speed;if(distance>=0)data.distance=distance;if(visualScale>0)data.visualScale=visualScale;if(Mathf.Abs(rotation)>.01f)data.rotationZ=rotation;if(message!=null)data.message=message;return data;
        }

        static RuntimeContentPack EnsureShowcasePack(RuntimeContentPack source)
        {
            EnsureFolders();var appearanceFolder=Content+"/Appearances";var categories=source.preparedAppearances.categories.Select(CloneCategory).ToArray();
            Add(categories,ItemKind.MovingPlatform,Option("showcase-2dkit-moving-platform","Plataforma tecnológica",Sprite(Kit+"/Art/Sprites/Interactables/MovingPlatform.png"),Vector2.one,true));
            Add(categories,ItemKind.Platform,Option("showcase-2dkit-bridge","Puente antiguo",Sprite(Kit+"/Art/Sprites/Interactables/Bridge.png"),Vector2.one,true,new[]{"plataforma"}));
            Add(categories,ItemKind.Hazard,Option("showcase-2dkit-spikes","Pinchos mecánicos",Sprite(Kit+"/Art/Sprites/Interactables/SpikesNew.png"),Vector2.one,true));
            Add(categories,ItemKind.Goal,Option("showcase-2dkit-door","Puerta del santuario",Sprite(Kit+"/Art/Sprites/Interactables/Door.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("showcase-2dkit-box","Caja empujable",Sprite(Kit+"/Art/Sprites/Interactables/PushableBox.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("showcase-2dkit-info","Señal misteriosa",Sprite(Kit+"/Art/Sprites/Interactables/InfoSign.png"),Vector2.one,true));
            Add(categories,ItemKind.Decoration,Option("showcase-2dkit-pressure","Placa de presión",Sprite(Kit+"/Art/Sprites/Interactables/PressurePad.png"),Vector2.one,true));
            var appearances=LoadOrCreate<ContentPackDefinition>(Content+"/ShowcaseAppearances.asset");appearances.id="creajuego-showcase";appearances.categories=categories;appearances.appearances=Array.Empty<AppearanceDefinition>();appearances.sceneServices=source.sceneServices;EditorUtility.SetDirty(appearances);
            var runtime=LoadOrCreate<RuntimeContentPack>(Content+"/ShowcaseRuntimePack.asset");runtime.id="creajuego-showcase";runtime.definitions=source.definitions;runtime.preparedAppearances=appearances;runtime.defaults=source.defaults.Select(d=>new RuntimeAppearanceDefault{kind=d.kind,appearanceId=d.appearanceId}).ToArray();runtime.sceneServices=source.sceneServices;runtime.productName="CreaJuego Showcase";runtime.tagline="El santuario perdido";runtime.brandIcon=source.brandIcon;EditorUtility.SetDirty(runtime);return runtime;

            AppearanceCategory CloneCategory(AppearanceCategory original)
            {
                var target=LoadOrCreate<AppearanceCategory>(appearanceFolder+"/"+original.kind+".asset");target.kind=original.kind;target.defaultAppearanceId=original.defaultAppearanceId;target.options=original.options.Where(o=>o!=null&&o.Preview!=null).Select(Clone).ToList();target.EnsureIds();EditorUtility.SetDirty(target);return target;
            }
        }

        static AppearanceOption Clone(AppearanceOption value)
        {
            var data=(IAppearanceData)value;return new AppearanceOption{id=value.id,displayName=value.displayName,sprite=data.sprite,controller=data.controller,animationProfile=data.animationProfile,idleClip=data.idleClip,moveClip=data.moveClip,jumpClip=data.jumpClip,attackClip=data.attackClip,scale=data.scale,offset=data.offset,flipX=data.flipX,preserveAspectWithoutPrefab=data.preserveAspect,definitionIds=value.definitionIds};
        }

        static void CreateAtmosphere()
        {
            var root=new GameObject("Ambientación del showcase");
            Ambient(root.transform,"Neblina izquierda",Sprite(Kit+"/Art/Sprites/VFX/Environment/Mist.png"),new Vector3(-7,-1,1),new Vector3(3,1.3f,1),new Color(.75f,.9f,1,.22f),-20);
            Ambient(root.transform,"Neblina derecha",Sprite(Kit+"/Art/Sprites/VFX/Environment/Mist.png"),new Vector3(7,1,1),new Vector3(2.5f,1.2f,1),new Color(.75f,.9f,1,.18f),-20);
            Ambient(root.transform,"Haz de luz",Sprite(Kit+"/Art/Sprites/VFX/Environment/LightShaft.png"),new Vector3(2,3,1),new Vector3(2,2,1),new Color(1,.95f,.7f,.22f),-15);
            var audio=root.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Kit+"/Audio/Environment/EnvironmentalAmbiance.ogg");audio.loop=true;audio.playOnAwake=true;audio.volume=.14f;audio.spatialBlend=0;
        }

        static void Ambient(Transform parent,string name,Sprite sprite,Vector3 position,Vector3 scale,Color color,int order)
        {
            if(sprite==null)return;var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=scale;var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
        }

        static void Add(IEnumerable<AppearanceCategory> categories,ItemKind kind,AppearanceOption option)
        {
            if(option?.Preview==null)return;var category=categories.FirstOrDefault(c=>c!=null&&c.kind==kind);if(category==null)return;category.options.RemoveAll(o=>o!=null&&o.id==option.id);category.options.Add(option);category.EnsureIds();EditorUtility.SetDirty(category);
        }

        static AppearanceOption Option(string id,string name,Sprite sprite,Vector2 scale,bool preserveAspect,string[] definitionIds=null)
            =>new AppearanceOption{id=id,displayName=name,sprite=sprite,scale=scale,preserveAspectWithoutPrefab=preserveAspect,definitionIds=definitionIds??Array.Empty<string>()};

        static Sprite Sprite(string path,string preferredName=null)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if(!string.IsNullOrEmpty(preferredName)){var exact=sprites.FirstOrDefault(s=>s.name.Equals(preferredName,StringComparison.OrdinalIgnoreCase));if(exact!=null)return exact;var contains=sprites.FirstOrDefault(s=>s.name.IndexOf(preferredName,StringComparison.OrdinalIgnoreCase)>=0);if(contains!=null)return contains;}
            return sprites.FirstOrDefault()??AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Camera MakeCamera(string name,Vector3 position,Color color,float size)
        {
            var camera=new GameObject(name).AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size;camera.transform.position=position;camera.backgroundColor=color;camera.clearFlags=CameraClearFlags.SolidColor;return camera;
        }

        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }

        static void EnsureFolders()
        {
            System.IO.Directory.CreateDirectory(Root+"/Scenes");System.IO.Directory.CreateDirectory(Content+"/Appearances");AssetDatabase.Refresh();
        }
    }
}