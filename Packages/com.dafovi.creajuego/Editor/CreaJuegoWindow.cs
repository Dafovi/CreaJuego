using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public sealed class CreaJuegoWindow : EditorWindow
    {
        private ScrollView catalog,gameItems;
        private VisualElement actions;
        private Label status,flow;
        [MenuItem("CreaJuego/Abrir taller")]
        public static void Open() {
            GetWindow<SceneView>();
            CreaJuegoPropertiesWindow.Open();CreaJuegoPlayBarWindow.Open();
            GetWindow<CreaJuegoWindow>("CreaJuego").Focus();
        }
        [MenuItem("CreaJuego/Abrir elementos")]
        public static void OpenElements()=>GetWindow<CreaJuegoWindow>("CreaJuego");
        private void OnEnable() {
            Selection.selectionChanged+=ShowSelection;Undo.undoRedoPerformed+=UndoChanged;
            EditorApplication.hierarchyChanged+=RefreshSceneItems;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode+=SceneChanged;
            EditorApplication.playModeStateChanged+=PlayChanged;EditorApplication.projectChanged+=RefreshCatalog;
            ObjectChangeEvents.changesPublished+=ObjectsChanged;
        }
        private void OnDisable() {
            Selection.selectionChanged-=ShowSelection;Undo.undoRedoPerformed-=UndoChanged;
            EditorApplication.hierarchyChanged-=RefreshSceneItems;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode-=SceneChanged;
            EditorApplication.playModeStateChanged-=PlayChanged;EditorApplication.projectChanged-=RefreshCatalog;
            ObjectChangeEvents.changesPublished-=ObjectsChanged;
        }
        private static T Styled<T>(T element,string style) where T:VisualElement=>WorkshopWindowStyle.Styled(element,style);
        private static Image Icon(GameItemDefinition d)=>WorkshopWindowStyle.Icon(d);
        public void CreateGUI() {
            minSize=new Vector2(280,400);WorkshopWindowStyle.Apply(this);
            var root=rootVisualElement;root.AddToClassList("elements-window");
            root.Add(Styled(new Label("CreaJuego"),"title"));
            flow=Styled(new Label(){name="flujo"},"hint");root.Add(flow);
            root.Add(Styled(new Label("AÑADIR AL JUEGO"),"section-title"));
            root.Add(Styled(new Label("Elige algo para añadir a tu juego."),"hint"));
            catalog=Styled(new ScrollView {name="catalogo"},"catalog");root.Add(catalog);
            root.Add(new WorldToolsView()); root.Add(Styled(new Label("MI JUEGO"),"section-title"));
            gameItems=Styled(new ScrollView {name="mi-juego"},"game-items");root.Add(gameItems);
            actions=Styled(new VisualElement(),"actions");root.Add(actions);
            status=Styled(new Label(){name="estado"},"status");root.Add(status);
            RefreshCatalog();RefreshSceneItems();ShowSelection();PlayChanged(default);
        }        private void RefreshCatalog()
        {
            if(catalog==null) return;
            catalog.Clear();
            var cards=Styled(new VisualElement(),"creation-grid");
            foreach(var definition in ItemService.WorkshopCatalog()) {
                var button=Styled(new Button(()=>CreateItem(definition)){name="crear-"+definition.id,tooltip=definition.category+" · "+definition.description},"card");
                button.Add(Icon(definition));
                button.Add(Styled(new Label("+ "+definition.displayName),"card-title"));
                cards.Add(button);
            }
            catalog.Add(cards);
            catalog.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            MarkSelected();
        }
        private void RefreshIcons()
        {
            var selected=EducationalSelection.Items().FirstOrDefault(i=>i!=null);
            rootVisualElement.Query<Image>().ForEach(image=> {
                if(image.userData is GameItem item && item!=null) image.sprite=WorkshopWindowStyle.Preview(item);
                else if(image.userData is GameItemDefinition definition) image.sprite=selected!=null && selected.definition==definition ? WorkshopWindowStyle.Preview(selected) : WorkshopWindowStyle.Preview(definition);
            });
        }
        private void MarkSelected()
        {
            if(gameItems==null) return;
            var selected=EducationalSelection.Items();
            gameItems.Query<Button>().ForEach(b=> {
                bool chosen=b.userData is GameItem item && item!=null && selected.Contains(item);
                b.EnableInClassList("selected",chosen);
                var marker=b.Q<Label>("marca-seleccion");
                if(marker!=null) marker.text=chosen ? "✓" : "";
            });
            RefreshFlow(); RefreshIcons();
        }
        private void RefreshFlow()
        {
            if(flow==null)return;
            flow.text=EditorApplication.isPlayingOrWillChangePlaymode ? "Paso 4 de 4 · Jugar" :
                EducationalSelection.Items().Any(i=>i!=null) ? "Paso 3 de 4 · Personalizar" :
                SceneItemService.Entries().Length>0 ? "Paso 2 de 4 · Seleccionar" : "Paso 1 de 4 · Añadir";
        }
        private void ObjectsChanged(ref ObjectChangeEventStream stream)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool structure=false;
            for(int i=0;i<stream.length;i++) {
                var kind=stream.GetEventType(i);
                if(kind==ObjectChangeKind.CreateGameObjectHierarchy || kind==ObjectChangeKind.DestroyGameObjectHierarchy || kind==ObjectChangeKind.ChangeScene)
                    structure=true;
            }
            if(structure) RefreshSceneItems();
            RefreshIcons();
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
                var button=Styled(new Button(()=>SceneItemService.Select(item)){userData=item, tooltip="Seleccionar "+entry.label},"scene-item");
                if(item.definition!=null) button.Add(WorkshopWindowStyle.Icon(item));
                button.Add(Styled(new Label(entry.label),"scene-item-name"));
                button.Add(Styled(new Label("") {name="marca-seleccion"},"selection-mark"));
                gameItems.Add(button);
            }
            foreach(var boundary in SceneObjects.All<InvisibleBoundary>(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
            {
                var target=boundary;
                gameItems.Add(Styled(new Button(()=>Selection.activeGameObject=target.gameObject){text="▥ "+target.name,tooltip="Seleccionar límite invisible"},"scene-item"));
            }
            if(gameItems.childCount==0) {
                gameItems.Add(Styled(new Label("Tu juego todavía está vacío."),"empty-title"));
                gameItems.Add(Styled(new Label("Elige algo arriba para comenzar."),"hint"));
            }
            gameItems.scrollOffset=offset;
            MarkSelected(); 
        }
        private void CreateItem(GameItemDefinition definition)
        {
            try
            {
                var point=SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView.pivot : Vector3.zero; point.z=0;
                var item=ItemService.Create(definition,point); EditorGUIUtility.PingObject(item.gameObject);
                
                RefreshSceneItems();
                status.text=definition.displayName+" creado. Muévelo en la escena y ajusta sus propiedades.";
            }
            catch(Exception error){status.text=error.Message;}
        }
        private void ShowSelection()
        {
            if(actions==null)return;
            MarkSelected();actions.Clear();
            var items=EducationalSelection.Items();
            bool one=items.Length==1 && items[0]!=null && items[0].definition!=null && !EditorUtility.IsPersistent(items[0]);
            var item=one?items[0]:null;
            var duplicate=new Button(()=>RunAction(()=>ItemService.Duplicate(item))) {name="duplicar",text="Duplicar"};
            duplicate.SetEnabled(one && item.definition.allowMultiple);
            var delete=Styled(new Button(()=>RunAction(()=>ItemService.Delete(item))){name="eliminar",text="Eliminar"},"danger-action");
            delete.SetEnabled(one);
            var find=new Button(()=> {Selection.activeGameObject=item.gameObject;EditorWindow.GetWindow<SceneView>().FrameSelected();}) {name="encontrar",text="Encontrar"};
            find.SetEnabled(one);
            actions.Add(duplicate);actions.Add(delete);actions.Add(find);
            actions.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            status.text=one?item.definition.description:"Selecciona un elemento de Mi juego.";
        }
        private void RunAction(Action action) {
            try {action();RefreshSceneItems();ShowSelection();status.text="Ctrl+Z deshace el cambio. Ctrl+Y lo rehace.";}
            catch(Exception error){status.text=error.Message;}
        }
        private void PlayChanged(PlayModeStateChange state) {
            if(catalog==null)return;
            catalog.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            ShowSelection();RefreshFlow();
        }
    }
}
