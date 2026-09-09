using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public sealed class WorldToolsView : Foldout
    {
        private readonly ObjectField sprite;
        private readonly Label message;
        public WorldToolsView()
        {
            text="Escenario"; value=false; AddToClassList("world-tools");
            sprite=new ObjectField("Fondo"){objectType=typeof(Sprite),allowSceneObjects=false,name="fondo"};
            Add(sprite);
            var choices=new VisualElement(); Add(choices);
            void RefreshChoices() {
                choices.Clear();
                var categories=ItemService.Catalog().Where(d=>d.appearancePack!=null).Select(d=>d.appearancePack.CategoryFor(ItemKind.Background)).Where(c=>c!=null).Distinct();
                foreach(var category in categories) foreach(var option in category.options.Where(o=>o!=null)) {
                    var choice=new Button(()=>Run(()=>WorldAuthoringService.SetBackground(option.Preview))){text=option.displayName};
                    choice.SetEnabled(option.Preview!=null); choices.Add(choice);
                }
            }
            RegisterCallback<AttachToPanelEvent>(_=>{EditorApplication.projectChanged+=RefreshChoices;RefreshChoices();});
            RegisterCallback<DetachFromPanelEvent>(_=>EditorApplication.projectChanged-=RefreshChoices);
            sprite.RegisterValueChangedCallback(evt=>Run(()=>WorldAuthoringService.SetBackground(evt.newValue as Sprite)));
            Add(new Button(()=>Run(()=>WorldAuthoringService.SetBackground(null))){text="Quitar fondo"});
            Add(new Button(()=>Run(()=>WorldAuthoringService.AddBoundary(SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView.pivot : Vector3.zero))){text="Añadir límite invisible",name="crear-limite"});
            Add(new Label("Mueve los límites en Escena y ajusta su tamaño en Propiedades."){style={whiteSpace=WhiteSpace.Normal}});
            message=new Label(){style={whiteSpace=WhiteSpace.Normal}}; Add(message);
            RegisterCallback<AttachToPanelEvent>(_=>{
                Undo.undoRedoPerformed+=Refresh;
                EditorApplication.hierarchyChanged+=Refresh;
                EditorApplication.playModeStateChanged+=PlayChanged;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_=>{
                Undo.undoRedoPerformed-=Refresh;
                EditorApplication.hierarchyChanged-=Refresh;
                EditorApplication.playModeStateChanged-=PlayChanged;
            });
        }
        private void Run(Action action)
        {
            try {action(); message.text="Ctrl+Z deshace el cambio."; }
            catch(Exception error){message.text=error.Message;}
            Refresh();
        }
        private void PlayChanged(PlayModeStateChange state)=>Refresh();
        private void Refresh()
        {
            var background=WorldAuthoringService.Background();
            sprite.SetValueWithoutNotify(background!=null ? background.GetComponent<SpriteRenderer>().sprite : null);
            SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }
    }
}

