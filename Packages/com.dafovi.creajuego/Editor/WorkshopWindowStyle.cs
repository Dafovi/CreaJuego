using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    internal static class WorkshopWindowStyle
    {
        public static T Styled<T>(T element,string style) where T:VisualElement {element.AddToClassList(style);return element;}
        public static Image Icon(GameItemDefinition d)=>Styled(new Image {sprite=d.icon,name="icono-"+d.id},"item-icon");
        public static void Apply(EditorWindow window)
        {
            var root=window.rootVisualElement; root.Clear();root.AddToClassList("creajuego");
            var script=MonoScript.FromScriptableObject(window);
            string directory=Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            var sheet=AssetDatabase.LoadAssetAtPath<StyleSheet>(directory+"/Styles/CreaJuegoTheme.uss");
            if(sheet!=null && !root.styleSheets.Contains(sheet))root.styleSheets.Add(sheet);
        }
    }
}
