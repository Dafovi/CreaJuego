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
        const string WorkshopSourceDirectory="Assets/Sprites para prueba";
        const string WorkshopSpriteDirectory=ContentDirectory+"/WorkshopSprites";
        [MenuItem("CreaJuego/Web/Preparar Parity 1")]
        public static void Prepare()
        {
            var pack=EnsureRuntimePack();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("CreaJuego Web",typeof(RuntimeAuthoringController),typeof(RuntimeAuthoringInput),typeof(RuntimeAuthoringUI));
            var controller=root.GetComponent<RuntimeAuthoringController>();controller.ui=root.GetComponent<RuntimeAuthoringUI>();controller.contentPack=pack;controller.definitions=pack.definitions;controller.sceneServicesPrefab=pack.sceneServices;
            controller.buildRoot=new GameObject("Runtime Authoring Root").transform;
            controller.buildCamera=MakeCamera("Cámara de construcción",new Vector3(0,1,-10),new Color(.04f,.06f,.1f));controller.buildCamera.rect=new Rect(260f/1280f,82f/720f,740f/1280f,564f/720f);controller.buildCamera.gameObject.AddComponent<AudioListener>();
            controller.gameCamera=MakeCamera("Cámara de juego",new Vector3(0,0,-10),new Color(.16f,.24f,.36f));controller.gameCamera.enabled=false;var gameListener=controller.gameCamera.gameObject.AddComponent<AudioListener>();gameListener.enabled=false;
            controller.ui.PrepareEditableLayout();controller.PrepareEditableScene();controller.LoadStarterLevel(false);controller.CaptureEditableScene(true);controller.ui.Refresh();EditorUtility.SetDirty(controller);EditorUtility.SetDirty(controller.ui);EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("CREAJUEGO_WEB_PARITY_SCENE_READY");
        }
        [MenuItem("CreaJuego/Web/Restaurar nivel inicial")]
        public static void ApplyStarterLevelToScene()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var controller=UnityEngine.Object.FindAnyObjectByType<RuntimeAuthoringController>();
            if(controller==null)throw new InvalidOperationException("La escena Web no contiene RuntimeAuthoringController.");
            controller.LoadStarterLevel(false);controller.buildCamera.transform.position=new Vector3(0,1.5f,-10);controller.buildCamera.orthographicSize=19;
            if(!controller.CaptureEditableScene(true))throw new InvalidOperationException("No se pudo capturar el nivel inicial.");controller.ui.RefreshEditableLayout();
            EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();Debug.Log("CREAJUEGO_WEB_STARTER_LEVEL_READY "+controller.Project.objects.Count);
        }
        [MenuItem("CreaJuego/Web/Actualizar contenido y nivel inicial")]
        public static void RefreshContentAndStarterLevel()
        {
            EnsureRuntimePack();ApplyStarterLevelToScene();Debug.Log("CREAJUEGO_WEB_CONTENT_AND_STARTER_READY");
        }
        [MenuItem("CreaJuego/Web/Configurar plataformas atravesables")]
        public static void ApplyOneWayPlatformCollision()
        {
            foreach(var id in new[]{"plataforma","movil","rampa"})
            {
                var prefabPath=$"{ContentDirectory}/Prefabs/{id}.prefab";
                var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                try{ConfigureOneWaySurface(contents);PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);}
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CREAJUEGO_WEB_ONE_WAY_PLATFORMS_READY");
        }
        public static RuntimeContentPack EnsureRuntimePack()
        {
            EnsureFolder("Assets/CreaJuegoWeb","Content");EnsureFolder(ContentDirectory,"Prefabs");EnsureFolder(ContentDirectory,"Appearances");EnsureFolder(ContentDirectory,"WorkshopSprites");RemoveLegacyStairs();
            var kinds=new[]{ItemKind.Player,ItemKind.Platform,ItemKind.MovingPlatform,ItemKind.Prize,ItemKind.Hazard,ItemKind.Enemy,ItemKind.Goal,ItemKind.Decoration,ItemKind.Background};
            var sources=AssetDatabase.FindAssets("t:GameItemDefinition",new[]{"Assets/CreaJuegoPacks/Starter/Content"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(d=>d!=null&&kinds.Contains(d.kind)&&d.id!="piso").OrderBy(d=>d.order).ToArray();
            var baseDefinitions=sources.Select(PrepareDefinition).ToArray();var platform=baseDefinitions.First(d=>d.id=="plataforma");var preparedDefinitions=baseDefinitions.Concat(PrepareStructures(platform)).Concat(new[]{PrepareBackgroundDefinition(EnsureBackgroundSprite())}).ToArray();var sourcePack=sources.Select(d=>d.appearancePack).FirstOrDefault(p=>p!=null);PrepareSourceRecommendations(sourcePack);var categories=kinds.Select(kind=>PrepareCategory(sourcePack?.CategoryFor(kind),kind)).Where(c=>c!=null).ToArray();EnsureMinimumOptions(categories);
            var preparedPack=LoadOrCreate<ContentPackDefinition>(ContentDirectory+"/WebAppearances.asset");preparedPack.id="web-parity-1";preparedPack.categories=categories;preparedPack.appearances=Array.Empty<AppearanceDefinition>();preparedPack.sceneServices=null;EditorUtility.SetDirty(preparedPack);
            var defaults=sources.Select(source=>
            {
                var category=Array.Find(categories,c=>c.kind==source.kind);var option=PreferredDefault(category,source);return new RuntimeAppearanceDefault{kind=source.kind,appearanceId=option?.id??""};
            }).Concat(new[]{new RuntimeAppearanceDefault{kind=ItemKind.Background,appearanceId="web-cielo-azul"}}).GroupBy(d=>d.kind).Select(g=>g.First()).ToArray();
            var runtime=LoadOrCreate<RuntimeContentPack>(ContentDirectory+"/WebRuntimePack.asset");runtime.id="web-parity-1";runtime.definitions=preparedDefinitions;runtime.preparedAppearances=preparedPack;runtime.defaults=defaults;runtime.sceneServices=AssetDatabase.LoadAssetAtPath<ContentPackDefinition>("Assets/CreaJuegoPacks/Starter/Content/StarterPack.asset").sceneServices;runtime.productName="Gamer";runtime.tagline="Crea tu propia aventura";runtime.brandIcon=preparedDefinitions.FirstOrDefault(d=>d.kind==ItemKind.Player)?.icon;EditorUtility.SetDirty(runtime);return runtime;
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
                    if(source.kind==ItemKind.Player){if(item.GetComponent<PlayerFallRecovery>()==null)item.gameObject.AddComponent<PlayerFallRecovery>();var bodyCollider=item.GetComponent<BoxCollider2D>();if(bodyCollider!=null)bodyCollider.edgeRadius=Mathf.Min(.12f,Mathf.Min(bodyCollider.size.x,bodyCollider.size.y)*.24f);}
                    if(source.kind==ItemKind.Platform||source.kind==ItemKind.MovingPlatform)ConfigureOneWaySurface(contents);
                }
                PrefabUtility.SaveAsPrefabAsset(contents,safePrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            var target=LoadOrCreate<GameItemDefinition>($"{ContentDirectory}/{source.id}.asset");EditorUtility.CopySerialized(source,target);target.runtimeOnly=true;target.appearancePack=null;target.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(safePrefabPath);
            // Se conserva el backend para abrir niveles antiguos, pero la plataforma móvil
            // queda fuera del catálogo del taller hasta tener una interacción estable.
            if(source.kind==ItemKind.MovingPlatform){target.availableInWorkshop=false;target.extraInWorkshop=false;}
            if(preparedDefault?.Preview!=null)target.icon=preparedDefault.Preview;EditorUtility.SetDirty(target);return target;
        }
        static void PrepareSourceRecommendations(ContentPackDefinition sourcePack)
        {
            if(sourcePack==null)return;
            var platform=sourcePack.CategoryFor(ItemKind.Platform);if(platform!=null)
            {
                platform.options.RemoveAll(o=>o!=null&&(o.id=="plains-ground"||o.id=="web-escalera"||o.id=="platformer-stone-tile"));
                AddPrepared(platform,new AppearanceOption{id="platformer-stone-tile",displayName="Bloques de piedra",sprite=LoadTiledSprite("Assets/PlatformerTileset/TileSet/SprTiles.png","Sprites 1_0"),scale=Vector2.one,definitionIds=new[]{"plataforma"}});
                platform.EnsureIds();EditorUtility.SetDirty(platform);
            }
            var player=sourcePack.CategoryFor(ItemKind.Player);var witch=player?.Find("tiny-dungeon-84");if(witch!=null){witch.controller=null;witch.animationProfile=null;witch.idleClip=witch.moveClip=witch.jumpClip=witch.attackClip=null;EditorUtility.SetDirty(player);}
            var goal=sourcePack.CategoryFor(ItemKind.Goal);if(goal!=null)
            {
                goal.options.RemoveAll(o=>o!=null&&(o.id=="platformer-finish-chest"||o.id=="web-meta-cofre"));
                goal.EnsureIds();EditorUtility.SetDirty(goal);
            }
        }
        static AppearanceCategory PrepareCategory(AppearanceCategory source,ItemKind kind)
        {
            if(source==null&&kind!=ItemKind.Background)return null;var target=LoadOrCreate<AppearanceCategory>($"{ContentDirectory}/Appearances/{kind}.asset");target.kind=kind;target.defaultAppearanceId=source!=null?source.defaultAppearanceId:"";target.options.Clear();
            foreach(var original in (source!=null?source.options:Enumerable.Empty<AppearanceOption>()).Where(o=>o!=null&&o.Preview!=null))
            {
                var data=(IAppearanceData)original;target.options.Add(new AppearanceOption{id=original.id,displayName=original.displayName,sprite=data.sprite,controller=data.controller,animationProfile=data.animationProfile,idleClip=data.idleClip,moveClip=data.moveClip,jumpClip=data.jumpClip,attackClip=data.attackClip,scale=data.scale,offset=data.offset,flipX=data.flipX,preserveAspectWithoutPrefab=data.preserveAspect,definitionIds=kind==ItemKind.Platform?new[]{"plataforma"}:original.definitionIds});
            }
            if(kind==ItemKind.Background){if(!target.options.Any(o=>o.id=="web-cielo-azul"))target.options.Add(new AppearanceOption{id="web-cielo-azul",displayName="Cielo azul",sprite=EnsureBackgroundSprite(),scale=Vector2.one,preserveAspectWithoutPrefab=true});if(!target.options.Any(o=>o.id=="web-atardecer"))target.options.Add(new AppearanceOption{id="web-atardecer",displayName="Atardecer",sprite=EnsureGradientSprite("BackgroundSunset.png",new Color(.15f,.08f,.25f),new Color(1f,.48f,.25f)),scale=Vector2.one,preserveAspectWithoutPrefab=true});if(!target.options.Any(o=>o.id=="web-noche"))target.options.Add(new AppearanceOption{id="web-noche",displayName="Noche",sprite=EnsureGradientSprite("BackgroundNight.png",new Color(.015f,.025f,.09f),new Color(.08f,.18f,.38f)),scale=Vector2.one,preserveAspectWithoutPrefab=true});if(target.Find(target.defaultAppearanceId)==null)target.defaultAppearanceId="web-cielo-azul";}            if(kind==ItemKind.Platform)
            {
                target.options.RemoveAll(o=>o!=null&&(o.id=="web-escalera"||o.id=="plains-ground"||o.id=="skull-side-wall"||o.id=="skull-side-wall-wide"));
                AddPrepared(target,new AppearanceOption{id="platformer-stone-tile",displayName="Bloques de piedra",sprite=LoadTiledSprite("Assets/PlatformerTileset/TileSet/SprTiles.png","Sprites 1_0"),scale=Vector2.one,definitionIds=new[]{"plataforma"}});
                AddPrepared(target,new AppearanceOption{id="creajuego-wall-light",displayName="Muro de piedra",sprite=EnsureWallSprite("WallSurface.png",new Color(.25f,.31f,.38f),new Color(.48f,.57f,.64f)),scale=Vector2.one,definitionIds=new[]{"muro"}});
                AddPrepared(target,new AppearanceOption{id="creajuego-wall-dark",displayName="Muro oscuro",sprite=EnsureWallSprite("WallSurfaceDark.png",new Color(.12f,.15f,.2f),new Color(.3f,.38f,.46f)),scale=Vector2.one,definitionIds=new[]{"muro"}});
                AddPrepared(target,new AppearanceOption{id="creajuego-soil-light",displayName="Terreno de piedra",sprite=EnsureWallSprite("WallSurface.png",new Color(.25f,.31f,.38f),new Color(.48f,.57f,.64f)),scale=Vector2.one,definitionIds=new[]{"suelo"}});
                AddPrepared(target,new AppearanceOption{id="creajuego-soil-dark",displayName="Terreno oscuro",sprite=EnsureWallSprite("WallSurfaceDark.png",new Color(.12f,.15f,.2f),new Color(.3f,.38f,.46f)),scale=Vector2.one,definitionIds=new[]{"suelo"}});
                AddPrepared(target,new AppearanceOption{id="creajuego-ramp",displayName="Rampa de madera",sprite=EnsureRampSprite(),scale=Vector2.one,definitionIds=new[]{"rampa"}});
            }
            if(kind==ItemKind.Goal)
            {
                target.options.RemoveAll(o=>o!=null&&(o.id=="platformer-finish-chest"||o.id=="web-meta-cofre"));
                AddPrepared(target,new AppearanceOption{id="skull-shrine-goal",displayName="Santuario",sprite=LoadSprite("Assets/NovaDevs/2D Platformer - Skull Garden  Tilesets Asset Pack/Architecture/Shrine 1.png","Shrine 1_0"),scale=new Vector2(.18f,.18f),preserveAspectWithoutPrefab=true});
            }
            AddWorkshopAppearances(target,kind);
            target.EnsureIds();EditorUtility.SetDirty(target);return target;
        }
        static void AddWorkshopAppearances(AppearanceCategory target,ItemKind kind)
        {
            AppearanceOption Option(string id,string name,string source,string output,Vector2 scale,bool preserve=true,string[] definitionIds=null,int maxSize=512,bool crop=true)
                =>new AppearanceOption{id=id,displayName=name,sprite=PrepareWorkshopSprite(source,output,maxSize,crop),scale=scale,preserveAspectWithoutPrefab=preserve,definitionIds=definitionIds};
            switch(kind)
            {
                case ItemKind.Background:AddPrepared(target,Option("workshop-forest-background","Valle del castillo","Fondo.png","ForestBackground.png",Vector2.one,true,null,1024,false));break;
                case ItemKind.Platform:
                    AddPrepared(target,Option("workshop-grass-platform","Tierra con césped","Plataforma.png","GrassPlatform.png",Vector2.one,false,new[]{"plataforma"}));
                    AddPrepared(target,new AppearanceOption{id="workshop-grass-wall",displayName="Tierra profunda",sprite=PrepareWorkshopSquareTile("Muro.png","GrassWall.png"),scale=Vector2.one,preserveAspectWithoutPrefab=false,definitionIds=new[]{"muro","suelo"}});
                    AddPrepared(target,Option("workshop-grass-ramp","Pendiente con césped","Rampa.png","GrassRamp.png",Vector2.one,false,new[]{"rampa"}));
                    break;
                case ItemKind.MovingPlatform:AddPrepared(target,Option("workshop-grass-moving","Plataforma flotante","Plataforma movil.png","GrassMovingPlatform.png",Vector2.one,false));break;
                case ItemKind.Prize:AddPrepared(target,Option("workshop-gold-coin","Moneda dorada","Moneda.png","GoldCoin.png",new Vector2(.1f,.1f)));break;
                case ItemKind.Hazard:AddPrepared(target,Option("workshop-grass-spikes","Pinchos del bosque","Pincho.png","GrassSpikes.png",new Vector2(.25f,.25f)));break;
                case ItemKind.Goal:AddPrepared(target,Option("workshop-red-flag","Bandera roja","Meta.png","RedFlag.png",new Vector2(.35f,.35f)));break;
                case ItemKind.Decoration:
                    AddPrepared(target,Option("workshop-large-tree","Árbol frondoso","Arbol.png","LargeTree.png",new Vector2(.5f,.5f)));
                    AddPrepared(target,Option("workshop-flower-bush","Arbusto con flores","Arbusto.png","FlowerBush.png",new Vector2(.35f,.35f)));
                    break;
            }
        }
        static GameItemDefinition[] PrepareStructures(GameItemDefinition platform)
        {
            var wall=EnsureWallSprite("WallSurface.png",new Color(.25f,.31f,.38f),new Color(.48f,.57f,.64f));
            var ramp=EnsureRampSprite();
            return new[]{
                PrepareStructure(platform,"suelo","Suelo","Una base sólida para apoyar el recorrido y evitar plataformas flotantes.",0,23,wall!=null?wall:platform.icon),
                PrepareStructure(platform,"muro","Muro","Una pared firme que limita o divide el recorrido.",90,24,wall!=null?wall:platform.icon),
                PrepareStructure(platform,"rampa","Rampa","Una superficie inclinada para subir o bajar.",18,25,ramp!=null?ramp:platform.icon)
            };
        }        static GameItemDefinition PrepareStructure(GameItemDefinition source,string id,string name,string description,float rotation,int order,Sprite icon)
        {
            var prefabPath=$"{ContentDirectory}/Prefabs/{id}.prefab";var contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.prefab));
            try
            {
                contents.name=id;contents.transform.rotation=Quaternion.Euler(0,0,rotation);
                if(id=="rampa")ConfigureOneWaySurface(contents);
                else
                {
                    var oneWay=contents.GetComponent<OneWayPlatformSurface>();if(oneWay!=null)UnityEngine.Object.DestroyImmediate(oneWay);
                    var effector=contents.GetComponent<PlatformEffector2D>();if(effector!=null)UnityEngine.Object.DestroyImmediate(effector);
                    var collider=contents.GetComponent<Collider2D>();if(collider!=null)collider.usedByEffector=false;
                }
                PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            var target=LoadOrCreate<GameItemDefinition>($"{ContentDirectory}/{id}.asset");EditorUtility.CopySerialized(source,target);target.name=id;target.id=id;target.displayName=name;target.description=description;target.learningHint=id=="suelo"?"Hazlo ancho y alto para construir una base sólida bajo tu nivel.":"Puedes cambiar su ancho, alto y dirección desde Propiedades.";target.icon=icon;target.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);target.order=order;target.allowMultiple=true;EditorUtility.SetDirty(target);return target;
        }
        static void ConfigureOneWaySurface(GameObject platform)
        {
            var surface=platform.GetComponent<OneWayPlatformSurface>()??platform.AddComponent<OneWayPlatformSurface>();
            surface.Configure();
            EditorUtility.SetDirty(surface);
            EditorUtility.SetDirty(platform.GetComponent<Collider2D>());
            EditorUtility.SetDirty(platform.GetComponent<PlatformEffector2D>());
        }
        static void EnsureMinimumOptions(AppearanceCategory[] categories)
        {
            var goal=categories.FirstOrDefault(c=>c.kind==ItemKind.Goal);if(goal==null)return;
            goal.options.RemoveAll(o=>o!=null&&(o.id=="platformer-finish-chest"||o.id=="web-meta-cofre"));goal.EnsureIds();EditorUtility.SetDirty(goal);
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
        static Sprite EnsureGradientSprite(string fileName,Color bottom,Color top)
        {
            var path=ContentDirectory+"/"+fileName;var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++){var color=Color.Lerp(bottom,top,y/63f);for(int x=0;x<64;x++)texture.SetPixel(x,y,color);}texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);ImportSprite(path,64);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite EnsureWallSprite(string fileName,Color dark,Color light)
        {
            var path=ContentDirectory+"/"+fileName;var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null){ImportSprite(path,16);return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
            var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);var mortar=new Color(.08f,.1f,.13f);
            for(int y=0;y<16;y++)for(int x=0;x<16;x++){bool horizontal=y==0||y==8;int shifted=x+(y>=8?4:0);bool vertical=shifted%8==0;var color=horizontal||vertical?mortar:Color.Lerp(dark,light,(x+y%8)/23f);texture.SetPixel(x,y,color);}
            texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);ImportSprite(path,16);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }        static Sprite EnsureRampSprite()
        {
            const string path=ContentDirectory+"/RampSurface.png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null){ImportSprite(path,16);return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
            var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);Color dark=new Color(.35f,.18f,.08f),light=new Color(.72f,.42f,.16f),edge=new Color(.92f,.67f,.3f);
            for(int y=0;y<16;y++)for(int x=0;x<16;x++){bool seam=x==0||y==0;texture.SetPixel(x,y,seam?dark:(y>12?edge:light));}
            texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);ImportSprite(path,16);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite LoadTiledSprite(string path,string preferredName)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null){var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);if(settings.spriteMeshType!=SpriteMeshType.FullRect){settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();}}
            return LoadSprite(path,preferredName);
        }        static Sprite LoadSprite(string path,string preferredName)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();return sprites.FirstOrDefault(sprite=>sprite.name.Equals(preferredName,StringComparison.OrdinalIgnoreCase))??sprites.FirstOrDefault();
        }
        static Sprite PrepareWorkshopSprite(string sourceFile,string outputFile,int maxSize,bool crop)
        {
            var sourcePath=WorkshopSourceDirectory+"/"+sourceFile;var outputPath=WorkshopSpriteDirectory+"/"+outputFile;
            if(!System.IO.File.Exists(sourcePath))return null;
            var sourceTime=System.IO.File.GetLastWriteTimeUtc(sourcePath);var outputTime=System.IO.File.Exists(outputPath)?System.IO.File.GetLastWriteTimeUtc(outputPath):DateTime.MinValue;
            if(sourceTime>outputTime)
            {
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(System.IO.File.ReadAllBytes(sourcePath),false);
                var minX=0;var minY=0;var maxX=source.width-1;var maxY=source.height-1;
                if(crop)
                {
                    minX=source.width;minY=source.height;maxX=-1;maxY=-1;var pixels=source.GetPixels32();
                    for(var y=0;y<source.height;y++)for(var x=0;x<source.width;x++)if(pixels[y*source.width+x].a>4){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                    if(maxX<minX){minX=0;minY=0;maxX=source.width-1;maxY=source.height-1;}
                    const int margin=2;minX=Mathf.Max(0,minX-margin);minY=Mathf.Max(0,minY-margin);maxX=Mathf.Min(source.width-1,maxX+margin);maxY=Mathf.Min(source.height-1,maxY+margin);
                }
                var width=maxX-minX+1;var height=maxY-minY+1;var factor=Mathf.Min(1,maxSize/(float)Mathf.Max(width,height));var outputWidth=Mathf.Max(1,Mathf.RoundToInt(width*factor));var outputHeight=Mathf.Max(1,Mathf.RoundToInt(height*factor));
                var result=new Texture2D(outputWidth,outputHeight,TextureFormat.RGBA32,false);
                for(var y=0;y<outputHeight;y++)for(var x=0;x<outputWidth;x++){var sampleX=minX+Mathf.Min(width-1,Mathf.FloorToInt((x+.5f)/outputWidth*width));var sampleY=minY+Mathf.Min(height-1,Mathf.FloorToInt((y+.5f)/outputHeight*height));result.SetPixel(x,y,source.GetPixel(sampleX,sampleY));}
                result.Apply();System.IO.File.WriteAllBytes(outputPath,result.EncodeToPNG());UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(result);
            }
            ImportSprite(outputPath,100);return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }
        static Sprite PrepareWorkshopSquareTile(string sourceFile,string outputFile)
        {
            var sourcePath=WorkshopSourceDirectory+"/"+sourceFile;var outputPath=WorkshopSpriteDirectory+"/"+outputFile;if(!System.IO.File.Exists(sourcePath))return null;
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(System.IO.File.ReadAllBytes(sourcePath),false);var pixels=source.GetPixels32();var minX=source.width;var minY=source.height;var maxX=-1;var maxY=-1;
            for(var y=0;y<source.height;y++)for(var x=0;x<source.width;x++)if(pixels[y*source.width+x].a>4){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            if(maxX<minX){minX=0;minY=0;maxX=source.width-1;maxY=source.height-1;}
            var opaqueWidth=maxX-minX+1;var opaqueHeight=maxY-minY+1;var cropSize=Mathf.Max(16,Mathf.Min(Mathf.RoundToInt(opaqueWidth*.62f),Mathf.RoundToInt(opaqueHeight*.35f)));var startX=Mathf.Clamp(minX+(opaqueWidth-cropSize)/2,minX,maxX-cropSize+1);var startY=Mathf.Clamp(minY+Mathf.RoundToInt(opaqueHeight*.22f),minY,maxY-cropSize+1);const int outputSize=256;
            var result=new Texture2D(outputSize,outputSize,TextureFormat.RGBA32,false);
            for(var y=0;y<outputSize;y++)for(var x=0;x<outputSize;x++){var sampleX=startX+Mathf.Min(cropSize-1,Mathf.FloorToInt((x+.5f)/outputSize*cropSize));var sampleY=startY+Mathf.Min(cropSize-1,Mathf.FloorToInt((y+.5f)/outputSize*cropSize));result.SetPixel(x,y,source.GetPixel(sampleX,sampleY));}
            result.Apply();System.IO.File.WriteAllBytes(outputPath,result.EncodeToPNG());UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(result);ImportSprite(outputPath,100);return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }
        static void AddPrepared(AppearanceCategory category,AppearanceOption option)
        {
            if(category==null||option?.Preview==null)return;category.options.RemoveAll(existing=>existing!=null&&existing.id==option.id);category.options.Add(option);
        }
        static void RemoveLegacyStairs()
        {
            foreach(var path in new[]{ContentDirectory+"/escalera.asset",ContentDirectory+"/Prefabs/escalera.prefab",ContentDirectory+"/Stairs.png"})if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
        }        static void ImportSprite(string path,float pixelsPerUnit){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=pixelsPerUnit;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();}
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
