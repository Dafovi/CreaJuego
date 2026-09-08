using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CreaJuego.Editor
{
    public sealed class CreaJuegoWindow : EditorWindow
    {
        private ScrollView catalog, properties, validation, gameItems;
        private Label status;
        private Button play, prepare;
        private SerializedObject binding;

        [MenuItem("CreaJuego/Abrir taller")]
        public static void Open() => GetWindow<CreaJuegoWindow>("CreaJuego Lab");
        private void OnEnable()
        {
            Selection.selectionChanged += ShowSelection; Undo.undoRedoPerformed += UndoChanged; EditorApplication.hierarchyChanged += RefreshSceneItems; UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += SceneChanged;
            EditorApplication.playModeStateChanged += PlayChanged; EditorApplication.projectChanged += RefreshCatalog;
        }
        private void OnDisable()
        {
            Selection.selectionChanged -= ShowSelection; Undo.undoRedoPerformed -= UndoChanged; EditorApplication.hierarchyChanged -= RefreshSceneItems; UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= SceneChanged;
            EditorApplication.playModeStateChanged -= PlayChanged; EditorApplication.projectChanged -= RefreshCatalog;
            properties?.Unbind(); binding?.Dispose(); binding = null;
        }
        private static T Styled<T>(T element, string className) where T : VisualElement { element.AddToClassList(className); return element; }
        public void CreateGUI()
        {
            minSize = new Vector2(660,480);
            var root = rootVisualElement; root.Clear(); root.AddToClassList("creajuego");
            string directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(directory + "/Styles/CreaJuegoTheme.uss");
            if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            var header = Styled(new VisualElement(), "header");
            var brand = Styled(new VisualElement(), "brand");
            brand.Add(Styled(new Label("CREA JUEGO"), "title"));
            brand.Add(Styled(new Label("Crea una pequeña experiencia jugable"), "subtitle"));
            header.Add(brand);
            play = Styled(new Button(TogglePlay) { name="jugar", text="▶ JUGAR" }, "primary");
            header.Add(play); root.Add(header);
            var tools = Styled(new VisualElement(), "toolbar");
            prepare = new Button(PrepareScene) { name="preparar-escena", text="Preparar escena", tooltip="Añade la cámara y el marcador del taller. Puedes deshacerlo." };
            tools.Add(prepare);
            tools.Add(Styled(new Label("Primero crea. Luego descubre cómo funciona."), "hint"));
            root.Add(tools);
            validation = Styled(new ScrollView { name="validacion" }, "validation");
            validation.style.display=DisplayStyle.None; root.Add(validation);
            var columns=Styled(new VisualElement(), "columns");
            catalog=Styled(new ScrollView { name="catalogo" }, "catalog");
            properties=Styled(new ScrollView { name="propiedades" }, "properties");
            var left=Styled(new VisualElement(),"left-column");
            left.Add(Styled(new Label("AÑADIR AL JUEGO"),"section-title"));
            left.Add(Styled(new Label("Crea un elemento nuevo."),"hint"));
            left.Add(catalog);
            left.Add(Styled(new Label("MI JUEGO"),"section-title"));
            left.Add(Styled(new Label("Selecciona lo que ya has añadido."),"hint"));
            gameItems=Styled(new ScrollView {name="mi-juego"},"game-items");
            left.Add(gameItems); columns.Add(left); columns.Add(properties); root.Add(columns);
            status=Styled(new Label("Añade un elemento y muévelo en la vista Escena.") { name="estado" }, "status"); root.Add(status);
            status.schedule.Execute(()=> { if (EditorApplication.isPlaying) status.text=SceneService.RuntimeHelp(); }).Every(250);
            RefreshCatalog(); RefreshSceneItems(); ShowSelection(); PlayChanged(default);
        }
        private void RefreshCatalog()
        {
            if(catalog==null) return;
            catalog.Clear();
            foreach(var group in ItemService.WorkshopCatalog().GroupBy(d=>d.category))
            {
                catalog.Add(Styled(new Label(group.Key.ToUpperInvariant()),"category"));
                foreach(var definition in group)
                {
                    var button=Styled(new Button(()=>CreateItem(definition)){name="crear-"+definition.id,tooltip=definition.description},"card");
                    button.Add(Icon(definition));
                    var text=Styled(new VisualElement(),"card-copy");
                    text.Add(Styled(new Label("+ Añadir "+definition.displayName),"card-title"));
                    text.Add(Styled(new Label(definition.description),"hint"));
                    button.Add(text); catalog.Add(button);
                }
            }
            catalog.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            MarkSelected();
        }
        private static Image Icon(GameItemDefinition definition) => Styled(new Image { sprite=definition.icon, name="icono-"+definition.id },"item-icon");
        private void MarkSelected()
        {
            if(gameItems==null) return;
            var selected=Selection.gameObjects;
            gameItems.Query<Button>().ForEach(b=>b.EnableInClassList("selected",
                b.userData is GameItem item && item!=null && selected.Contains(item.gameObject)));
        }
        private void UndoChanged() { RefreshSceneItems(); ShowSelection(); }
        private void SceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next) { RefreshSceneItems(); ShowSelection(); }
        private void RefreshSceneItems()
        {
            if(gameItems==null) return;
            var offset=gameItems.scrollOffset;
            gameItems.Clear();
            foreach(var entry in SceneItemService.Entries())
            {
                var item=entry.item;
                var button=Styled(new Button(()=>SceneItemService.Select(item)){text=entry.label, userData=item, tooltip="Seleccionar "+entry.label},"scene-item");
                gameItems.Add(button);
            }
            if(gameItems.childCount==0) gameItems.Add(Styled(new Label("Todavía no has añadido elementos."),"hint"));
            gameItems.scrollOffset=offset;
            MarkSelected();
        }
        private void CreateItem(GameItemDefinition definition)
        {
            try
            {
                var point=SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView.pivot : Vector3.zero; point.z=0;
                var item=ItemService.Create(definition,point); EditorGUIUtility.PingObject(item.gameObject);
                validation.style.display=DisplayStyle.None;
                RefreshSceneItems();
                status.text=definition.displayName+" creado. Muévelo en la escena y ajusta sus propiedades.";
            }
            catch(Exception error){status.text=error.Message;}
        }
        private void ShowSelection()
        {
            if(properties==null) return;
            properties.Unbind(); properties.Clear(); binding?.Dispose(); binding=null; MarkSelected();
            properties.Add(Styled(new Label("SELECCIÓN"),"section-title"));
            var items=Selection.gameObjects.Select(g=>g.GetComponent<GameItem>()).ToArray();
            if(items.Length==0 || items.Any(i=>i==null || i.definition==null || EditorUtility.IsPersistent(i)))
            {
                properties.Add(Styled(new Label("Elige un elemento de Mi juego para darle tu toque."),"empty")); return;
            }
            var definition=items[0].definition;
            if(items.Any(i=>i.definition!=definition))
            {
                properties.Add(Styled(new Label("Selecciona elementos del mismo tipo para editarlos juntos."),"empty")); return;
            }
            var heading=Styled(new VisualElement(),"selection-heading"); heading.Add(Icon(definition));
            heading.Add(Styled(new Label(items.Length>1 ? definition.displayName+" × "+items.Length : SceneItemService.Label(items[0])),"selection-title")); properties.Add(heading);
            properties.Add(Styled(new Label(definition.learningHint),"selection-help"));
            if(!EditorApplication.isPlayingOrWillChangePlaymode) status.text=definition.learningHint;
            binding=new SerializedObject(items.Cast<UnityEngine.Object>().ToArray());
            VisualElement groupPanel=null; string lastGroup=null;
            foreach(var descriptor in definition.properties)
            {
                var p=binding.FindProperty(descriptor.path);
                if(p==null){ properties.Add(new HelpBox("No está disponible: "+descriptor.label,HelpBoxMessageType.Warning)); continue; }
                if(groupPanel==null || lastGroup!=descriptor.group)
                {
                    groupPanel=Styled(new VisualElement(),"property-group"); properties.Add(groupPanel);
                    if(!string.IsNullOrEmpty(descriptor.group)) groupPanel.Add(Styled(new Label(descriptor.group.ToUpperInvariant()),"group-title"));
                    lastGroup=descriptor.group;
                }
                var row=Styled(new VisualElement { name="fila-"+descriptor.path },"property");
                VisualElement field;
                switch(descriptor.control)
                {
                    case EducationalControl.Float:
                        var slider=new Slider(descriptor.label,descriptor.minimum,descriptor.maximum){showInputField=true};
                        slider.BindProperty(p); field=slider; break;
                    case EducationalControl.Integer:
                        var integer=new IntegerField(descriptor.label);
                        integer.RegisterValueChangedCallback(evt=>{
                            int bounded=Mathf.Clamp(evt.newValue,(int)descriptor.minimum,(int)descriptor.maximum);
                            if(bounded!=evt.newValue){evt.StopImmediatePropagation(); integer.value=bounded;}
                        }); integer.BindProperty(p); field=integer; break;
                    case EducationalControl.Toggle:
                        var toggle=new Toggle(descriptor.label); toggle.BindProperty(p); field=toggle; break;
                    case EducationalControl.Text:
                        var text=Styled(new TextField(descriptor.label){multiline=true,maxLength=120},"message");
                        text.BindProperty(p); field=text; break;
                    default:
                        var color=new ColorField(descriptor.label){showAlpha=false}; color.BindProperty(p); field=color;
                        row.TrackPropertyValue(p,_=>{foreach(var item in items) ItemAppearance.Apply(item,true);}); break;
                }
                field.name="propiedad-"+descriptor.path; field.tooltip=descriptor.help; row.Add(field);
                if(!string.IsNullOrEmpty(descriptor.unit)) row.Add(Styled(new Label(descriptor.unit),"unit"));
                row.Add(Styled(new Label(descriptor.help),"hint"));
                if(!string.IsNullOrEmpty(descriptor.visibleWhen))
                {
                    var condition=binding.FindProperty(descriptor.visibleWhen);
                    if(condition!=null && condition.propertyType==SerializedPropertyType.Boolean)
                    {
                        void Visibility(SerializedProperty value)=>row.style.display=value.hasMultipleDifferentValues || value.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                        Visibility(condition); row.TrackPropertyValue(condition,Visibility);
                    }
                }
                groupPanel.Add(row);
            }
            properties.Add(Styled(new Label("ACCIONES"),"category"));
            var actions=Styled(new VisualElement(),"actions");
            var duplicate=new Button(()=>RunAction(()=>ItemService.Duplicate(items[0]))){text="Duplicar",name="duplicar",tooltip=definition.allowMultiple?"Crea una copia con los mismos ajustes.":"Este tipo de juego utiliza un solo personaje."};
            duplicate.SetEnabled(items.Length==1 && definition.allowMultiple);
            var delete=new Button(()=> {
                try { var message=ItemService.Delete(items[0]); RefreshSceneItems(); status.text=message; } catch(Exception error){status.text=error.Message;}
            }){text="Eliminar",name="eliminar",tooltip="Puedes recuperarlo con Ctrl+Z."};
            delete.SetEnabled(items.Length==1);
            var find=new Button(()=>{Selection.activeGameObject=items[0].gameObject; SceneView.lastActiveSceneView?.FrameSelected();}){text="Encontrar",tooltip="Enfoca el elemento en la escena."};
            find.SetEnabled(items.Length==1);
            actions.Add(duplicate); actions.Add(delete); actions.Add(find); properties.Add(actions);
            properties.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }
        private void RunAction(System.Action action)
        {
            try { action(); RefreshSceneItems(); status.text="Ctrl+Z deshace el cambio. Ctrl+Y lo rehace."; } catch(Exception error){status.text=error.Message;}
        }
        private void ShowChecks(System.Collections.Generic.List<SceneCheck> checks)
        {
            validation.Clear(); validation.style.display=DisplayStyle.Flex;
            validation.Add(Styled(new Label(checks.Any(c=>!c.passed)?"TU JUEGO NECESITA ALGO":"TU JUEGO ESTÁ LISTO"),"group-title"));
            foreach(var check in checks)
            {
                validation.Add(Styled(new Label((check.passed?"✓ ":"• ")+check.label+(check.passed?"":": "+check.help)),check.passed?"check-ok":"check-missing"));
            }
        }
        private void PrepareScene()
        {
            try { SceneService.Prepare(); var checks=SceneService.Validate(); ShowChecks(checks); status.text="Escena preparada. Añade tu personaje y el recorrido."; }
            catch(Exception error){status.text=error.Message;}
        }
        private void TogglePlay()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.ExitPlaymode();return;}
            if(!SceneService.TryPlay(out var checks)){ShowChecks(checks);status.text=checks.First(c=>!c.passed).help;}
            else validation.style.display=DisplayStyle.None;
        }
        private void PlayChanged(PlayModeStateChange state)
        {
            if(play==null)return;
            bool playing=EditorApplication.isPlayingOrWillChangePlaymode;
            play.text=playing?"■ DETENER":"▶ JUGAR";
            catalog.SetEnabled(!playing);properties.SetEnabled(!playing);prepare.SetEnabled(!playing);
            status.text=playing?"Haz clic en Juego para jugar tu recorrido.":"Los cambios se hacen antes de jugar. Ctrl+Z deshace tu último cambio.";
        }
    }
}

