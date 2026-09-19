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
        RuntimeAuthoringController c;Font font;Transform catalog,list,properties,editTop;Text status,save;Button modeButton;Toggle snap;Action<GameItem> selectionChanged;bool worldSelected,listInitialized;string appearanceSearch="";ItemKind? searchKind;
        static readonly Color Panel=new Color(.07f,.09f,.14f,.96f),Blue=new Color(.12f,.36f,.72f),Selected=new Color(.12f,.58f,.78f);
        public int VisibleItemRowCount=>list==null?0:list.Cast<Transform>().Count(row=>row.gameObject.activeSelf);
        public string VisiblePropertiesText=>properties==null?"":string.Join("\n",properties.GetComponentsInChildren<Text>(false).Select(t=>t.text));
        void Start(){c=GetComponent<RuntimeAuthoringController>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Build();selectionChanged=OnSelection;c.Selection.SelectionChanged+=selectionChanged;c.ProjectChanged+=Refresh;Refresh();}
        void OnDestroy(){if(c!=null){if(selectionChanged!=null)c.Selection.SelectionChanged-=selectionChanged;c.ProjectChanged-=Refresh;}}
        void OnSelection(GameItem item){if(item!=null)worldSelected=false;if(item!=null&&item.definition!=null&&searchKind!=item.definition.kind){appearanceSearch="";searchKind=item.definition.kind;}Refresh();}
        public void Build()
        {
            var canvas=New("CreaJuego Web",transform).AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);canvas.gameObject.AddComponent<GraphicRaycaster>();
            if(EventSystem.current==null){var events=New("Eventos",transform);events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();}
            var top=PanelRect("Barra",canvas.transform,new Vector2(0,660),new Vector2(1280,60));Label(top,"CreaJuego Web",23,new Vector2(12,8),new Vector2(200,44));editTop=New("Herramientas",top).transform;
            ButtonAt(editTop,"Nuevo",new Vector2(205,10),()=>c.NewProject(),new Vector2(75,40));ButtonAt(editTop,"Guardar",new Vector2(285,10),c.SaveNow,new Vector2(82,40));ButtonAt(editTop,"Abrir",new Vector2(372,10),c.LoadLast,new Vector2(70,40));ButtonAt(editTop,"↶",new Vector2(447,10),c.Undo,new Vector2(42,40));ButtonAt(editTop,"↷",new Vector2(494,10),c.Redo,new Vector2(42,40));
            ButtonAt(editTop,"Ver todo",new Vector2(545,10),c.FrameAll,new Vector2(90,40));ButtonAt(editTop,"Encontrar",new Vector2(640,10),c.FrameSelected,new Vector2(92,40));ButtonAt(editTop,"Nivel",new Vector2(737,10),()=>{worldSelected=true;c.Selection.Clear();Refresh();},new Vector2(70,40));
            snap=ToggleAt(editTop,"Alinear automáticamente",new Vector2(817,10),value=>c.SetSnap(value),new Vector2(235,40));save=Label(top,"",13,new Vector2(900,8),new Vector2(170,44));modeButton=ButtonAt(top,"JUGAR",new Vector2(1080,8),c.ToggleMode,new Vector2(185,44));
            var left=PanelRect("Añadir y lista",canvas.transform,new Vector2(0,45),new Vector2(230,615));Label(left,"AÑADIR AL JUEGO",17,new Vector2(12,570),new Vector2(210,32));catalog=New("Catálogo",left).transform;Rect(catalog).anchoredPosition=new Vector2(10,300);Rect(catalog).sizeDelta=new Vector2(210,270);
            Label(left,"MI JUEGO",17,new Vector2(12,263),new Vector2(200,30));list=Scroll("Mi juego",left,new Vector2(10,48),new Vector2(210,215));ButtonAt(left,"Duplicar",new Vector2(10,7),c.DuplicateSelected,new Vector2(98,34));ButtonAt(left,"Eliminar",new Vector2(112,7),c.DeleteSelected,new Vector2(98,34));
            var right=PanelRect("Propiedades",canvas.transform,new Vector2(1000,45),new Vector2(280,615));Label(right,"PROPIEDADES",20,new Vector2(12,570),new Vector2(255,32));properties=Scroll("Controles",right,new Vector2(10,15),new Vector2(260,550));
            var bottom=PanelRect("Estado",canvas.transform,new Vector2(0,0),new Vector2(1280,45));status=Label(bottom,"Construye tu juego.",15,new Vector2(16,5),new Vector2(1248,35));
        }
        public void Refresh()
        {
            if(c==null||catalog==null)return;float listScroll=listInitialized?list.parent.GetComponent<ScrollRect>().verticalNormalizedPosition:1;Clear(catalog);Clear(list);Clear(properties);
            var defs=(c.contentPack!=null?c.contentPack.definitions:c.definitions).Where(d=>d!=null&&d.availableInWorkshop&&d.kind!=ItemKind.MovingPlatform&&d.kind!=ItemKind.Background).OrderBy(d=>d.order).ToArray();int y=260;
            foreach(var d in defs){var captured=d;ButtonAt(catalog,"+ "+d.displayName,new Vector2(0,y-=36),()=>c.Create(captured.id,BuildCenter()),new Vector2(205,32));}
            int row=0;foreach(var data in c.Project.objects){var captured=data;var def=c.Find(data.definitionId);var button=ButtonAt(list,def!=null?def.displayName:data.definitionId,new Vector2(0,row++*-31),()=>Select(captured.instanceId),new Vector2(195,29));button.gameObject.AddComponent<RuntimeItemRow>().instanceId=data.instanceId;}ResizeContent(list,row*31);list.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=listScroll;listInitialized=true;
            int py=0;if(worldSelected)WorldProperties(ref py);else{var item=c.Selection.SelectedItem;var data=c.SelectedData();if(item==null||data==null||item.definition==null){Label(properties,"Selecciona algo de Mi juego o del nivel para cambiar sus propiedades.",15,new Vector2(4,-py),new Vector2(245,62));py+=68;}else{Label(properties,item.definition.displayName.ToUpperInvariant(),19,new Vector2(4,-py),new Vector2(245,34));py+=38;TryItemProperties(item.definition.kind,data,ref py);TryAppearance(item.definition.kind,data,ref py);}}
            ResizeContent(properties,Mathf.Max(550,py+20));bool build=c.Mode==AuthoringMode.Build;modeButton.GetComponentInChildren<Text>().text=build?"JUGAR":"VOLVER A CONSTRUIR";editTop.gameObject.SetActive(build);catalog.parent.gameObject.SetActive(build);properties.parent.parent.gameObject.SetActive(build);snap.SetIsOnWithoutNotify(c.Project.alignAutomatically);
        }
        void WorldProperties(ref int y)
        {
            Label(properties,"NIVEL",19,new Vector2(4,-y),new Vector2(245,32));y+=38;Label(properties,"Tamaño del nivel",15,new Vector2(4,-y),new Vector2(245,28));y+=32;
            foreach(RuntimeLevelSize size in Enum.GetValues(typeof(RuntimeLevelSize))){var captured=size;string name=size==RuntimeLevelSize.Small?"Pequeño":size==RuntimeLevelSize.Medium?"Mediano":"Grande";var button=ButtonAt(properties,name,new Vector2(4,-y),()=>c.SetLevelSize(captured),new Vector2(240,34));button.GetComponent<Image>().color=c.Project.levelSize==size?Selected:Blue;y+=39;}
            Label(properties,"El marco azul muestra la zona válida. Si el personaje cae por debajo, vuelve al inicio.",14,new Vector2(4,-y),new Vector2(240,70));y+=75;
        }
        void ItemProperties(ItemKind kind,RuntimeItemData data,ref int y)
        {
            if(kind==ItemKind.Player){Slider("Velocidad","speed",data.speed,.2f,5,ref y);Slider("Fuerza de salto","jump",data.jump,5,18,ref y);Slider("Puntos de vida","health",data.health,1,10,ref y);}
            else if(kind==ItemKind.Platform)Slider("Ancho","width",data.platformWidth,.5f,20,ref y);
            else if(kind==ItemKind.Prize)Slider("Puntos","points",data.points,1,100,ref y);
            else if(kind==ItemKind.Hazard)Slider("Daño","damage",data.damage,1,10,ref y);
            else if(kind==ItemKind.Enemy){Slider("Vida","health",data.health,1,10,ref y);Slider("Daño","damage",data.damage,1,10,ref y);}
            else if(kind==ItemKind.Goal)Input("Mensaje",data.message,ref y);
            else{Label(properties,"Arrastra para mover este elemento.",14,new Vector2(4,-y),new Vector2(240,38));y+=42;}
        }
        void Appearance(ItemKind kind,RuntimeItemData data,ref int y)
        {
            Label(properties,"APARIENCIA",17,new Vector2(4,-y),new Vector2(240,30));y+=34;var category=c.contentPack?.CategoryFor(kind);if(category==null){Label(properties,"Este elemento usa su apariencia preparada.",13,new Vector2(4,-y),new Vector2(240,36));y+=42;}
            else
            {
                if(category.options.Count>8){Search(data,ref y);}
                var options=c.contentPack.OptionsFor(kind,appearanceSearch);foreach(var option in options.Take(24)){var captured=option;var button=ButtonAt(properties,option.displayName,new Vector2(4,-y),()=>c.SetAppearance(captured.id),new Vector2(240,43),option.Preview);button.GetComponent<Image>().color=data.appearanceId==option.id&&string.IsNullOrEmpty(data.customImageBase64)?Selected:Blue;y+=47;}
                if(options.Length>24){Label(properties,"Refina la búsqueda para ver más opciones.",12,new Vector2(4,-y),new Vector2(240,34));y+=38;}
            }
            var custom=ButtonAt(properties,"Elegir imagen…",new Vector2(4,-y),c.PickImage,new Vector2(240,38));if(!string.IsNullOrEmpty(data.customImageBase64))custom.GetComponent<Image>().color=Selected;y+=44;Label(properties,"PNG o JPG · máximo 2 MB y 2048 × 2048",11,new Vector2(4,-y),new Vector2(240,30));y+=34;
        }
        void Search(RuntimeItemData data,ref int y)
        {
            var go=New("Buscar apariencias",properties);go.AddComponent<Image>().color=Color.white;var input=go.AddComponent<InputField>();var text=Label(go.transform,appearanceSearch,13,new Vector2(8,0),new Vector2(220,32));text.color=Color.black;input.textComponent=text;input.text=appearanceSearch;input.onEndEdit.AddListener(value=>{appearanceSearch=value;Refresh();});Rect(go.transform).anchoredPosition=new Vector2(4,-y);Rect(go.transform).sizeDelta=new Vector2(240,32);y+=38;
        }
        void Slider(string label,string path,float value,float min,float max,ref int y)
        {
            Label(properties,label+"  "+value.ToString("0.#"),14,new Vector2(4,-y),new Vector2(240,23));y+=25;var go=New("Slider "+label,properties);var image=go.AddComponent<Image>();image.color=new Color(.2f,.25f,.35f);var slider=go.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;slider.wholeNumbers=path=="health"||path=="damage"||path=="points";var fill=New("Fill",go.transform).AddComponent<Image>();fill.color=new Color(.15f,.55f,1);slider.fillRect=Rect(fill.transform);Anchor(slider.fillRect);var handle=New("Handle",go.transform).AddComponent<Image>();handle.color=Color.white;slider.handleRect=Rect(handle.transform);slider.handleRect.sizeDelta=new Vector2(15,25);Rect(go.transform).anchoredPosition=new Vector2(4,-y);Rect(go.transform).sizeDelta=new Vector2(240,20);slider.onValueChanged.AddListener(v=>c.SetFloat(path,v,false));go.AddComponent<RuntimeSliderCommit>().commit=c.CommitEdit;y+=32;
        }
        void Input(string label,string value,ref int y){Label(properties,label,14,new Vector2(4,-y),new Vector2(240,24));y+=27;var go=New("Mensaje",properties);go.AddComponent<Image>().color=Color.white;var input=go.AddComponent<InputField>();var text=Label(go.transform,value,13,new Vector2(6,2),new Vector2(225,52));text.color=Color.black;text.alignment=TextAnchor.UpperLeft;input.textComponent=text;input.text=value;Rect(go.transform).anchoredPosition=new Vector2(4,-y);Rect(go.transform).sizeDelta=new Vector2(240,56);input.onEndEdit.AddListener(c.SetMessage);y+=63;}
        public void SelectInstance(string id)=>c.Selection.Select(id);
        public bool ClickItemRow(string id){var row=list?.GetComponentsInChildren<RuntimeItemRow>(false).FirstOrDefault(r=>r.instanceId==id);if(row==null)return false;row.GetComponent<Button>().onClick.Invoke();return true;}
        void Select(string id)=>SelectInstance(id);
        Vector3 BuildCenter(){var point=c.buildCamera.ViewportToWorldPoint(new Vector3(.5f,.5f,-c.buildCamera.transform.position.z));point.z=0;return point;}
        public void SetStatus(string value){if(status!=null)status.text=value;}public void SetSaveState(string value){if(save!=null)save.text=value;}
        Transform Scroll(string name,Transform parent,Vector2 pos,Vector2 size){var viewport=New(name,parent);viewport.AddComponent<Image>().color=new Color(0,0,0,.12f);viewport.AddComponent<Mask>().showMaskGraphic=true;Rect(viewport.transform).anchoredPosition=pos;Rect(viewport.transform).sizeDelta=size;var content=New("Contenido",viewport.transform).transform;Rect(content).anchorMin=new Vector2(0,1);Rect(content).anchorMax=new Vector2(1,1);Rect(content).pivot=new Vector2(0,1);Rect(content).anchoredPosition=Vector2.zero;var scroll=viewport.AddComponent<ScrollRect>();scroll.content=Rect(content);scroll.viewport=Rect(viewport.transform);scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=24;scroll.verticalNormalizedPosition=1;return content;}
        void ResizeContent(Transform content,float height){Rect(content).sizeDelta=new Vector2(0,Mathf.Max(height,content.parent.GetComponent<RectTransform>().rect.height));}
        Transform PanelRect(string name,Transform parent,Vector2 pos,Vector2 size){var go=New(name,parent);go.AddComponent<Image>().color=Panel;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size;return go.transform;}
        Button ButtonAt(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction action,Vector2? size=null,Sprite icon=null){var go=New(text,parent);go.AddComponent<Image>().color=Blue;var button=go.AddComponent<Button>();button.onClick.AddListener(action);Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size??new Vector2(95,40);int inset=icon!=null?45:0;if(icon!=null){var image=New("Miniatura",go.transform).AddComponent<Image>();image.sprite=icon;image.preserveAspect=true;Rect(image.transform).anchoredPosition=new Vector2(4,4);Rect(image.transform).sizeDelta=new Vector2(35,(size??new Vector2(95,40)).y-8);}var label=Label(go.transform,text,14,new Vector2(inset,0),new Vector2((size??new Vector2(95,40)).x-inset,(size??new Vector2(95,40)).y));label.alignment=TextAnchor.MiddleCenter;return button;}
        Toggle ToggleAt(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction<bool> action,Vector2 size){var go=New(text,parent);Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size;var toggle=go.AddComponent<Toggle>();var bg=New("Fondo",go.transform).AddComponent<Image>();bg.color=Color.white;Rect(bg.transform).anchoredPosition=new Vector2(0,8);Rect(bg.transform).sizeDelta=new Vector2(24,24);var check=New("Check",bg.transform).AddComponent<Image>();check.color=new Color(.1f,.55f,1);Anchor(Rect(check.transform));Rect(check.transform).offsetMin=Vector2.one*4;Rect(check.transform).offsetMax=Vector2.one*-4;toggle.targetGraphic=bg;toggle.graphic=check;var label=Label(go.transform,text,13,new Vector2(30,0),new Vector2(size.x-30,size.y));toggle.onValueChanged.AddListener(action);return toggle;}
        Text Label(Transform parent,string value,int size,Vector2 pos,Vector2 dimensions){var go=New("Texto",parent);var text=go.AddComponent<Text>();text.raycastTarget=false;text.font=font;text.fontSize=size;text.text=value;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=dimensions;return text;}
        static GameObject New(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;var top=parent!=null&&parent.name=="Contenido"?new Vector2(0,1):Vector2.zero;rect.anchorMin=rect.anchorMax=rect.pivot=top;return go;}
        static RectTransform Rect(Transform transform)=>(RectTransform)transform;static void Anchor(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}static void Clear(Transform transform){for(int i=transform.childCount-1;i>=0;i--){var child=transform.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}}
        void TryItemProperties(ItemKind kind,RuntimeItemData data,ref int y){try{ItemProperties(kind,data,ref y);}catch(Exception exception){Debug.LogException(exception);Label(properties,"No se pudieron mostrar algunos ajustes.",13,new Vector2(4,-y),new Vector2(240,40));y+=44;}}
        void TryAppearance(ItemKind kind,RuntimeItemData data,ref int y){try{Appearance(kind,data,ref y);}catch(Exception exception){Debug.LogException(exception);Label(properties,"No se pudo mostrar Apariencia.",13,new Vector2(4,-y),new Vector2(240,40));y+=44;}}
    }
    public sealed class RuntimeItemRow:MonoBehaviour{public string instanceId;}
    public sealed class RuntimeSliderCommit:MonoBehaviour,IPointerUpHandler{public Action commit;public void OnPointerUp(PointerEventData eventData)=>commit?.Invoke();}
}