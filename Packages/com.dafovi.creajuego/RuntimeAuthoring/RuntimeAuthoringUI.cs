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
        [SerializeField] Canvas canvas;
        [SerializeField] Transform catalog,list,properties,editTop,top,hearts;
        [SerializeField] Text status,save,flow,readinessTitle,readinessChecks,brandTitle,brandTagline;
        [SerializeField] Button newButton,saveButton,openButton,undoButton,redoButton,frameAllButton,frameSelectedButton,levelButton,duplicateButton,deleteButton,modeButton,returnButton;
        [SerializeField] Toggle snap;
        [SerializeField] GameObject leftPanel,rightPanel,readinessPanel,brandPanel,gameplayHudPanel;
        [SerializeField] Text hudObjective,hudScore,hudEnemies;
        [SerializeField, Tooltip("Recalcula paneles y cámara según la resolución. Déjalo desactivado para respetar el layout guardado en el Canvas.")] bool adaptLayoutAtRuntime;
        RuntimeAuthoringController c;Font font;CanvasScaler scaler;Action<GameItem> selectionChanged;bool worldSelected,listInitialized;string appearanceSearch="",statusOverride="";ItemKind? searchKind;int screenWidth,screenHeight,lastHudHealth=-1,lastHudScore=-1,lastHudEnemies=-1;

        static readonly Color Background=Hex("171D27"),Header=Hex("111822"),Panel=Hex("222B39"),Soft=Hex("273344"),Selected=Hex("304D73"),TextColor=Hex("EDF2FA"),Muted=Hex("AFBDD1"),Accent=Hex("80B4FF"),Border=Hex("45556C"),Success=Hex("08743F"),SuccessBright=Hex("48C78A"),Warning=Hex("3B3020"),WarningText=Hex("F2C778"),Danger=Hex("5A2731"),DangerText=Hex("FF8C9C");
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}

        public int VisibleItemRowCount=>list==null?0:list.Cast<Transform>().Count(row=>row.gameObject.activeSelf);
        public int VisibleCatalogIconCount=>catalog==null?0:catalog.GetComponentsInChildren<Image>(false).Count(image=>image.name=="Miniatura"&&image.sprite!=null);
        public int VisibleSceneItemIconCount=>list==null?0:list.GetComponentsInChildren<Image>(false).Count(image=>image.name=="Miniatura"&&image.sprite!=null);
        public Sprite VisibleSelectionIcon=>properties?.GetComponentsInChildren<Image>(false).FirstOrDefault(image=>image.name=="Miniatura")?.sprite;
        public string VisiblePropertiesText=>properties==null?"":string.Join("\n",properties.GetComponentsInChildren<Text>(false).Select(t=>t.text));
        public string BrandText=>(brandTitle?.text??"")+" "+(brandTagline?.text??"");
        public string FlowText=>flow!=null?flow.text:"";
        public string ReadinessText=>readinessTitle!=null?readinessTitle.text:"";
        public bool GameplayHudVisible=>gameplayHudPanel!=null&&gameplayHudPanel.activeSelf;
        public int GameplayHeartCount=>hearts==null?0:hearts.GetComponentsInChildren<Image>(false).Count(image=>image.sprite==RuntimeIconLibrary.Heart);
        public string GameplayHudText=>(hudObjective?.text??"")+" "+(hudScore?.text??"")+" "+(hudEnemies?.text??"")+" "+(hearts!=null?string.Join(" ",hearts.GetComponentsInChildren<Text>(false).Select(t=>t.text)):"");
        public bool HasPreparedLayout=>canvas!=null&&catalog!=null&&list!=null&&properties!=null&&status!=null&&modeButton!=null&&leftPanel!=null&&rightPanel!=null&&readinessPanel!=null;

        void Start(){Initialize();if(!HasPreparedLayout)Build();BindStaticActions();selectionChanged=OnSelection;c.Selection.SelectionChanged+=selectionChanged;c.ProjectChanged+=Refresh;Refresh();}
        void OnDestroy(){if(c!=null){if(selectionChanged!=null)c.Selection.SelectionChanged-=selectionChanged;c.ProjectChanged-=Refresh;}}
        void LateUpdate(){ApplyResponsiveLayout();UpdateGameplayHud();}
        void OnSelection(GameItem item){if(item!=null)worldSelected=false;if(item!=null&&item.definition!=null&&searchKind!=item.definition.kind){appearanceSearch="";searchKind=item.definition.kind;}Refresh();}
        void Initialize(){c=GetComponent<RuntimeAuthoringController>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(canvas!=null)scaler=canvas.GetComponent<CanvasScaler>();ResolveBrandLabels();ApplyBranding();}

        void ResolveBrandLabels()
        {
            if(brandPanel==null||brandTitle!=null&&brandTagline!=null)return;
            var labels=brandPanel.GetComponentsInChildren<Text>(true).OrderByDescending(label=>label.fontSize).ToArray();
            if(brandTitle==null)brandTitle=labels.FirstOrDefault();
            if(brandTagline==null)brandTagline=labels.FirstOrDefault(label=>label!=brandTitle);
        }

        void ApplyBranding()
        {
            ResolveBrandLabels();
            if(brandTitle!=null)brandTitle.text=!string.IsNullOrWhiteSpace(c?.contentPack?.productName)?c.contentPack.productName:CreaJuegoBranding.ProductName;
            if(brandTagline!=null)brandTagline.text=!string.IsNullOrWhiteSpace(c?.contentPack?.tagline)?c.contentPack.tagline:CreaJuegoBranding.Tagline;
        }

        public void PrepareEditableLayout()
        {
            Initialize();
            for(int i=transform.childCount-1;i>=0;i--){var child=transform.GetChild(i);if(child.GetComponent<Canvas>()!=null||child.GetComponent<EventSystem>()!=null)DestroyNow(child.gameObject);}
            Build();
        }

        public void Build()
        {
            var backdrop=New("CreaJuego Web UI",transform);
            canvas=backdrop.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            scaler=backdrop.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1f;backdrop.AddComponent<GraphicRaycaster>();Anchor(Rect(backdrop.transform));
            if(EventSystem.current==null){var events=New("Eventos",transform);events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();}

            top=PanelRect("Cabecera",canvas.transform,new Vector2(0,646),new Vector2(1280,74),Header,true);brandPanel=New("Marca",top);Rect(brandPanel.transform).anchoredPosition=new Vector2(14,8);Rect(brandPanel.transform).sizeDelta=new Vector2(300,58);
            var brandIcon=New("Icono CreaJuego",brandPanel.transform).AddComponent<Image>();brandIcon.sprite=BrandSprite();brandIcon.preserveAspect=true;brandIcon.raycastTarget=false;Rect(brandIcon.transform).anchoredPosition=new Vector2(0,6);Rect(brandIcon.transform).sizeDelta=new Vector2(46,46);
            brandTitle=Label(brandPanel.transform,CreaJuegoBranding.ProductName,25,new Vector2(58,22),new Vector2(220,30),TextColor,FontStyle.Bold);brandTagline=Label(brandPanel.transform,CreaJuegoBranding.Tagline,12,new Vector2(59,6),new Vector2(230,20),Muted);ApplyBranding();
            flow=Label(top,"",14,new Vector2(330,13),new Vector2(620,48),Muted);flow.alignment=TextAnchor.MiddleCenter;
            save=Label(top,"",12,new Vector2(930,15),new Vector2(135,44),Muted);save.alignment=TextAnchor.MiddleRight;
            returnButton=ButtonAt(top,"VOLVER A CONSTRUIR",new Vector2(1065,14),null,new Vector2(200,46),RuntimeIconLibrary.Back,Hex("075BD8"),TextColor,14);returnButton.gameObject.SetActive(false);

            leftPanel=PanelRect("Panel de elementos",canvas.transform,new Vector2(0,0),new Vector2(260,646),Panel,true).gameObject;
            Label(leftPanel.transform,"AÑADIR AL JUEGO",18,new Vector2(14,600),new Vector2(230,30),Accent,FontStyle.Bold);Label(leftPanel.transform,"Elige algo para añadir a tu juego.",12,new Vector2(14,578),new Vector2(230,22),Muted);
            catalog=Scroll("Catálogo",leftPanel.transform,new Vector2(10,270),new Vector2(240,304));
            Label(leftPanel.transform,"MI JUEGO",18,new Vector2(14,236),new Vector2(220,30),Accent,FontStyle.Bold);
            list=Scroll("Mi juego",leftPanel.transform,new Vector2(10,50),new Vector2(240,184));
            duplicateButton=ButtonAt(leftPanel.transform,"Duplicar",new Vector2(10,8),null,new Vector2(114,36),null,Soft,TextColor,13);
            deleteButton=ButtonAt(leftPanel.transform,"Eliminar",new Vector2(130,8),null,new Vector2(120,36),null,Danger,DangerText,13);

            editTop=PanelRect("Herramientas",canvas.transform,new Vector2(260,596),new Vector2(740,50),new Color(Header.r,Header.g,Header.b,.94f),true);
            newButton=ButtonAt(editTop,"Nuevo",new Vector2(8,7),null,new Vector2(67,36));saveButton=ButtonAt(editTop,"Guardar",new Vector2(79,7),null,new Vector2(74,36));openButton=ButtonAt(editTop,"Abrir",new Vector2(157,7),null,new Vector2(62,36));undoButton=ButtonAt(editTop,"",new Vector2(223,7),null,new Vector2(38,36),RuntimeIconLibrary.Undo);redoButton=ButtonAt(editTop,"",new Vector2(265,7),null,new Vector2(38,36),RuntimeIconLibrary.Redo);
            frameAllButton=ButtonAt(editTop,"Ver todo",new Vector2(307,7),null,new Vector2(82,36));frameSelectedButton=ButtonAt(editTop,"Encontrar",new Vector2(393,7),null,new Vector2(88,36));levelButton=ButtonAt(editTop,"Nivel",new Vector2(485,7),null,new Vector2(58,36));
            snap=ToggleAt(editTop,"Encajar en cuadrícula",new Vector2(552,8),null,new Vector2(180,34));

            rightPanel=PanelRect("Panel de propiedades",canvas.transform,new Vector2(1000,82),new Vector2(280,564),Panel,true).gameObject;
            Label(rightPanel.transform,"PROPIEDADES",20,new Vector2(14,520),new Vector2(250,32),Accent,FontStyle.Bold);properties=Scroll("Controles",rightPanel.transform,new Vector2(10,16),new Vector2(260,502));

            readinessPanel=PanelRect("Estado para jugar",canvas.transform,new Vector2(260,0),new Vector2(1020,82),Hex("183B30"),true).gameObject;
            var seal=PanelRect("Indicador",readinessPanel.transform,new Vector2(14,11),new Vector2(60,60),Success,true);var sealIcon=New("Listo",seal).AddComponent<Image>();sealIcon.sprite=RuntimeIconLibrary.Check;sealIcon.color=TextColor;sealIcon.preserveAspect=true;sealIcon.raycastTarget=false;Rect(sealIcon.transform).anchoredPosition=new Vector2(12,12);Rect(sealIcon.transform).sizeDelta=new Vector2(36,36);
            readinessTitle=Label(readinessPanel.transform,"",18,new Vector2(88,39),new Vector2(300,28),SuccessBright,FontStyle.Bold);
            status=Label(readinessPanel.transform,"Construye tu juego.",13,new Vector2(88,12),new Vector2(330,27),TextColor);
            readinessChecks=Label(readinessPanel.transform,"",12,new Vector2(420,13),new Vector2(390,54),TextColor);readinessChecks.alignment=TextAnchor.MiddleLeft;
            modeButton=ButtonAt(readinessPanel.transform,"JUGAR",new Vector2(820,15),null,new Vector2(185,52),RuntimeIconLibrary.Play,Success,TextColor,18);
            BuildGameplayHud();ApplyResponsiveLayout(true);
        }

        void BindStaticActions()
        {
            Bind(newButton,()=>{statusOverride="";c.NewProject();});Bind(saveButton,c.SaveNow);Bind(openButton,c.LoadLast);Bind(undoButton,c.Undo);Bind(redoButton,c.Redo);Bind(frameAllButton,c.FrameAll);Bind(frameSelectedButton,c.FrameSelected);
            Bind(levelButton,()=>{worldSelected=true;c.Selection.Clear();Refresh();});Bind(duplicateButton,c.DuplicateSelected);Bind(deleteButton,c.DeleteSelected);Bind(modeButton,c.ToggleMode);Bind(returnButton,c.ToggleMode);
            snap.onValueChanged.RemoveAllListeners();snap.onValueChanged.AddListener(value=>c.SetSnap(value));
        }
        static void Bind(Button button,UnityEngine.Events.UnityAction action){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}

        public void Refresh()
        {
            if(c==null||catalog==null)return;ApplyBranding();float listScroll=listInitialized?list.parent.GetComponent<ScrollRect>().verticalNormalizedPosition:1;Clear(catalog);Clear(list);Clear(properties);
            var defs=(c.contentPack!=null?c.contentPack.definitions:c.definitions).Where(d=>d!=null&&d.availableInWorkshop).OrderBy(d=>d.order).ToArray();
            for(int i=0;i<defs.Length;i++)
            {
                var definition=defs[i];int column=i%2,row=i/2;var button=ButtonAt(catalog,"+ "+definition.displayName,new Vector2(column*116,-row*58),()=>c.Create(definition.id,BuildCenter()),new Vector2(110,52),definition.icon,Soft,Accent,12);button.name="Añadir "+definition.id;
            }
            ResizeContent(catalog,Mathf.CeilToInt(defs.Length/2f)*58);
            int itemRow=0;foreach(var data in c.Project.objects)
            {
                var captured=data;var definition=c.Find(data.definitionId);bool selected=data.instanceId==c.SelectedId();var button=ButtonAt(list,definition!=null?definition.displayName:data.definitionId,new Vector2(0,itemRow++*-35),()=>Select(captured.instanceId),new Vector2(230,33),Preview(data,definition),selected?Selected:Panel,TextColor,13);button.gameObject.AddComponent<RuntimeItemRow>().instanceId=data.instanceId;
                if(selected){var mark=Label(button.transform,"✓",15,new Vector2(205,0),new Vector2(22,33),Accent,FontStyle.Bold);mark.alignment=TextAnchor.MiddleCenter;}
            }
            ResizeContent(list,itemRow*35);list.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=listScroll;listInitialized=true;
            int py=0;if(worldSelected)WorldProperties(ref py);else
            {
                var item=c.Selection.SelectedItem;var data=c.SelectedData();if(item==null||data==null||item.definition==null){EmptyProperties(ref py);}else{SelectionHeader(item,data,ref py);TryItemProperties(item.definition.kind,data,ref py);TryAppearance(item.definition.kind,data,ref py);}
            }
            ResizeContent(properties,Mathf.Max(502,py+20));bool build=c.Mode==AuthoringMode.Build;returnButton.gameObject.SetActive(!build);
            editTop.gameObject.SetActive(build);leftPanel.SetActive(build);rightPanel.SetActive(build);readinessPanel.SetActive(build);brandPanel.SetActive(build);flow.gameObject.SetActive(build);save.gameObject.SetActive(build);gameplayHudPanel?.SetActive(!build);snap.SetIsOnWithoutNotify(c.Project.alignAutomatically);UpdateFlow();UpdateReadiness();ApplyResponsiveLayout(true);
        }

        void UpdateFlow()
        {
            int current=c.Mode==AuthoringMode.Play?4:c.Selection.SelectedItem!=null?3:c.Project.objects.Count>0?2:1;
            string[] names={"Añadir","Seleccionar","Personalizar","Jugar"};flow.text=string.Join("     ",names.Select((name,index)=>(index+1==current?"● ":"○ ")+(index+1)+"  "+name));flow.color=Accent;
        }
        void UpdateReadiness()
        {
            if(readinessPanel==null)return;var error=RuntimePreflight.Validate(c.Project,c.Find,new RuntimePreflightContext{cameraAvailable=c.gameCamera!=null});bool ready=string.IsNullOrEmpty(error);
            readinessPanel.GetComponent<Image>().color=ready?Hex("183B30"):Warning;readinessTitle.text=ready?"¡Todo listo para jugar!":"Tu juego necesita un ajuste";readinessTitle.color=ready?SuccessBright:WarningText;
            bool player=Has(ItemKind.Player),platform=Has(ItemKind.Platform),interaction=Has(ItemKind.Prize)||Has(ItemKind.Hazard)||Has(ItemKind.Enemy),goal=Has(ItemKind.Goal);
            readinessChecks.text=(player?"[OK]":"[ ]")+" Jugador    "+(platform?"[OK]":"[ ]")+" Plataforma\n"+(interaction?"[OK]":"[ ]")+" Algo con qué interactuar    "+(goal?"[OK]":"[ ]")+" Meta";
            status.text=!string.IsNullOrEmpty(statusOverride)?statusOverride:ready?"Tu juego tiene los elementos necesarios.":error;
        }
        bool Has(ItemKind kind)=>c.Project.objects.Any(data=>c.Find(data.definitionId)?.kind==kind);

        void EmptyProperties(ref int y)
        {
            var card=PanelRect("Ayuda de selección",properties,new Vector2(4,-y),new Vector2(248,112),Soft,true);Label(card,"Selecciona un elemento",17,new Vector2(14,66),new Vector2(220,30),TextColor,FontStyle.Bold);Label(card,"Elige algo de Mi juego o haz clic en el nivel para cambiar sus propiedades.",13,new Vector2(14,12),new Vector2(220,56),Muted);y+=124;
        }
        void SelectionHeader(GameItem item,RuntimeItemData data,ref int y)
        {
            var card=PanelRect("Elemento elegido",properties,new Vector2(4,-y),new Vector2(248,112),Soft,true);var icon=New("Miniatura",card).AddComponent<Image>();icon.sprite=ResolvePreview(data,item.definition,c.contentPack,item);icon.preserveAspect=true;icon.raycastTarget=false;Rect(icon.transform).anchoredPosition=new Vector2(12,18);Rect(icon.transform).sizeDelta=new Vector2(72,76);
            Label(card,item.definition.displayName.ToUpperInvariant(),18,new Vector2(94,62),new Vector2(140,30),Accent,FontStyle.Bold);Label(card,item.definition.description,12,new Vector2(94,14),new Vector2(140,50),Muted);y+=124;
        }
        void Group(string title,ref int y){Label(properties,title,15,new Vector2(6,-y),new Vector2(244,28),Accent,FontStyle.Bold);y+=34;}
        void WorldProperties(ref int y)
        {
            Group("NIVEL",ref y);Label(properties,"Tamaño del nivel",14,new Vector2(6,-y),new Vector2(240,26),TextColor);y+=30;
            foreach(RuntimeLevelSize size in Enum.GetValues(typeof(RuntimeLevelSize))){var captured=size;string name=size==RuntimeLevelSize.Small?"Pequeño":size==RuntimeLevelSize.Medium?"Mediano":size==RuntimeLevelSize.Large?"Grande":"Muy grande";var button=ButtonAt(properties,name,new Vector2(6,-y),()=>c.SetLevelSize(captured),new Vector2(238,36),null,c.Project.levelSize==size?Selected:Soft,TextColor,13);y+=41;}
            var help=PanelRect("Ayuda del nivel",properties,new Vector2(6,-y),new Vector2(238,70),Hex("263B55"),true);Label(help,"El marco azul muestra la zona válida. Si el personaje cae, vuelve al inicio.",12,new Vector2(10,7),new Vector2(218,56),TextColor);y+=78;
        }
        void ItemProperties(ItemKind kind,RuntimeItemData data,ref int y)
        {
            if(kind!=ItemKind.Background){var size=c.SelectedWorldSize;Group("TAMAÑO",ref y);Slider("Ancho","sizeX",size.x,.25f,30,ref y);Slider("Alto","sizeY",size.y,.25f,30,ref y);}
            if(kind==ItemKind.Player){Group("MOVIMIENTO",ref y);Slider("Velocidad","speed",data.speed,.2f,5,ref y);Slider("Fuerza de salto","jump",data.jump,5,18,ref y);Group("PUNTOS DE VIDA",ref y);Slider("Puntos de vida","health",data.health,1,10,ref y);}
            else if(kind==ItemKind.Platform){if(data.definitionId=="muro"||data.definitionId=="rampa")Direction(data,ref y);}
            else if(kind==ItemKind.MovingPlatform){Group("MOVIMIENTO",ref y);Slider("Velocidad","speed",data.speed,.2f,3,ref y);Slider("Distancia","distance",data.distance,.5f,10,ref y);MovementHelp(ref y);}
            else if(kind==ItemKind.Background){Group("FONDO",ref y);Label(properties,"El fondo cubre todo el lienzo visible y sólo puede haber uno.",13,new Vector2(6,-y),new Vector2(238,46),Muted);y+=52;}
            else if(kind==ItemKind.Prize){Group("PREMIO",ref y);Slider("Puntos","points",data.points,1,100,ref y);}
            else if(kind==ItemKind.Hazard){Group("PELIGRO",ref y);Slider("Daño","damage",data.damage,1,10,ref y);}
            else if(kind==ItemKind.Enemy){Group("MOVIMIENTO",ref y);Slider("Velocidad","speed",data.speed,.2f,3,ref y);Slider("Distancia","distance",data.distance,.5f,10,ref y);MovementHelp(ref y);Group("COMBATE",ref y);Slider("Vida","health",data.health,1,10,ref y);Slider("Daño","damage",data.damage,1,10,ref y);}
            else if(kind==ItemKind.Goal){Group("MENSAJE FINAL",ref y);Input("Mensaje",data.message,ref y);}
            else{Group("POSICIÓN",ref y);Label(properties,"Arrastra para mover este elemento por el nivel.",13,new Vector2(6,-y),new Vector2(238,44),Muted);y+=50;}
        }
        void MovementHelp(ref int y){var help=PanelRect("Ayuda de recorrido",properties,new Vector2(6,-y),new Vector2(238,52),Hex("263B55"),true);Label(help,"La línea azul o naranja muestra el recorrido completo.",12,new Vector2(10,5),new Vector2(218,42),TextColor);y+=60;}
        void Direction(RuntimeItemData data,ref int y)
        {
            Label(properties,data.definitionId=="muro"?"Dirección del muro":"Dirección de subida",13,new Vector2(6,-y),new Vector2(238,22),TextColor);y+=27;
            bool right=data.definitionId=="muro"?Mathf.Abs(Mathf.DeltaAngle(data.rotationZ,0))<20:data.rotationZ>=0;
            ButtonAt(properties,data.definitionId=="muro"?"Horizontal":"Sube a la derecha",new Vector2(6,-y),()=>c.SetStructureDirection(true),new Vector2(114,36),RuntimeIconLibrary.ArrowRight,right?Selected:Soft,TextColor,11);
            ButtonAt(properties,data.definitionId=="muro"?"Vertical":"Sube a la izquierda",new Vector2(130,-y),()=>c.SetStructureDirection(false),new Vector2(114,36),RuntimeIconLibrary.ArrowLeft,!right?Selected:Soft,TextColor,11);y+=43;
        }

        void Appearance(ItemKind kind,RuntimeItemData data,ref int y)
        {
            Group("APARIENCIA",ref y);var category=c.contentPack?.CategoryFor(kind);if(category==null){Label(properties,"Este elemento usa su apariencia preparada.",12,new Vector2(6,-y),new Vector2(238,38),Muted);y+=44;}
            else
            {
                if(category.options.Count>8)Search(data,ref y);
                var options=c.contentPack.OptionsFor(kind,appearanceSearch,data.definitionId);foreach(var option in options.Take(24)){var captured=option;bool selected=data.appearanceId==option.id&&string.IsNullOrEmpty(data.customImageBase64);ButtonAt(properties,option.displayName,new Vector2(6,-y),()=>c.SetAppearance(captured.id),new Vector2(238,46),option.Preview,selected?Selected:Soft,TextColor,13);y+=51;}
                if(options.Length>24){Label(properties,"Refina la búsqueda para ver más opciones.",12,new Vector2(6,-y),new Vector2(238,34),Muted);y+=38;}
            }
            var custom=ButtonAt(properties,"Elegir imagen…",new Vector2(6,-y),c.PickImage,new Vector2(238,40),null,!string.IsNullOrEmpty(data.customImageBase64)?Selected:Soft,TextColor,13);y+=46;Label(properties,"PNG o JPG · máximo 2 MB y 2048 × 2048",11,new Vector2(6,-y),new Vector2(238,30),Muted);y+=34;
        }
        void Search(RuntimeItemData data,ref int y)
        {
            var go=New("Buscar apariencias",properties);var background=go.AddComponent<Image>();background.color=TextColor;var input=go.AddComponent<InputField>();var text=Label(go.transform,appearanceSearch,13,new Vector2(10,0),new Vector2(218,34),Header);input.textComponent=text;input.text=appearanceSearch;input.onEndEdit.AddListener(value=>{appearanceSearch=value;Refresh();});Rect(go.transform).anchoredPosition=new Vector2(6,-y);Rect(go.transform).sizeDelta=new Vector2(238,34);AddOutline(go,Border);y+=41;
        }
        void Slider(string label,string path,float value,float min,float max,ref int y)
        {
            Label(properties,label,13,new Vector2(6,-y),new Vector2(170,22),TextColor);var number=Label(properties,value.ToString("0.#"),13,new Vector2(180,-y),new Vector2(64,22),Accent,FontStyle.Bold);number.alignment=TextAnchor.MiddleRight;y+=26;
            var go=New("Slider "+label,properties);var image=go.AddComponent<Image>();image.color=Border;var slider=go.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;slider.wholeNumbers=path=="health"||path=="damage"||path=="points";var fill=New("Fill",go.transform).AddComponent<Image>();fill.color=Accent;slider.fillRect=Rect(fill.transform);Anchor(slider.fillRect);var handle=New("Handle",go.transform).AddComponent<Image>();handle.color=TextColor;slider.handleRect=Rect(handle.transform);slider.handleRect.sizeDelta=new Vector2(16,24);Rect(go.transform).anchoredPosition=new Vector2(6,-y);Rect(go.transform).sizeDelta=new Vector2(238,18);slider.onValueChanged.AddListener(v=>{number.text=v.ToString("0.#");c.SetFloat(path,v,false);});go.AddComponent<RuntimeSliderCommit>().commit=c.CommitEdit;y+=34;
        }
        void Input(string label,string value,ref int y)
        {
            Label(properties,label,13,new Vector2(6,-y),new Vector2(238,22),TextColor);y+=27;var go=New("Mensaje",properties);var background=go.AddComponent<Image>();background.color=Soft;var input=go.AddComponent<InputField>();var text=Label(go.transform,value,13,new Vector2(8,4),new Vector2(222,54),TextColor);text.alignment=TextAnchor.UpperLeft;input.textComponent=text;input.text=value;Rect(go.transform).anchoredPosition=new Vector2(6,-y);Rect(go.transform).sizeDelta=new Vector2(238,62);AddOutline(go,Border);input.onEndEdit.AddListener(c.SetMessage);y+=69;
        }

        public void SelectInstance(string id)=>c.Selection.Select(id);
        public bool ClickItemRow(string id){var row=list?.GetComponentsInChildren<RuntimeItemRow>(false).FirstOrDefault(r=>r.instanceId==id);if(row==null)return false;row.GetComponent<Button>().onClick.Invoke();return true;}
        void Select(string id)=>SelectInstance(id);
        Vector3 BuildCenter(){var point=c.buildCamera.ViewportToWorldPoint(new Vector3(.5f,.5f,-c.buildCamera.transform.position.z));point.z=0;return point;}
        public void SetStatus(string value){statusOverride=value??"";if(status!=null)status.text=statusOverride;}
        public void SetSaveState(string value){if(save!=null)save.text=value;}

        Sprite BrandSprite(){if(c?.contentPack?.brandIcon!=null)return c.contentPack.brandIcon;var player=c?.contentPack?.definitions.FirstOrDefault(def=>def!=null&&def.kind==ItemKind.Player);return player!=null?player.icon:null;}
        Sprite Preview(RuntimeItemData data,GameItemDefinition definition)
        {
            var marker=c.buildRoot!=null?c.buildRoot.GetComponentsInChildren<RuntimeAuthoredItem>(true).FirstOrDefault(value=>value.instanceId==data.instanceId):null;var item=marker!=null?marker.GetComponent<GameItem>():null;
            return ResolvePreview(data,definition,c.contentPack,item);
        }
        public static Sprite ResolvePreview(RuntimeItemData data,GameItemDefinition definition,RuntimeContentPack pack,GameItem item)
        {
            if(item!=null&&item.customSprite!=null)return item.customSprite;
            var option=pack?.CategoryFor(definition!=null?definition.kind:ItemKind.Decoration)?.Find(data.appearanceId);if(option?.Preview!=null)return option.Preview;
            var rendered=item!=null?ItemVisual.Resolve(item):null;return rendered!=null?rendered.sprite:definition?.icon;
        }
        Transform Scroll(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var viewport=New(name,parent);var image=viewport.AddComponent<Image>();image.color=new Color(0,0,0,.12f);viewport.AddComponent<Mask>().showMaskGraphic=true;Rect(viewport.transform).anchoredPosition=pos;Rect(viewport.transform).sizeDelta=size;AddOutline(viewport,Border);
            var content=New("Contenido",viewport.transform).transform;Rect(content).anchorMin=new Vector2(0,1);Rect(content).anchorMax=new Vector2(1,1);Rect(content).pivot=new Vector2(0,1);Rect(content).anchoredPosition=Vector2.zero;var scroll=viewport.AddComponent<ScrollRect>();scroll.content=Rect(content);scroll.viewport=Rect(viewport.transform);scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=24;scroll.verticalNormalizedPosition=1;return content;
        }
        void ResizeContent(Transform content,float height){Rect(content).sizeDelta=new Vector2(0,Mathf.Max(height,content.parent.GetComponent<RectTransform>().rect.height));}
        Transform PanelRect(string name,Transform parent,Vector2 pos,Vector2 size,Color color,bool outline=false){var go=New(name,parent);var image=go.AddComponent<Image>();image.color=color;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size;if(outline)AddOutline(go,Border);return go.transform;}
        Button ButtonAt(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction action,Vector2? size=null,Sprite icon=null,Color? color=null,Color? textColor=null,int fontSize=13)
        {
            var go=New(text,parent);var image=go.AddComponent<Image>();image.color=color??Soft;var button=go.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Hex("30445E");colors.pressedColor=Hex("3B5779");colors.selectedColor=Color.white;button.colors=colors;if(action!=null)button.onClick.AddListener(action);Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size??new Vector2(95,40);AddOutline(go,Border);
            int inset=icon!=null?44:0;if(icon!=null){var preview=New("Miniatura",go.transform).AddComponent<Image>();preview.sprite=icon;preview.preserveAspect=true;preview.raycastTarget=false;Rect(preview.transform).anchoredPosition=new Vector2(5,5);Rect(preview.transform).sizeDelta=new Vector2(36,(size??new Vector2(95,40)).y-10);}
            var label=Label(go.transform,text,fontSize,new Vector2(inset,0),new Vector2((size??new Vector2(95,40)).x-inset,(size??new Vector2(95,40)).y),textColor??TextColor,FontStyle.Bold);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        Toggle ToggleAt(Transform parent,string text,Vector2 pos,UnityEngine.Events.UnityAction<bool> action,Vector2 size)
        {
            var go=New(text,parent);Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=size;var toggle=go.AddComponent<Toggle>();var bg=New("Fondo",go.transform).AddComponent<Image>();bg.color=TextColor;Rect(bg.transform).anchoredPosition=new Vector2(0,5);Rect(bg.transform).sizeDelta=new Vector2(24,24);var check=New("Check",bg.transform).AddComponent<Image>();check.color=Accent;check.sprite=RuntimeIconLibrary.Check;check.preserveAspect=true;Anchor(Rect(check.transform));Rect(check.transform).offsetMin=Vector2.one*4;Rect(check.transform).offsetMax=Vector2.one*-4;toggle.targetGraphic=bg;toggle.graphic=check;var grid=New("Icono de cuadrícula",go.transform).AddComponent<Image>();grid.sprite=RuntimeIconLibrary.Grid;grid.color=Accent;grid.preserveAspect=true;grid.raycastTarget=false;Rect(grid.transform).anchoredPosition=new Vector2(30,8);Rect(grid.transform).sizeDelta=new Vector2(18,18);Label(go.transform,text,12,new Vector2(51,0),new Vector2(size.x-51,size.y),TextColor);if(action!=null)toggle.onValueChanged.AddListener(action);return toggle;
        }
        Text Label(Transform parent,string value,int size,Vector2 pos,Vector2 dimensions,Color? color=null,FontStyle style=FontStyle.Normal)
        {
            var go=New("Texto",parent);var text=go.AddComponent<Text>();text.raycastTarget=false;text.font=font;text.fontSize=size;text.fontStyle=style;text.text=value;text.color=color??TextColor;text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;Rect(go.transform).anchoredPosition=pos;Rect(go.transform).sizeDelta=dimensions;return text;
        }
        static void AddOutline(GameObject go,Color color){var outline=go.AddComponent<Outline>();outline.effectColor=color;outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=true;}
        static GameObject New(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;var top=parent!=null&&parent.name=="Contenido"?new Vector2(0,1):Vector2.zero;rect.anchorMin=rect.anchorMax=rect.pivot=top;return go;}
        static RectTransform Rect(Transform transform)=>(RectTransform)transform;
        static void Anchor(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Clear(Transform transform){for(int i=transform.childCount-1;i>=0;i--){var child=transform.GetChild(i).gameObject;child.SetActive(false);DestroyNow(child);}}
        static void DestroyNow(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void BuildGameplayHud()
        {
            gameplayHudPanel=PanelRect("Marcador del juego",top,new Vector2(0,0),new Vector2(1280,70),new Color(Header.r,Header.g,Header.b,.94f),true).gameObject;returnButton.transform.SetAsLastSibling();
            hearts=New("Vidas",gameplayHudPanel.transform).transform;Rect(hearts).anchoredPosition=new Vector2(18,13);Rect(hearts).sizeDelta=new Vector2(250,44);
            hudScore=Label(gameplayHudPanel.transform,"PUNTOS  0",17,new Vector2(290,13),new Vector2(170,44),TextColor,FontStyle.Bold);
            hudEnemies=Label(gameplayHudPanel.transform,"ENEMIGOS  0",17,new Vector2(470,13),new Vector2(190,44),TextColor,FontStyle.Bold);
            hudObjective=Label(gameplayHudPanel.transform,"OBJETIVO · Llega a la meta",15,new Vector2(675,13),new Vector2(380,44),Accent,FontStyle.Bold);
            gameplayHudPanel.SetActive(false);
        }
        void UpdateGameplayHud()
        {
            if(c==null||c.Mode!=AuthoringMode.Play||gameplayHudPanel==null||!gameplayHudPanel.activeSelf)return;
            var metrics=FindObjectsByType<MonoBehaviour>().OfType<IWorkshopSessionMetrics>().FirstOrDefault();
            if(metrics==null)return;
            if(lastHudHealth!=metrics.CurrentHealth){lastHudHealth=metrics.CurrentHealth;Clear(hearts);Label(hearts,"VIDAS",14,new Vector2(0,0),new Vector2(58,44),Muted,FontStyle.Bold);for(int i=0;i<Mathf.Min(10,lastHudHealth);i++){var heart=New("Vida "+(i+1),hearts).AddComponent<Image>();heart.sprite=RuntimeIconLibrary.Heart;heart.color=DangerText;heart.preserveAspect=true;heart.raycastTarget=false;Rect(heart.transform).anchoredPosition=new Vector2(62+i*27,9);Rect(heart.transform).sizeDelta=new Vector2(24,24);}}
            if(lastHudScore!=metrics.Score){lastHudScore=metrics.Score;hudScore.text="PUNTOS  "+metrics.Score;}
            if(lastHudEnemies!=metrics.EnemiesRemaining){lastHudEnemies=metrics.EnemiesRemaining;hudEnemies.text="ENEMIGOS  "+metrics.EnemiesRemaining;}
            hudObjective.text=metrics.State==GameSessionState.Won?"META COMPLETADA · "+metrics.Objective:metrics.State==GameSessionState.Lost?"SIN VIDAS · Vuelve a construir para ajustar el nivel.":"OBJETIVO · "+metrics.Objective;hudObjective.color=metrics.State==GameSessionState.Won?SuccessBright:metrics.State==GameSessionState.Lost?DangerText:Accent;
        }
        void ApplyResponsiveLayout(bool force=false)
        {
            if(canvas==null||Screen.width<=0||Screen.height<=0||!force&&screenWidth==Screen.width&&screenHeight==Screen.height)return;
            screenWidth=Screen.width;screenHeight=Screen.height;if(scaler!=null)scaler.matchWidthOrHeight=1f;
            float effectiveWidth=720f*Screen.width/Mathf.Max(1f,Screen.height);
            PinSidePanel(leftPanel?.GetComponent<RectTransform>(),true,260);
            PinSidePanel(rightPanel?.GetComponent<RectTransform>(),false,280);
            if(c?.buildCamera!=null)
            {
                var cameraRect=c.buildCamera.rect;
                cameraRect.x=260/effectiveWidth;
                cameraRect.width=Mathf.Max(.1f,(effectiveWidth-540)/effectiveWidth);
                c.buildCamera.rect=cameraRect;
            }
            if(!adaptLayoutAtRuntime)return;
            Stretch(top as RectTransform,0,0,646,0);Stretch(leftPanel?.GetComponent<RectTransform>(),0,effectiveWidth-260,0,74);Stretch(rightPanel?.GetComponent<RectTransform>(),effectiveWidth-280,0,82,74);
            Stretch(editTop as RectTransform,260,280,596,74);Stretch(readinessPanel?.GetComponent<RectTransform>(),260,0,0,638);Stretch(gameplayHudPanel?.GetComponent<RectTransform>(),0,0,0,0);
            if(returnButton!=null){var r=Rect(returnButton.transform);r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-15,14);}
            if(save!=null){var r=Rect(save.transform);r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-215,15);}
            if(c?.buildCamera!=null)c.buildCamera.rect=new Rect(260/effectiveWidth,82/720f,Mathf.Max(.1f,(effectiveWidth-540)/effectiveWidth),564/720f);
        }
        static void PinSidePanel(RectTransform rect,bool left,float width)
        {
            if(rect==null)return;
            rect.anchorMin=new Vector2(left?0:1,rect.anchorMin.y);rect.anchorMax=new Vector2(left?0:1,rect.anchorMax.y);
            rect.pivot=new Vector2(.5f,rect.pivot.y);rect.anchoredPosition=new Vector2((left?1:-1)*width*.5f,rect.anchoredPosition.y);rect.sizeDelta=new Vector2(width,rect.sizeDelta.y);
        }
        static void Stretch(RectTransform rect,float left,float right,float bottom,float top)
        {
            if(rect==null)return;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);
        }
        void TryItemProperties(ItemKind kind,RuntimeItemData data,ref int y){try{ItemProperties(kind,data,ref y);}catch(Exception exception){Debug.LogException(exception);Label(properties,"No se pudieron mostrar algunos ajustes.",13,new Vector2(6,-y),new Vector2(238,40),WarningText);y+=44;}}
        void TryAppearance(ItemKind kind,RuntimeItemData data,ref int y){try{Appearance(kind,data,ref y);}catch(Exception exception){Debug.LogException(exception);Label(properties,"No se pudo mostrar Apariencia.",13,new Vector2(6,-y),new Vector2(238,40),WarningText);y+=44;}}
    }

    public static class RuntimeIconLibrary
    {
        static Sprite check,play,back,undo,redo,left,right,heart,grid;
        public static Sprite Check=>check??=Make(16,(x,y)=>Mathf.Abs(y-(x<7?x+2:15-x))<2&&x>2&&x<14);
        public static Sprite Play=>play??=Make(16,(x,y)=>x>3&&x<13&&Mathf.Abs(y-8)<=x-3);
        public static Sprite Back=>back??=Make(16,(x,y)=>x>=3&&x<=13&&(Mathf.Abs(y-8)<=1||Mathf.Abs(y-(15-x))<=1&&x<8));
        public static Sprite Undo=>undo??=Make(16,(x,y)=>(x>=3&&x<=12&&Mathf.Abs(y-10)<=1)||(x>=2&&x<=7&&Mathf.Abs(y-(x+3))<=1));
        public static Sprite Redo=>redo??=Mirror(Undo);
        public static Sprite ArrowLeft=>left??=Make(16,(x,y)=>Mathf.Abs(y-8)<=1&&x>3&&x<14||Mathf.Abs(y-(x+4))<=1&&x<8);
        public static Sprite ArrowRight=>right??=Mirror(ArrowLeft);
        public static Sprite Grid=>grid??=Make(16,(x,y)=>(x==2||x==7||x==12||y==2||y==7||y==12)&&x>=2&&x<=12&&y>=2&&y<=12);
        public static Sprite Heart=>heart??=Make(16,(x,y)=>y>2&&y<13&&((x>1&&x<8&&(x-5)*(x-5)+(y-11)*(y-11)<18)||(x>=8&&x<15&&(x-11)*(x-11)+(y-11)*(y-11)<18)||y<10&&Mathf.Abs(x-8)<y-1));
        static Sprite Mirror(Sprite source){var pixels=source.texture.GetPixels();var mirrored=new Color[pixels.Length];int w=source.texture.width;for(int y=0;y<w;y++)for(int x=0;x<w;x++)mirrored[y*w+x]=pixels[y*w+(w-1-x)];return From(w,mirrored);}
        static Sprite Make(int size,Func<int,int,bool> pixel){var colors=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)colors[y*size+x]=pixel(x,y)?Color.white:Color.clear;return From(size,colors);}
        static Sprite From(int size,Color[] pixels){var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};texture.SetPixels(pixels);texture.Apply();return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);}
    }
}
