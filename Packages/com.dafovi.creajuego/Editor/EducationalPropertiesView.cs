using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public sealed class EducationalPropertiesView : ScrollView, IDisposable
    {
        private SerializedObject binding;
        public EducationalPropertiesView() { name="propiedades"; AddToClassList("properties"); }
        private static T Styled<T>(T element,string style) where T:VisualElement=>WorkshopWindowStyle.Styled(element,style);
        private static Image Icon(GameItemDefinition d)=>WorkshopWindowStyle.Icon(d);        public void ShowSelection()
        {
            var properties=this;
            properties.Unbind(); properties.Clear(); binding?.Dispose(); binding=null;
            properties.Add(Styled(new Label("Propiedades"),"section-title"));
            var boundary=Selection.activeGameObject!=null ? Selection.activeGameObject.GetComponent<InvisibleBoundary>() : null;
            if(boundary!=null && !EditorUtility.IsPersistent(boundary)) {
                properties.Add(new Label("LÍMITE INVISIBLE"));
                properties.Add(new Label("Bloquea el paso, pero no aparece al jugar. Muévelo en Escena."){style={whiteSpace=WhiteSpace.Normal}});
                binding=new SerializedObject(boundary.GetComponent<BoxCollider2D>());
                foreach(var axis in new[]{"x","y"}) {
                    var field=new FloatField(axis=="x" ? "Anchura" : "Altura");
                    field.RegisterValueChangedCallback(evt=>{
                        if(evt.newValue<.1f){evt.StopImmediatePropagation();field.value=.1f;}
                    });
                    field.BindProperty(binding.FindProperty("m_Size").FindPropertyRelative(axis));
                    properties.Add(field);
                }
                properties.Add(new Button(()=>Undo.DestroyObjectImmediate(boundary.gameObject)){text="Eliminar límite"});
                properties.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                return;
            }
            properties.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            var items=EducationalSelection.Items();
            if(items.Length==0 || items.Any(i=>i==null || i.definition==null || EditorUtility.IsPersistent(i)))
            {
                properties.Add(Styled(new Label("Selecciona algo de Mi juego para cambiar sus propiedades."),"empty")); return;
            }
            var definition=items[0].definition;
            if(items.Any(i=>i.definition!=definition))
            {
                properties.Add(Styled(new Label("Selecciona elementos del mismo tipo para editarlos juntos."),"empty")); return;
            }
            var heading=Styled(new VisualElement(),"selection-heading"); heading.Add(WorkshopWindowStyle.Icon(items[0]));
            heading.Add(Styled(new Label(items.Length>1 ? definition.displayName+" × "+items.Length : SceneItemService.Label(items[0])),"selection-title")); properties.Add(heading);
            heading.Add(Styled(new Label(definition.description),"selection-help"));

            binding=new SerializedObject(items.Cast<UnityEngine.Object>().ToArray());
            VisualElement groupPanel=null; string lastGroup=null;
            foreach(var descriptor in definition.properties)
            {
                if(descriptor.path==nameof(GameItem.tint) && items.Any(i=>i.SelectedAppearance!=null || i.customSprite!=null)) continue;
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
            var appearancePanel=Styled(new VisualElement(),"property-group");
            appearancePanel.Add(Styled(new Label("APARIENCIA"),"group-title"));
            var scaleRow=Styled(new VisualElement { name="fila-"+nameof(GameItem.visualScale) },"property");
            var scaleProperty=binding.FindProperty(nameof(GameItem.visualScale));
            var scaleField=new Slider("Tamaño",.1f,5f){showInputField=true,name="propiedad-"+nameof(GameItem.visualScale),tooltip="Haz más grande o más pequeño sólo el dibujo."};
            scaleField.BindProperty(scaleProperty);
            scaleRow.TrackPropertyValue(scaleProperty,_=>{foreach(var item in items) ItemAppearance.Apply(item,true);});
            scaleRow.Add(scaleField);
            scaleRow.Add(Styled(new Label("×"),"unit"));
            scaleRow.Add(Styled(new Label("1 mantiene el tamaño normal. No cambia las colisiones ni el movimiento."),"hint"));
            appearancePanel.Add(scaleRow);
            properties.Add(appearancePanel);
            properties.Add(new AppearanceSelector(items));
            properties.Add(Styled(new Label(definition.learningHint),"context-help"));
            properties.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }
        public void Dispose() { this.Unbind(); binding?.Dispose(); binding=null; }
    }
}

