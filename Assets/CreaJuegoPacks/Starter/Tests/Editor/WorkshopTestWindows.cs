using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using CreaJuego.Editor;
namespace CreaJuego.Starter.Tests
{
    internal static class WorkshopTestWindows
    {
        public static VisualElement Properties=>EditorWindow.GetWindow<CreaJuegoPropertiesWindow>().rootVisualElement;
        public static VisualElement Play=>EditorWindow.GetWindow<CreaJuegoPlayBarWindow>().rootVisualElement;
        public static void Open() {
            var properties=EditorWindow.GetWindow<CreaJuegoPropertiesWindow>();
            if(properties.rootVisualElement.childCount==0)properties.CreateGUI();
            var play=EditorWindow.GetWindow<CreaJuegoPlayBarWindow>();
            if(play.rootVisualElement.childCount==0)play.CreateGUI();
        }
        public static void Close() {
            foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoPropertiesWindow>())w.Close();
            foreach(var w in Resources.FindObjectsOfTypeAll<CreaJuegoPlayBarWindow>())w.Close();
        }
    }
}
