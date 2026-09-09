using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    internal static class WorkshopWindowStyle
    {
        public static T Styled<T>(T element,string style) where T:VisualElement {element.AddToClassList(style);return element;}
        public static Sprite Preview(GameItemDefinition d) {
            if(d==null) return null;
            var category=d.appearancePack?.CategoryFor(d.kind);
            if(category!=null) foreach(var option in category.options) if(option!=null && option.Preview!=null) return option.Preview;
            var source=d.prefab!=null ? d.prefab.GetComponent<GameItem>() : null;
            return source!=null ? Preview(source) : d.appearancePack?.DefaultFor(d.kind)?.sprite ?? d.icon;
        }
        public static Sprite Preview(GameItem item) => item.customSprite!=null ? item.customSprite : item.SelectedAppearance!=null && item.SelectedAppearance.sprite!=null ? item.SelectedAppearance.sprite : ItemVisual.Resolve(item)?.sprite ?? item.definition?.icon;
        public static Image Icon(GameItemDefinition d)=>Styled(new Image {sprite=Preview(d),name="icono-"+d.id,userData=d,scaleMode=ScaleMode.ScaleToFit},"item-icon");
        public static Image Icon(GameItem item)=>Styled(new Image {sprite=Preview(item),name="icono-"+item.definition.id,userData=item,scaleMode=ScaleMode.ScaleToFit},"item-icon");
        public static void Apply(EditorWindow window)
        {
            var root=window.rootVisualElement; root.Clear();root.AddToClassList("creajuego");
            var script=MonoScript.FromScriptableObject(window);
            string directory=Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            var sheet=AssetDatabase.LoadAssetAtPath<StyleSheet>(directory+"/Styles/CreaJuegoTheme.uss");
            if(sheet!=null && !root.styleSheets.Contains(sheet))root.styleSheets.Add(sheet);
            WorkshopTheme.Attach(root);
        }
    }
}


