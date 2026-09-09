using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CreaJuego.Editor
{
    public static class EducationalSelection
    {
        // SceneView commonly picks Visual; keep Unity's actual selection untouched.
        public static GameItem[] Items() => Selection.objects.Select(Resolve).Distinct().ToArray();
        static GameItem Resolve(Object selected)
        {
            var go=selected as GameObject;
            if(go==null && selected is Component component) go=component.gameObject;
            return go!=null ? go.GetComponentInParent<GameItem>(true) : null;
        }
    }
}
