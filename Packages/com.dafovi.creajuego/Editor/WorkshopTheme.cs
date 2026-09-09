using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace CreaJuego.Editor
{
    public static class WorkshopTheme
    {
        const string Key="CreaJuego.DarkTheme";
        public static bool Dark { get=>EditorPrefs.GetBool(Key,EditorGUIUtility.isProSkin); set { EditorPrefs.SetBool(Key,value); Refresh(); } }
        public static void Refresh()
        {
            foreach(var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if(window is CreaJuegoWindow || window is CreaJuegoPropertiesWindow || window is CreaJuegoPlayBarWindow)
                {
                    var root=window.rootVisualElement; root.EnableInClassList("dark",Dark);
                    root.Q<Toggle>("modo-oscuro")?.SetValueWithoutNotify(Dark); window.Repaint();
                }
        }
        public static void Attach(VisualElement root)
        {
            root.EnableInClassList("dark",Dark);
            var toggle=new Toggle("Modo oscuro") { name="modo-oscuro",value=Dark,tooltip="Cambia el aspecto de las tres ventanas de CreaJuego." };
            toggle.AddToClassList("theme-toggle"); toggle.RegisterValueChangedCallback(e=>Dark=e.newValue); root.Add(toggle);
        }
    }
}
