using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CreaJuego.Web
{
    public enum AuthoringMode{Build,Play}
    [RequireComponent(typeof(RuntimeGameplayCamera))]
    public sealed class RuntimeAuthoringController:MonoBehaviour
    {
        public RuntimeContentPack contentPack;
        public GameItemDefinition[] definitions=Array.Empty<GameItemDefinition>(); // v1 scene compatibility
        public GameObject sceneServicesPrefab;
        public Camera buildCamera,gameCamera;
        public Transform buildRoot;
        public RuntimeAuthoringUI ui;
        [SerializeField] CreaJuegoProjectData editableSceneProject;
        public CreaJuegoProjectData Project{get;private set;}=new CreaJuegoProjectData();
        public AuthoringMode Mode{get;private set;}=AuthoringMode.Build;
        public RuntimeSelectionService Selection{get;}=new RuntimeSelectionService();
        public RuntimeHistory History{get;}=new RuntimeHistory();
        public RuntimeGameplayCamera GameplayCamera=>GetComponent<RuntimeGameplayCamera>();
        public Transform PlayRoot=>playRoot;
        public GameItem PlayPlayer=>playPlayer;
        public event Action ProjectChanged;
        readonly List<UnityEngine.Object> imageAssets=new List<UnityEngine.Object>();
        Transform playRoot;GameObject services;GameItem playPlayer;IProjectStorage storage;bool dirty;float saveAt;

        void Awake()
        {
            storage=new FileProjectStorage();if(buildRoot==null)buildRoot=new GameObject("Nivel en construcción").transform;if(ui==null)ui=GetComponent<RuntimeAuthoringUI>();Selection.Bind(ResolveAuthoredItem);
            if(editableSceneProject!=null){Project=CloneProject(editableSceneProject);EnsureDefaultAppearances(Project);History.Reset(Project);}else NewProject(false);
        }
        void Start(){if(Application.absoluteURL.Contains("stress=100"))CreateLargeStressProject();else if(Application.absoluteURL.Contains("stress=1"))CreateStressProject();Debug.Log($"CREAJUEGO_WEB_READY mode={Mode} objects={Project.objects.Count}");}
        void Update(){if(dirty&&Time.unscaledTime>=saveAt)SaveNow();}
        void OnDestroy(){ReleaseImages();}
        public GameItemDefinition Find(string id)=>contentPack!=null?contentPack.Find(id):definitions.FirstOrDefault(d=>d!=null&&d.id==id);
        public bool HasEditableScene=>editableSceneProject!=null&&buildRoot!=null&&buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(true).Length>0;
        public void PrepareEditableScene()
        {
            if(buildRoot==null)buildRoot=new GameObject("Runtime Authoring Root").transform;if(ui==null)ui=GetComponent<RuntimeAuthoringUI>();Selection.Bind(ResolveAuthoredItem);
            NewProject(false);editableSceneProject=ReadEditableSceneProject()??CloneProject(Project);
        }
        public CreaJuegoProjectData ReadEditableSceneProject()
        {
            if(editableSceneProject==null||buildRoot==null)return null;var markers=buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(true);if(markers.Length==0)return null;
            var data=CloneProject(editableSceneProject);data.objects.Clear();
            foreach(var marker in markers)
            {
                var item=marker.GetComponent<GameItem>();if(item==null||item.definition==null)continue;var authored=RuntimeItemData.From(item,item.definition.id);authored.instanceId=marker.instanceId;
                if(item.definition.kind==ItemKind.Platform){var box=item.GetComponent<BoxCollider2D>();if(box!=null)authored.platformWidth=box.size.x;}
                data.objects.Add(authored);
            }
            return data.objects.Count==0?null:data;
        }
        public bool CaptureEditableScene(bool allowRemoval=false){var data=ReadEditableSceneProject();if(data==null)return false;if(!allowRemoval&&editableSceneProject!=null&&data.objects.Count<editableSceneProject.objects.Count)return false;editableSceneProject=data;return true;}
        static CreaJuegoProjectData CloneProject(CreaJuegoProjectData value)=>ProjectSerializer.FromJson(ProjectSerializer.ToJson(value));

        public void NewProject(bool notify=true)
        {
            ExitPlay(false);Project=new CreaJuegoProjectData();Project.bounds=RuntimeLevelBounds.For(Project.levelSize);
            Seed("plataforma",new Vector3(-5,-2),6);Seed("plataforma",new Vector3(2,-2),6);Seed("jugador",new Vector3(-5,-1.25f));
            Seed("premio",new Vector3(-2,-1.15f));Seed("peligro",new Vector3(1,-1.4f));Seed("enemigo",new Vector3(4,-1.15f));Seed("meta",new Vector3(7,-1.15f));Seed("decoracion",new Vector3(-8,1.5f));
            History.Reset(Project);Rebuild();if(notify)Changed(false);
        }
        public void CreateStressProject()=>CreateStress(false);
        public void CreateLargeStressProject()=>CreateStress(true);
        void CreateStress(bool large)
        {
            ExitPlay(false);Project=new CreaJuegoProjectData{projectName=large?"Prueba 100 objetos":"Prueba 43 objetos",teamName="CreaJuego",levelSize=RuntimeLevelSize.Large,bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Large)};
            if(large)
            {
                for(int i=0;i<50;i++)Seed("plataforma",new Vector3(-18+i%10*4,-3+i/10*4),3.5f);
                Seed("jugador",new Vector3(-18,-2.25f));for(int i=0;i<28;i++)Seed("decoracion",new Vector3(-20+i%14*3,2+i/14*5));
                for(int i=0;i<10;i++)Seed("premio",new Vector3(-16+i*3.5f,-1));for(int i=0;i<5;i++)Seed("peligro",new Vector3(-10+i*5,-2.4f));for(int i=0;i<5;i++)Seed("enemigo",new Vector3(-14+i*7,-2.1f));Seed("meta",new Vector3(18,-2.1f));
            }
            else
            {
                for(int i=0;i<20;i++)Seed("plataforma",new Vector3(-9+i%10*2,-2+i/10*3),1.75f);
                Seed("jugador",new Vector3(-9,-1.25f));for(int i=0;i<10;i++)Seed("decoracion",new Vector3(-8+i*1.8f,2.5f));
                for(int i=0;i<5;i++)Seed("premio",new Vector3(-7+i*3,.2f));for(int i=0;i<3;i++)Seed("peligro",new Vector3(-3+i*3,-1.4f));for(int i=0;i<3;i++)Seed("enemigo",new Vector3(-5+i*5,-1.1f));Seed("meta",new Vector3(9,-1.1f));
            }
            History.Reset(Project);Rebuild();Changed(false);Debug.Log($"CREAJUEGO_WEB_STRESS_READY objects={Project.objects.Count}");
        }
        void Seed(string id,Vector3 position,float width=0)
        {
            var definition=Find(id);if(definition==null||definition.prefab==null)return;var item=definition.prefab.GetComponent<GameItem>();if(item==null)return;
            var data=RuntimeItemData.From(item,id);EnsureDefaultAppearance(data,definition);data.position=RuntimeSnap.Position(position,Project.alignAutomatically);if(width>0)data.platformWidth=RuntimeSnap.Width(width,Project.alignAutomatically);Project.objects.Add(data);
        }
        public GameItem Create(string id,Vector3 position)
        {
            if(Mode!=AuthoringMode.Build)return null;var definition=Find(id);if(definition==null||definition.prefab==null)return null;
            if(!definition.allowMultiple&&Project.objects.Any(o=>o.definitionId==id)){ui?.SetStatus("Este juego usa un solo "+definition.displayName+".");return null;}
            var data=RuntimeItemData.From(definition.prefab.GetComponent<GameItem>(),id);EnsureDefaultAppearance(data,definition);data.position=RuntimeSnap.Position(position,Project.alignAutomatically);Project.objects.Add(data);History.Record(Project);Rebuild(data.instanceId);Changed();return Selection.SelectedItem;
        }
        public void DeleteSelected(){var id=SelectedId();if(id==null)return;Project.objects.RemoveAll(o=>o.instanceId==id);History.Record(Project);Rebuild();Changed();}
        public void DuplicateSelected(){var source=SelectedData();if(source==null)return;var definition=Find(source.definitionId);if(definition==null||!definition.allowMultiple)return;var copy=source.Clone();copy.instanceId=Guid.NewGuid().ToString("N");copy.position=RuntimeSnap.Position(copy.position+new Vector3(1,.5f),Project.alignAutomatically);Project.objects.Add(copy);History.Record(Project);Rebuild(copy.instanceId);Changed();}
        public void MoveSelected(Vector3 position,bool record){var data=SelectedData();if(data==null)return;position.z=data.position.z;position=RuntimeSnap.Position(position,Project.alignAutomatically);data.position=position;if(Selection.SelectedItem!=null)Selection.SelectedItem.transform.position=position;if(record)CommitEdit();}
        public void ResizeSelected(float width,float centerX,bool record)
        {
            var data=SelectedData();var item=Selection.SelectedItem;if(data==null||item==null||item.definition.kind!=ItemKind.Platform)return;
            data.platformWidth=RuntimeSnap.Width(width,Project.alignAutomatically);data.position.x=Project.alignAutomatically?Mathf.Round(centerX/RuntimeSnap.Step)*RuntimeSnap.Step:centerX;item.transform.position=data.position;RuntimePlatformGeometry.Apply(item,data.platformWidth);if(record)CommitEdit();
        }
        public void SetFloat(string path,float value,bool record=true)
        {
            var data=SelectedData();if(data==null)return;switch(path){case "speed":data.speed=value;break;case "jump":data.jump=value;break;case "health":data.health=Mathf.RoundToInt(value);break;case "points":data.points=Mathf.RoundToInt(value);break;case "damage":data.damage=Mathf.RoundToInt(value);break;case "width":ResizeSelected(value,data.position.x,record);return;}
            ApplySelected(data);if(record)CommitEdit();
        }
        public void SetMessage(string value){var data=SelectedData();if(data==null)return;data.message=value;ApplySelected(data);CommitEdit();}
        public void SetAppearance(string id)
        {
            var data=SelectedData();if(data==null)return;data.appearanceId=id??"";data.customImageBase64=null;data.appearanceChosen=true;ApplySelected(data);CommitEdit();
        }
        public void SetSnap(bool value){if(Project.alignAutomatically==value)return;Project.alignAutomatically=value;CommitEdit();}
        public void SetLevelSize(RuntimeLevelSize value){if(Project.levelSize==value)return;Project.levelSize=value;Project.bounds=RuntimeLevelBounds.For(value);CommitEdit();}
        public void CommitEdit(){History.Record(Project);Changed();}
        void ApplySelected(RuntimeItemData data){var item=Selection.SelectedItem;if(item!=null&&SelectedId()==data.instanceId)ApplyData(item,data);}
        public void Undo(){var value=History.Undo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void Redo(){var value=History.Redo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void SaveNow(){storage.Save(ProjectSerializer.ToJson(Project));dirty=false;ui?.SetSaveState("Guardado ✓");}
        public void LoadLast(){if(!storage.Exists){ui?.SetStatus("Todavía no hay un proyecto guardado.");return;}ExitPlay(false);Project=ProjectSerializer.FromJson(storage.Load());EnsureDefaultAppearances(Project);History.Reset(Project);Rebuild();Changed(false);ui?.SetStatus("Proyecto abierto.");}

        public string EnterPlay()
        {
            if(Mode==AuthoringMode.Play)return null;var error=RuntimePreflight.Validate(Project,Find,new RuntimePreflightContext{cameraAvailable=gameCamera!=null});if(error!=null){ui?.SetStatus(error);return error;}
            Selection.Clear();buildRoot.gameObject.SetActive(false);services=sceneServicesPrefab!=null?Instantiate(sceneServicesPrefab):null;
            if(services!=null){foreach(var camera in services.GetComponentsInChildren<Camera>(true))camera.enabled=false;foreach(var listener in services.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;}
            playRoot=new GameObject("Partida temporal").transform;playRoot.gameObject.SetActive(false);foreach(var data in Project.objects)InstantiateItem(data,playRoot,false);
            CreatePlayBoundaries(playRoot,Project.bounds);playRoot.gameObject.SetActive(true);playPlayer=playRoot.GetComponentsInChildren<GameItem>().First(i=>i.definition.kind==ItemKind.Player);
            var authored=Project.objects.First(o=>o.definitionId==playPlayer.definition.id);var recovery=playPlayer.GetComponent<PlayerFallRecovery>();recovery.ConfigureWorldLimit(authored.position,Project.bounds.bottom);
            Mode=AuthoringMode.Play;if(buildCamera!=null){buildCamera.enabled=false;var buildListener=buildCamera.GetComponent<AudioListener>();if(buildListener!=null)buildListener.enabled=false;}var gameListener=gameCamera.GetComponent<AudioListener>();if(gameListener!=null)gameListener.enabled=true;GameplayCamera.Activate(gameCamera,playPlayer,playRoot);ui?.Refresh();Debug.Log($"CREAJUEGO_WEB_MODE PLAY objects={Project.objects.Count}");return null;
        }
        public void ExitPlay(bool rebuild=true)
        {
            if(Mode!=AuthoringMode.Play)return;GameplayCamera.Deactivate(gameCamera);if(playRoot!=null){playRoot.gameObject.SetActive(false);DestroySafe(playRoot.gameObject);}if(services!=null){services.SetActive(false);DestroySafe(services);}playRoot=null;playPlayer=null;
            Mode=AuthoringMode.Build;if(gameCamera!=null){var gameListener=gameCamera.GetComponent<AudioListener>();if(gameListener!=null)gameListener.enabled=false;}if(buildCamera!=null){buildCamera.enabled=true;var buildListener=buildCamera.GetComponent<AudioListener>();if(buildListener!=null)buildListener.enabled=true;}if(buildRoot!=null)buildRoot.gameObject.SetActive(true);if(rebuild)Rebuild();ui?.Refresh();Debug.Log($"CREAJUEGO_WEB_MODE BUILD objects={Project.objects.Count}");
        }
        static void CreatePlayBoundaries(Transform parent,RuntimeLevelBounds bounds)
        {
            void Wall(string name,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(BoxCollider2D),typeof(RuntimeLevelBoundary));go.transform.SetParent(parent,false);go.transform.position=position;go.GetComponent<BoxCollider2D>().size=size;}
            var height=bounds.top-bounds.bottom;var width=bounds.right-bounds.left;Wall("Límite izquierdo",new Vector2(bounds.left-.25f,(bounds.top+bounds.bottom)*.5f),new Vector2(.5f,height));Wall("Límite derecho",new Vector2(bounds.right+.25f,(bounds.top+bounds.bottom)*.5f),new Vector2(.5f,height));Wall("Límite superior",new Vector2((bounds.left+bounds.right)*.5f,bounds.top+.25f),new Vector2(width,.5f));
        }
        public void PickImage(){if(SelectedData()==null)return;if(!RuntimeImageImport.Pick(gameObject.name))ui?.SetStatus("La selección de archivos se prueba dentro de la build WebGL.");}
        public void ReceiveImageError(string message)=>ui?.SetStatus(string.IsNullOrWhiteSpace(message)?RuntimeImageImport.TooLargeMessage:message);
        public void ReceiveImageDataUrl(string value)
        {
            var data=SelectedData();if(data==null)return;if(!RuntimeImageImport.ValidateDataUrl(value,out var error)){ui?.SetStatus(error);return;}data.customImageBase64=value;data.appearanceId="";data.appearanceChosen=true;ApplySelected(data);CommitEdit();
        }
        public void ToggleMode(){if(Mode==AuthoringMode.Build)EnterPlay();else ExitPlay();}

        public void Rebuild(string selectId=null)
        {
            if(buildRoot==null)return;Selection.Bind(ResolveAuthoredItem);selectId??=Selection.SelectedInstanceId;buildRoot.gameObject.SetActive(false);ReleaseImages();for(int i=buildRoot.childCount-1;i>=0;i--){buildRoot.GetChild(i).gameObject.SetActive(false);DestroySafe(buildRoot.GetChild(i).gameObject);}
            GameItem selected=null;foreach(var data in Project.objects){var item=InstantiateItem(data,buildRoot,true);var marker=item.gameObject.AddComponent<RuntimeAuthoredItem>();marker.instanceId=data.instanceId;if(data.instanceId==selectId)selected=item;}
            buildRoot.gameObject.SetActive(true);foreach(var behaviour in buildRoot.GetComponentsInChildren<MonoBehaviour>())if(!(behaviour is GameItem)&&!(behaviour is ItemVisual)&&!(behaviour is RuntimeAuthoredItem))behaviour.enabled=false;foreach(var body in buildRoot.GetComponentsInChildren<Rigidbody2D>())body.simulated=false;
            Selection.Select(selected!=null?selectId:null);
        }
        GameItem InstantiateItem(RuntimeItemData data,Transform parent,bool authoring)
        {
            var definition=Find(data.definitionId);var go=Instantiate(definition.prefab,parent);go.name=definition.displayName;go.SetActive(false);var item=go.GetComponent<GameItem>();item.definition=definition;ApplyData(item,data);
            if(authoring){foreach(var behaviour in go.GetComponents<MonoBehaviour>())if(!(behaviour is GameItem)&&!(behaviour is ItemVisual))behaviour.enabled=false;foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;}
            go.SetActive(true);if(authoring)foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;return item;
        }
        void ApplyData(GameItem item,RuntimeItemData data)
        {
            EnsureDefaultAppearance(data,item.definition);data.Apply(item);item.appearanceCategory=contentPack!=null?contentPack.CategoryFor(item.definition.kind):null;
            item.customSprite=null;if(!string.IsNullOrEmpty(data.customImageBase64)&&RuntimeImageImport.TryDecodeDataUrl(data.customImageBase64,out var sprite,out var texture,out _)){imageAssets.Add(sprite);imageAssets.Add(texture);item.customSprite=sprite;}
            var visual=item.GetComponent<ItemVisual>();if(visual!=null)visual.Apply();if(item.definition.kind==ItemKind.Platform)RuntimePlatformGeometry.Apply(item,data.platformWidth);foreach(var backend in item.GetComponents<MonoBehaviour>().OfType<IItemBackend>())backend.ApplyConfiguration();
        }
        void EnsureDefaultAppearances(CreaJuegoProjectData project)
        {
            if(project?.objects==null)return;foreach(var data in project.objects)EnsureDefaultAppearance(data,Find(data.definitionId));
        }
        void EnsureDefaultAppearance(RuntimeItemData data,GameItemDefinition definition)
        {
            if(data==null||definition==null||data.appearanceChosen||!string.IsNullOrEmpty(data.customImageBase64)||contentPack==null)return;
            bool missing=string.IsNullOrEmpty(data.appearanceId);
            bool formerPlayerDefault=definition.kind==ItemKind.Player&&data.appearanceId=="tiny-dungeon-84";
            bool formerEnemyDefault=definition.kind==ItemKind.Enemy&&data.appearanceId=="tiny-dungeon-120";
            if(missing||formerPlayerDefault||formerEnemyDefault)data.appearanceId=contentPack.DefaultFor(definition.kind);
        }
        public void FrameAll()
        {
            var items=buildRoot.GetComponentsInChildren<GameItem>();if(items.Length==0)return;var bounds=ItemBounds(items[0]);for(int i=1;i<items.Length;i++)bounds.Encapsulate(ItemBounds(items[i]));Frame(bounds);
        }
        public void FrameSelected(){if(Selection.SelectedItem!=null)Frame(ItemBounds(Selection.SelectedItem));}
        void Frame(Bounds bounds){if(buildCamera==null)return;var aspect=Mathf.Max(.1f,buildCamera.pixelWidth/(float)Mathf.Max(1,buildCamera.pixelHeight));buildCamera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);buildCamera.orthographicSize=Mathf.Max(2,Mathf.Max(bounds.extents.y,bounds.extents.x/aspect)*1.25f);}
        static Bounds ItemBounds(GameItem item){var collider=item.GetComponent<Collider2D>();if(collider!=null)return collider.bounds;var renderer=ItemVisual.Resolve(item);return renderer!=null?renderer.bounds:new Bounds(item.transform.position,Vector3.one);}
        GameItem ResolveAuthoredItem(string instanceId)=>buildRoot==null?null:buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(false).FirstOrDefault(m=>m.instanceId==instanceId)?.GetComponent<GameItem>();
        public string SelectedId()=>Selection.SelectedInstanceId;
        public RuntimeItemData SelectedData(){var id=SelectedId();return id==null?null:Project.objects.FirstOrDefault(o=>o.instanceId==id);}
        void Changed(bool notify=true){MarkDirty();if(notify)ProjectChanged?.Invoke();ui?.Refresh();}
        void MarkDirty(){dirty=true;saveAt=Time.unscaledTime+1;ui?.SetSaveState("Guardando…");}
        void ReleaseImages(){foreach(var value in imageAssets)if(value!=null)DestroySafe(value);imageAssets.Clear();}
        static void DestroySafe(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnApplicationQuit(){if(dirty)SaveNow();}
    }

    public sealed class RuntimeLevelBoundary:MonoBehaviour{}
}