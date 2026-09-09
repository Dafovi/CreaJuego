using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    [CustomEditor(typeof(AppearanceCategory))]
    public sealed class AppearanceCategoryEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root=new VisualElement();
            root.Add(new HelpBox("Añade opciones a esta lista. Cada opción puede usar un Sprite o un prefab con un único SpriteRenderer. No necesitas crear archivos individuales. El orden de la lista es el orden del selector.",HelpBoxMessageType.Info));
            var kinds=System.Enum.GetValues(typeof(ItemKind)).Cast<ItemKind>().ToArray();
            var labels=kinds.Select(AppearanceCategoryMigration.Label).ToList();
            var kindProperty=serializedObject.FindProperty("kind");
            var dropdown=new DropdownField("Categoría",labels,kindProperty.enumValueIndex);
            dropdown.RegisterValueChangedCallback(evt=>{
                serializedObject.Update(); kindProperty.enumValueIndex=labels.IndexOf(evt.newValue); serializedObject.ApplyModifiedProperties();
            });
            root.TrackPropertyValue(kindProperty,p=>dropdown.SetValueWithoutNotify(labels[p.enumValueIndex]));
            root.Add(dropdown);
            root.Add(new PropertyField(serializedObject.FindProperty("options")));
            root.Add(new HelpBox("Los prefabs son referencias visuales: no se copian sus scripts, físicas, materiales ni jerarquías. Para animaciones, el controlador debe animar el SpriteRenderer principal y el perfil debe indicar los estados. Ajusta Escala y Desplazamiento aquí.",HelpBoxMessageType.Info));
            var warnings=new VisualElement(); root.Add(warnings);
            void Validate() {
                warnings.Clear();
                foreach(var option in ((AppearanceCategory)target).options)
                    if(option!=null && option.ValidationError!=null) warnings.Add(new HelpBox((option.displayName??"Opción")+": "+option.ValidationError,HelpBoxMessageType.Warning));
            }
            Validate();
            root.TrackSerializedObjectValue(serializedObject,_=>{
                Validate(); ItemAppearance.RefreshScene();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            });
            return root;
        }
    }
}


