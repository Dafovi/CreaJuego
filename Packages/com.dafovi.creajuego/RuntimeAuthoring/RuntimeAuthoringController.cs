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
        Transform playRoot;GameObject services;GameItem playPlayer;IProjectStorage storage,recoveryStorage;bool dirty;float saveAt;

        void Awake()
        {
            storage=new FileProjectStorage();recoveryStorage=new FileProjectStorage(System.IO.Path.Combine(Application.persistentDataPath,"recuperacion.creajuego"));if(buildRoot==null)buildRoot=new GameObject("Nivel en construcción").transform;if(ui==null)ui=GetComponent<RuntimeAuthoringUI>();Selection.Bind(ResolveAuthoredItem);
            if(editableSceneProject!=null){Project=CloneProject(editableSceneProject);EnsureDefaultAppearances(Project);History.Reset(Project);}else NewProject(false);
        }
        void Start(){if(Application.absoluteURL.Contains("stress=100"))CreateLargeStressProject();else if(Application.absoluteURL.Contains("stress=1"))CreateStressProject();Debug.Log($"CREAJUEGO_WEB_READY mode={Mode} objects={Project.objects.Count}");}
        void Update(){if(dirty&&Time.unscaledTime>=saveAt)SaveRecovery();}
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
        public bool CaptureEditableScene(bool allowRemoval=false){var previous=editableSceneProject;editableSceneProject=CloneProject(Project);var data=ReadEditableSceneProject();if(data==null){editableSceneProject=previous;return false;}if(!allowRemoval&&previous!=null&&data.objects.Count<previous.objects.Count){editableSceneProject=previous;return false;}editableSceneProject=data;return true;}
        static CreaJuegoProjectData CloneProject(CreaJuegoProjectData value)=>ProjectSerializer.FromJson(ProjectSerializer.ToJson(value));

        public void NewProject(bool notify=true)
        {
            ExitPlay(false);Project=new CreaJuegoProjectData();Project.bounds=RuntimeLevelBounds.For(Project.levelSize);
            History.Reset(Project);Rebuild();if(notify)Changed(false);
        }
        public void LoadStarterLevel(bool notify=true)
        {
            ExitPlay(false);Project=new CreaJuegoProjectData{projectName="La aventura del bosque",teamName="Mi equipo",levelSize=RuntimeLevelSize.Small,bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Small)};
            Starter("fondo",Vector3.zero,0,"platformer-sky-evening");
            Starter("plataforma",new Vector3(-18,-6),8,"platformer-stone-tile");Starter("plataforma",new Vector3(-7,-6),8,"tiny-dungeon-37");Starter("plataforma",new Vector3(5,-6),10,"platformer-stone-tile");Starter("plataforma",new Vector3(17,-6),7,"tiny-dungeon-38");
            Starter("plataforma",new Vector3(-18,-1.5f),6,"tiny-dungeon-37");Starter("plataforma",new Vector3(-9,-1.5f),6,"platformer-stone-tile");Starter("plataforma",new Vector3(5,-1.5f),8,"tiny-dungeon-38");Starter("plataforma",new Vector3(16,-1.5f),7,"platformer-stone-tile");
            Starter("plataforma",new Vector3(-15,3),8,"platformer-stone-tile");Starter("plataforma",new Vector3(-4,3),7,"tiny-dungeon-38");Starter("plataforma",new Vector3(7,3),8,"tiny-dungeon-37");Starter("plataforma",new Vector3(17,3),6,"platformer-stone-tile");
            Starter("plataforma",new Vector3(-13,7.5f),8,"tiny-dungeon-38");Starter("plataforma",new Vector3(0,7.5f),9,"platformer-stone-tile");Starter("plataforma",new Vector3(14,7.5f),8,"tiny-dungeon-37");
            var leftRamp=Starter("rampa",new Vector3(-12,-3.8f),5,"creajuego-ramp");if(leftRamp!=null)leftRamp.rotationZ=18;
            var rightRamp=Starter("rampa",new Vector3(11,.7f),5,"creajuego-ramp");if(rightRamp!=null)rightRamp.rotationZ=-18;
            var moving=Starter("movil",new Vector3(-2,-.4f),0,"plains-moving-ground");if(moving!=null){moving.distance=5;moving.speed=1.4f;}
            var player=Starter("jugador",new Vector3(-20,-5.1f),0,"5b84b5bdbf244cecb12b976bbf0aa6c7");if(player!=null){player.speed=3;player.jump=14;}
            var prizePositions=new[]{new Vector3(-14,-.6f),new Vector3(-7,3.9f),new Vector3(0,8.4f),new Vector3(6,-.6f),new Vector3(13,3.9f),new Vector3(19,-5.1f)};
            var prizeLooks=new[]{"tiny-dungeon-116","tiny-dungeon-1027","platformer-treasure","tiny-dungeon-116","tiny-dungeon-1027","platformer-treasure"};
            for(var i=0;i<prizePositions.Length;i++)Starter("premio",prizePositions[i],0,prizeLooks[i]);
            var hazardPositions=new[]{new Vector3(-3,-5.35f),new Vector3(10,-5.35f),new Vector3(2,2.65f),new Vector3(19,2.65f)};
            var hazardLooks=new[]{"plains-spikes","platformer-kit-spikes","tiny-dungeon-104","plains-spikes"};
            for(var i=0;i<hazardPositions.Length;i++)Starter("peligro",hazardPositions[i],0,hazardLooks[i]);
            var enemyA=Starter("enemigo",new Vector3(-10,-.55f),0,"platformer-kit-gobbat");if(enemyA!=null)enemyA.distance=4;
            var enemyB=Starter("enemigo",new Vector3(8,-.55f),0,"platformer-kit-scarecrow");if(enemyB!=null)enemyB.distance=5;
            var enemyC=Starter("enemigo",new Vector3(2,8.4f),0,"platformer-kit-gobbler");if(enemyC!=null)enemyC.distance=4;
            Starter("meta",new Vector3(17,8.5f),0,"tiny-dungeon-33");
            var houseA=Starter("decoracion",new Vector3(-13,9),0,"platformer-house");if(houseA!=null)houseA.visualScale=.75f;
            var houseB=Starter("decoracion",new Vector3(14,9),0,"platformer-house");if(houseB!=null)houseB.visualScale=.65f;
            var decorations=new[]{("platformer-tree",new Vector3(-22,-4),.8f),("plains-plant",new Vector3(-21,1),1.5f),("tiny-dungeon-3126",new Vector3(21,-4),1.2f),("tiny-dungeon-3129",new Vector3(22,1),1.35f)};
            foreach(var decoration in decorations){var item=Starter("decoracion",decoration.Item2,0,decoration.Item1);if(item!=null)item.visualScale=decoration.Item3;}
            History.Reset(Project);Rebuild();FrameAll();if(notify)Changed(false);
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
        RuntimeItemData Starter(string id,Vector3 position,float width=0,string appearance=null)
        {
            var data=Seed(id,position,width);if(data==null)return null;if(!string.IsNullOrEmpty(appearance)){data.appearanceId=appearance;data.appearanceChosen=true;}return data;
        }
        RuntimeItemData Seed(string id,Vector3 position,float width=0)
        {
            var definition=Find(id);if(definition==null||definition.prefab==null)return null;var item=definition.prefab.GetComponent<GameItem>();if(item==null)return null;
            var data=RuntimeItemData.From(item,id);EnsureDefaultAppearance(data,definition);data.position=RuntimeSnap.Position(position,Project.alignAutomatically);if(width>0)data.platformWidth=RuntimeSnap.Width(width,Project.alignAutomatically);Project.objects.Add(data);return data;
        }
        public GameItem Create(string id,Vector3 position)
        {
            if(Mode!=AuthoringMode.Build)return null;var definition=Find(id);if(definition==null||definition.prefab==null)return null;
            if(!definition.allowMultiple&&Project.objects.Any(o=>o.definitionId==id)){ui?.SetStatus("Este juego usa un solo "+definition.displayName+".");return null;}
            var data=RuntimeItemData.From(definition.prefab.GetComponent<GameItem>(),id);EnsureDefaultAppearance(data,definition);data.position=definition.kind==ItemKind.Background?Vector3.zero:RuntimeSnap.Position(position,Project.alignAutomatically);Project.objects.Add(data);History.Record(Project);Rebuild(data.instanceId);Changed();return Selection.SelectedItem;
        }
        public void DeleteSelected(){var id=SelectedId();if(id==null)return;Project.objects.RemoveAll(o=>o.instanceId==id);History.Record(Project);Rebuild();Changed();}
        public void DuplicateSelected(){var source=SelectedData();if(source==null)return;var definition=Find(source.definitionId);if(definition==null||!definition.allowMultiple)return;var copy=source.Clone();copy.instanceId=Guid.NewGuid().ToString("N");copy.position=RuntimeSnap.Position(copy.position+new Vector3(1,.5f),Project.alignAutomatically);Project.objects.Add(copy);History.Record(Project);Rebuild(copy.instanceId);Changed();}
        public void MoveSelected(Vector3 position,bool record){var data=SelectedData();if(data==null)return;position.z=data.position.z;position=RuntimeSnap.Position(position,Project.alignAutomatically);data.position=position;if(Selection.SelectedItem!=null)Selection.SelectedItem.transform.position=position;if(record)CommitEdit();}
        public void ResizeSelected(float width,float centerX,bool record)
        {
            var data=SelectedData();var item=Selection.SelectedItem;if(data==null||item==null||item.definition.kind!=ItemKind.Platform)return;
            data.platformWidth=RuntimeSnap.Width(width,Project.alignAutomatically);data.position.x=Project.alignAutomatically?Mathf.Round(centerX/RuntimeSnap.Step)*RuntimeSnap.Step:centerX;item.transform.position=data.position;RuntimePlatformGeometry.Apply(item,data.platformWidth);if(record)CommitEdit();
        }
        public Vector2 SelectedWorldSize
        {
            get{var item=Selection.SelectedItem;if(item==null)return Vector2.one;var bounds=ItemBounds(item);return new Vector2(bounds.size.x,bounds.size.y);}
        }
        public void ResizeSelected(Vector2 worldSize,Vector2 worldCenter,bool record)
        {
            var data=SelectedData();var item=Selection.SelectedItem;if(data==null||item==null||item.definition==null||item.definition.kind==ItemKind.Background)return;
            var bounds=ItemBounds(item);if(bounds.size.x<.001f||bounds.size.y<.001f)return;
            worldSize.x=RuntimeSnap.Width(Mathf.Clamp(worldSize.x,.25f,30),Project.alignAutomatically);worldSize.y=RuntimeSnap.Width(Mathf.Clamp(worldSize.y,.25f,30),Project.alignAutomatically);
            var scale=data.scale;if(Mathf.Abs(scale.x)<.001f)scale.x=1;if(Mathf.Abs(scale.y)<.001f)scale.y=1;
            scale.x=Mathf.Clamp(scale.x*(worldSize.x/bounds.size.x),-20,20);scale.y=Mathf.Clamp(scale.y*(worldSize.y/bounds.size.y),-20,20);
            if(Mathf.Abs(scale.x)<.05f)scale.x=.05f*Mathf.Sign(scale.x==0?1:scale.x);if(Mathf.Abs(scale.y)<.05f)scale.y=.05f*Mathf.Sign(scale.y==0?1:scale.y);
            data.scale=scale;var delta=new Vector3(worldCenter.x-bounds.center.x,worldCenter.y-bounds.center.y);data.position+=delta;if(Project.alignAutomatically)data.position=RuntimeSnap.Position(data.position,true);
            item.transform.position=data.position;item.transform.localScale=data.scale;if(item.definition.kind==ItemKind.Platform)RuntimePlatformGeometry.Apply(item,data.platformWidth);Physics2D.SyncTransforms();if(record)CommitEdit();
        }        public void SetFloat(string path,float value,bool record=true)
        {
            var data=SelectedData();if(data==null)return;
            switch(path)
            {
                case "speed":data.speed=value;break;case "distance":data.distance=value;break;case "jump":data.jump=value;break;
                case "health":data.health=Mathf.RoundToInt(value);break;case "points":data.points=Mathf.RoundToInt(value);break;case "damage":data.damage=Mathf.RoundToInt(value);break;
                case "visualScale":data.visualScale=Mathf.Clamp(value,.1f,5f);break;case "width":ResizeSelected(value,data.position.x,record);return;
                case "sizeX":{var size=SelectedWorldSize;size.x=value;var center=ItemBounds(Selection.SelectedItem).center;ResizeSelected(size,new Vector2(center.x,center.y),record);return;}
                case "sizeY":{var size=SelectedWorldSize;size.y=value;var center=ItemBounds(Selection.SelectedItem).center;ResizeSelected(size,new Vector2(center.x,center.y),record);return;}
            }
            ApplySelected(data);if(record)CommitEdit();
        }        public void SetMessage(string value){var data=SelectedData();if(data==null)return;data.message=value;ApplySelected(data);CommitEdit();}
        public void SetAppearance(string id)
        {
            var data=SelectedData();if(data==null)return;data.appearanceId=id??"";data.customImageBase64=null;data.appearanceChosen=true;ApplySelected(data);CommitEdit();
        }
        public void SetSnap(bool value)
        {
            if(Project.alignAutomatically==value)return;Project.alignAutomatically=value;
            var selected=SelectedData();if(value&&selected!=null){selected.position=RuntimeSnap.Position(selected.position,true);selected.platformWidth=RuntimeSnap.Width(selected.platformWidth,true);Rebuild(selected.instanceId);}
            CommitEdit();ui?.SetStatus(value?"Cuadrícula activada: los elementos encajan cada 0,25 unidades.":"Movimiento libre activado.");
        }
        public void SetStructureDirection(bool facesRight)
        {
            var data=SelectedData();if(data==null)return;
            if(data.definitionId=="muro")data.rotationZ=facesRight?0:90;
            else if(data.definitionId=="rampa")data.rotationZ=facesRight?18:-18;
            else return;
            ApplySelected(data);CommitEdit();
        }
        public void SetLevelSize(RuntimeLevelSize value){if(Project.levelSize==value)return;Project.levelSize=value;Project.bounds=RuntimeLevelBounds.For(value);Rebuild(SelectedId());CommitEdit();}
        public void CommitEdit(){History.Record(Project);Changed();}
        void ApplySelected(RuntimeItemData data){var item=Selection.SelectedItem;if(item!=null&&SelectedId()==data.instanceId){ApplyData(item,data);item.GetComponent<RuntimeCanvasBackground>()?.Configure(buildCamera);}}
        public void Undo(){var value=History.Undo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void Redo(){var value=History.Redo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void ConfigureStorage(IProjectStorage manual,IProjectStorage recovery){storage=manual??throw new ArgumentNullException(nameof(manual));recoveryStorage=recovery??throw new ArgumentNullException(nameof(recovery));}
        public void SaveNow()
        {
            try{var json=ProjectSerializer.ToJson(Project);storage.Save(json);recoveryStorage.Save(json);dirty=false;RuntimeProjectFiles.Flush();ui?.SetSaveState("Guardado ✓");ui?.SetStatus("Proyecto guardado en este navegador.");}
            catch(Exception exception){ui?.SetStatus("No se pudo guardar el proyecto: "+exception.Message);}
        }
        public void SaveRecovery()
        {
            try{recoveryStorage.Save(ProjectSerializer.ToJson(Project));dirty=false;RuntimeProjectFiles.Flush();ui?.SetSaveState("Autoguardado ✓");}
            catch(Exception exception){ui?.SetStatus("No se pudo crear la recuperación automática: "+exception.Message);}
        }
        public void LoadLast()
        {
            if(!storage.Exists){ui?.SetStatus("Todavía no hay un proyecto guardado. Usa Guardar primero.");return;}
            try{ApplyProjectJson(storage.Load());ui?.SetStatus("Proyecto guardado abierto.");}
            catch(Exception exception){ui?.SetStatus("No se pudo abrir el proyecto: "+exception.Message);}
        }
        public void DownloadProject()
        {
            SaveNow();var filename=SafeFilename(Project.projectName)+".creajuego";
            if(!RuntimeProjectFiles.Download(filename,ProjectSerializer.ToJson(Project)))ui?.SetStatus("La descarga de copias está disponible en la versión web.");
            else ui?.SetStatus("Copia descargada. Guárdala para abrirla después o llevarla a otro computador.");
        }
        public void PickProjectFile(){if(!RuntimeProjectFiles.Pick(gameObject.name))ui?.SetStatus("La importación de copias está disponible en la versión web.");}
        public void ReceiveProjectJson(string json)
        {
            try{ApplyProjectJson(json);SaveNow();ui?.SetStatus("Copia importada y guardada correctamente.");}
            catch(Exception exception){ui?.SetStatus("No se pudo importar esta copia: "+exception.Message);}
        }
        public void ReceiveProjectError(string message)=>ui?.SetStatus(string.IsNullOrWhiteSpace(message)?"No se pudo leer esta copia del nivel.":message);
        void ApplyProjectJson(string json){ExitPlay(false);Project=ProjectSerializer.FromJson(json);EnsureDefaultAppearances(Project);History.Reset(Project);Rebuild();Changed(false);}
        static string SafeFilename(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "mi-juego";var invalid=System.IO.Path.GetInvalidFileNameChars();var clean=new string(value.Trim().Select(character=>invalid.Contains(character)?'-':character).ToArray());return string.IsNullOrWhiteSpace(clean)?"mi-juego":clean;
        }

        public string EnterPlay()
        {
            if(Mode==AuthoringMode.Play)return null;var error=RuntimePreflight.Validate(Project,Find,new RuntimePreflightContext{cameraAvailable=gameCamera!=null});if(error!=null){ui?.SetStatus(error);return error;}
            Selection.Clear();buildRoot.gameObject.SetActive(false);services=sceneServicesPrefab!=null?Instantiate(sceneServicesPrefab):null;
            if(services!=null){foreach(var camera in services.GetComponentsInChildren<Camera>(true))camera.enabled=false;foreach(var listener in services.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;foreach(var serviceCanvas in services.GetComponentsInChildren<Canvas>(true))serviceCanvas.enabled=false;}
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
            buildRoot.gameObject.SetActive(true);foreach(var behaviour in buildRoot.GetComponentsInChildren<MonoBehaviour>())if(!KeepEnabledWhileBuilding(behaviour))behaviour.enabled=false;foreach(var body in buildRoot.GetComponentsInChildren<Rigidbody2D>())body.simulated=false;
            Selection.Select(selected!=null?selectId:null);
        }
        GameItem InstantiateItem(RuntimeItemData data,Transform parent,bool authoring)
        {
            var definition=Find(data.definitionId);var go=Instantiate(definition.prefab,parent);go.name=definition.displayName;go.SetActive(false);var item=go.GetComponent<GameItem>();item.definition=definition;ApplyData(item,data);item.GetComponent<RuntimeCanvasBackground>()?.Configure(authoring?buildCamera:gameCamera);
            if(authoring){foreach(var behaviour in go.GetComponents<MonoBehaviour>())if(!KeepEnabledWhileBuilding(behaviour))behaviour.enabled=false;foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;}
            go.SetActive(true);if(authoring)foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;return item;
        }
        static bool KeepEnabledWhileBuilding(MonoBehaviour behaviour)=>behaviour is GameItem||behaviour is ItemVisual||behaviour is RuntimeAuthoredItem||behaviour is RuntimeCanvasBackground||behaviour.GetComponentInParent<RuntimeCanvasBackground>()!=null;
        void ApplyData(GameItem item,RuntimeItemData data)
        {
            EnsureDefaultAppearance(data,item.definition);data.Apply(item);item.appearanceCategory=contentPack!=null?contentPack.CategoryFor(item.definition.kind):null;
            item.customSprite=null;if(!string.IsNullOrEmpty(data.customImageBase64)&&RuntimeImageImport.TryDecodeDataUrl(data.customImageBase64,out var sprite,out var texture,out _)){imageAssets.Add(sprite);imageAssets.Add(texture);item.customSprite=sprite;}
            var visual=EnsureVisual(item);if(visual!=null)visual.Apply();if(item.definition.kind==ItemKind.Platform)RuntimePlatformGeometry.Apply(item,data.platformWidth);item.GetComponent<RuntimeCanvasBackground>()?.Refresh();foreach(var backend in item.GetComponents<MonoBehaviour>().OfType<IItemBackend>())backend.ApplyConfiguration();
        }
        static ItemVisual EnsureVisual(GameItem item)
        {
            if(item==null||item.definition==null||item.definition.kind==ItemKind.Background)return item!=null?item.GetComponent<ItemVisual>():null;
            var visual=item.GetComponent<ItemVisual>();if(visual!=null&&visual.renderer!=null)return visual;
            if(visual==null)visual=item.gameObject.AddComponent<ItemVisual>();
            visual.renderer=item.GetComponent<SpriteRenderer>()??item.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            visual.geometrySource=visual.renderer;visual.animator=item.GetComponentInChildren<Animator>(true);return visual;
        }
        void EnsureDefaultAppearances(CreaJuegoProjectData project)
        {
            if(project?.objects==null)return;foreach(var data in project.objects)EnsureDefaultAppearance(data,Find(data.definitionId));
        }
        void EnsureDefaultAppearance(RuntimeItemData data,GameItemDefinition definition)
        {
            if(data==null||definition==null||data.appearanceChosen||!string.IsNullOrEmpty(data.customImageBase64)||contentPack==null)return;
            if(string.IsNullOrEmpty(data.appearanceId)){var preferred=definition.id=="plataforma"?"platformer-stone-tile":definition.id=="muro"?"creajuego-wall-light":definition.id=="rampa"?"creajuego-ramp":null;if(preferred!=null&&contentPack.CategoryFor(definition.kind)?.Find(preferred)!=null){data.appearanceId=preferred;return;}}
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
        void OnApplicationQuit(){if(dirty)SaveRecovery();}
    }

    public sealed class RuntimeLevelBoundary:MonoBehaviour{}
}
