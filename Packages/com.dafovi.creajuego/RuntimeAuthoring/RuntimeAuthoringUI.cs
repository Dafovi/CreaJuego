using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace CreaJuego.Web
{
    public sealed class RuntimeAuthoringUI:MonoBehaviour
    {
        RuntimeAuthoringController c;Font font;Transform catalog,list,properties;Text status,save,modeTitle;Button modeButton;Action<GameItem> selectionChanged;static readonly Color Panel=new Color(.07f,.09f,.14f,.96f);
        void Start(){c=GetComponent<RuntimeAuthoringController>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Build();selectionChanged=_=>Refresh();c.Selection.SelectionChanged+=selectionChanged;c.ProjectChanged+=Refresh;Refresh();}
        void OnDestroy(){if(c!=null){if(selectionChanged!=null)c.Selection.SelectionChanged-=selectionChanged;c.ProjectChanged-=Refresh;}}
        public void Build()
        {
            var canvas=New("CreaJuego Web",transform).AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1280,720);canvas.gameObject.AddComponent<GraphicRaycaster>();
            if(EventSystem.current==null){var events=New("Eventos",transform);events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();}
            var top=PanelRect("Barra",canvas.transform,new Vector2(0,660),new Vector2(1280,60));Label(top,"CreaJuego Web",24,new Vector2(15,8),new Vector2(210,44));
            ButtonAt(top,"Nuevo",new Vector2(220,10),()=>c.NewProject());ButtonAt(top,"Guardar",new Vector2(315,10),c.SaveNow);ButtonAt(top,"Abrir",new Vector2(420,10),c.LoadLast);
            ButtonAt(top,"↶",new Vector2(505,10),c.Undo,new Vector2(45,40));ButtonAt(top,"↷",new Vector2(555,10),c.Redo,new Vector2(45,40));save=Label(top,"",14,new Vector2(610,10),new Vector2(170,40));
            modeButton=ButtonAt(top,"JUGAR",new Vector2(1080,8),c.ToggleMode,new Vector2(180,44));
            var left=PanelRect("Añadir y lista",canvas.transform,new Vector2(0,45),new Vector2(230,615));Label(left,"AÑADIR AL JUEGO",17,new Vector2(12,565),new Vector2(210,35));
            catalog=New("Catálogo",left).transform;Rect(catalog).anchoredPosition=new Vector2(10,255);Rect(catalog).sizeDelta=new Vector2(210,315);
            Label(left,"MI JUEGO",17,new Vector2(12,225),new Vector2(200,30));list=New("Mi juego",left).transform;Rect(list).anchoredPosition=new Vector2(10,40);Rect(list).sizeDelta=new Vector2(210,185);
            ButtonAt(left,"Duplicar",new Vector2(10,3),c.DuplicateSelected,new Vector2(98,34));ButtonAt(left,"Eliminar",new Vector2(112,3),c.DeleteSelected,new Vector2(98,34));
            var right=PanelRect("Propiedades",canvas.transform,new Vector2(1020,45),new Vector2(260,615));Label(right,"PROPIEDADES",20,new Vector2(12,565),new Vector2(235,35));properties=New("Controles",right).transform;Rect(properties).anchoredPosition=new Vector2(10,70);Rect(properties).sizeDelta=new Vector2(240,495);
            var bottom=PanelRect("Estado",canvas.transform,new Vector2(0,0),new Vector2(1280,45));status=Label(bottom,"Construye tu juego.",15,new Vector2(16,5),new Vector2(1248,35));
        }
        public void Refresh()
        {
            if(c==null||catalog==null)return;Clear(catalog);Clear(list);Clear(properties);
            var defs=c.definitions.Where(d=>d!=null&&d.availableInWorkshop&&d.kind!=ItemKind.MovingPlatform&&d.kind!=ItemKind.Background).OrderBy(d=>d.order).ToArray();
            int y=280;foreach(var d in defs){var captured=d;ButtonAt(catalog,"+ "+d.displayName,new Vector2(0,y-=38),()=>c.Create(captured.id,BuildCenter()),new Vector2(205,34));}
            y=180;foreach(var d in c.Project.objects){var captured=d;var def=c.Find(d.definitionId);ButtonAt(list,def!=null?def.displayName:d.definitionId,new Vector2(0,y-=32),()=>Select(captured.instanceId),new Vector2(205,29));}
            var item=c.Selection.SelectedItem;var data=c.SelectedData();y=450;if(item==null||data==null)Label(properties,"Elige un elemento.",16,new Vector2(5,y),new Vector2(220,35));else{Label(properties,item.definition.displayName.ToUpperInvariant(),19,new Vector2(5,y),new Vector2(220,35));Properties(item.definition.kind,data,ref y);}
            bool build=c.Mode==AuthoringMode.Build;modeButton.GetComponentInChildren<Text>().text=build?"JUGAR":"VOLVER A CONSTRUIR";modeTitle=modeButton.GetComponentInChildren<Text>();catalog.parent.gameObject.SetActive(build);properties.parent.gameObject.SetActive(build);
        }
        void Properties(ItemKind kind,RuntimeItemData d,ref int y)
        {
            if(kind==ItemKind.Player){Slider("Velocidad","speed",d.speed,.2f,5,ref y);Slider("Salto","jump",d.jump,5,18,ref y);Slider("Puntos de vida","health",d.health,1,10,ref y);}
            else if(kind==ItemKind.Prize)Slider("Puntos","points",d.points,1,100,ref y);
            else if(kind==ItemKind.Hazard)Slider("Daño","damage",d.damage,1,10,ref y);
            else if(kind==ItemKind.Enemy){Slider("Vida","health",d.health,1,10,ref y);Slider("Daño","damage",d.damage,1,10,ref y);}
            else if(kind==ItemKind.Goal)Input("Mensaje",d.message,ref y);
            else Label(properties,kind==ItemKind.Platform?"Arrastra para cambiar su posición.":"Arrastra para mover este elemento.",14,new Vector2(5,y-=55),new Vector2(225,50));
            ButtonAt(properties,"Imagen del computador",new Vector2(5,y-=50),c.PickImage,new Vector2(220,36));
        }
        void Slider(string label,string path,float value,float min,float max,ref int y){Label(properties,label+"  "+value.ToString("0.#"),14,new Vector2(5,y-=45),new Vector2(225,22));var go=New("Slider "+label,properties);var s=go.AddComponent<Slider>();s.minValue=min;s.maxValue=max;s.value=value;s.wholeNumbers=path=="health"||path=="damage"||path=="points";Rect(go.transform).anchoredPosition=new Vector2(5,y-=25);Rect(go.transform).sizeDelta=new Vector2(220,20);var bg=go.AddComponent<Image>();bg.color=new Color(.2f,.25f,.35f);var fill=New("Fill",go.transform).AddComponent<Image>();fill.color=new Color(.15f,.55f,1);s.fillRect=Rect(fill.transform);Anchor(s.fillRect);var handle=New("Handle",go.transform).AddComponent<Image>();handle.color=Color.white;s.handleRect=Rect(handle.transform);s.handleRect.sizeDelta=new Vector2(15,25);s.onValueChanged.AddListener(v=>c.SetFloat(path,v));}
        void Input(string label,string value,ref int y){Label(properties,label,14,new Vector2(5,y-=35),new Vector2(225,25));var go=New("Mensaje",properties);var image=go.AddComponent<Image>();image.color=Color.white;var input=go.AddComponent<InputField>();var text=Label(go.transform,value,14,new Vector2(6,3),new Vector2(208,55));text.color=Color.black;text.alignment=TextAnchor.UpperLeft;input.textComponent=text;input.text=value;Rect(go.transform).anchoredPosition=new Vector2(5,y-=65);Rect(go.transform).sizeDelta=new Vector2(220,60);input.onEndEdit.AddListener(c.SetMessage);}
        void Select(string id){var marker=FindObjectsByType<RuntimeAuthoredItem>(FindObjectsInactive.Exclude).FirstOrDefault(m=>m.instanceId==id);c.Selection.Select(marker!=null?marker.gameObject:null);}
        Vector3 BuildCenter(){var camera=c.buildCamera;var point=camera.ViewportToWorldPoint(new Vector3(.5f,.5f,-camera.transform.position.z));point.z=0;return point;}
        public void SetStatus(string value){if(status!=null)status.text=value;}public void SetSaveState(string value){if(save!=null)save.text=value;}
        Transform PanelRect(string name,Transform parent,Vector2 pos,Vector2 size){var go=New(name,parent);go.AddComponent<Image>().color=Panel;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size;return go.transform;}
        Button ButtonAt(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction action,Vector2? size=null){var go=New(text,parent);go.AddComponent<Image>().color=new Color(.12f,.36f,.72f);var b=go.AddComponent<Button>();b.onClick.AddListener(action);Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size??new Vector2(95,40);var l=Label(go.transform,text,14,Vector2.zero,Rect(go.transform).sizeDelta);l.alignment=TextAnchor.MiddleCenter;return b;}
        Text Label(Transform parent,string value,int size,Vector2 pos,Vector2 dimensions){var go=New("Texto",parent);var t=go.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleLeft;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=dimensions;return t;}
        static GameObject New(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;return go;}static RectTransform Rect(Transform t)=>(RectTransform)t;static void Anchor(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}static void Clear(Transform t){for(int i=t.childCount-1;i>=0;i--)Destroy(t.GetChild(i).gameObject);}
    }
}