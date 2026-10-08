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
        public RuntimeGameTypeDefinition[] GameTypes=>contentPack!=null?contentPack.GameTypes:RuntimeGameTypeCatalog.Defaults;
        public bool IsCatchMode=>Project!=null&&Project.gameTypeId=="catch-and-dodge";
        public Transform PlayRoot=>playRoot;
        public GameItem PlayPlayer=>playPlayer;
        public event Action ProjectChanged;
        readonly List<UnityEngine.Object> imageAssets=new List<UnityEngine.Object>();
        readonly Dictionary<string,Sprite> mediaSprites=new Dictionary<string,Sprite>();
        Transform playRoot;GameObject services;GameItem playPlayer;IProjectStorage storage,recoveryStorage;bool dirty;float saveAt;
        string pendingImageTarget,editingMediaId;MediaAssetSource pendingMediaSource;bool importChanged,importedAppliedToTarget,updatedMediaAsset;int importedImageCount;

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
            var data=CloneProject(editableSceneProject);var previousItems=data.objects.Where(value=>value!=null&&!string.IsNullOrEmpty(value.instanceId)).ToDictionary(value=>value.instanceId);data.objects.Clear();
            foreach(var marker in markers)
            {
                var item=marker.GetComponent<GameItem>();if(item==null||item.definition==null)continue;var authored=RuntimeItemData.From(item,item.definition.id);authored.instanceId=marker.instanceId;
                if(previousItems.TryGetValue(marker.instanceId,out var previous)){authored.mediaAssetId=previous.mediaAssetId;authored.customImageBase64=previous.customImageBase64;}
                if(item.definition.kind==ItemKind.Platform){var box=item.GetComponent<BoxCollider2D>();if(box!=null)authored.platformWidth=box.size.x;}
                data.objects.Add(authored);
            }
            return data.objects.Count==0?null:data;
        }
        public bool CaptureEditableScene(bool allowRemoval=false){var previous=editableSceneProject;editableSceneProject=CloneProject(Project);var data=ReadEditableSceneProject();if(data==null){editableSceneProject=previous;return false;}if(!allowRemoval&&previous!=null&&data.objects.Count<previous.objects.Count){editableSceneProject=previous;return false;}editableSceneProject=data;return true;}
        static CreaJuegoProjectData CloneProject(CreaJuegoProjectData value)=>ProjectSerializer.FromJson(ProjectSerializer.ToJson(value));

        public void NewProject(bool notify=true)
        {
            NewProjectFor("platformer",notify);
        }
        public bool NewProjectFor(string gameTypeId,bool notify=true)
        {
            var type=GameTypes.FirstOrDefault(value=>value!=null&&value.id==gameTypeId);if(type==null||!type.available)return false;
            ExitPlay(false);Project=new CreaJuegoProjectData();Project.bounds=RuntimeLevelBounds.For(Project.levelSize);
            Project.gameTypeId=type.id;
            if(type.id=="catch-and-dodge"){Project.levelSize=RuntimeLevelSize.Small;Project.bounds=new RuntimeLevelBounds{left=-9,right=9,bottom=-5,top=7};}
            History.Reset(Project);Rebuild();if(type.id=="catch-and-dodge")Frame(new Bounds(new Vector3(0,1,0),new Vector3(18,12,1)));if(notify)Changed(false);
            return true;
        }
        public bool DefinitionAvailable(GameItemDefinition definition)
        {
            if(definition==null||!definition.availableInWorkshop)return false;if(!IsCatchMode)return true;
            return definition.kind==ItemKind.Player||definition.kind==ItemKind.Prize||definition.kind==ItemKind.Hazard||definition.kind==ItemKind.Decoration||definition.kind==ItemKind.Background;
        }
        public void LoadStarterLevel(bool notify=true)
        {
            ExitPlay(false);Project=new CreaJuegoProjectData{projectName="La aventura del bosque",teamName="Mi equipo",levelSize=RuntimeLevelSize.Small,bounds=RuntimeLevelBounds.For(RuntimeLevelSize.Small)};
            Starter("fondo",Vector3.zero,0,"workshop-forest-background");
            const string floor="workshop-grass-platform",wall="workshop-grass-wall";
            StarterSolidBlock(-20,-8,8,6,floor,wall);StarterSolidBlock(-9,-6,8,5,floor,wall);StarterSolidBlock(3,-4,8,4,floor,wall);StarterSolidBlock(16,-2,10,5,floor,wall);
            StarterSolidBlock(13,0,6,3,floor,wall);StarterSolidBlock(17,2,10,4,floor,wall);StarterSolidBlock(5,4,8,4,floor,wall);StarterSolidBlock(-7,6,8,4,floor,wall);
            StarterSolidBlock(-19,8,8,5,floor,wall);StarterSolidBlock(-7,10,8,4,floor,wall);StarterSolidBlock(6,12,8,4,floor,wall);StarterSolidBlock(19,14,8,5,floor,wall);
            StarterRamp(new Vector2(-16,-8),new Vector2(-13,-6),"workshop-grass-ramp");StarterRamp(new Vector2(-5,-6),new Vector2(-1,-4),"workshop-grass-ramp");StarterRamp(new Vector2(7,-4),new Vector2(11,-2),"workshop-grass-ramp");
            StarterRamp(new Vector2(12,2),new Vector2(9,4),"workshop-grass-ramp");StarterRamp(new Vector2(1,4),new Vector2(-3,6),"workshop-grass-ramp");StarterRamp(new Vector2(-11,6),new Vector2(-15,8),"workshop-grass-ramp");
            StarterRamp(new Vector2(-15,8),new Vector2(-11,10),"workshop-grass-ramp");StarterRamp(new Vector2(-3,10),new Vector2(2,12),"workshop-grass-ramp");StarterRamp(new Vector2(10,12),new Vector2(15,14),"workshop-grass-ramp");
            var moving=Starter("movil",new Vector3(0,9),0,"workshop-grass-moving");if(moving!=null){moving.distance=4;moving.speed=1.2f;}
            var player=StarterOnSurface("jugador",-22,-8,"5b84b5bdbf244cecb12b976bbf0aa6c7");if(player!=null){player.speed=3;player.jump=14;}
            foreach(var position in new[]{new Vector2(-18,-8),new Vector2(-9,-6),new Vector2(3,-4),new Vector2(13,-2),new Vector2(19,2),new Vector2(5,4),new Vector2(-7,6),new Vector2(-19,8),new Vector2(-7,10),new Vector2(6,12),new Vector2(17,14)})StarterOnSurface("premio",position.x,position.y,"workshop-gold-coin");
            foreach(var position in new[]{new Vector2(-19,-8),new Vector2(17,-2),new Vector2(6,4),new Vector2(-6,10),new Vector2(7,12)})StarterOnSurface("peligro",position.x,position.y,"workshop-grass-spikes");
            var enemyA=StarterOnSurface("enemigo",-10,-6,"platformer-kit-gobbat");if(enemyA!=null)enemyA.distance=2.5f;
            var enemyB=StarterOnSurface("enemigo",4,-4,"platformer-kit-scarecrow");if(enemyB!=null)enemyB.distance=3;
            var enemyC=StarterOnSurface("enemigo",16,2,"platformer-kit-gobbler");if(enemyC!=null)enemyC.distance=3;
            var enemyD=StarterOnSurface("enemigo",-18,8,"platformer-kit-gobbat");if(enemyD!=null)enemyD.distance=2.5f;
            StarterOnSurface("meta",21,14,"workshop-red-flag");
            foreach(var decoration in new[]{("workshop-large-tree",new Vector3(-22,-4.5f),.8f),("workshop-flower-bush",new Vector3(-9,-5.2f),1f),("workshop-large-tree",new Vector3(17,4.8f),.75f),("workshop-flower-bush",new Vector3(5,12.7f),.85f)}){var item=Starter("decoracion",decoration.Item2,0,decoration.Item1);if(item!=null)item.visualScale=decoration.Item3;}
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
        RuntimeItemData StarterOnSurface(string id,float x,float surfaceY,string appearance=null)
        {
            var definition=Find(id);var itemCollider=definition!=null&&definition.prefab!=null?definition.prefab.GetComponent<BoxCollider2D>():null;
            var platform=Find("plataforma");var surfaceCollider=platform!=null&&platform.prefab!=null?platform.prefab.GetComponent<BoxCollider2D>():null;
            var surfaceTop=surfaceY+(surfaceCollider!=null?surfaceCollider.offset.y+surfaceCollider.size.y*.5f:.225f);
            var localBottom=itemCollider!=null?itemCollider.offset.y-itemCollider.size.y*.5f:0;
            return Starter(id,new Vector3(x,surfaceTop-localBottom),0,appearance);
        }
        RuntimeItemData StarterRamp(Vector2 firstEdge,Vector2 secondEdge,string appearance="creajuego-ramp")
        {
            var delta=secondEdge-firstEdge;var ramp=Starter("rampa",(firstEdge+secondEdge)*.5f,delta.magnitude,appearance);if(ramp==null)return null;
            var angle=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;if(angle>90)angle-=180;else if(angle<-90)angle+=180;ramp.rotationZ=angle;return ramp;
        }
        void StarterSolidBlock(float centerX,float surfaceY,float width,float depth,string floorAppearance,string wallAppearance)
        {
            Starter("plataforma",new Vector3(centerX,surfaceY),width,floorAppearance);
            var wall=Starter("muro",new Vector3(centerX,surfaceY-.225f-depth*.5f),width,wallAppearance);
            if(wall==null)return;
            wall.rotationZ=0;
            wall.scale=new Vector3(1,Mathf.Max(.5f,depth/.45f),1);
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
            var data=RuntimeItemData.From(definition.prefab.GetComponent<GameItem>(),id);EnsureDefaultAppearance(data,definition);data.position=definition.kind==ItemKind.Background?Vector3.zero:RuntimeSnap.Position(position,Project.alignAutomatically);
            if(IsCatchMode&&definition.kind==ItemKind.Player)data.position.y=Project.bounds.bottom+1;
            else if(IsCatchMode&&(definition.kind==ItemKind.Prize||definition.kind==ItemKind.Hazard))data.position.y=Project.bounds.top-1;
            Project.objects.Add(data);History.Record(Project);Rebuild(data.instanceId);Changed();return Selection.SelectedItem;
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
                case "speed":data.speed=value;break;case "distance":data.distance=value;break;case "jump":data.jump=value;break;case "spawnInterval":data.spawnInterval=Mathf.Clamp(value,.1f,5);break;
                case "health":data.health=Mathf.RoundToInt(value);break;case "points":data.points=Mathf.RoundToInt(value);break;case "damage":data.damage=Mathf.RoundToInt(value);break;
                case "visualScale":data.visualScale=Mathf.Clamp(value,.1f,5f);break;case "width":ResizeSelected(value,data.position.x,record);return;
                case "sizeX":{var size=SelectedWorldSize;size.x=value;var center=ItemBounds(Selection.SelectedItem).center;ResizeSelected(size,new Vector2(center.x,center.y),record);return;}
                case "sizeY":{var size=SelectedWorldSize;size.y=value;var center=ItemBounds(Selection.SelectedItem).center;ResizeSelected(size,new Vector2(center.x,center.y),record);return;}
            }
            ApplySelected(data);if(record)CommitEdit();
        }        public void SetMessage(string value){var data=SelectedData();if(data==null)return;data.message=value;ApplySelected(data);CommitEdit();}
        public void SetAppearance(string id)
        {
            var data=SelectedData();if(data==null)return;data.appearanceId=id??"";data.mediaAssetId=null;data.customImageBase64=null;data.appearanceChosen=true;ApplySelected(data);CommitEdit();
        }
        public void SetMediaAsset(string id)
        {
            var data=SelectedData();if(data==null||Project.mediaAssets.All(asset=>asset==null||asset.id!=id))return;data.mediaAssetId=id;data.customImageBase64=null;data.appearanceId="";data.appearanceChosen=true;ApplySelected(data);CommitEdit();
        }
        public long MediaBytes=>Project.mediaAssets.Where(asset=>asset!=null).Sum(asset=>(long)RuntimeImageImport.DataBytes(asset.dataUrl)+(asset.originalDataUrl==asset.dataUrl?0:RuntimeImageImport.DataBytes(asset.originalDataUrl)));
        public int MediaUsageCount(string id)=>Project.objects.Count(item=>item.mediaAssetId==id);
        public void RenameMediaAsset(string id,string displayName)
        {
            var asset=FindMedia(id);if(asset==null||string.IsNullOrWhiteSpace(displayName))return;displayName=displayName.Trim();if(asset.displayName==displayName)return;asset.displayName=displayName;History.Record(Project);Changed();ui?.SetStatus("Imagen renombrada.");
        }
        public void CycleMediaCategory(string id)
        {
            var asset=FindMedia(id);if(asset==null)return;string[] categories={"personajes","enemigos","escenarios","objetos","fondos","otros"};int index=Array.IndexOf(categories,asset.categoryId);asset.categoryId=categories[(index+1+categories.Length)%categories.Length];History.Record(Project);Changed();
        }
        public void DeleteMediaAsset(string id,bool force=false)
        {
            if(string.IsNullOrEmpty(id))return;int uses=MediaUsageCount(id);if(uses>0&&!force){ui?.SetStatus("Esta imagen está en uso por "+uses+" elemento"+(uses==1?".":"s."));return;}
            if(force)foreach(var item in Project.objects.Where(item=>item.mediaAssetId==id)){item.mediaAssetId=null;item.customImageBase64=null;item.appearanceId="";item.appearanceChosen=false;}
            if(Project.mediaAssets.RemoveAll(asset=>asset!=null&&asset.id==id)==0)return;History.Record(Project);Rebuild(SelectedId());Changed();ui?.SetStatus("Imagen eliminada de Mi biblioteca.");
        }
        string MediaCategoryForTarget()
        {
            var target=string.IsNullOrEmpty(pendingImageTarget)?null:Project.objects.FirstOrDefault(item=>item.instanceId==pendingImageTarget);var kind=target!=null?Find(target.definitionId)?.kind:null;
            if(kind==ItemKind.Player)return "personajes";if(kind==ItemKind.Enemy)return "enemigos";if(kind==ItemKind.Background)return "fondos";if(kind==ItemKind.Platform||kind==ItemKind.MovingPlatform)return "escenarios";if(kind==ItemKind.Prize||kind==ItemKind.Hazard||kind==ItemKind.Goal||kind==ItemKind.Decoration)return "objetos";return "otros";
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
        public void SetTargetScore(float value,bool record=true){Project.targetScore=Mathf.Clamp(Mathf.RoundToInt(value),1,100);if(record)CommitEdit();}
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
            bool catchMode=IsCatchMode;Selection.Clear();buildRoot.gameObject.SetActive(false);services=!catchMode&&sceneServicesPrefab!=null?Instantiate(sceneServicesPrefab):null;
            if(services!=null){foreach(var camera in services.GetComponentsInChildren<Camera>(true))camera.enabled=false;foreach(var listener in services.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;foreach(var serviceCanvas in services.GetComponentsInChildren<Canvas>(true))serviceCanvas.enabled=false;}
            playRoot=new GameObject("Partida temporal").transform;playRoot.gameObject.SetActive(false);var instances=new List<(GameItem item,RuntimeItemData data)>();foreach(var data in Project.objects)instances.Add((InstantiateItem(data,playRoot,false),data));
            playPlayer=instances.First(pair=>pair.item.definition.kind==ItemKind.Player).item;
            if(catchMode)ConfigureCatchMode(instances);else
            {
                CreatePlayBoundaries(playRoot,Project.bounds);var authored=Project.objects.First(o=>o.definitionId==playPlayer.definition.id);var recovery=playPlayer.GetComponent<PlayerFallRecovery>();recovery.ConfigureWorldLimit(authored.position,Project.bounds.bottom);
            }
            playRoot.gameObject.SetActive(true);Mode=AuthoringMode.Play;if(buildCamera!=null){buildCamera.enabled=false;var buildListener=buildCamera.GetComponent<AudioListener>();if(buildListener!=null)buildListener.enabled=false;}var gameListener=gameCamera.GetComponent<AudioListener>();if(gameListener!=null)gameListener.enabled=true;if(catchMode)GameplayCamera.ActivateStatic(gameCamera,Project.bounds);else GameplayCamera.Activate(gameCamera,playPlayer,playRoot);ui?.Refresh();Debug.Log($"CREAJUEGO_WEB_MODE PLAY type={Project.gameTypeId} objects={Project.objects.Count}");return null;
        }
        void ConfigureCatchMode(List<(GameItem item,RuntimeItemData data)> instances)
        {
            var sessionObject=new GameObject("Reglas de Atrapa y esquiva",typeof(RuntimeCatchSession));sessionObject.transform.SetParent(playRoot,false);var session=sessionObject.GetComponent<RuntimeCatchSession>();var playerData=instances.First(pair=>pair.item.definition.kind==ItemKind.Player).data;session.Configure(playerData.health,Project.targetScore);
            foreach(var pair in instances)
            {
                var item=pair.item;var kind=item.definition.kind;
                foreach(var behaviour in item.GetComponents<MonoBehaviour>())if(behaviour!=item&&!(behaviour is ItemVisual)&&!(behaviour is RuntimeCanvasBackground))behaviour.enabled=false;
                var body=item.GetComponent<Rigidbody2D>();var collider=item.GetComponent<Collider2D>();
                if(kind==ItemKind.Player){if(body==null)body=item.gameObject.AddComponent<Rigidbody2D>();if(collider==null)collider=item.gameObject.AddComponent<BoxCollider2D>();var player=item.gameObject.AddComponent<RuntimeCatchPlayer>();player.Configure(session,Project.bounds);}
                else if(kind==ItemKind.Prize||kind==ItemKind.Hazard){if(body==null)body=item.gameObject.AddComponent<Rigidbody2D>();if(collider==null)collider=item.gameObject.AddComponent<BoxCollider2D>();var falling=item.gameObject.AddComponent<RuntimeFallingObject>();falling.Configure(session,Project.bounds,pair.data.spawnInterval);}
                else if(collider!=null)collider.enabled=false;
            }
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
        public void PickImage()=>BeginImageEditor("file",SelectedId());
        public void CaptureImage()=>BeginImageEditor("camera",SelectedId(),null,MediaAssetSource.Camera);
        public void EditMediaAsset(string id)
        {
            var asset=FindMedia(id);if(asset==null)return;BeginImageEditor("edit",null,id,asset.source,asset.displayName,asset.originalDataUrl??asset.dataUrl,RuntimeImageEditSettings.From(asset));
        }
        public void PickImages()=>BeginImageImport(null);
        void BeginImageImport(string targetId)
        {
            ResetImageImport(targetId);pendingMediaSource=MediaAssetSource.File;
            if(!RuntimeImageImport.PickMany(gameObject.name))ui?.SetStatus("La selección múltiple de imágenes se prueba dentro de la build WebGL.");
        }
        void BeginImageEditor(string mode,string targetId,string mediaId=null,MediaAssetSource source=MediaAssetSource.File,string imageName=null,string dataUrl=null,RuntimeImageEditSettings settings=null)
        {
            ResetImageImport(targetId);editingMediaId=mediaId;pendingMediaSource=source;
            if(!RuntimeImageImport.OpenEditor(gameObject.name,mode,imageName,dataUrl,settings))ui?.SetStatus("El recorte de imágenes y la cámara se prueban dentro de la versión web.");
        }
        void ResetImageImport(string targetId)
        {
            pendingImageTarget=targetId;editingMediaId=null;importChanged=false;importedAppliedToTarget=false;updatedMediaAsset=false;importedImageCount=0;
        }
        public void ReceiveImageError(string message)=>ui?.SetStatus(string.IsNullOrWhiteSpace(message)?RuntimeImageImport.TooLargeMessage:message);
        public void ReceiveImageDataUrl(string value)
        {
            ReceiveImageImport(JsonUtility.ToJson(new RuntimeImageImportPayload{name="Mi imagen",dataUrl=value}));ReceiveImageBatchComplete("1");
        }
        public void ReceiveImageImport(string json)
        {
            RuntimeImageImportPayload payload;try{payload=JsonUtility.FromJson<RuntimeImageImportPayload>(json);}catch{ui?.SetStatus("No se pudo leer esta imagen.");return;}
            if(payload==null){ui?.SetStatus("No se pudo leer esta imagen.");return;}if(!RuntimeImageImport.ValidateDataUrl(payload.dataUrl,out var error)){ui?.SetStatus(error??"No se pudo leer esta imagen.");return;}
            if(!string.IsNullOrWhiteSpace(payload.originalDataUrl)&&!RuntimeImageImport.ValidateDataUrl(payload.originalDataUrl,out error)){ui?.SetStatus(error??"No se pudo conservar la imagen original.");return;}
            MediaAssetData asset=null;
            if(!string.IsNullOrEmpty(editingMediaId))
            {
                asset=FindMedia(editingMediaId);if(asset==null){ui?.SetStatus("Esta imagen ya no está en Mi biblioteca.");return;}
                var total=Project.mediaAssets.Where(value=>value!=null).Sum(value=>(long)RuntimeImageImport.DataBytes(value.dataUrl)+(value.originalDataUrl==value.dataUrl?0:RuntimeImageImport.DataBytes(value.originalDataUrl)))-RuntimeImageImport.DataBytes(asset.dataUrl)-(asset.originalDataUrl==asset.dataUrl?0:RuntimeImageImport.DataBytes(asset.originalDataUrl));
                var original=string.IsNullOrWhiteSpace(payload.originalDataUrl)?payload.dataUrl:payload.originalDataUrl;
                var required=(long)RuntimeImageImport.DataBytes(payload.dataUrl)+(original==payload.dataUrl?0:RuntimeImageImport.DataBytes(original));
                if(total+required>RuntimeImageImport.MaxProjectBytes){ui?.SetStatus("Mi biblioteca llegó a su límite de 24 MB. Usa imágenes más pequeñas o elimina algunas.");return;}
                asset.dataUrl=payload.dataUrl;asset.originalDataUrl=original;ApplyImageEdits(asset,payload);if(!string.IsNullOrWhiteSpace(payload.name))asset.displayName=payload.name;asset.source=pendingMediaSource;importChanged=true;updatedMediaAsset=true;
            }
            else
            {
                asset=Project.mediaAssets.FirstOrDefault(value=>value!=null&&value.dataUrl==payload.dataUrl);
                if(asset==null)
                {
                    var original=string.IsNullOrWhiteSpace(payload.originalDataUrl)?payload.dataUrl:payload.originalDataUrl;
                    var total=MediaBytes;var required=(long)RuntimeImageImport.DataBytes(payload.dataUrl)+(original==payload.dataUrl?0:RuntimeImageImport.DataBytes(original));if(total+required>RuntimeImageImport.MaxProjectBytes){ui?.SetStatus("Mi biblioteca llegó a su límite de 24 MB. Usa imágenes más pequeñas o elimina algunas.");return;}
                    asset=MediaAssetData.Create(payload.name,payload.dataUrl,pendingMediaSource);asset.originalDataUrl=original;ApplyImageEdits(asset,payload);asset.categoryId=MediaCategoryForTarget();Project.mediaAssets.Add(asset);importChanged=true;importedImageCount++;
                }
            }
            if(!string.IsNullOrEmpty(pendingImageTarget)&&!importedAppliedToTarget)
            {
                var data=Project.objects.FirstOrDefault(value=>value.instanceId==pendingImageTarget);if(data!=null){data.mediaAssetId=asset.id;data.customImageBase64=null;data.appearanceId="";data.appearanceChosen=true;importChanged=true;importedAppliedToTarget=true;}
            }
        }
        static void ApplyImageEdits(MediaAssetData asset,RuntimeImageImportPayload payload)
        {
            asset.cropWidth=payload.cropWidth>0?Mathf.Clamp01(payload.cropWidth):1;asset.cropHeight=payload.cropHeight>0?Mathf.Clamp01(payload.cropHeight):1;asset.cropX=Mathf.Clamp(payload.cropX,0,1-asset.cropWidth);asset.cropY=Mathf.Clamp(payload.cropY,0,1-asset.cropHeight);
            asset.paintEnabled=payload.paintEnabled;asset.removeBackground=payload.removeBackground;asset.paintColor=string.IsNullOrWhiteSpace(payload.paintColor)?"#4f8cff":payload.paintColor;
            asset.backgroundColor=string.IsNullOrWhiteSpace(payload.backgroundColor)?"#ffffff":payload.backgroundColor;asset.backgroundTolerance=Mathf.Clamp(payload.backgroundTolerance,0,100);
        }
        public void ReceiveImageBatchComplete(string ignored)
        {
            if(importChanged){History.Record(Project);Rebuild(pendingImageTarget);Changed();ui?.SetStatus(updatedMediaAsset?"Imagen actualizada en Mi biblioteca.":importedImageCount==0?"Imagen aplicada desde Mi biblioteca.":importedImageCount==1?"Imagen añadida a Mi biblioteca.":importedImageCount+" imágenes añadidas a Mi biblioteca.");}
            ResetImageImport(null);
        }
        public MediaAssetData FindMedia(string id)=>string.IsNullOrEmpty(id)?null:Project.mediaAssets.FirstOrDefault(asset=>asset!=null&&asset.id==id);
        public Sprite MediaPreview(string id)
        {
            if(string.IsNullOrEmpty(id))return null;if(mediaSprites.TryGetValue(id,out var existing)&&existing!=null)return existing;var asset=FindMedia(id);if(asset==null||!RuntimeImageImport.TryDecodeDataUrl(asset.dataUrl,out var sprite,out var texture,out _))return null;imageAssets.Add(sprite);imageAssets.Add(texture);mediaSprites[id]=sprite;return sprite;
        }
        public void ToggleMode(){if(Mode==AuthoringMode.Build)EnterPlay();else ExitPlay();}
        public void Replay(){if(Mode!=AuthoringMode.Play)return;ExitPlay();EnterPlay();}

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
            item.customSprite=null;var shared=MediaPreview(data.mediaAssetId);if(shared!=null)item.customSprite=shared;else if(!string.IsNullOrEmpty(data.customImageBase64)&&RuntimeImageImport.TryDecodeDataUrl(data.customImageBase64,out var sprite,out var texture,out _)){imageAssets.Add(sprite);imageAssets.Add(texture);item.customSprite=sprite;}
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
            if(data==null||definition==null||data.appearanceChosen||!string.IsNullOrEmpty(data.mediaAssetId)||!string.IsNullOrEmpty(data.customImageBase64)||contentPack==null)return;
            if(string.IsNullOrEmpty(data.appearanceId)){var preferred=definition.id=="plataforma"?"platformer-stone-tile":definition.id=="suelo"?"creajuego-soil-light":definition.id=="muro"?"creajuego-wall-light":definition.id=="rampa"?"creajuego-ramp":null;if(preferred!=null&&contentPack.CategoryFor(definition.kind)?.Find(preferred)!=null){data.appearanceId=preferred;return;}}
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
        void ReleaseImages(){foreach(var value in imageAssets)if(value!=null)DestroySafe(value);imageAssets.Clear();mediaSprites.Clear();}
        static void DestroySafe(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnApplicationQuit(){if(dirty)SaveRecovery();}
    }

    public sealed class RuntimeLevelBoundary:MonoBehaviour{}
}
