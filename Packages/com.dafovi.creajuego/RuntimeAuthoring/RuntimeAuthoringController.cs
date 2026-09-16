using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CreaJuego.Web
{
    public enum AuthoringMode{Build,Play}
    public sealed class RuntimeAuthoringController:MonoBehaviour
    {
        public GameItemDefinition[] definitions=Array.Empty<GameItemDefinition>(); public GameObject sceneServicesPrefab;
        public Camera buildCamera,gameCamera; public Transform buildRoot; public RuntimeAuthoringUI ui; public CreaJuegoProjectData Project{get;private set;}=new CreaJuegoProjectData();
        public AuthoringMode Mode{get;private set;}=AuthoringMode.Build; public RuntimeSelectionService Selection{get;}=new RuntimeSelectionService(); public RuntimeHistory History{get;}=new RuntimeHistory();
        public event Action ProjectChanged; readonly List<Texture2D> textures=new List<Texture2D>(); Transform playRoot; GameObject services; IProjectStorage storage; bool dirty; float saveAt;
        void Awake(){storage=new FileProjectStorage();if(buildRoot==null){buildRoot=new GameObject("Nivel en construcción").transform;}if(ui==null)ui=GetComponent<RuntimeAuthoringUI>();NewProject(false);}
        void Start(){if(Application.absoluteURL.Contains("stress=1"))CreateStressProject();Debug.Log($"CREAJUEGO_WEB_READY mode={Mode} objects={Project.objects.Count}");}
        void Update(){if(dirty&&Time.unscaledTime>=saveAt)SaveNow();}
        public GameItemDefinition Find(string id)=>definitions.FirstOrDefault(d=>d!=null&&d.id==id);
        public void NewProject(bool notify=true){ExitPlay(false);Project=new CreaJuegoProjectData();Seed("jugador",new Vector3(-6,-1));Seed("plataforma",new Vector3(0,-2));Seed("plataforma",new Vector3(-3,-.5f));Seed("premio",new Vector3(-2,.2f));Seed("peligro",new Vector3(1,-1.5f));Seed("enemigo",new Vector3(3,-1.2f));Seed("meta",new Vector3(6,-1));Seed("decoracion",new Vector3(-5,1.5f));History.Reset(Project);Rebuild();if(notify)Changed(false);}
        public void CreateStressProject()
        {
            ExitPlay(false);Project=new CreaJuegoProjectData{projectName="Prueba de rendimiento",teamName="CreaJuego"};
            Seed("jugador",new Vector3(-9,-1));
            for(int i=0;i<20;i++)Seed("plataforma",new Vector3(-9+i%10*2,-2+i/10*3));
            for(int i=0;i<10;i++)Seed("decoracion",new Vector3(-8+i*1.8f,2.5f));
            for(int i=0;i<5;i++)Seed("premio",new Vector3(-7+i*3,.2f));
            for(int i=0;i<3;i++)Seed("peligro",new Vector3(-3+i*3,-1.4f));
            for(int i=0;i<3;i++)Seed("enemigo",new Vector3(-5+i*5,-1.1f));
            Seed("meta",new Vector3(9,-1));
            History.Reset(Project);Rebuild();Changed(false);Debug.Log($"CREAJUEGO_WEB_STRESS_READY objects={Project.objects.Count}");
        }
        void Seed(string id,Vector3 position){var definition=Find(id);if(definition==null||definition.prefab==null)return;var data=RuntimeItemData.From(definition.prefab.GetComponent<GameItem>());data.definitionId=id;data.position=position;Project.objects.Add(data);}
        public GameItem Create(string id,Vector3 position)
        {
            if(Mode!=AuthoringMode.Build)return null;var definition=Find(id);if(definition==null||definition.prefab==null)return null;
            if(!definition.allowMultiple&&Project.objects.Any(o=>o.definitionId==id)){ui?.SetStatus("Este juego usa un solo "+definition.displayName+".");return null;}
            var data=RuntimeItemData.From(definition.prefab.GetComponent<GameItem>());data.definitionId=id;data.position=position;Project.objects.Add(data);History.Record(Project);Rebuild(data.instanceId);Changed();return Selection.SelectedItem;
        }
        public void DeleteSelected(){var id=SelectedId();if(id==null)return;Project.objects.RemoveAll(o=>o.instanceId==id);History.Record(Project);Rebuild();Changed();}
        public void DuplicateSelected(){var source=SelectedData();if(source==null)return;var definition=Find(source.definitionId);if(definition==null||!definition.allowMultiple)return;var copy=source.Clone();copy.instanceId=Guid.NewGuid().ToString("N");copy.position+=new Vector3(1,.5f);Project.objects.Add(copy);History.Record(Project);Rebuild(copy.instanceId);Changed();}
        public void MoveSelected(Vector3 position,bool record){var data=SelectedData();if(data==null)return;position.z=data.position.z;data.position=position;if(Selection.SelectedItem!=null)Selection.SelectedItem.transform.position=position;if(record){History.Record(Project);Changed();}else MarkDirty();}
        public void SetFloat(string path,float value){var d=SelectedData();if(d==null)return;switch(path){case "speed":d.speed=value;break;case "jump":d.jump=value;break;case "health":d.health=Mathf.RoundToInt(value);break;case "points":d.points=Mathf.RoundToInt(value);break;case "damage":d.damage=Mathf.RoundToInt(value);break;}History.Record(Project);Rebuild(d.instanceId);Changed();}
        public void SetMessage(string value){var d=SelectedData();if(d==null)return;d.message=value;History.Record(Project);Rebuild(d.instanceId);Changed();}
        public void Undo(){var value=History.Undo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void Redo(){var value=History.Redo();if(value!=null){Project=value;Rebuild();Changed();}}
        public void SaveNow(){storage.Save(ProjectSerializer.ToJson(Project));dirty=false;ui?.SetSaveState("Guardado ✓");}
        public void LoadLast(){if(!storage.Exists){ui?.SetStatus("Todavía no hay un proyecto guardado.");return;}ExitPlay(false);Project=ProjectSerializer.FromJson(storage.Load());History.Reset(Project);Rebuild();Changed(false);ui?.SetStatus("Proyecto abierto.");}
        public string EnterPlay()
        {
            if(Mode==AuthoringMode.Play)return null;var error=RuntimePreflight.Validate(Project,Find);if(error!=null){ui?.SetStatus(error);return error;}
            Selection.Clear();buildRoot.gameObject.SetActive(false);services=sceneServicesPrefab!=null?Instantiate(sceneServicesPrefab):null;if(services!=null){foreach(var camera in services.GetComponentsInChildren<Camera>(true))camera.enabled=false;foreach(var listener in services.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;}playRoot=new GameObject("Partida temporal").transform;playRoot.gameObject.SetActive(false);
            foreach(var data in Project.objects){var item=InstantiateItem(data,playRoot,false);item.gameObject.SetActive(true);}
            playRoot.gameObject.SetActive(true);Mode=AuthoringMode.Play;Debug.Log($"CREAJUEGO_WEB_MODE PLAY objects={Project.objects.Count}");if(buildCamera!=null)buildCamera.enabled=false;if(gameCamera!=null)gameCamera.enabled=true;ui?.Refresh();return null;
        }
        public void ExitPlay(bool rebuild=true)
        {
            if(Mode!=AuthoringMode.Play)return;if(playRoot!=null){playRoot.gameObject.SetActive(false);DestroyImmediateSafe(playRoot.gameObject);}if(services!=null){services.SetActive(false);DestroyImmediateSafe(services);}
            Mode=AuthoringMode.Build;Debug.Log($"CREAJUEGO_WEB_MODE BUILD objects={Project.objects.Count}");if(gameCamera!=null)gameCamera.enabled=false;if(buildCamera!=null)buildCamera.enabled=true;if(buildRoot!=null)buildRoot.gameObject.SetActive(true);if(rebuild)Rebuild();ui?.Refresh();
        }
        public void PickImage(){if(SelectedData()==null)return;if(!RuntimeImageImport.Pick(gameObject.name))ui?.SetStatus("La selección de archivos se prueba dentro de la build WebGL.");}
        public void ReceiveImageDataUrl(string value){var data=SelectedData();if(data==null)return;data.customImageBase64=value;History.Record(Project);Rebuild(data.instanceId);Changed();}
        public void ToggleMode(){if(Mode==AuthoringMode.Build)EnterPlay();else ExitPlay();}
        public void Rebuild(string selectId=null)
        {
            if(buildRoot==null)return;buildRoot.gameObject.SetActive(false);Selection.Clear();foreach(var texture in textures)if(texture!=null)Destroy(texture);textures.Clear();
            for(int i=buildRoot.childCount-1;i>=0;i--){buildRoot.GetChild(i).gameObject.SetActive(false);DestroyImmediateSafe(buildRoot.GetChild(i).gameObject);}
            GameItem selected=null;foreach(var data in Project.objects){var item=InstantiateItem(data,buildRoot,true);var marker=item.gameObject.AddComponent<RuntimeAuthoredItem>();marker.instanceId=data.instanceId;if(data.instanceId==selectId)selected=item;}
            buildRoot.gameObject.SetActive(true);foreach(var behaviour in buildRoot.GetComponentsInChildren<MonoBehaviour>())if(!(behaviour is GameItem)&&!(behaviour is RuntimeAuthoredItem))behaviour.enabled=false;foreach(var body in buildRoot.GetComponentsInChildren<Rigidbody2D>())body.simulated=false;
            if(selected!=null)Selection.Select(selected.gameObject);ProjectChanged?.Invoke();ui?.Refresh();
        }
        GameItem InstantiateItem(RuntimeItemData data,Transform parent,bool authoring)
        {
            var definition=Find(data.definitionId);var go=Instantiate(definition.prefab,parent);go.name=definition.displayName;go.SetActive(false);var item=go.GetComponent<GameItem>();item.definition=definition;data.Apply(item);
            if(authoring){foreach(var behaviour in go.GetComponents<MonoBehaviour>())if(!(behaviour is GameItem)&&!(behaviour is ItemVisual))behaviour.enabled=false;foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;}
            go.SetActive(true);if(!string.IsNullOrEmpty(data.customImageBase64)){var sprite=RuntimeImageImport.DecodeDataUrl(data.customImageBase64,out var texture);if(sprite!=null){textures.Add(texture);item.customSprite=sprite;var visual=item.GetComponent<ItemVisual>();if(visual!=null)visual.Apply();}}if(authoring){foreach(var body in go.GetComponents<Rigidbody2D>())body.simulated=false;}return item;
        }
        static void DestroyImmediateSafe(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        public string SelectedId()=>Selection.SelectedItem!=null?Selection.SelectedItem.GetComponent<RuntimeAuthoredItem>()?.instanceId:null;
        public RuntimeItemData SelectedData(){var id=SelectedId();return id==null?null:Project.objects.FirstOrDefault(o=>o.instanceId==id);}
        void Changed(bool historyAlready=true){MarkDirty();ProjectChanged?.Invoke();ui?.Refresh();}
        void MarkDirty(){dirty=true;saveAt=Time.unscaledTime+1;ui?.SetSaveState("Guardando…");}
        void OnApplicationQuit(){if(dirty)SaveNow();}
    }
    public sealed class RuntimeAuthoredItem:MonoBehaviour{public string instanceId;}
    public sealed class RuntimePerformanceProbe:MonoBehaviour
    {
        RuntimeAuthoringController controller;float started;int frames;
        void Awake(){controller=GetComponent<RuntimeAuthoringController>();started=Time.realtimeSinceStartup;}
        void Update()
        {
            frames++;var elapsed=Time.realtimeSinceStartup-started;if(elapsed<5)return;
            var fps=frames/Mathf.Max(elapsed,.001f);var memory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            Debug.Log($"CREAJUEGO_WEB_PERF fps={fps:0.0} memoryBytes={memory} objects={(controller!=null?controller.Project.objects.Count:0)} mode={(controller!=null?controller.Mode.ToString():"Unknown")}");
            frames=0;started=Time.realtimeSinceStartup;
        }
    }}